﻿using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Player;
using Milky.WpfApi;
using Milky.WpfApi.Commands;
using System.Collections.Generic;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;

namespace Milky.OsuPlayer.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private bool _isNavigationCollapsed;
        private bool _isMiniMode;
        private List<Collection> _collection;
        private bool _isLyricWindowShown;
        private bool _isLyricWindowLocked;
        private bool _showWelcome;
        private PlayerViewModel _player;
        private bool _guideSyncing;
        private bool _guideSelectedDb;

        public bool IsNavigationCollapsed
        {
            get => _isNavigationCollapsed;
            set
            {
                _isNavigationCollapsed = value;
                OnPropertyChanged();
            }
        }

        public bool IsMiniMode
        {
            get => _isMiniMode;
            set
            {
                _isMiniMode = value;
                OnPropertyChanged();
            }
        }

        public bool IsLyricWindowShown
        {
            get => _isLyricWindowShown;
            set
            {
                _isLyricWindowShown = value;
                OnPropertyChanged();
            }
        }

        public bool IsLyricWindowLocked
        {
            get => _isLyricWindowLocked;
            set
            {
                _isLyricWindowLocked = value;
                OnPropertyChanged();
            }
        }

        public bool ShowWelcome
        {
            get => _showWelcome;
            set
            {
                _showWelcome = value;
                OnPropertyChanged();
            }
        }

        public List<Collection> Collection
        {
            get => _collection;
            set
            {
                _collection = value;
                OnPropertyChanged();
            }
        }

        public ICommand CollapseCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    Execute.OnUiThread(() =>
                    {
                        IsNavigationCollapsed = !IsNavigationCollapsed;
                    });
                });
            }
        }

        public PlayerViewModel Player
        {
            get => _player;
            set
            {
                _player = value;
                OnPropertyChanged();
            }
        }

        public bool GuideSyncing
        {
            get => _guideSyncing;
            set
            {
                _guideSyncing = value;
                OnPropertyChanged();
            }
        }

        public bool GuideSelectedDb
        {
            get => _guideSelectedDb;
            set
            {
                _guideSelectedDb = value;
                OnPropertyChanged();
            }
        }
    }
}
