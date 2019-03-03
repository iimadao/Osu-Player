﻿using Microsoft.Win32;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.I18N;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Lyric;
using Milky.OsuPlayer.Media.Lyric.SourceProvider;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Auto;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Kugou;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Netease;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.QQMusic;
using Milky.OsuPlayer.Utils;
using Newtonsoft.Json;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Milky.OsuPlayer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static Config Config { get; set; }
        public static UiMetadata UiMetadata { get; set; } = new UiMetadata();
        public static bool UseDbMode => Config.General.DbPath != null;


        public static List<BeatmapEntry> Beatmaps => BeatmapEntryQuery.BeatmapDb?.Beatmaps;

        public static ComponentPlayer Player;
        //public static MusicPlayer MusicPlayer;
        //public static HitsoundPlayer HitsoundPlayer;

        public static LyricProvider LyricProvider;
        public static readonly PlayerList PlayerList = new PlayerList();
        public static readonly Updater Updater = new Updater();

        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;

            if (!LoadConfig())
                Environment.Exit(0);
            CreateDirectories();
            InitLocalDb();
            SaveConfig();
            ReloadLyricProvider();
            RedirectHandler.Redirect();
            SetAlignment();
            LoadOsuDbAsync().Wait();
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        public static void SetAlignment()
        {
            //获取系统是以Left-handed（true）还是Right-handed（false）
            var ifLeft = SystemParameters.MenuDropAlignment;

            if (ifLeft)
            {
                // change to false
                var t = typeof(SystemParameters);
                var field = t.GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
                field?.SetValue(null, false);

                ifLeft = SystemParameters.MenuDropAlignment;
            }
        }

        private static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!e.IsTerminating) return;
            MessageBox.Show(string.Format("发生严重错误，即将退出。。。详情请查看error.log。{0}{1}", Environment.NewLine, (e.ExceptionObject as Exception)?.Message), "Osu Player", MessageBoxButton.OK, MessageBoxImage.Error);
            File.AppendAllText("error.log", string.Format(@"===================={0}===================={1}{2}{3}{4}", DateTime.Now, Environment.NewLine, e.ExceptionObject, Environment.NewLine, Environment.NewLine));
            Environment.Exit(1);
        }

        public static void ReloadLyricProvider(bool useStrict = true)
        {
            Config.Lyric.StrictMode = useStrict;
            Settings.StrictMatch = useStrict;
            SourceProviderBase provider;
            switch (Config.Lyric.LyricSource)
            {
                case LyricSource.Auto:
                    provider = new AutoSourceProvider(new SourceProviderBase[]
                    {
                        new NeteaseSourceProvider(),
                        new KugouSourceProvider(),
                        new QQMusicSourceProvider()
                    });
                    break;
                case LyricSource.Netease:
                    provider = new NeteaseSourceProvider();
                    break;
                case LyricSource.Kugou:
                    provider = new KugouSourceProvider();
                    break;
                case LyricSource.QqMusic:
                    provider = new QQMusicSourceProvider();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            LyricProvider = new LyricProvider(provider, LyricProvideType.Original);
        }

        private static void InitLocalDb()
        {
            var defCol = DbOperate.GetCollections().Where(k => k.Locked);
            if (!defCol.Any()) DbOperate.AddCollection("最喜爱的", true);
        }

        private static bool LoadConfig()
        {
            var file = Domain.ConfigFile;
            if (!File.Exists(file))
            {
                CreateConfig();
            }
            else
            {
                try
                {
                    Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(file));
                }
                catch (JsonException e)
                {
                    var result = MessageBox.Show(@"载入配置文件时失败，用默认配置覆盖继续打开吗？\r\n" + e.Message,
                        "Osu Player", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        CreateConfig();
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        private static async Task LoadOsuDbAsync()
        {
            string dbPath = Config.General.DbPath;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            {
                var osuProcess = Process.GetProcesses().Where(x => x.ProcessName == "osu!").ToArray();
                if (osuProcess.Length == 1)
                {
                    var di = new FileInfo(osuProcess[0].MainModule.FileName).Directory;
                    if (di != null && di.Exists)
                        dbPath = Path.Combine(di.FullName, "osu!.db");
                }

                if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
                {
                    var result = BrowseDb(out var chosedPath);
                    if (!result.HasValue || !result.Value)
                    {
                        MessageBox.Show(@"你尚未初始化osu!db，因此部分功能将不可用。", "Osu Player", MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    if (!File.Exists(chosedPath))
                    {
                        MessageBox.Show(@"指定文件不存在。", "Osu Player", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    dbPath = chosedPath;
                }

                //if (dbPath == null) return;
                Config.General.DbPath = dbPath;
            }

            await BeatmapEntryQuery.LoadNewDbAsync(dbPath);
        }

        public static bool? BrowseDb(out string chosedPath)
        {
            OpenFileDialog fbd = new OpenFileDialog
            {
                Title = @"请选择osu所在目录内的""osu!.db""",
                Filter = @"Beatmap Database|osu!.db"
            };
            var result = fbd.ShowDialog();
            chosedPath = fbd.FileName;
            return result;
        }

        private static void CreateConfig()
        {
            Config = new Config();
            File.WriteAllText(Domain.ConfigFile, JsonConvert.SerializeObject(Config));
        }

        public static void SaveConfig()
        {
            File.WriteAllText(Domain.ConfigFile, JsonConvert.SerializeObject(Config, Formatting.Indented));
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        private static void CreateDirectories()
        {
            Type t = typeof(Domain);
            var infos = t.GetProperties();
            foreach (var item in infos)
            {
                if (!item.Name.EndsWith("Path")) continue;
                try
                {
                    string path = (string)item.GetValue(null, null);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
                catch (Exception)
                {
                    Console.WriteLine(@"未创建：" + item.Name);
                }
            }
        }
    }
}
