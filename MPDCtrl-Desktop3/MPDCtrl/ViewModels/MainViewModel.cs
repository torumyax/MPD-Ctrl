﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Media;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Input;
using System.IO;
using System.ComponentModel;
using System.Windows.Threading;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using MPDCtrl.Common;
using MPDCtrl.Views;
using MPDCtrl.Models;
using MPDCtrl.Models.Classes;
using MPDCtrl.ViewModels.Classes;

namespace MPDCtrl.ViewModels
{
    /// TODO: 
    /// 
    /// ソース全般：完全なリファクタリング。特にプロトコルから。
    /// 
    /// リソース系：翻訳やスタイル、名前の整理と見直し。
    /// 
    /// UI：キューの絞り込み検索が絶対欲しい。検索アイコンの場所で。
    /// UI：ステータスバー右下で現在のprofileを表示してプルダウンで簡単に切り替えられるようにしたい。
    /// UI：テーマの切り替え
    /// 
    /// 
    /// 「プレイリストの名前変更」をインラインで。
    /// Files: "追加して再生"メニューを追加。
    /// Files: "再読み込み" context menu.
    /// 
    /// 
    /// [未定]
    /// 検索で、ExactかContainのオプション。
    /// Ctrl+F検索とFilesから直接プレイリストに追加できるように「プレイリストに追加」をコンテキストメニューで。
    ///  "Save Selected to playlist" context menu at Search Result listview.
    ///  "Save this search result as new playlist".
    /// Listview の水平スクロールバーのデザイン。
    /// スライダーの上でスクロールして音量変更。
    /// スライダーの変更時にブレる件。
    /// ピクチャーインピクチャー？


    /// 更新履歴：
    /// v3.0.3 基本のコマンドとidleからの更新ができるようになった・・・。
    /// v3.0.2 MPCを作り直し中。とりあえず接続とデータ取得まで。
    /// v3.0.1 MPCを作り直し中。 
    /// v3.0.0 v2.1.2から分岐。レイアウトを見直し。


    /// Key Gestures:
    /// Ctrl+S Show Settings
    /// Ctrl+F Show Find 
    /// Ctrl+P Playback Play
    /// Ctrl+U QueueListview Queue Move Up
    /// Ctrl+D QueueListview Queue Move Down
    /// Ctrl+Delete QueueListview Queue Selected Item delete.
    /// Ctrl+J QueueListview Jump to now playing.
    /// Space Play > reserved for listview..
    /// Ctrl+Delete QueueListview Remove from Queue
    /// Esc Close dialogs.


    public class MainViewModel : ViewModelBase
    {
        #region == Basic ==  

        // Application name
        const string _appName = "MPDCtrl";

        // Application version
        const string _appVer = "v3.0.3";

        public static string AppVer
        {
            get
            {
                return _appVer;
            }
        }

        // Application Title (for system)
        public static string AppTitle
        {
            get
            {
                return _appName;
            }
        }

        // Application Window Title (for display)
        public static string AppTitleVer
        {
            get
            {
                return _appName + " " + _appVer;
            }
        }

        // For the application config file folder
        const string _appDeveloper = "torum";

        // For DebugOutputWindow etc.
        public static bool DeveloperMode
        {
            get
            {
#if DEBUG
                return true;
#else
                return false; 
#endif
            }
        }

        #endregion

        #region == 設定フォルダ関連 ==  

        private static string _envDataFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static string _appDataFolder;
        private static string _appConfigFilePath;

        private bool _isFullyLoaded;
        public bool IsFullyLoaded
        {
            get
            {
                return _isFullyLoaded;
            }
            set
            {
                if (_isFullyLoaded == value)
                    return;

                _isFullyLoaded = value;
                this.NotifyPropertyChanged("IsFullyLoaded");
            }
        }

        private bool _isFullyRendered;
        public bool IsFullyRendered
        {
            get
            {
                return _isFullyRendered;
            }
            set
            {
                if (_isFullyRendered == value)
                    return;

                _isFullyRendered = value;
                this.NotifyPropertyChanged("IsFullyRendered");
            }
        }

        #endregion

        #region == レイアウト関連 ==

        // TODO:現在のレイアウトでは無効

        private double _mainLeftPainActualWidth = 241;
        public double MainLeftPainActualWidth
        {
            get
            {
                return _mainLeftPainActualWidth;
            }
            set
            {
                if (value == _mainLeftPainActualWidth) return;

                _mainLeftPainActualWidth = value;

                NotifyPropertyChanged("MainLeftPainActualWidth");
            }
        }

        private double _mainLeftPainWidth = 241;
        public double MainLeftPainWidth
        {
            get
            {
                return _mainLeftPainWidth;
            }
            set
            {
                if (value == _mainLeftPainWidth) return;

                _mainLeftPainWidth = value;

                NotifyPropertyChanged("MainLeftPainWidth");
            }
        }

        #region == Queueカラムヘッダー ==

        private bool _queueColumnHeaderPositionVisibility = true;
        public bool QueueColumnHeaderPositionVisibility
        {
            get
            {
                return _queueColumnHeaderPositionVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderPositionVisibility)
                    return;

                _queueColumnHeaderPositionVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderPositionVisibility");
            }
        }

        private double _queueColumnHeaderPositionWidth = 53;
        public double QueueColumnHeaderPositionWidth
        {
            get
            {
                return _queueColumnHeaderPositionWidth;
            }
            set
            {
                if (value == _queueColumnHeaderPositionWidth)
                    return;

                _queueColumnHeaderPositionWidth = value;

                if (value > 0)
                    QueueColumnHeaderPositionWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderPositionWidth");
            }
        }

        private double _queueColumnHeaderPositionWidthUser = 53;
        public double QueueColumnHeaderPositionWidthRestore
        {
            get
            {
                return _queueColumnHeaderPositionWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderPositionWidthUser)
                    return;

                _queueColumnHeaderPositionWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderPositionWidthRestore");
            }
        }

        private bool _queueColumnHeaderNowPlayingVisibility = true;
        public bool QueueColumnHeaderNowPlayingVisibility
        {
            get
            {
                return _queueColumnHeaderNowPlayingVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderNowPlayingVisibility)
                    return;

                _queueColumnHeaderNowPlayingVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderNowPlayingVisibility");
            }
        }

        private double _queueColumnHeaderNowPlayingWidth = 32;
        public double QueueColumnHeaderNowPlayingWidth
        {
            get
            {
                return _queueColumnHeaderNowPlayingWidth;
            }
            set
            {
                if (value == _queueColumnHeaderNowPlayingWidth)
                    return;

                _queueColumnHeaderNowPlayingWidth = value;

                if (value > 0)
                    QueueColumnHeaderNowPlayingWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderNowPlayingWidth");
            }
        }

        private double _queueColumnHeaderNowPlayingWidthUser = 32;
        public double QueueColumnHeaderNowPlayingWidthRestore
        {
            get
            {
                return _queueColumnHeaderNowPlayingWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderNowPlayingWidthUser)
                    return;

                _queueColumnHeaderNowPlayingWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderNowPlayingWidthRestore");
            }
        }

        private double _queueColumnHeaderTitleWidth = 180;
        public double QueueColumnHeaderTitleWidth
        {
            get
            {
                return _queueColumnHeaderTitleWidth;
            }
            set
            {
                if (value == _queueColumnHeaderTitleWidth)
                    return;

                _queueColumnHeaderTitleWidth = value;

                if (value > 0)
                    QueueColumnHeaderTitleWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderTitleWidth");
            }
        }

        private double _queueColumnHeaderTitleWidthUser = 180;
        public double QueueColumnHeaderTitleWidthRestore
        {
            get
            {
                return _queueColumnHeaderTitleWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderTitleWidthUser)
                    return;

                _queueColumnHeaderTitleWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderTitleWidthRestore");
            }
        }

        private bool _queueColumnHeaderTimeVisibility = true;
        public bool QueueColumnHeaderTimeVisibility
        {
            get
            {
                return _queueColumnHeaderTimeVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderTimeVisibility)
                    return;

                _queueColumnHeaderTimeVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderTimeVisibility");
            }
        }

        private double _queueColumnHeaderTimeWidth = 62;
        public double QueueColumnHeaderTimeWidth
        {
            get
            {
                return _queueColumnHeaderTimeWidth;
            }
            set
            {
                if (value == _queueColumnHeaderTimeWidth)
                    return;

                _queueColumnHeaderTimeWidth = value;

                if (value > 0)
                    QueueColumnHeaderTitleWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderTimeWidth");
            }
        }

        private double _queueColumnHeaderTimeWidthUser = 62;
        public double QueueColumnHeaderTimeWidthRestore
        {
            get
            {
                return _queueColumnHeaderTimeWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderTimeWidthUser)
                    return;

                _queueColumnHeaderTimeWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderTimeWidthRestore");
            }
        }

        private bool _queueColumnHeaderArtistVisibility = true;
        public bool QueueColumnHeaderArtistVisibility
        {
            get
            {
                return _queueColumnHeaderArtistVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderArtistVisibility)
                    return;

                _queueColumnHeaderArtistVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderArtistVisibility");
            }
        }

        private double _queueColumnHeaderArtistWidth = 120;
        public double QueueColumnHeaderArtistWidth
        {
            get
            {
                return _queueColumnHeaderArtistWidth;
            }
            set
            {
                if (value == _queueColumnHeaderArtistWidth)
                    return;

                _queueColumnHeaderArtistWidth = value;

                if (value > 0)
                    QueueColumnHeaderArtistWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderArtistWidth");
            }
        }

        private double _queueColumnHeaderArtistWidthUser = 120;
        public double QueueColumnHeaderArtistWidthRestore
        {
            get
            {
                return _queueColumnHeaderArtistWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderArtistWidthUser)
                    return;

                _queueColumnHeaderArtistWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderArtistWidthRestore");
            }
        }

        private bool _queueColumnHeaderAlbumVisibility = true;
        public bool QueueColumnHeaderAlbumVisibility
        {
            get
            {
                return _queueColumnHeaderAlbumVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderAlbumVisibility)
                    return;

                _queueColumnHeaderAlbumVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderAlbumVisibility");
            }
        }

        private double _queueColumnHeaderAlbumWidth = 120;
        public double QueueColumnHeaderAlbumWidth
        {
            get
            {
                return _queueColumnHeaderAlbumWidth;
            }
            set
            {
                if (value == _queueColumnHeaderAlbumWidth)
                    return;

                _queueColumnHeaderAlbumWidth = value;

                if (value > 0)
                    QueueColumnHeaderAlbumWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderAlbumWidth");
            }
        }

        private double _queueColumnHeaderAlbumWidthUser = 120;
        public double QueueColumnHeaderAlbumWidthRestore
        {
            get
            {
                return _queueColumnHeaderAlbumWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderAlbumWidthUser)
                    return;

                _queueColumnHeaderAlbumWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderAlbumWidthRestore");
            }
        }

        private bool _queueColumnHeaderGenreVisibility = true;
        public bool QueueColumnHeaderGenreVisibility
        {
            get
            {
                return _queueColumnHeaderGenreVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderGenreVisibility)
                    return;

                _queueColumnHeaderGenreVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderGenreVisibility");
            }
        }

        private double _queueColumnHeaderGenreWidth = 100;
        public double QueueColumnHeaderGenreWidth
        {
            get
            {
                return _queueColumnHeaderGenreWidth;
            }
            set
            {
                if (value == _queueColumnHeaderGenreWidth)
                    return;

                _queueColumnHeaderGenreWidth = value;

                if (value > 0)
                    QueueColumnHeaderGenreWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderGenreWidth");
            }
        }

        private double _queueColumnHeaderGenreWidthUser = 100;
        public double QueueColumnHeaderGenreWidthRestore
        {
            get
            {
                return _queueColumnHeaderGenreWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderGenreWidthUser)
                    return;

                _queueColumnHeaderGenreWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderGenreWidthRestore");
            }
        }

        private bool _queueColumnHeaderLastModifiedVisibility = true;
        public bool QueueColumnHeaderLastModifiedVisibility
        {
            get
            {
                return _queueColumnHeaderLastModifiedVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderLastModifiedVisibility)
                    return;

                _queueColumnHeaderLastModifiedVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderLastModifiedVisibility");
            }
        }

        private double _queueColumnHeaderLastModifiedWidth = 180;
        public double QueueColumnHeaderLastModifiedWidth
        {
            get
            {
                return _queueColumnHeaderLastModifiedWidth;
            }
            set
            {
                if (value == _queueColumnHeaderLastModifiedWidth)
                    return;

                _queueColumnHeaderLastModifiedWidth = value;

                if (value > 0)
                    QueueColumnHeaderLastModifiedWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderLastModifiedWidth");
            }
        }

        private double _queueColumnHeaderLastModifiedWidthUser = 180;
        public double QueueColumnHeaderLastModifiedWidthRestore
        {
            get
            {
                return _queueColumnHeaderLastModifiedWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderLastModifiedWidthUser)
                    return;

                _queueColumnHeaderLastModifiedWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderLastModifiedWidthRestore");
            }
        }

        #endregion

        #endregion

        #region == 画面表示切り替え系 ==  

        private bool _isConnected;
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                if (_isConnected == value)
                    return;

                _isConnected = value;
                NotifyPropertyChanged("IsConnected");

                if (_isConnected)
                {
                    IsConnectionSettingShow = false;
                    IsConnecting = false;
                    IsNotConnectingNorConnected = false;
                }
                else
                {
                    IsConnectionSettingShow = true;
                    if (!IsConnecting)
                    {
                        IsNotConnectingNorConnected = true;
                    }
                }
            }
        }

        private bool _isConnecting;
        public bool IsConnecting
        {
            get
            {
                return _isConnecting;
            }
            set
            {
                if (_isConnecting == value)
                    return;

                _isConnecting = value;
                NotifyPropertyChanged("IsConnecting");
                NotifyPropertyChanged("IsNotConnecting");
                

                if (_isConnecting)
                {
                    IsNotConnectingNorConnected = false;
                }
                else
                {
                    if (!IsConnected)
                    {
                        IsNotConnectingNorConnected = true;
                    }
                }
            }
        }

        private bool _isNotConnectingNorConnected = true;
        public bool IsNotConnectingNorConnected
        {
            get
            {
                return _isNotConnectingNorConnected;
            }
            set
            {
                if (_isNotConnectingNorConnected == value)
                    return;

                _isNotConnectingNorConnected = value;
                NotifyPropertyChanged("IsNotConnectingNorConnected");
            }
        }

        public bool IsNotConnecting
        {
            get
            {
                return !_isConnecting;
            }
        }

        private bool _isSettingsShow;
        public bool IsSettingsShow
        {
            get { return _isSettingsShow; }
            set
            {
                if (_isSettingsShow == value)
                    return;

                _isSettingsShow = value;

                if (_isSettingsShow)
                {
                    if (CurrentProfile == null)
                    {
                        IsConnectionSettingShow = false;
                    }
                    else
                    {
                        IsConnectionSettingShow = false;
                    }
                }
                else
                {
                    if (CurrentProfile == null)
                    {
                        IsConnectionSettingShow = true;
                    }
                    else
                    {
                        if (!IsConnected)
                        {
                            IsConnectionSettingShow = true;
                        }
                    }
                }

                NotifyPropertyChanged("IsSettingsShow");

            }
        }

        private bool _isConnectionSettingShow;
        public bool IsConnectionSettingShow
        {
            get { return _isConnectionSettingShow; }
            set
            {
                if (_isConnectionSettingShow == value)
                    return;

                _isConnectionSettingShow = value;
                NotifyPropertyChanged("IsConnectionSettingShow");
            }
        }
        
        private bool _isComfirmationDialogShow;
        public bool IsComfirmationDialogShow
        {
            get
            {
                return _isComfirmationDialogShow;
            }
            set
            {
                if (_isComfirmationDialogShow == value)
                    return;

                _isComfirmationDialogShow = value;
                NotifyPropertyChanged("IsComfirmationDialogShow");
            }
        }
        
        private bool _isInformationDialogShow;
        public bool IsInformationDialogShow
        {
            get
            {
                return _isInformationDialogShow;
            }
            set
            {
                if (_isInformationDialogShow == value)
                    return;

                _isInformationDialogShow = value;
                NotifyPropertyChanged("IsInformationDialogShow");
            }
        }

        private bool _isInputDialogShow;
        public bool IsInputDialogShow
        {
            get
            {
                return _isInputDialogShow;
            }
            set
            {
                if (_isInputDialogShow == value)
                    return;

                _isInputDialogShow = value;
                NotifyPropertyChanged("IsInputDialogShow");
            }
        }

        private bool _isChangePasswordDialogShow;
        public bool IsChangePasswordDialogShow
        {
            get
            {
                return _isChangePasswordDialogShow;
            }
            set
            {
                if (_isChangePasswordDialogShow == value)
                    return;

                _isChangePasswordDialogShow = value;
                NotifyPropertyChanged("IsChangePasswordDialogShow");
            }
        }

        private bool _isPlaylistSelectDialogShow;
        public bool IsPlaylistSelectDialogShow
        {
            get
            {
                return _isPlaylistSelectDialogShow;
            }
            set
            {
                if (_isPlaylistSelectDialogShow == value)
                    return;

                _isPlaylistSelectDialogShow = value;
                NotifyPropertyChanged("IsPlaylistSelectDialogShow");
            }
        }

        private bool _isFindDialogShow;
        public bool IsFindDialogShow
        {
            get
            {
                return _isFindDialogShow;
            }
            set
            {
                if (_isFindDialogShow == value)
                    return;

                _isFindDialogShow = value;
                NotifyPropertyChanged("IsFindDialogShow");
            }
        }

        public bool IsCurrentProfileSet
        {
            get
            {
                if (Profiles.Count > 1)
                    return true;
                else
                    return false;
            }
        }

        private bool _isAlbumArtVisible;
        public bool IsAlbumArtVisible
        {
            get
            {
                return _isAlbumArtVisible;
            }
            set
            {
                if (_isAlbumArtVisible == value)
                    return;

                _isAlbumArtVisible = value;
                NotifyPropertyChanged("IsAlbumArtVisible");
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set
            {
                if (_isBusy == value)
                    return;

                _isBusy = value;
                NotifyPropertyChanged("IsBusy");
            }
        }

        // TODO;
        private bool _isShowDebugWindow;
        public bool IsShowDebugWindow
        {
            get { return _isShowDebugWindow; }
            set
            {
                if (_isShowDebugWindow == value)
                    return;

                _isShowDebugWindow = value;
                
                NotifyPropertyChanged("IsShowDebugWindow");
            }
        }

        private bool _isShowAckWindow
;
        public bool IsShowAckWindow

        {
            get { return _isShowAckWindow; }
            set
            {
                if (_isShowAckWindow == value)
                    return;

                _isShowAckWindow = value;

                NotifyPropertyChanged("IsShowAckWindow");
            }
        }

        #endregion

        #region == コントロール関連 ==  

        private SongInfo _currentSong;
        public SongInfo CurrentSong
        {
            get
            {
                return _currentSong;
            }
            set
            {
                if (_currentSong == value)
                    return;

                _currentSong = value;
                NotifyPropertyChanged("CurrentSong");
                NotifyPropertyChanged("CurrentSongTitle");
                NotifyPropertyChanged("CurrentSongArtist");
                NotifyPropertyChanged("CurrentSongAlbum");

                if (value == null)
                    _elapsedTimer.Stop();
            }
        }

        public string CurrentSongTitle
        {
            get
            {
                if (_currentSong != null)
                {
                    return _currentSong.Title;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string CurrentSongArtist
        {
            get
            {
                if (_currentSong != null)
                {
                    if (!string.IsNullOrEmpty(_currentSong.Artist))
                        return _currentSong.Artist.Trim();
                    else
                        return "";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string CurrentSongAlbum
        {
            get
            {
                if (_currentSong != null)
                {
                    if (!string.IsNullOrEmpty(_currentSong.Album))
                        return _currentSong.Album.Trim();
                    else
                        return "";
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        #region == Playback ==  

        private static string _pathPlayButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string _pathPauseButton = "M15,16H13V8H15M11,16H9V8H11M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        //private static string _pathStopButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private string _playButton = _pathPlayButton;
        public string PlayButton
        {
            get
            {
                return _playButton;
            }
            set
            {
                if (_playButton == value)
                    return;

                _playButton = value;
                NotifyPropertyChanged("PlayButton");
            }
        }

        private double _volume;
        public double Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    NotifyPropertyChanged("Volume");

                    if (_mpc != null)
                    {
                        if (Convert.ToDouble(_mpc.MpdStatus.MpdVolume) != _volume)
                        {
                            // If we have a timer and we are in this event handler, a user is still interact with the slider
                            // we stop the timer
                            if (_volumeDelayTimer != null)
                                _volumeDelayTimer.Stop();

                            //System.Diagnostics.Debug.WriteLine("Volume value is still changing. Skipping.");

                            // we always create a new instance of DispatcherTimer
                            _volumeDelayTimer = new System.Timers.Timer();
                            _volumeDelayTimer.AutoReset = false;

                            // if one second passes, that means our user has stopped interacting with the slider
                            // we do real event
                            _volumeDelayTimer.Interval = (double)1000;
                            _volumeDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeVolume);

                            _volumeDelayTimer.Start();
                        }
                    }
                }
            }
        }

        private System.Timers.Timer _volumeDelayTimer = null;
        private void DoChangeVolume(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_mpc != null)
            {
                if (Convert.ToDouble(_mpc.MpdStatus.MpdVolume) != _volume)
                {
                    if (SetVolumeCommand.CanExecute(null))
                    {
                        SetVolumeCommand.Execute(null);
                    }
                }
            }
        }

        private bool _repeat;
        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                _repeat = value;
                NotifyPropertyChanged("Repeat");

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdRepeat != value)
                    {
                        if (SetRpeatCommand.CanExecute(null))
                        {
                            SetRpeatCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private bool _random;
        public bool Random
        {
            get { return _random; }
            set
            {
                _random = value;
                NotifyPropertyChanged("Random");

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdRandom != value)
                    {
                        if (SetRandomCommand.CanExecute(null))
                        {
                            SetRandomCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private bool _consume;
        public bool Consume
        {
            get { return _consume; }
            set
            {
                _consume = value;
                NotifyPropertyChanged("Consume");

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdConsume != value)
                    {
                        if (SetConsumeCommand.CanExecute(null))
                        {
                            SetConsumeCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private bool _single;
        public bool Single
        {
            get { return _single; }
            set
            {
                _single = value;
                NotifyPropertyChanged("Single");

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdSingle != value)
                    {
                        if (SetSingleCommand.CanExecute(null))
                        {
                            SetSingleCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private double _time;
        public double Time
        {
            get
            {
                return _time;
            }
            set
            {
                _time = value;
                NotifyPropertyChanged("Time");
            }
        }

        private double _elapsed;
        public double Elapsed
        {
            get
            {
                return _elapsed;
            }
            set
            {
                if ((value < _time) && _elapsed != value)
                {
                    _elapsed = value;
                    NotifyPropertyChanged("Elapsed");

                    // If we have a timer and we are in this event handler, a user is still interact with the slider
                    // we stop the timer
                    if (_elapsedDelayTimer != null)
                        _elapsedDelayTimer.Stop();

                    //System.Diagnostics.Debug.WriteLine("Elapsed value is still changing. Skipping.");

                    // we always create a new instance of DispatcherTimer
                    _elapsedDelayTimer = new System.Timers.Timer();
                    _elapsedDelayTimer.AutoReset = false;

                    // if one second passes, that means our user has stopped interacting with the slider
                    // we do real event
                    _elapsedDelayTimer.Interval = (double)1000;
                    _elapsedDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeElapsed);

                    _elapsedDelayTimer.Start();
                }
            }
        }

        private System.Timers.Timer _elapsedDelayTimer = null;
        private void DoChangeElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_mpc != null)
            {
                if ((_elapsed < _time))
                {
                    if (SetSeekCommand.CanExecute(null))
                    {
                        SetSeekCommand.Execute(null);
                    }
                }
            }
        }


        #endregion


        #region == AlbumArt == 

        private ImageSource _albumArtDefault = null;
        private ImageSource _albumArt;
        public ImageSource AlbumArt
        {
            get
            {
                return _albumArt;
            }
            set
            {
                if (_albumArt == value)
                    return;

                _albumArt = value;
                NotifyPropertyChanged("AlbumArt");
            }
        }

        #endregion

        #endregion

        #region == メイン画面のメニューと各画面の機能 ==

        #region == Playlists ==  

        public ObservableCollection<string> Playlists
        {
            get
            {
                if (_mpc != null)
                {
                    return _mpc.Playlists;
                }
                else
                {
                    return null;
                }
            }
        }

        private string _selecctedPlaylist;
        public string SelectedPlaylist
        {
            get
            {
                return _selecctedPlaylist;
            }
            set
            {
                if (_selecctedPlaylist != value)
                {
                    _selecctedPlaylist = value;
                    NotifyPropertyChanged("SelectedPlaylist");
                }
            }
        }

        #endregion

        #region == Local Files ==
        /*
        private ObservableCollection<NodeFile> _localFiles = new ObservableCollection<NodeFile>();
        public ObservableCollection<NodeFile> LocalFiles
        {
            get
            {
                return _localFiles;
            }
            set
            {
                if (_localFiles != value)
                {
                    _localFiles = value;
                    NotifyPropertyChanged("LocalFiles");
                }
            }
        }

        private NodeFile _selecctedLocalfile;
        public NodeFile SelectedLocalfile
        {
            get
            {
                return _selecctedLocalfile;
            }
            set
            {
                if (_selecctedLocalfile != value)
                {
                    _selecctedLocalfile = value;
                    NotifyPropertyChanged("SelectedLocalfile");
                }
            }
        }
        */
        #endregion

        #region == Queue ==  

        private ObservableCollection<SongInfo> _queue = new ObservableCollection<SongInfo>();
        public ObservableCollection<SongInfo> Queue
        {
            get
            {
                if (_mpc != null)
                {
                    return _queue;
                    //return _mpc.CurrentQueue;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (_queue == value)
                    return;

                _queue = value;
                NotifyPropertyChanged("Queue");
            }
        }

        private SongInfo _selectedSong;
        public SongInfo SelectedSong
        {
            get
            {
                return _selectedSong;
            }
            set
            {
                if (_selectedSong == value)
                    return;

                _selectedSong = value;
                NotifyPropertyChanged("SelectedSong");
            }
        }

        #endregion

        #region == Browse ==

        private DirectoryTreeBuilder _musicDirectories = new DirectoryTreeBuilder();
        public ObservableCollection<NodeTree> MusicDirectories
        {
            get { return _musicDirectories.Children; }
            set
            {
                _musicDirectories.Children = value;
                NotifyPropertyChanged(nameof(MusicDirectories));
            }
        }

        private NodeTree _selectedNodeDirectory = new NodeDirectory(".",new Uri(@"file:///./"));
        public NodeTree SelectedNodeDirectory
        {
            get { return _selectedNodeDirectory; }
            set
            {
                if (_selectedNodeDirectory == value)
                    return;

                _selectedNodeDirectory = value;
                NotifyPropertyChanged(nameof(SelectedNodeDirectory));

                if (_selectedNodeDirectory == null)
                    return;

                if (MusicEntries == null)
                    return;
                if (MusicEntries.Count == 0)
                    return;

                // TODO: 絞り込みモードか、マッチしたフォルダ内だけかの切り替え
                bool filteringMode = true;

                // Treeview で選択ノードが変更されたのでListview でフィルターを掛ける。
                var collectionView = CollectionViewSource.GetDefaultView(MusicEntries);
                if (collectionView == null)
                    return;

                try
                {
                    collectionView.Filter = x =>
                    {
                        var entry = (NodeFile)x;

                        if (entry == null)
                            return false;

                        if (entry.FileUri == null)
                            return false;

                        string path = entry.FileUri.LocalPath; //person.FileUri.AbsoluteUri;
                        if (string.IsNullOrEmpty(path))
                            return false;
                        string filename = System.IO.Path.GetFileName(path);//System.IO.Path.GetFileName(uri.LocalPath);
                        if (string.IsNullOrEmpty(filename))
                            return false;

                        if ((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath == "/")
                        {
                            if (filteringMode)
                            {
                                // 絞り込みモード
                                if (!string.IsNullOrEmpty(_filterQuery))
                                {
                                    return (filename.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                // マッチしたフォルダ内だけ
                                path = path.Replace("/", "");

                                if (!string.IsNullOrEmpty(_filterQuery))
                                {
                                    return ((path == filename) && filename.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return (path == filename);
                                }
                            }
                        }
                        else
                        {
                            path = path.Replace(("/" + filename), "");

                            if (filteringMode)
                            {
                                // 絞り込みモード

                                if (!string.IsNullOrEmpty(_filterQuery))
                                {
                                    return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath));
                                }
                            }
                            else
                            {
                                // マッチしたフォルダ内だけ
                                if (!string.IsNullOrEmpty(_filterQuery))
                                {
                                    return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return (path == (_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath);
                                }
                            }
                        }
                    };

                }
                catch (Exception e)
                {
                    Debug.WriteLine("collectionView.Filter = x => " + e.Message);
                }

            }
        }

        private ObservableCollection<NodeFile> _musicEntries = new ObservableCollection<NodeFile>();
        public ObservableCollection<NodeFile> MusicEntries
        {
            get
            {
                return _musicEntries; 
            }
            set
            {
                if (value == _musicEntries)
                    return;

                _musicEntries = value;
                NotifyPropertyChanged(nameof(MusicEntries));

            }
        }

        private string _filterQuery = "";
        public string FilterQuery
        {
            get
            {
                return _filterQuery;
            }
            set
            {
                if (_filterQuery == value)
                    return;

                _filterQuery = value;
                NotifyPropertyChanged("FilterQuery");

                /*
                var collectionView = CollectionViewSource.GetDefaultView(MusicEntries);

                collectionView.Filter = x =>
                {
                    var entry = (NodeFile)x;

                    string test = entry.FilePath + entry.Name;

                    // 絞り込み
                    return (test.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                };
                collectionView.Refresh();

                */

                if (_selectedNodeDirectory == null)
                    return;

                // TODO: 絞り込みモードか、マッチしたフォルダ内だけかの切り替え
                bool filteringMode = true;

                // Treeview で選択ノードが変更されたのでListview でフィルターを掛ける。
                var collectionView = CollectionViewSource.GetDefaultView(MusicEntries);
                collectionView.Filter = x =>
                {
                    var entry = (NodeFile)x;

                    if (entry.FileUri == null)
                        return false;

                    string path = entry.FileUri.LocalPath; //person.FileUri.AbsoluteUri;
                    string filename = System.IO.Path.GetFileName(path);//System.IO.Path.GetFileName(uri.LocalPath);

                    if ((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath == "/")
                    {
                        if (filteringMode)
                        {
                            // 絞り込みモード
                            if (FilterQuery != "")
                            {
                                return (filename.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            // マッチしたフォルダ内だけ
                            path = path.Replace("/", "");

                            if (FilterQuery != "")
                            {
                                return ((path == filename) && filename.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return (path == filename);
                            }
                        }
                    }
                    else
                    {
                        path = path.Replace(("/" + filename), "");

                        if (filteringMode)
                        {
                            // 絞り込みモード

                            if (FilterQuery != "")
                            {
                                return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath));
                            }
                        }
                        else
                        {
                            // マッチしたフォルダ内だけ
                            if (FilterQuery != "")
                            {
                                return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return (path == (_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath);
                            }
                        }
                    }
                };

                collectionView.Refresh();

            }
        }

        #endregion

        #region == Search ==

        public ObservableCollection<Song> SearchResult
        {
            get
            {
                if (_mpc != null)
                {
                    return _mpc.SearchResult;
                }
                else
                {
                    return null;
                }
            }
        }

        private SearchTags _selectedSearchTags = SearchTags.Title;
        public SearchTags SelectedSearchTags
        {
            get
            {
                return _selectedSearchTags;
            }
            set
            {
                if (_selectedSearchTags == value)
                    return;

                _selectedSearchTags = value;
                NotifyPropertyChanged("SelectedSearchTags");
            }
        }

        private string _searchQuery;
        public string SearchQuery
        {
            get
            {
                return _searchQuery;
            }
            set
            {
                if (_searchQuery == value)
                    return;

                _searchQuery = value;
                NotifyPropertyChanged("SearchQuery");
            }
        }

        #endregion

        #region == MenuTree ==

        private MenuTreeBuilder _mainMenuItems = new MenuTreeBuilder();
        public ObservableCollection<NodeTree> MainMenuItems
        {
            get { return _mainMenuItems.Children; }
            set
            {
                _mainMenuItems.Children = value;
                NotifyPropertyChanged(nameof(MainMenuItems));
            }
        }

        private bool _isQueueVisible = true;
        public bool IsQueueVisible
        {
            get { return _isQueueVisible; }
            set
            {
                if (_isQueueVisible == value)
                    return;

                _isQueueVisible = value;
                NotifyPropertyChanged("IsQueueVisible");
            }
        }

        private bool _isPlaylistsVisible = true;
        public bool IsPlaylistsVisible
        {
            get { return _isPlaylistsVisible; }
            set
            {
                if (_isPlaylistsVisible == value)
                    return;

                _isPlaylistsVisible = value;
                NotifyPropertyChanged("IsPlaylistsVisible");
            }
        }

        private bool _isPlaylistItemVisible = true;
        public bool IsPlaylistItemVisible
        {
            get { return _isPlaylistItemVisible; }
            set
            {
                if (_isPlaylistItemVisible == value)
                    return;

                _isPlaylistItemVisible = value;
                NotifyPropertyChanged("IsPlaylistItemVisible");
            }
        }

        private bool _isBrowseVisible = true;
        public  bool IsBrowseVisible
        {
            get { return _isBrowseVisible; }
            set
            {
                if (_isBrowseVisible == value)
                    return;

                _isBrowseVisible = value;
                NotifyPropertyChanged("IsBrowseVisible");
            }
        }

        private bool _isSearchVisible = true;
        public bool IsSearchVisible
        {
            get { return _isSearchVisible; }
            set
            {
                if (_isSearchVisible == value)
                    return;

                _isSearchVisible = value;
                NotifyPropertyChanged("IsSearchVisible");
            }
        }

        private NodeTree _selectedNodeMenu = new NodeMenu("root");
        public NodeTree SelectedNodeMenu
        {
            get { return _selectedNodeMenu; }
            set
            {
                if (_selectedNodeMenu == value)
                    return;

                if (value == null)
                {
                    //Debug.WriteLine("selectedNodeMenu is null");
                    return;
                }

                if (value is NodeMenuQueue)
                {
                    //Debug.WriteLine("selectedNodeMenu is NodeMenuQueue");
                    IsQueueVisible = true;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = false;
                    IsBrowseVisible = false;
                    IsSearchVisible = false;
                }
                else if (value is NodeMenuPlaylists)
                {
                    //Debug.WriteLine("selectedNodeMenu is NodeMenuPlaylists");
                    IsQueueVisible = false;
                    IsPlaylistsVisible = true;
                    IsPlaylistItemVisible = false;
                    IsBrowseVisible = false;
                    IsSearchVisible = false;
                }
                else if (value is NodeMenuPlaylistItem)
                {
                    //Debug.WriteLine("selectedNodeMenu is NodeMenuPlaylistItem");
                    IsQueueVisible = false;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = true;
                    IsBrowseVisible = false;
                    IsSearchVisible = false;
                }
                else if (value is NodeMenuBrowse)
                {
                    //Debug.WriteLine("selectedNodeMenu is NodeMenuBrowse");

                    // TODO: TEST
                    IsBusy = true;

                    IsQueueVisible = false;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = false;
                    IsBrowseVisible = true;
                    IsSearchVisible = false;

                    IsBusy = false;
                }
                else if (value is NodeMenuSearch)
                {
                    //Debug.WriteLine("selectedNodeMenu is NodeMenuSearch");
                    IsQueueVisible = false;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = false;
                    IsBrowseVisible = false;
                    IsSearchVisible = true;
                }
                else if (value is NodeMenu)
                {
                    //Debug.WriteLine("selectedNodeMenu is NodeMenu ...unknown:" + _selectedNodeMenu.Name);

                    if (value.Name != "root")
                        throw new NotImplementedException();

                    IsQueueVisible = true;
                }
                else
                {
                    //Debug.WriteLine(value.Name);

                    throw new NotImplementedException();
                }

                _selectedNodeMenu = value;
                NotifyPropertyChanged(nameof(SelectedNodeMenu));


            }
        }

        #endregion

        #endregion

        #region == ステータス系 == 

        private string _connectionStatusMessage;
        public string ConnectionStatusMessage
        {
            get
            {
                return _connectionStatusMessage;
            }
            set
            {
                _connectionStatusMessage = value;
                NotifyPropertyChanged("ConnectionStatusMessage");
            }
        }

        private string _mpdStatusMessage;
        public string MpdStatusMessage
        {
            get
            {
                return _mpdStatusMessage;
            }
            set
            {
                _mpdStatusMessage = value;
                NotifyPropertyChanged("MpdStatusMessage");
            }
        }

        private static string _pathDefaultNoneButton = "";

        private static string _pathConnectingButton = "M11 14H9C9 9.03 13.03 5 18 5V7C14.13 7 11 10.13 11 14M18 11V9C15.24 9 13 11.24 13 14H15C15 12.34 16.34 11 18 11M7 4C7 2.89 6.11 2 5 2S3 2.89 3 4 3.89 6 5 6 7 5.11 7 4M11.45 4.5H9.45C9.21 5.92 8 7 6.5 7H3.5C2.67 7 2 7.67 2 8.5V11H8V8.74C9.86 8.15 11.25 6.5 11.45 4.5M19 17C20.11 17 21 16.11 21 15S20.11 13 19 13 17 13.89 17 15 17.89 17 19 17M20.5 18H17.5C16 18 14.79 16.92 14.55 15.5H12.55C12.75 17.5 14.14 19.15 16 19.74V22H22V19.5C22 18.67 21.33 18 20.5 18Z";
        private static string _pathConnectedButton = "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M12 20C7.59 20 4 16.41 4 12S7.59 4 12 4 20 7.59 20 12 16.41 20 12 20M16.59 7.58L10 14.17L7.41 11.59L6 13L10 17L18 9L16.59 7.58Z";
        //private static string _pathConnectedButton = "";
        //private static string _pathDisconnectedButton = "";
        private static string _pathNewConnectionButton = "M20,4C21.11,4 22,4.89 22,6V18C22,19.11 21.11,20 20,20H4C2.89,20 2,19.11 2,18V6C2,4.89 2.89,4 4,4H20M8.5,15V9H7.25V12.5L4.75,9H3.5V15H4.75V11.5L7.3,15H8.5M13.5,10.26V9H9.5V15H13.5V13.75H11V12.64H13.5V11.38H11V10.26H13.5M20.5,14V9H19.25V13.5H18.13V10H16.88V13.5H15.75V9H14.5V14A1,1 0 0,0 15.5,15H19.5A1,1 0 0,0 20.5,14Z";
        private static string _pathErrorInfoButton = "M23,12L20.56,14.78L20.9,18.46L17.29,19.28L15.4,22.46L12,21L8.6,22.47L6.71,19.29L3.1,18.47L3.44,14.78L1,12L3.44,9.21L3.1,5.53L6.71,4.72L8.6,1.54L12,3L15.4,1.54L17.29,4.72L20.9,5.54L20.56,9.22L23,12M20.33,12L18.5,9.89L18.74,7.1L16,6.5L14.58,4.07L12,5.18L9.42,4.07L8,6.5L5.26,7.09L5.5,9.88L3.67,12L5.5,14.1L5.26,16.9L8,17.5L9.42,19.93L12,18.81L14.58,19.92L16,17.5L18.74,16.89L18.5,14.1L20.33,12M11,15H13V17H11V15M11,7H13V13H11V7";

        private string _statusButton = _pathDefaultNoneButton;
        public string StatusButton
        {
            get
            {
                return _statusButton;
            }
            set
            {
                if (_statusButton == value)
                    return;

                _statusButton = value;
                NotifyPropertyChanged("StatusButton");
            }
        }

        private static string _pathMpdOkButton = "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M12 20C7.59 20 4 16.41 4 12S7.59 4 12 4 20 7.59 20 12 16.41 20 12 20M16.59 7.58L10 14.17L7.41 11.59L6 13L10 17L18 9L16.59 7.58Z";

        private static string _pathErrorMpdAckButton = "M11,15H13V17H11V15M11,7H13V13H11V7M12,2C6.47,2 2,6.5 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20Z";

        private string _mpdStatusButton = _pathMpdOkButton;
        public string MpdStatusButton
        {
            get
            {
                return _mpdStatusButton;
            }
            set
            {
                if (_mpdStatusButton == value)
                    return;

                _mpdStatusButton = value;
                NotifyPropertyChanged("MpdStatusButton");
            }
        }

        private bool _isUpdatingMpdDb;
        public bool IsUpdatingMpdDb
        {
            get
            {
                return _isUpdatingMpdDb;
            }
            set
            {
                _isUpdatingMpdDb = value;
                NotifyPropertyChanged("IsUpdatingMpdDb");
            }
        }

        private string _mpdVersion;
        public string MpdVersion
        {
            get
            {
                if (_mpdVersion != "")
                    return "MPD v" + _mpdVersion;
                else
                    return _mpdVersion;

            }
            set
            {
                if (value == _mpdVersion)
                    return;

                _mpdVersion = value;
                NotifyPropertyChanged("MpdVersion");
            }
        }

        #endregion

        #region == 設定画面 ==

        private ObservableCollection<Profile> _profiles = new ObservableCollection<Profile>();
        public ObservableCollection<Profile> Profiles
        {
            get { return _profiles; }
        }

        private Profile _currentProfile;
        public Profile CurrentProfile
        {
            get { return _currentProfile; }
            set
            {

                if (_currentProfile == value)
                    return;

                _currentProfile = value;

                SelectedProfile = _currentProfile;

                NotifyPropertyChanged("CurrentProfile");

                NotifyPropertyChanged("IsCurrentProfileNull");
            }
        }

        private Profile _selectedProfile;
        public Profile SelectedProfile
        {
            get
            {
                return _selectedProfile;
            }
            set
            {
                if (_selectedProfile == value)
                    return;

                _selectedProfile = value;

                if (_selectedProfile != null)
                {
                    ClearErrror("Host");
                    ClearErrror("Port");
                    Host = SelectedProfile.Host;
                    Port = SelectedProfile.Port.ToString();
                    Password = SelectedProfile.Password;
                    SetIsDefault = SelectedProfile.IsDefault;
                }
                else
                {
                    ClearErrror("Host");
                    ClearErrror("Port");
                    Host = "";
                    Port = "6600";
                    Password = "";
                }

                NotifyPropertyChanged("SelectedProfile");
            }
        }

        private string _host = "";
        public string Host
        {
            get { return _host; }
            set
            {
                ClearErrror("Host");
                _host = value;

                // Validate input.
                if (value == "")
                {
                    SetError("Host", MPDCtrl.Properties.Resources.Settings_ErrorHostMustBeSpecified);

                }
                else if (value == "localhost")
                {
                    _host = "127.0.0.1";
                }
                else
                {
                    IPAddress ipAddress = null;
                    try
                    {
                        ipAddress = IPAddress.Parse(value);
                        if (ipAddress != null)
                        {
                            _host = value;
                        }
                    }
                    catch
                    {
                        //System.FormatException
                        SetError("Host", MPDCtrl.Properties.Resources.Settings_ErrorHostInvalidAddressFormat);
                    }
                }

                NotifyPropertyChanged("Host");
            }
        }

        private int _port = 6600;
        public string Port
        {
            get { return _port.ToString(); }
            set
            {
                ClearErrror("Port");

                if (value == "")
                {
                    SetError("Port", MPDCtrl.Properties.Resources.Settings_ErrorPortMustBeSpecified);
                    _port = 0;
                }
                else
                {
                    // Validate input. Test with i;
                    if (Int32.TryParse(value, out int i))
                    {
                        //Int32.TryParse(value, out _defaultPort)
                        // Change the value only when test was successfull.
                        _port = i;
                        ClearErrror("Port");
                    }
                    else
                    {
                        SetError("Port", MPDCtrl.Properties.Resources.Settings_ErrorInvalidPortNaN);
                        _port = 0;
                    }
                }

                NotifyPropertyChanged("Port");
            }
        }

        private string _password = "";
        public string Password
        {
            get
            {
                return DummyPassword(_password);
            }
            set
            {
                // Don't. if (_password == value) ...

                _password = value;

                NotifyPropertyChanged("IsNotPasswordSet");
                NotifyPropertyChanged("IsPasswordSet");
                NotifyPropertyChanged("Password");
            }
        }

        private string Encrypt(string s)
        {
            if (String.IsNullOrEmpty(s)) { return ""; }

            byte[] entropy = new byte[] { 0x72, 0xa2, 0x12, 0x04 };

            try
            {
                byte[] userData = System.Text.Encoding.UTF8.GetBytes(s);

                byte[] encryptedData = ProtectedData.Protect(userData, entropy, DataProtectionScope.CurrentUser);

                return System.Convert.ToBase64String(encryptedData);
            }
            catch
            {
                return "";
            }
        }

        private string Decrypt(string s)
        {
            if (String.IsNullOrEmpty(s)) { return ""; }

            byte[] entropy = new byte[] { 0x72, 0xa2, 0x12, 0x04 };

            try
            {
                byte[] encryptedData = System.Convert.FromBase64String(s);

                byte[] userData = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);

                return System.Text.Encoding.UTF8.GetString(userData);
            }
            catch
            {
                return "";
            }
        }

        private string DummyPassword(string s)
        {
            if (String.IsNullOrEmpty(s)) { return ""; }
            string e = "";
            for (int i = 1; i <= s.Length; i++)
            {
                e = e + "*";
            }
            return e;
        }

        public bool IsPasswordSet
        {
            get
            {
                if (SelectedProfile != null)
                {
                    if (!string.IsNullOrEmpty(SelectedProfile.Password))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsNotPasswordSet
        {
            get
            {
                if (IsPasswordSet)
                    return false;
                else
                    return true;
            }
        }

        private bool _setIsDefault = true;
        public bool SetIsDefault
        {
            get { return _setIsDefault; }
            set
            {
                if (_setIsDefault == value)
                    return;

                if (value == false)
                {
                    if (Profiles.Count == 1)
                    {
                        return;
                    }
                }

                _setIsDefault = value;

                NotifyPropertyChanged("SetIsDefault");
            }
        }

        private bool _isUpdateOnStartup = false;
        public bool IsUpdateOnStartup
        {
            get { return _isUpdateOnStartup; }
            set
            {
                if (_isUpdateOnStartup == value)
                    return;

                _isUpdateOnStartup = value;

                NotifyPropertyChanged("IsUpdateOnStartup");
            }
        }

        private bool _isAutoScrollToNowPlaying = false;
        public bool IsAutoScrollToNowPlaying
        {
            get { return _isAutoScrollToNowPlaying; }
            set
            {
                if (_isAutoScrollToNowPlaying == value)
                    return;

                _isAutoScrollToNowPlaying = value;

                NotifyPropertyChanged("IsAutoScrollToNowPlaying");
            }
        }

        private bool _isSaveLog;
        public bool IsSaveLog
        {
            get { return _isSaveLog; }
            set
            {
                if (_isSaveLog == value)
                    return;

                _isSaveLog = value;

                NotifyPropertyChanged("IsSaveLog");
            }
        }


        private string _settingProfileEditMessage;
        public string SettingProfileEditMessage
        {
            get
            {
                return _settingProfileEditMessage;
            }
            set
            {
                _settingProfileEditMessage = value;
                NotifyPropertyChanged("SettingProfileEditMessage");
            }
        }

        #endregion

        #region == ダイアログ ==

        private string _dialogTitle;
        public string DialogTitle
        {
            get { return _dialogTitle; }
            set
            {
                if (_dialogTitle == value)
                    return;

                _dialogTitle = value;
                NotifyPropertyChanged("DialogTitle");
            }
        }

        private string _dialogMessage;
        public string DialogMessage
        {
            get { return _dialogMessage; }
            set
            {
                if (_dialogMessage == value)
                    return;

                _dialogMessage = value;
                NotifyPropertyChanged("DialogMessage");
            }
        }

        private string _dialogInputText;
        public string DialogInputText
        {
            get { return _dialogInputText; }
            set
            {
                if (_dialogInputText == value)
                    return;

                _dialogInputText = value;
                NotifyPropertyChanged("DialogInputText");
            }
        }

        public ObservableCollection<String> PlaylistNamesWithNew { get; set; } = new ObservableCollection<String>();

        // Not smart...
        public Func<string, bool> DialogResultFunction { get; set; }
        public Func<string, string, bool> DialogResultFunctionWith2Params { get; set; }
        public Func<string, List<string>, bool> DialogResultFunctionWith2ParamsWithObject { get; set; }
        public string DialogResultFunctionParamString { get; set; }
        public List<string> DialogResultFunctionParamObject { get; set; }

        public void ResetDialog()
        {
            DialogTitle = "";
            DialogMessage = "";
            DialogInputText = "";
            DialogResultFunction = null;
            DialogResultFunctionWith2Params = null;
            DialogResultFunctionWith2ParamsWithObject = null;
            DialogResultFunctionParamString = "";
            DialogResultFunctionParamObject = null;
        }

        #endregion

        #region == イベント ==

        // DebugWindow
        public delegate void DebugWindowShowHideEventHandler();
        public event DebugWindowShowHideEventHandler DebugWindowShowHide;

        public event EventHandler<string> DebugWindowOutput;

        public delegate void DebugWindowClearEventHandler();
        public event DebugWindowClearEventHandler DebugWindowClear;


        // AckWindow
        public delegate void AckWindowShowHideEventHandler();
        public event AckWindowShowHideEventHandler AckWindowShowHide;

        public event EventHandler<string> AckWindowOutput;

        public delegate void AckWindowClearEventHandler();
        public event AckWindowClearEventHandler AckWindowClear;

        // Queue listview ScrollIntoView
        public event EventHandler<int> ScrollIntoView;

        #endregion

        private MPC _mpc = new MPC();

        public MainViewModel()
        {
            #region == データ保存フォルダ ==

            // データ保存フォルダの取得
            _appDataFolder = _envDataFolder + System.IO.Path.DirectorySeparatorChar + _appDeveloper + System.IO.Path.DirectorySeparatorChar + _appName;
            // 設定ファイルのパス
            _appConfigFilePath = _appDataFolder + System.IO.Path.DirectorySeparatorChar + _appName + ".config";
            // 存在していなかったら作成
            System.IO.Directory.CreateDirectory(_appDataFolder);

            #endregion

            #region == コマンド初期化 ==

            PlayCommand = new RelayCommand(PlayCommand_ExecuteAsync, PlayCommand_CanExecute);
            PlayStopCommand = new RelayCommand(PlayStopCommand_Execute, PlayStopCommand_CanExecute);
            PlayPauseCommand = new RelayCommand(PlayPauseCommand_Execute, PlayPauseCommand_CanExecute);
            PlayNextCommand = new RelayCommand(PlayNextCommand_ExecuteAsync, PlayNextCommand_CanExecute);
            PlayPrevCommand = new RelayCommand(PlayPrevCommand_ExecuteAsync, PlayPrevCommand_CanExecute);
            ChangeSongCommand = new RelayCommand(ChangeSongCommand_ExecuteAsync, ChangeSongCommand_CanExecute);

            SetRpeatCommand = new RelayCommand(SetRpeatCommand_ExecuteAsync, SetRpeatCommand_CanExecute);
            SetRandomCommand = new RelayCommand(SetRandomCommand_ExecuteAsync, SetRandomCommand_CanExecute);
            SetConsumeCommand = new RelayCommand(SetConsumeCommand_ExecuteAsync, SetConsumeCommand_CanExecute);
            SetSingleCommand = new RelayCommand(SetSingleCommand_ExecuteAsync, SetSingleCommand_CanExecute);

            SetVolumeCommand = new RelayCommand(SetVolumeCommand_ExecuteAsync, SetVolumeCommand_CanExecute);
            VolumeMuteCommand = new RelayCommand(VolumeMuteCommand_Execute, VolumeMuteCommand_CanExecute);
            VolumeUpCommand = new RelayCommand(VolumeUpCommand_Execute, VolumeUpCommand_CanExecute);
            VolumeDownCommand = new RelayCommand(VolumeDownCommand_Execute, VolumeDownCommand_CanExecute);

            SetSeekCommand = new RelayCommand(SetSeekCommand_ExecuteAsync, SetSeekCommand_CanExecute);

            PlaylistListviewEnterKeyCommand = new RelayCommand(PlaylistListviewEnterKeyCommand_ExecuteAsync, PlaylistListviewEnterKeyCommand_CanExecute);
            PlaylistListviewLoadPlaylistCommand = new RelayCommand(PlaylistListviewLoadPlaylistCommand_ExecuteAsync, PlaylistListviewLoadPlaylistCommand_CanExecute);
            PlaylistListviewClearLoadPlaylistCommand = new RelayCommand(PlaylistListviewClearLoadPlaylistCommand_ExecuteAsync, PlaylistListviewClearLoadPlaylistCommand_CanExecute);
            PlaylistListviewLeftDoubleClickCommand = new GenericRelayCommand<String>(param => PlaylistListviewLeftDoubleClickCommand_ExecuteAsync(param), param => PlaylistListviewLeftDoubleClickCommand_CanExecute());
            ChangePlaylistCommand = new RelayCommand(ChangePlaylistCommand_ExecuteAsync, ChangePlaylistCommand_CanExecute);
            PlaylistListviewRemovePlaylistCommand = new GenericRelayCommand<String>(param => PlaylistListviewRemovePlaylistCommand_Execute(param), param => PlaylistListviewRemovePlaylistCommand_CanExecute());
            PlaylistListviewRenamePlaylistCommand = new GenericRelayCommand<String>(param => PlaylistListviewRenamePlaylistCommand_Execute(param), param => PlaylistListviewRenamePlaylistCommand_CanExecute());

            LocalfileListviewEnterKeyCommand = new GenericRelayCommand<object>(param => LocalfileListviewEnterKeyCommand_Execute(param), param => LocalfileListviewEnterKeyCommand_CanExecute());
            LocalfileListviewAddCommand = new GenericRelayCommand<object>(param => LocalfileListviewAddCommand_Execute(param), param => LocalfileListviewAddCommand_CanExecute());
            LocalfileListviewLeftDoubleClickCommand = new GenericRelayCommand<object>(param => LocalfileListviewLeftDoubleClickCommand_Execute(param), param => LocalfileListviewLeftDoubleClickCommand_CanExecute());

            QueueListviewEnterKeyCommand = new RelayCommand(QueueListviewEnterKeyCommand_ExecuteAsync, QueueListviewEnterKeyCommand_CanExecute);
            QueueListviewLeftDoubleClickCommand = new GenericRelayCommand<SongInfo>(param => QueueListviewLeftDoubleClickCommand_ExecuteAsync(param), param => QueueListviewLeftDoubleClickCommand_CanExecute());
            QueueListviewDeleteCommand = new GenericRelayCommand<object>(param => QueueListviewDeleteCommand_Execute(param), param => QueueListviewDeleteCommand_CanExecute());
            QueueListviewClearCommand = new RelayCommand(QueueListviewClearCommand_ExecuteAsync, QueueListviewClearCommand_CanExecute);
            QueueListviewSaveCommand = new RelayCommand(QueueListviewSaveCommand_ExecuteAsync, QueueListviewSaveCommand_CanExecute);
            QueueListviewMoveUpCommand = new GenericRelayCommand<object>(param => QueueListviewMoveUpCommand_Execute(param), param => QueueListviewMoveUpCommand_CanExecute());
            QueueListviewMoveDownCommand = new GenericRelayCommand<object>(param => QueueListviewMoveDownCommand_Execute(param), param => QueueListviewMoveDownCommand_CanExecute());
            QueueListviewPlaylistAddCommand = new GenericRelayCommand<object>(param => QueueListviewPlaylistAddCommand_Execute(param), param => QueueListviewPlaylistAddCommand_CanExecute());

            ShowSettingsCommand = new RelayCommand(ShowSettingsCommand_Execute, ShowSettingsCommand_CanExecute);
            SettingsOKCommand = new RelayCommand(SettingsOKCommand_Execute, SettingsOKCommand_CanExecute);

            NewProfileCommand = new RelayCommand(NewProfileCommand_Execute, NewProfileCommand_CanExecute);
            DeleteProfileCommand = new RelayCommand(DeleteProfileCommand_Execute, DeleteProfileCommand_CanExecute);
            SaveProfileCommand = new GenericRelayCommand<object>(param => SaveProfileCommand_Execute(param), param => SaveProfileCommand_CanExecute());
            UpdateProfileCommand = new GenericRelayCommand<object>(param => UpdateProfileCommand_Execute(param), param => UpdateProfileCommand_CanExecute());
            ChangeConnectionProfileCommand = new GenericRelayCommand<object>(param => ChangeConnectionProfileCommand_Execute(param), param => ChangeConnectionProfileCommand_CanExecute());

            ListAllCommand = new RelayCommand(ListAllCommand_ExecuteAsync, ListAllCommand_CanExecute);

            ComfirmationDialogOKCommand = new RelayCommand(ComfirmationDialogOKCommand_Execute, ComfirmationDialogOKCommand_CanExecute);
            ComfirmationDialogCancelCommand = new RelayCommand(ComfirmationDialogCancelCommand_Execute, ComfirmationDialogCancelCommand_CanExecute);
            InformationDialogOKCommand = new RelayCommand(InformationDialogOKCommand_Execute, InformationDialogOKCommand_CanExecute);

            InputDialogOKCommand = new RelayCommand(InputDialogOKCommand_Execute, InputDialogOKCommand_CanExecute);
            InputDialogCancelCommand = new RelayCommand(InputDialogCancelCommand_Execute, InputDialogCancelCommand_CanExecute);

            ShowChangePasswordDialogCommand = new GenericRelayCommand<object>(param => ShowChangePasswordDialogCommand_Execute(param), param => ShowChangePasswordDialogCommand_CanExecute());
            ChangePasswordDialogOKCommand = new GenericRelayCommand<object>(param => ChangePasswordDialogOKCommand_Execute(param), param => ChangePasswordDialogOKCommand_CanExecute());
            ChangePasswordDialogCancelCommand = new RelayCommand(ChangePasswordDialogCancelCommand_Execute, ChangePasswordDialogCancelCommand_CanExecute);

            PlaylistSelectDialogOKCommand = new GenericRelayCommand<string>(param => PlaylistSelectDialogOKCommand_Execute(param), param => PlaylistSelectDialogOKCommand_CanExecute());
            PlaylistSelectDialogCancelCommand = new RelayCommand(PlaylistSelectDialogCancelCommand_Execute, PlaylistSelectDialogCancelCommand_CanExecute);

            EscapeCommand = new RelayCommand(EscapeCommand_ExecuteAsync, EscapeCommand_CanExecute);

            QueueColumnHeaderPositionShowHideCommand = new RelayCommand(QueueColumnHeaderPositionShowHideCommand_Execute, QueueColumnHeaderPositionShowHideCommand_CanExecute);
            QueueColumnHeaderNowPlayingShowHideCommand = new RelayCommand(QueueColumnHeaderNowPlayingShowHideCommand_Execute, QueueColumnHeaderNowPlayingShowHideCommand_CanExecute);
            QueueColumnHeaderTimeShowHideCommand = new RelayCommand(QueueColumnHeaderTimeShowHideCommand_Execute, QueueColumnHeaderTimeShowHideCommand_CanExecute);
            QueueColumnHeaderArtistShowHideCommand = new RelayCommand(QueueColumnHeaderArtistShowHideCommand_Execute, QueueColumnHeaderArtistShowHideCommand_CanExecute);
            QueueColumnHeaderAlbumShowHideCommand = new RelayCommand(QueueColumnHeaderAlbumShowHideCommand_Execute, QueueColumnHeaderAlbumShowHideCommand_CanExecute);
            QueueColumnHeaderGenreShowHideCommand = new RelayCommand(QueueColumnHeaderGenreShowHideCommand_Execute, QueueColumnHeaderGenreShowHideCommand_CanExecute);
            QueueColumnHeaderLastModifiedShowHideCommand = new RelayCommand(QueueColumnHeaderLastModifiedShowHideCommand_Execute, QueueColumnHeaderLastModifiedShowHideCommand_CanExecute);

            ShowFindCommand = new RelayCommand(ShowFindCommand_Execute, ShowFindCommand_CanExecute);
            ShowFindCancelCommand = new RelayCommand(ShowFindCancelCommand_Execute, ShowFindCancelCommand_CanExecute);

            SearchExecCommand = new RelayCommand(SearchExecCommand_Execute, SearchExecCommand_CanExecute);
            FilterClearCommand = new RelayCommand(FilterClearCommand_Execute, FilterClearCommand_CanExecute);

            SongsListviewAddCommand = new GenericRelayCommand<object>(param => SongsListviewAddCommand_Execute(param), param => SongsListviewAddCommand_CanExecute());
            SongFilesListviewAddCommand = new GenericRelayCommand<object>(param => SongFilesListviewAddCommand_Execute(param), param => SongFilesListviewAddCommand_CanExecute());

            ScrollIntoNowPlayingCommand = new RelayCommand(ScrollIntoNowPlayingCommand_Execute, ScrollIntoNowPlayingCommand_CanExecute);

            ClearDebugTextCommand = new RelayCommand(ClearDebugTextCommand_Execute, ClearDebugTextCommand_CanExecute);
            ShowDebugWindowCommand = new RelayCommand(ShowDebugWindowCommand_Execute, ShowDebugWindowCommand_CanExecute);
            ClearAckTextCommand = new RelayCommand(ClearAckTextCommand_Execute, ClearAckTextCommand_CanExecute);
            ShowAckWindowCommand = new RelayCommand(ShowAckWindowCommand_Execute, ShowAckWindowCommand_CanExecute);


            #endregion

            #region == イベント ==

            _mpc.IsBusy += new MPC.IsBusyEvent(OnMpcIsBusy);

            _mpc.MpdConnected += new MPC.IsMpdConnectedEvent(OnMpdConnected);

            _mpc.DebugOutput += new MPC.DebugOutputEvent(OnDebugOutput);


            _mpc.MpdPlayerStatusChanged += new MPC.MpdPlayerStatusChangedEvent(OnMpdPlayerStatusChanged);
            _mpc.MpdCurrentQueueChanged += new MPC.MpdCurrentQueueChangedEvent(OnMpdCurrentQueueChanged);
            _mpc.MpdPlaylistsChanged += new MPC.MpdPlaylistsChangedEvent(OnMpdPlaylistsChanged);


            /*
            _mpc.Connected += new MPC.MpdConnected(OnMpdConnected);
            _mpc.StatusChanged += new MPC.MpdStatusChanged(OnStatusChanged);
            _mpc.StatusUpdate += new MPC.MpdStatusUpdate(OnMpdStatusUpdate);
            _mpc.ErrorReturned += new MPC.MpdError(OnError);
            _mpc.ErrorConnected += new MPC.MpdConnectionError(OnConnectionError);
            _mpc.ConnectionStatusChanged += new MPC.MpdConnectionStatusChanged(OnConnectionStatusChanged);
            
            _mpc.OnAlbumArtStatusChanged += new MPC.MpdAlbumArtStatusChanged(OnAlbumArtStatusChanged);

            if (DeveloperMode)
            {
                _mpc.DataReceived += new MPC.MpdDataReceived(OnDataReceived);
                _mpc.DataSent += new MPC.MpdDataSent(OnDataSent);
            }
            */

            #endregion

            #region == タイマー ==  

            // Init Song's time elapsed timer.
            _elapsedTimer = new System.Timers.Timer(500);
            _elapsedTimer.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedTimer);

            #endregion

        }

        #region == タイマー ==

        private System.Timers.Timer _elapsedTimer;
        private void ElapsedTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((_elapsed < _time) && (_mpc.MpdStatus.MpdState == Status.MpdPlayState.Play))
            {
                _elapsed += 0.5;
                NotifyPropertyChanged("Elapsed");
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        #endregion

        #region == メソッド ==

        private void Start(string host, int port, string password)
        {
            _mpc.MpdConnect(host, port);

            // for debug.
            ShowDebugWindowCommand_Execute();
        }

        private void UpdateButtonStatus()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    //Play button
                    switch (_mpc.MpdStatus.MpdState)
                    {
                        case Status.MpdPlayState.Play:
                            {
                                PlayButton = _pathPauseButton;
                                break;
                            }
                        case Status.MpdPlayState.Pause:
                            {
                                PlayButton = _pathPlayButton;
                                break;
                            }
                        case Status.MpdPlayState.Stop:
                            {
                                PlayButton = _pathPlayButton;
                                break;
                            }

                            //_pathStopButton
                    }

                    // "quietly" update.

                    double tmpVol = Convert.ToDouble(_mpc.MpdStatus.MpdVolume);
                    if (_volume != tmpVol)
                    {
                        _volume = tmpVol;
                        NotifyPropertyChanged("Volume");
                    }
                    //_volume = Convert.ToDouble(_mpc.MpdStatus.MpdVolume);
                    //NotifyPropertyChanged("Volume");

                    _random = _mpc.MpdStatus.MpdRandom;
                    NotifyPropertyChanged("Random");

                    _repeat = _mpc.MpdStatus.MpdRepeat;
                    NotifyPropertyChanged("Repeat");

                    _consume = _mpc.MpdStatus.MpdConsume;
                    NotifyPropertyChanged("Consume");

                    _single = _mpc.MpdStatus.MpdSingle;
                    NotifyPropertyChanged("Single");

                    // no need to care about "double" updates for time.
                    Time = _mpc.MpdStatus.MpdSongTime;

                    _elapsed = _mpc.MpdStatus.MpdSongElapsed;
                    //NotifyPropertyChanged("Elapsed");

                    //start elapsed timer.
                    if (_mpc.MpdStatus.MpdState == Status.MpdPlayState.Play)
                    {
                        if (!_elapsedTimer.Enabled)
                            _elapsedTimer.Start();
                    }
                    else
                    {
                        _elapsedTimer.Stop();
                    }

                    //
                    //Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());

                }
                catch
                {
                    Debug.WriteLine("Error@UpdateButtonStatus");
                }

            });

        }

        // TODO:
        private bool CheckPlaylistNameExists(string playlistName)
        {
            bool match = false;

            if (Playlists.Count > 0)
            {

                foreach (var hoge in Playlists)
                {
                    if (hoge.ToLower() == playlistName.ToLower())
                    {
                        match = true;
                        break;
                    }
                }
            }

            return match;
        }

        private void UpdateStatus()
        {
            UpdateButtonStatus();

            Application.Current.Dispatcher.Invoke(() =>
            {
                bool isSongChanged = false;
                bool isCurrentSongWasNull = false;

                if (CurrentSong != null)
                {
                    if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                    {
                        isSongChanged = true;

                        // Clear IsPlaying icon
                        CurrentSong.IsPlaying = false;

                        IsAlbumArtVisible = false;
                        AlbumArt = _albumArtDefault;
                    }
                }
                else
                {
                    isCurrentSongWasNull = true;
                }

                // Sets Current Song
                var item = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                if (item != null)
                {
                    CurrentSong = (item as SongInfo);
                    CurrentSong.IsPlaying = true;

                    if (isSongChanged)
                    {
                        if (IsAutoScrollToNowPlaying)
                            ScrollIntoView?.Invoke(this, CurrentSong.Index);

                        // AlbumArt
                        if (!String.IsNullOrEmpty(CurrentSong.file))
                        {
                            //Debug.WriteLine("AlbumArt isPlayer: " + CurrentSong.file);
                            //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                        }
                    }
                    else
                    {
                        if (isCurrentSongWasNull)
                        {
                            if (IsAutoScrollToNowPlaying)
                                ScrollIntoView?.Invoke(this, CurrentSong.Index);

                            // AlbumArt
                            if (!String.IsNullOrEmpty(CurrentSong.file))
                            {
                                //Debug.WriteLine("AlbumArt isPlayer: isCurrentSongWasNull");
                                //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                            }
                        }
                        else
                        {
                            //Debug.WriteLine("AlbumArt isPlayer: !isSongChanged.");
                        }
                    }
                }
                else
                {
                    CurrentSong = null;

                    IsAlbumArtVisible = false;
                    AlbumArt = _albumArtDefault;
                }
            });

        }

        private void UpdateCurrentQueue()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Queue.Count > 0)
                {
                    IsBusy = true;

                    // 削除する曲の一時リスト
                    List<SongInfo> _tmpQueue = new List<SongInfo>();

                    // 既存のリストの中で新しいリストにないものを削除
                    foreach (var sng in Queue)
                    {
                        var queitem = _mpc.CurrentQueue.FirstOrDefault(i => i.Id == sng.Id);
                        if (queitem == null)
                        {
                            // 削除リストに追加
                            _tmpQueue.Add(sng);
                        }
                    }

                    // 削除リストをループ
                    foreach (var hoge in _tmpQueue)
                    {
                        Queue.Remove(hoge);
                    }

                    // 新しいリストの中から既存のリストにないものを追加または更新
                    foreach (var sng in _mpc.CurrentQueue)
                    {
                        var fuga = Queue.FirstOrDefault(i => i.Id == sng.Id);
                        if (fuga != null)
                        {
                            // TODO:
                            fuga.Pos = sng.Pos;
                            //fuga.Id = sng.Id; // 流石にIDは変わらないだろう。
                            fuga.LastModified = sng.LastModified;
                            //fuga.Time = sng.Time; // format exception が煩い。
                            fuga.Title = sng.Title;
                            fuga.Artist = sng.Artist;
                            fuga.Album = sng.Album;
                            fuga.AlbumArtist = sng.AlbumArtist;
                            fuga.Composer = sng.Composer;
                            fuga.Date = sng.Date;
                            fuga.duration = sng.duration;
                            fuga.file = sng.file;
                            fuga.Genre = sng.Genre;
                            fuga.Track = sng.Track;

                            //Queue.Move(fuga.Index, sng.Index);
                            fuga.Index = sng.Index;
                        }
                        else
                        {
                            Queue.Add(sng);
                            //Queue.Insert(sng.Index, sng);
                        }
                    }

                    // Set Current and NowPlaying.
                    var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (curitem != null)
                    {
                        CurrentSong = (curitem as SongInfo);
                        CurrentSong.IsPlaying = true;

                        if (IsAutoScrollToNowPlaying)
                            ScrollIntoView?.Invoke(this, CurrentSong.Index);

                        // AlbumArt
                        if (_mpc.AlbumArt.SongFilePath != curitem.file)
                        {
                            IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;

                            //Debug.WriteLine("AlbumArt isCurrentQueue: " + CurrentSong.file);

                            if (!String.IsNullOrEmpty(CurrentSong.file))
                            {
                                //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                            }
                        }
                    }
                    else
                    {
                        CurrentSong = null;

                        IsAlbumArtVisible = false;
                        AlbumArt = _albumArtDefault;
                    }

                    // 移動したりするとPosが変更されても順番が反映されないので、
                    var collectionView = CollectionViewSource.GetDefaultView(Queue);
                    collectionView.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                    collectionView.Refresh();

                }
                else
                {
                    IsBusy = true;

                    foreach (var sng in _mpc.CurrentQueue)
                    {
                        Queue.Add(sng);
                    }

                    // Set Current and NowPlaying.
                    var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (curitem != null)
                    {
                        CurrentSong = (curitem as SongInfo);
                        CurrentSong.IsPlaying = true;

                        if (IsAutoScrollToNowPlaying)
                            ScrollIntoView?.Invoke(this, CurrentSong.Index);

                        // AlbumArt
                        if (_mpc.AlbumArt.SongFilePath != curitem.file)
                        {
                            IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;

                            //Debug.WriteLine("AlbumArt isCurrentQueue from Clear: " + CurrentSong.file);
                            if (!String.IsNullOrEmpty(CurrentSong.file))
                            {
                                //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                            }
                        }
                    }
                    else
                    {
                        // just in case.
                        CurrentSong = null;

                        IsAlbumArtVisible = false;
                        AlbumArt = _albumArtDefault;
                    }

                    IsBusy = false;
                }
            });
        }

        private void UpdatePlaylists()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {

                NodeMenuPlaylists playlistDir = _mainMenuItems.PlaylistsDirectory;

                if (playlistDir != null)
                {
                    foreach (var hoge in _mpc.Playlists)
                    {
                        NodeMenuPlaylistItem fuga = new NodeMenuPlaylistItem(hoge);
                        playlistDir.Children.Add(fuga);
                    }
                }

            });
        }

        private void UpdateLocalFiles()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MusicEntries.Clear();

                foreach (var songfile in _mpc.LocalFiles)
                {
                    if (string.IsNullOrEmpty(songfile)) continue;

                    try
                    {
                        Uri uri = new Uri(@"file:///" + songfile);
                        if (uri.IsFile)
                        {
                            string filename = System.IO.Path.GetFileName(songfile);//System.IO.Path.GetFileName(uri.LocalPath);
                            NodeFile hoge = new NodeFile(filename, uri, songfile);

                            MusicEntries.Add(hoge);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(songfile + e.Message);

                        IsBusy = false;
                    }
                }

                //
                MusicDirectories.Clear();
                try
                {
                    _musicDirectories.Load(_mpc.LocalDirectories.ToList<String>());

                    if (MusicDirectories.Count > 0)
                    {
                        //SelectedNodeDirectory = MusicDirectories[0] as NodeDirectory;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("_musicDirectories.Load: " + e.Message);

                    IsBusy = false;
                }
            });
        }

        #endregion

        #region == イベント ==

        // 起動時の処理
        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            #region == アプリ設定のロード  ==

            try
            {
                // アプリ設定情報の読み込み
                if (File.Exists(_appConfigFilePath))
                {
                    XDocument xdoc = XDocument.Load(_appConfigFilePath);

                    #region == ウィンドウ関連 ==

                    if (sender is Window)
                    {
                        // Main Window element
                        var mainWindow = xdoc.Root.Element("MainWindow");
                        if (mainWindow != null)
                        {
                            var hoge = mainWindow.Attribute("top");
                            if (hoge != null)
                            {
                                (sender as Window).Top = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("left");
                            if (hoge != null)
                            {
                                (sender as Window).Left = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("height");
                            if (hoge != null)
                            {
                                (sender as Window).Height = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("width");
                            if (hoge != null)
                            {
                                (sender as Window).Width = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("state");
                            if (hoge != null)
                            {
                                if (hoge.Value == "Maximized")
                                {
                                    (sender as Window).WindowState = WindowState.Maximized;
                                }
                                else if (hoge.Value == "Normal")
                                {
                                    (sender as Window).WindowState = WindowState.Normal;
                                }
                                else if (hoge.Value == "Minimized")
                                {
                                    (sender as Window).WindowState = WindowState.Normal;
                                }
                            }
                        }
                    }



                    #endregion

                    #region == オプション設定 ==

                    var opts = xdoc.Root.Element("Options");
                    if (opts != null)
                    {
                        var hoge = opts.Attribute("AutoScrollToNowPlaying");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsAutoScrollToNowPlaying = true;
                            }
                            else
                            {
                                IsAutoScrollToNowPlaying = false;
                            }
                        }

                        hoge = opts.Attribute("UpdateOnStartup");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsUpdateOnStartup = true;
                            }
                            else
                            {
                                IsUpdateOnStartup = false;
                            }
                        }

                        if (DeveloperMode)
                        {
                            hoge = opts.Attribute("ShowDebugWindow");
                            if (hoge != null)
                            {
                                if (hoge.Value == "True")
                                {
                                    IsShowDebugWindow = true;

                                }
                                else
                                {
                                    IsShowDebugWindow = false;
                                }
                            }

                            hoge = opts.Attribute("SaveLog");
                            if (hoge != null)
                            {
                                if (hoge.Value == "True")
                                {
                                    IsSaveLog = true;

                                }
                                else
                                {
                                    IsSaveLog = false;
                                }
                            }
                        }
                    }

                    #endregion

                    #region == プロファイル設定  ==

                    var xProfiles = xdoc.Root.Element("Profiles");
                    if (xProfiles != null)
                    {
                        var profileList = xProfiles.Elements("Profile");

                        foreach (var p in profileList)
                        {
                            Profile pro = new Profile();

                            if (p.Attribute("Name") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Name").Value))
                                    pro.Name = p.Attribute("Name").Value;
                            }
                            if (p.Attribute("Host") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Host").Value))
                                    pro.Host = p.Attribute("Host").Value;
                            }
                            if (p.Attribute("Port") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Port").Value))
                                {
                                    try
                                    {
                                        pro.Port = Int32.Parse(p.Attribute("Port").Value);
                                    }
                                    catch
                                    {
                                        pro.Port = 6600;
                                    }
                                }
                            }
                            if (p.Attribute("Password") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Password").Value))
                                    pro.Password = Decrypt(p.Attribute("Password").Value);
                            }
                            if (p.Attribute("IsDefault") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("IsDefault").Value))
                                {
                                    if (p.Attribute("IsDefault").Value == "True")
                                    {
                                        pro.IsDefault = true;

                                        CurrentProfile = pro;
                                        SelectedProfile = pro;
                                    }
                                }
                            }

                            Profiles.Add(pro);
                        }
                    }
                    #endregion

                    #region == ヘッダーカラム設定 ==

                    var Headers = xdoc.Root.Element("Headers");///Queue/Position
                    if (Headers != null)
                    {
                        var Que = Headers.Element("Queue");
                        if (Que != null)
                        {
                            var column = Que.Element("Position");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                        {
                                            QueueColumnHeaderPositionVisibility = true;
                                        }
                                        else
                                        {
                                            QueueColumnHeaderPositionVisibility = false;
                                        }
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderPositionWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderPositionWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderPositionWidth > 0) 
                                    QueueColumnHeaderPositionWidthRestore = QueueColumnHeaderPositionWidth;
                            }
                            column = Que.Element("NowPlaying");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderNowPlayingVisibility = true;
                                        else
                                            QueueColumnHeaderNowPlayingVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderNowPlayingWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderNowPlayingWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderNowPlayingWidth > 0)
                                    QueueColumnHeaderNowPlayingWidthRestore = QueueColumnHeaderNowPlayingWidth;
                            }
                            column = Que.Element("Title");
                            if (column != null)
                            {
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderTitleWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                            if (QueueColumnHeaderTitleWidth < 120)
                                                QueueColumnHeaderTitleWidth = 160;
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderTitleWidth = 160;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderTitleWidth > 0)
                                    QueueColumnHeaderTitleWidthRestore = QueueColumnHeaderTitleWidth;
                            }
                            column = Que.Element("Time");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderTimeVisibility = true;
                                        else
                                            QueueColumnHeaderTimeVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderTimeWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderTimeWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderTimeWidth > 0)
                                    QueueColumnHeaderTimeWidthRestore = QueueColumnHeaderTimeWidth;
                            }
                            column = Que.Element("Artist");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderArtistVisibility = true;
                                        else
                                            QueueColumnHeaderArtistVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderArtistWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderArtistWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderArtistWidth > 0)
                                    QueueColumnHeaderArtistWidthRestore = QueueColumnHeaderArtistWidth;
                            }
                            column = Que.Element("Album");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderAlbumVisibility = true;
                                        else
                                            QueueColumnHeaderAlbumVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderAlbumWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderAlbumWidth= 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderAlbumWidth > 0)
                                    QueueColumnHeaderAlbumWidthRestore = QueueColumnHeaderAlbumWidth;
                            }
                            column = Que.Element("Genre");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderGenreVisibility = true;
                                        else
                                            QueueColumnHeaderGenreVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderGenreWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderGenreWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderGenreWidth > 0)
                                    QueueColumnHeaderGenreWidthRestore = QueueColumnHeaderGenreWidth;
                            }
                            column = Que.Element("LastModified");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderLastModifiedVisibility = true;
                                        else
                                            QueueColumnHeaderLastModifiedVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderLastModifiedWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderLastModifiedWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderLastModifiedWidth > 0)
                                    QueueColumnHeaderLastModifiedWidthRestore = QueueColumnHeaderLastModifiedWidth;
                            }
                        }
                    }

                    #endregion

                    #region == レイアウト ==

                    var lay = xdoc.Root.Element("Layout");
                    if (lay != null)
                    {
                        var leftpain = lay.Element("LeftPain");
                        if (leftpain != null)
                        {
                            if (leftpain.Attribute("Width") != null)
                            {
                                if (!string.IsNullOrEmpty(leftpain.Attribute("Width").Value))
                                {
                                    try
                                    {
                                        MainLeftPainWidth = Double.Parse(leftpain.Attribute("Width").Value.ToString());
                                    }
                                    catch
                                    {
                                        MainLeftPainWidth = 241;
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine("■■■■■ Error  設定ファイルのロード中 - FileNotFoundException : " + _appConfigFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("■■■■■ Error  設定ファイルのロード中: " + ex + " while opening : " + _appConfigFilePath);
            }

            #endregion

            IsFullyLoaded = true;

            NotifyPropertyChanged("IsCurrentProfileSet");
            if (CurrentProfile == null)
            {
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.Init_NewConnectionSetting;
                StatusButton = _pathNewConnectionButton;
                IsConnectionSettingShow = true;
            }
            else
            {
                IsConnectionSettingShow = false;

                Start(CurrentProfile.Host, CurrentProfile.Port, CurrentProfile.Password);
            }

            // log
            if (IsSaveLog)
            {
                App app = App.Current as App;
                if (app != null)
                {
                    app.IsSaveErrorLog = true;
                }
            }
        }

        // 起動後画面が描画された時の処理
        public void OnContentRendered(object sender, EventArgs e)
        {
            IsFullyRendered = true;
        }

        // 終了時の処理
        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            // 二重起動とか途中でシャットダウンした時にデータが消えないように。
            if (!IsFullyLoaded)
                return;

            double windowWidth = 780;

            #region == アプリ設定の保存 ==

            // 設定ファイル用のXMLオブジェクト
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

            // Root Document Element
            XmlElement root = doc.CreateElement(string.Empty, "App", string.Empty);
            doc.AppendChild(root);

            XmlAttribute attrs = doc.CreateAttribute("Version");
            attrs.Value = _appVer;
            root.SetAttributeNode(attrs);

            #region == ウィンドウ関連 ==

            // MainWindow
            if (sender is Window)
            {
                // Main Window element
                XmlElement mainWindow = doc.CreateElement(string.Empty, "MainWindow", string.Empty);

                Window w = (sender as Window);
                // Main Window attributes
                attrs = doc.CreateAttribute("height");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Height.ToString();
                }
                else
                {
                    attrs.Value = w.Height.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("width");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Width.ToString();
                    windowWidth = w.RestoreBounds.Width;
                }
                else
                {
                    attrs.Value = w.Width.ToString();
                    windowWidth = w.Width;

                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("top");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Top.ToString();
                }
                else
                {
                    attrs.Value = w.Top.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("left");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Left.ToString();
                }
                else
                {
                    attrs.Value = w.Left.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("state");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = "Maximized";
                }
                else if (w.WindowState == WindowState.Normal)
                {
                    attrs.Value = "Normal";

                }
                else if (w.WindowState == WindowState.Minimized)
                {
                    attrs.Value = "Minimized";
                }
                mainWindow.SetAttributeNode(attrs);

                // set MainWindow element to root.
                root.AppendChild(mainWindow);

            }

            #endregion

            #region == オプション設定の保存 ==

            XmlElement opts = doc.CreateElement(string.Empty, "Options", string.Empty);

            //
            attrs = doc.CreateAttribute("AutoScrollToNowPlaying");
            if (IsAutoScrollToNowPlaying)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            // 
            attrs = doc.CreateAttribute("UpdateOnStartup");
            if (IsUpdateOnStartup)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            //
            attrs = doc.CreateAttribute("ShowDebugWindow");
            if (IsShowDebugWindow)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            //
            attrs = doc.CreateAttribute("SaveLog");
            if (IsSaveLog)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);


            // 
            root.AppendChild(opts);

            #endregion

            #region == プロファイル設定  ==

            XmlElement xProfiles = doc.CreateElement(string.Empty, "Profiles", string.Empty);

            XmlElement xProfile;
            XmlAttribute xAttrs;

            foreach (var p in Profiles)
            {
                xProfile = doc.CreateElement(string.Empty, "Profile", string.Empty);

                xAttrs = doc.CreateAttribute("Name");
                xAttrs.Value = p.Name;
                xProfile.SetAttributeNode(xAttrs);

                xAttrs = doc.CreateAttribute("Host");
                xAttrs.Value = p.Host;
                xProfile.SetAttributeNode(xAttrs);

                xAttrs = doc.CreateAttribute("Port");
                xAttrs.Value = p.Port.ToString();
                xProfile.SetAttributeNode(xAttrs);

                xAttrs = doc.CreateAttribute("Password");
                xAttrs.Value = Encrypt(p.Password);
                xProfile.SetAttributeNode(xAttrs);

                if (p.IsDefault)
                {
                    xAttrs = doc.CreateAttribute("IsDefault");
                    xAttrs.Value = "True";
                    xProfile.SetAttributeNode(xAttrs);
                }

                xProfiles.AppendChild(xProfile);
            }

            root.AppendChild(xProfiles);

            #endregion

            #region == ヘッダーカラム設定の保存 ==

            XmlElement headers = doc.CreateElement(string.Empty, "Headers", string.Empty);

            XmlElement queueHeader;
            XmlElement queueHeaderColumn;

            XmlAttribute qAttrs;

            queueHeader = doc.CreateElement(string.Empty, "Queue", string.Empty);


            // Position
            queueHeaderColumn = doc.CreateElement(string.Empty, "Position", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderPositionVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderPositionWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderPositionWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Now Playing
            queueHeaderColumn = doc.CreateElement(string.Empty, "NowPlaying", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderNowPlayingVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderNowPlayingWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderNowPlayingWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Title skip visibility
            queueHeaderColumn = doc.CreateElement(string.Empty, "Title", string.Empty);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderTitleWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderTitleWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Time
            queueHeaderColumn = doc.CreateElement(string.Empty, "Time", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderTimeVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderTimeWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderTimeWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Artist
            queueHeaderColumn = doc.CreateElement(string.Empty, "Artist", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderArtistVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderArtistWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderArtistWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Album
            queueHeaderColumn = doc.CreateElement(string.Empty, "Album", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderAlbumVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderAlbumWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderAlbumWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Genre
            queueHeaderColumn = doc.CreateElement(string.Empty, "Genre", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderGenreVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderGenreWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderGenreWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Last Modified
            queueHeaderColumn = doc.CreateElement(string.Empty, "LastModified", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderLastModifiedVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderLastModifiedWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderLastModifiedWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            //
            headers.AppendChild(queueHeader);
            ////
            root.AppendChild(headers);

            #endregion

            #region == レイアウトの保存 (使ってない、というか無効) ==

            XmlElement lay = doc.CreateElement(string.Empty, "Layout", string.Empty);

            XmlElement leftpain;
            XmlAttribute lAttrs;

            // LeftPain
            leftpain = doc.CreateElement(string.Empty, "LeftPain", string.Empty);
            lAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
            {
                if (windowWidth > (MainLeftPainActualWidth - 24))
                {
                    lAttrs.Value = MainLeftPainActualWidth.ToString();
                }
                else
                {
                    lAttrs.Value = "241";
                }
            }
            else
            {
                lAttrs.Value = MainLeftPainWidth.ToString();
            }
            leftpain.SetAttributeNode(lAttrs);

            //
            lay.AppendChild(leftpain);
            ////
            root.AppendChild(lay);

            #endregion

            try
            {
                // 設定ファイルの保存
                doc.Save(_appConfigFilePath);
            }
            //catch (System.IO.FileNotFoundException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("■■■■■ Error  設定ファイルの保存中: " + ex + " while opening : " + _appConfigFilePath);
            }

            #endregion

            App app =  App.Current as App;
            if (app != null)
            {
                if (IsSaveLog)
                    app.SaveErrorLog(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + System.IO.Path.DirectorySeparatorChar + "MPDCtrl_errors.txt");
            }

            try
            {
                if (IsConnected)
                {
                    _mpc.MpdStop = true;
                    _mpc.MpdDisconnect();
                }
            }
            catch { }

        }

        private async void OnMpdConnected(MPC sender)
        {
            Debug.WriteLine("OK MPD " + _mpc.MpdVer + " @OnMpdConnected");

            MpdVersion = _mpc.MpdVer;

            MpdStatusButton = _pathConnectedButton;

            // これしないとCurrentSongが表示されない。
            IsConnected = true;

            CommandResult result = await _mpc.MpdSendPassword(_password);

            if (result.IsSuccess)
                result = await _mpc.MpdQueryStatus();

            if (result.IsSuccess)
            {
                UpdateStatus();

                result = await _mpc.MpdQueryCurrentQueue();
            }

            if (result.IsSuccess)
            {
                UpdateCurrentQueue();

                result = await _mpc.MpdQueryPlaylists();
            }

            if (result.IsSuccess)
            {
                UpdatePlaylists();

                // heavy stuff should be the last.
                result = await _mpc.MpdQueryListAll();
            }

            if (result.IsSuccess)
            {
                UpdateLocalFiles();
            }

            await _mpc.MpdSendIdle();


            // Start idle connection
            _mpc.MpdIdleConnectionStart(_mpc.MpdHost, _mpc.MpdPort, _mpc.MpdPassword);

        }

        private async void OnMpdPlayerStatusChanged(MPC sender)
        {
            await _mpc.MpdSendNoIdle();
            CommandResult result = await _mpc.MpdQueryStatus();
            if (result.IsSuccess)
            {
                UpdateStatus();
            }
            await _mpc.MpdSendIdle();
        }

        private async void OnMpdCurrentQueueChanged(MPC sender)
        {
            await _mpc.MpdSendNoIdle();
            CommandResult result = await _mpc.MpdQueryCurrentQueue();
            if (result.IsSuccess)
            {
                UpdateCurrentQueue();
            }
            await _mpc.MpdSendIdle();
        }

        private async void OnMpdPlaylistsChanged(MPC sender)
        {
            await _mpc.MpdSendNoIdle();
            CommandResult result = await _mpc.MpdQueryPlaylists();
            if (result.IsSuccess)
            {
                UpdateCurrentQueue();
            }
            await _mpc.MpdSendIdle();
        }


        // MPD changed nortifiation
        private void OnStatusChanged(MPC sender, object data)
        {
            //System.Diagnostics.Debug.WriteLine("OnStatusChanged " + data);

            UpdateButtonStatus();

            List<string> SubSystems = (data as string).Split('\n').ToList();

            foreach (string line in SubSystems)
            {
                if (line == "changed: playlist")
                {

                }
                else if (line == "changed: player")
                {

                }
                else if (line == "changed: options")
                {

                }
                else if (line == "changed: mixer")
                {

                }
                else if (line == "changed: stored_playlist")
                {

                }
            }

        }

        // MPD updated information
        private void OnMpdStatusUpdate(MPC sender, object data)
        {
            if ((data as string) == "isPlayer")
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateButtonStatus();

                    bool isSongChanged = false;
                    bool isCurrentSongWasNull = false;

                    if (CurrentSong != null)
                    {
                        if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                        {
                            isSongChanged = true;

                            // Clear IsPlaying icon
                            CurrentSong.IsPlaying = false;

                            IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;
                        }
                    }
                    else
                    {
                        isCurrentSongWasNull = true;
                    }

                    // Sets Current Song
                    var item = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (item != null)
                    {
                        CurrentSong = (item as SongInfo);
                        CurrentSong.IsPlaying = true;

                        if (isSongChanged)
                        {
                            if (IsAutoScrollToNowPlaying)
                                ScrollIntoView?.Invoke(this, CurrentSong.Index);

                            // AlbumArt
                            if (!String.IsNullOrEmpty(CurrentSong.file))
                            {
                                //Debug.WriteLine("AlbumArt isPlayer: " + CurrentSong.file);
                                //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                            }
                        }
                        else
                        {
                            if (isCurrentSongWasNull)
                            {
                                if (IsAutoScrollToNowPlaying)
                                    ScrollIntoView?.Invoke(this, CurrentSong.Index);

                                // AlbumArt
                                if (!String.IsNullOrEmpty(CurrentSong.file))
                                {
                                    //Debug.WriteLine("AlbumArt isPlayer: isCurrentSongWasNull");
                                    //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                                }
                            }
                            else
                            {
                                //Debug.WriteLine("AlbumArt isPlayer: !isSongChanged.");
                            }
                        }
                    }
                    else
                    {
                        //Debug.WriteLine("AlbumArt isPlayer: CurrentSong null");

                        CurrentSong = null;

                        IsAlbumArtVisible = false;
                        AlbumArt = _albumArtDefault;
                    }
                });

                /*
                UpdateButtonStatus();

                bool isSongChanged = false;
                if (CurrentSong != null)
                {
                    if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                    {
                        isSongChanged = true;

                        // Clear IsPlaying icon
                        CurrentSong.IsPlaying = false;
                        
                        IsAlbumArtVisible = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AlbumArt = _albumArtDefault;
                        });
                    }
                }

                IsBusy = true;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Sets Current Song
                    var item = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (item != null)
                    {
                        CurrentSong = (item as SongInfo);
                        (item as SongInfo).IsPlaying = true;

                        // IsAutoScrollToNowPlaying
                        //ScrollIntoView?.Invoke(this, CurrentSong.Index);
                    }
                    else
                    {
                        CurrentSong = null;
                    }
                });

                if (isSongChanged && (CurrentSong != null))
                {
                    // AlbumArt
                    if (!String.IsNullOrEmpty(CurrentSong.file))
                    {
                        _mpc.MpdQueryAlbumArt(CurrentSong.file);
                    }
                }

                IsBusy = false;

                */
            }
            else if ((data as string) == "isCurrentQueue")
            {
                IsBusy = true;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Queue.Count > 0)
                    {
                        IsBusy = true;

                        // 削除する曲の一時リスト
                        List<SongInfo> _tmpQueue = new List<SongInfo>();

                        // 既存のリストの中で新しいリストにないものを削除
                        foreach (var sng in Queue)
                        {
                            var queitem = _mpc.CurrentQueue.FirstOrDefault(i => i.Id == sng.Id);
                            if (queitem == null)
                            {
                                // 削除リストに追加
                                _tmpQueue.Add(sng);
                            }
                        }

                        // 削除リストをループ
                        foreach (var hoge in _tmpQueue)
                        {
                            Queue.Remove(hoge);
                        }

                        // 新しいリストの中から既存のリストにないものを追加または更新
                        foreach (var sng in _mpc.CurrentQueue)
                        {
                            var fuga = Queue.FirstOrDefault(i => i.Id == sng.Id);
                            if (fuga != null)
                            {
                                // TODO:
                                fuga.Pos = sng.Pos;
                                //fuga.Id = sng.Id; // 流石にIDは変わらないだろう。
                                fuga.LastModified = sng.LastModified;
                                //fuga.Time = sng.Time; // format exception が煩い。
                                fuga.Title = sng.Title;
                                fuga.Artist = sng.Artist;
                                fuga.Album = sng.Album;
                                fuga.AlbumArtist = sng.AlbumArtist;
                                fuga.Composer = sng.Composer;
                                fuga.Date = sng.Date;
                                fuga.duration = sng.duration;
                                fuga.file = sng.file;
                                fuga.Genre = sng.Genre;
                                fuga.Track = sng.Track;

                                //Queue.Move(fuga.Index, sng.Index);
                                fuga.Index = sng.Index;
                            }
                            else
                            {
                                Queue.Add(sng);
                                //Queue.Insert(sng.Index, sng);
                            }
                        }

                        // Set Current and NowPlaying.
                        var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                        if (curitem != null)
                        {
                            CurrentSong = (curitem as SongInfo);
                            CurrentSong.IsPlaying = true;

                            if (IsAutoScrollToNowPlaying)
                                ScrollIntoView?.Invoke(this, CurrentSong.Index);

                            // AlbumArt
                            if (_mpc.AlbumArt.SongFilePath != curitem.file)
                            {
                                IsAlbumArtVisible = false;
                                AlbumArt = _albumArtDefault;

                                //Debug.WriteLine("AlbumArt isCurrentQueue: " + CurrentSong.file);

                                if (!String.IsNullOrEmpty(CurrentSong.file))
                                {
                                    //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                                }
                            }
                        }
                        else
                        {
                            CurrentSong = null;

                            IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;
                        }

                        // 移動したりするとPosが変更されても順番が反映されないので、
                        var collectionView = CollectionViewSource.GetDefaultView(Queue);
                        collectionView.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                        collectionView.Refresh();

                    }
                    else
                    {
                        IsBusy = true;

                        foreach (var sng in _mpc.CurrentQueue)
                        {
                            Queue.Add(sng);
                        }

                        // Set Current and NowPlaying.
                        var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                        if (curitem != null)
                        {
                            CurrentSong = (curitem as SongInfo);
                            CurrentSong.IsPlaying = true;

                            if (IsAutoScrollToNowPlaying)
                                ScrollIntoView?.Invoke(this, CurrentSong.Index);

                            // AlbumArt
                            if (_mpc.AlbumArt.SongFilePath != curitem.file)
                            {
                                IsAlbumArtVisible = false;
                                AlbumArt = _albumArtDefault;

                                //Debug.WriteLine("AlbumArt isCurrentQueue from Clear: " + CurrentSong.file);
                                if (!String.IsNullOrEmpty(CurrentSong.file))
                                {
                                    //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                                }
                            }
                        }
                        else
                        {
                            // just in case.
                            CurrentSong = null;

                            IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;
                        }
                    }

                });

                IsBusy = false;
            }
            else if((data as string) == "isSongs")
            {
                // Find の結果か playlistinfoの結果
            }
            else if ((data as string) == "isStoredPlaylist")
            {
                // MPCが自動で見に行ってるから特にアクションなし。
            }
            else if ((data as string) == "isLocalFiles")
            {/*
                IsBusy = true;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    LocalFiles.Clear();

                    foreach (var songfile in _mpc.LocalFiles)
                    {
                        try
                        {
                            Uri uri = new Uri(@"file:///" + songfile);
                            if (uri.IsFile)
                            {
                                string filename = System.IO.Path.GetFileName(songfile);//System.IO.Path.GetFileName(uri.LocalPath);
                                NodeFile hoge = new NodeFile(filename, uri, songfile);

                                LocalFiles.Add(hoge);
                            }
                        }
                        catch { }
                    }

                    MusicDirectories.Clear();
                    
                    _musicDirectories.Load(_mpc.LocalDirectories.ToList<String>());

                    if (MusicDirectories.Count > 0)
                    {
                        _selectedNodeDirectory = _musicDirectories.Children[0] as NodeDirectory;
                    }

                    MusicEntries.Clear();

                    foreach (var songfile in _mpc.LocalFiles)
                    {
                        try
                        {
                            Uri uri = new Uri(@"file:///" + songfile);
                            if (uri.IsFile)
                            {
                                string filename = System.IO.Path.GetFileName(songfile);//System.IO.Path.GetFileName(uri.LocalPath);
                                NodeFile hoge = new NodeFile(filename, uri, songfile);

                                MusicEntries.Add(hoge);
                            }

                        }
                        catch { }
                    }
                });

                IsBusy = false;
                */
            }
            else if ((data as string) == "isUpdating_db")
            {
                System.Diagnostics.Debug.WriteLine("OnMpdStatusUpdate: isUpdating_db");
                ConnectionStatusMessage = "Updating db...";

                IsUpdatingMpdDb = true;
            }

        }

        private void OnAlbumArtStatusChanged(MPC sender)
        {

            // AlbumArt
            Application.Current.Dispatcher.Invoke(() =>
            {
                if ((!_mpc.AlbumArt.IsDownloading) && _mpc.AlbumArt.IsSuccess)
                {
                    if ((CurrentSong != null) && (_mpc.AlbumArt.AlbumImageSource != null))
                    {
                        if (!String.IsNullOrEmpty(CurrentSong.file))
                        {
                            if (CurrentSong.file == _mpc.AlbumArt.SongFilePath)
                            {
                                IsAlbumArtVisible = true;
                                AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                                AlbumArt = null;

                                AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                            }
                        }
                    }
                }
            });

        }

        // Raw string data Received
        private void OnDataReceived(MPC sender, object data)
        {
            if (DeveloperMode)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DebugWindowOutput?.Invoke(this, (data as string));
                });
        }

        private void OnDebugOutput(MPC sender, string data)
        {
            if (DeveloperMode)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DebugWindowOutput?.Invoke(this, data);
                });
        }


        // Raw string data Sent
        private void OnDataSent(MPC sender, object data)
        {
            if (DeveloperMode)
            {
                var s = (data as string);
                s = Regex.Replace(s, @"password .*?\n", "password *******\n");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DebugWindowOutput?.Invoke(this, s);
                });
            }
        }
        /*
        private void OnError(MPC sender, MPC.MpdErrorTypes errType, object data)
        {
            if (data == null) { return; }

            if (errType == MPC.MpdErrorTypes.CommandError)
            {
                string s = (data as string);
                string patternStr = @"[{\[].+?[}\]]";//@"[.*?]";
                s = System.Text.RegularExpressions.Regex.Replace(s, patternStr, string.Empty);
                s = s.Replace("ACK ", string.Empty);
                s = s.Replace("{} ", string.Empty);
                s = s.Replace("[] ", string.Empty);

                MpdStatusMessage = MPDCtrl.Properties.Resources.MPD_CommandError + ": " + s;
                MpdStatusButton = _pathErrorMpdAckButton;

                AckWindowOutput?.Invoke(this, MPDCtrl.Properties.Resources.MPD_CommandError + ": " + s);
            }
            else if (errType == MPC.MpdErrorTypes.ConnectionError)
            {
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectionError + ": " + (data as string);
                StatusButton = _pathErrorInfoButton;

                IsConnected = false;
                IsConnectionSettingShow = true;
            }
            else if (errType == MPC.MpdErrorTypes.StatusError)
            {
                MpdStatusMessage = MPDCtrl.Properties.Resources.MPD_StatusError + ": " + (data as string);
                MpdStatusButton = _pathErrorMpdAckButton;

                AckWindowOutput?.Invoke(this, MPDCtrl.Properties.Resources.MPD_StatusError + ": " + (data as string));
            }
            else if (errType == MPC.MpdErrorTypes.ErrorClear)
            {
                MpdStatusMessage = "";
                MpdStatusButton = _pathMpdOkButton;
            }
            else
            {
                // TODO:
                ConnectionStatusMessage = "Unknown error: " + (data as string);
                StatusButton = _pathErrorInfoButton;
            }
        }
        */

        private void OnConnectionError(MPC sender, object data)
        {
            if (data == null) { return; }

            IsConnected = false;
            IsConnectionSettingShow = true;

            ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectionError + ": " + (data as string);
            StatusButton = _pathErrorInfoButton;
        }

        private void OnConnectionStatusChanged(MPC sender, TCPC.ConnectionStatus status)
        {

            if (status == TCPC.ConnectionStatus.Connected)
            {
                IsConnected = true;
                IsConnecting = false;
                IsConnectionSettingShow = false;

                Debug.WriteLine("ConnectionStatus_Connected");
                ConnectionStatusMessage = "";// MPDCtrl.Properties.Resources.ConnectionStatus_Connected;
                StatusButton = _pathConnectedButton;
            }
            else if (status == TCPC.ConnectionStatus.Connecting)
            {
                IsConnected = false;
                IsConnecting = true;

                Debug.WriteLine("ConnectionStatus_Connecting");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Connecting;
                StatusButton = _pathConnectingButton;
            }
            else if (status == TCPC.ConnectionStatus.AutoReconnecting)
            {
                IsConnected = false;
                IsConnecting = true;
                IsConnectionSettingShow = true;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Reconnecting;
                StatusButton = _pathConnectingButton;
            }
            else if (status == TCPC.ConnectionStatus.ConnectFail_Timeout)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectFail_Timeout;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == TCPC.ConnectionStatus.DisconnectedByHost)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_DisconnectedByHost");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_DisconnectedByHost;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == TCPC.ConnectionStatus.DisconnectedByUser)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_DisconnectedByUser;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == TCPC.ConnectionStatus.Error)
            {
                // TODO: OnConnectionErrorと被る。

                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = "Error..";
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == TCPC.ConnectionStatus.SendFail_NotConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_NotConnected;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == TCPC.ConnectionStatus.SendFail_Timeout)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_Timeout;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == TCPC.ConnectionStatus.NeverConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_NeverConnected;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == TCPC.ConnectionStatus.Disconnecting)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_Disconnecting");
                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_NeverConnected;
                //StatusButton = _pathErrorInfoButton;
            }
            

        }

        private void OnMpcIsBusy(MPC sender, bool on)
        {
            this.IsBusy = on;
        }


        #endregion

        #region == コマンド ==

        #region == Playback play ==

        public ICommand PlayCommand { get; }
        public bool PlayCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            //if (IsBusy) return false;
            return true;
        }
        public async void PlayCommand_ExecuteAsync()
        {
            switch (_mpc.MpdStatus.MpdState)
            {
                case Status.MpdPlayState.Play:
                    {
                        //State>>Play: So, send Pause command
                        await _mpc.MpdPlaybackPause();
                        break;
                    }
                case Status.MpdPlayState.Pause:
                    {
                        //State>>Pause: So, send Resume command
                        await _mpc.MpdPlaybackResume();
                        break;
                    }
                case Status.MpdPlayState.Stop:
                    {
                        //State>>Stop: So, send Play command
                        await _mpc.MpdPlaybackPlay();
                        break;
                    }
            }

        }

        public ICommand PlayNextCommand { get; }
        public bool PlayNextCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            //if (IsBusy) return false;
            //if (Queue.Count < 1) { return false; }
            return true;
        }
        public async void PlayNextCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackNext();
        }

        public ICommand PlayPrevCommand { get; }
        public bool PlayPrevCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            //if (IsBusy) return false;
            //if (Queue.Count < 1) { return false; }
            return true;
        }
        public async void PlayPrevCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackPrev();
        }

        public ICommand ChangeSongCommand { get; set; }
        public bool ChangeSongCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            if (Queue.Count < 1) { return false; }
            if (_selectedSong == null) { return false; }
            return true;
        }
        public void ChangeSongCommand_ExecuteAsync()
        {
            //_mpc.MpdPlaybackPlay(_selectedSong.Id);
        }

        public ICommand PlayPauseCommand { get; }
        public bool PlayPauseCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void PlayPauseCommand_Execute()
        {
            //_mpc.MpdPlaybackPause();
        }

        public ICommand PlayStopCommand { get; }
        public bool PlayStopCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void PlayStopCommand_Execute()
        {
            //_mpc.MpdPlaybackStop();
        }

        #endregion

        #region == Playback opts ==

        public ICommand SetRandomCommand { get; }
        public bool SetRandomCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void SetRandomCommand_ExecuteAsync()
        {
            //_mpc.MpdSetRandom(_random);
        }

        public ICommand SetRpeatCommand { get; }
        public bool SetRpeatCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void SetRpeatCommand_ExecuteAsync()
        {
            //_mpc.MpdSetRepeat(_repeat);
        }

        public ICommand SetConsumeCommand { get; }
        public bool SetConsumeCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void SetConsumeCommand_ExecuteAsync()
        {
            //_mpc.MpdSetConsume(_consume);
        }

        public ICommand SetSingleCommand { get; }
        public bool SetSingleCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void SetSingleCommand_ExecuteAsync()
        {
            //_mpc.MpdSetSingle(_single);
        }

        #endregion

        #region == Playback seek and volume ==

        public ICommand SetVolumeCommand { get; }
        public bool SetVolumeCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public async void SetVolumeCommand_ExecuteAsync()
        {
            await _mpc.MpdSetVolume(Convert.ToInt32(_volume));
        }

        public ICommand SetSeekCommand { get; }
        public bool SetSeekCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void SetSeekCommand_ExecuteAsync()
        {
            //_mpc.MpdPlaybackSeek(_mpc.MpdStatus.MpdSongID, Convert.ToInt32(_elapsed));
        }

        public ICommand VolumeMuteCommand { get; }
        public bool VolumeMuteCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void VolumeMuteCommand_Execute()
        {
            //_mpc.MpdSetVolume(0);
        }


        public ICommand VolumeDownCommand { get; }
        public bool VolumeDownCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void VolumeDownCommand_Execute()
        {
            if (_volume >= 10)
            {
                //_mpc.MpdSetVolume(Convert.ToInt32(_volume - 10));
            }
        }

        public ICommand VolumeUpCommand { get; }
        public bool VolumeUpCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void VolumeUpCommand_Execute()
        {
            if (_volume <= 90)
            {
                //_mpc.MpdSetVolume(Convert.ToInt32(_volume + 10));
            }
        }

        #endregion

        #region == Local Files ==

        public ICommand ListAllCommand { get; }
        public bool ListAllCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void ListAllCommand_ExecuteAsync()
        {
            //_mpc.MpdQueryListAll();
        }

        public ICommand LocalfileListviewAddCommand { get; }
        public bool LocalfileListviewAddCommand_CanExecute()
        {
            return true;
        }
        public void LocalfileListviewAddCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 0)
            {
                var collection = items.Cast<NodeFile>();

                List<String> uriList = new List<String>();

                foreach (var item in collection)
                {
                    uriList.Add((item as NodeFile).OriginalFileUri);
                }

                //_mpc.MpdAdd(uriList);
            }
        }

        public ICommand LocalfileListviewEnterKeyCommand { get; set; }
        public bool LocalfileListviewEnterKeyCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void LocalfileListviewEnterKeyCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                var collection = items.Cast<NodeFile>();

                List<string> uriList = new List<string>();

                foreach (var item in collection)
                {
                    uriList.Add((item as NodeFile).OriginalFileUri);
                }

                //_mpc.MpdAdd(uriList);
            }
            else
            {
                //_mpc.MpdAdd((items[0] as NodeFile).OriginalFileUri);
            }
        }

        public ICommand LocalfileListviewLeftDoubleClickCommand { get; set; }
        public bool LocalfileListviewLeftDoubleClickCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void LocalfileListviewLeftDoubleClickCommand_Execute(object obj)
        {
            /*
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                var collection = items.Cast<String>();

                List<string> uriList = new List<string>();

                foreach (var item in collection)
                {
                    uriList.Add(item);
                }

                _mpc.MpdAdd(uriList);
            }
            else
            {
                _mpc.MpdAdd(items[0] as string);
            }
            */
        }

        public ICommand FilterClearCommand { get; }
        public bool FilterClearCommand_CanExecute()
        {
            if (string.IsNullOrEmpty(FilterQuery))
                return false;
            return true;
        }
        public void FilterClearCommand_Execute()
        {
            FilterQuery = "";
        }

        #endregion

        #region == Playlists ==

        public ICommand ChangePlaylistCommand { get; set; }
        public bool ChangePlaylistCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            if (_selecctedPlaylist == "") { return false; }
            return true;
        }
        public void ChangePlaylistCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == "")
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
            });

            //_mpc.MpdChangePlaylist(_selecctedPlaylist);
        }

        public ICommand PlaylistListviewLeftDoubleClickCommand { get; set; }
        public bool PlaylistListviewLeftDoubleClickCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void PlaylistListviewLeftDoubleClickCommand_ExecuteAsync(String playlist)
        {
            //_mpc.MpdLoadPlaylist(playlist);
        }

        public ICommand PlaylistListviewEnterKeyCommand { get; set; }
        public bool PlaylistListviewEnterKeyCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void PlaylistListviewEnterKeyCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == "")
                return;

            //_mpc.MpdLoadPlaylist(_selecctedPlaylist);
        }

        public ICommand PlaylistListviewLoadPlaylistCommand { get; set; }
        public bool PlaylistListviewLoadPlaylistCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void PlaylistListviewLoadPlaylistCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == "")
                return;

            //_mpc.MpdLoadPlaylist(_selecctedPlaylist);
        }

        public ICommand PlaylistListviewClearLoadPlaylistCommand { get; set; }
        public bool PlaylistListviewClearLoadPlaylistCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void PlaylistListviewClearLoadPlaylistCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == "")
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
            });

            //_mpc.MpdChangePlaylist(_selecctedPlaylist);
        }

        public ICommand PlaylistListviewRenamePlaylistCommand { get; set; }
        public bool PlaylistListviewRenamePlaylistCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void PlaylistListviewRenamePlaylistCommand_Execute(String playlist)
        {
            if (_selecctedPlaylist == "")
                return;

            ResetDialog();
            DialogTitle = MPDCtrl.Properties.Resources.Dialog_Input;
            DialogMessage = MPDCtrl.Properties.Resources.Dialog_NewPlaylistName;
            IsInputDialogShow = true;

            DialogResultFunction = null;
            DialogResultFunctionWith2Params = DoRenamePlaylist;
            DialogResultFunctionParamString = _selecctedPlaylist;
            //_mpc.MpdRenamePlaylist(_selecctedPlaylist, "Hogehoge hoge");
        }
        public bool DoRenamePlaylist(String oldPlaylistName, String newPlaylistName)
        {
            if (CheckPlaylistNameExists(newPlaylistName))
            {
                ResetDialog();
                DialogTitle = MPDCtrl.Properties.Resources.Dialog_Information;
                DialogMessage = MPDCtrl.Properties.Resources.Dialog_PlaylistNameAlreadyExists;
                IsInformationDialogShow = true;

                return false;
            }

            //_mpc.MpdRenamePlaylist(oldPlaylistName, newPlaylistName);

            return true;
        }

        public ICommand PlaylistListviewRemovePlaylistCommand { get; set; }
        public bool PlaylistListviewRemovePlaylistCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void PlaylistListviewRemovePlaylistCommand_Execute(String playlist)
        {
            if (_selecctedPlaylist == "")
                return;

            ResetDialog();
            DialogTitle = MPDCtrl.Properties.Resources.Dialog_Comfirmation;
            DialogMessage = MPDCtrl.Properties.Resources.Dialog_RemovePlaylistQ;
            IsComfirmationDialogShow = true;

            DialogResultFunction = DoRemovePlaylist;
            DialogResultFunctionParamString = _selecctedPlaylist;
            //_mpc.MpdRemovePlaylist(_selecctedPlaylist);
        }

        public bool DoRemovePlaylist(String playlist) 
        {
            if (Playlists.Count == 1)
            {
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Playlists.Clear();
                });
            }

            //_mpc.MpdRemovePlaylist(playlist);

            return true;
        }

        #endregion

        #region == Queue ==

        public ICommand QueueListviewSaveCommand { get; }
        public bool QueueListviewSaveCommand_CanExecute()
        {
            if (_mpc == null) return false;
            if (Queue.Count == 0) return false;
            return true;
        }
        public void QueueListviewSaveCommand_ExecuteAsync()
        {
            ResetDialog();
            DialogTitle = MPDCtrl.Properties.Resources.Dialog_Input;
            DialogMessage = MPDCtrl.Properties.Resources.Dialog_NewPlaylistName;
            IsInputDialogShow = true;

            DialogResultFunction = DoSave;
            DialogResultFunctionParamString = "";
            //_mpc.MpdSave("New Playlist");
        }

        public bool DoSave(string newPlaylistName)
        {
            if (CheckPlaylistNameExists(newPlaylistName))
            {
                ResetDialog();
                DialogTitle = MPDCtrl.Properties.Resources.Dialog_Information;
                DialogMessage = MPDCtrl.Properties.Resources.Dialog_PlaylistNameAlreadyExists;
                IsInformationDialogShow = true;

                return false;
            }

            //_mpc.MpdSave(newPlaylistName);
            return true;
        }

        public ICommand QueueListviewEnterKeyCommand { get; set; }
        public bool QueueListviewEnterKeyCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            if (Queue.Count < 1) { return false; }
            if (_selectedSong == null) { return false; }
            return true;
        }
        public void QueueListviewEnterKeyCommand_ExecuteAsync()
        {
            //_mpc.MpdPlaybackPlay(_selectedSong.Id);
        }

        public ICommand QueueListviewLeftDoubleClickCommand { get; set; }
        public bool QueueListviewLeftDoubleClickCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            if (Queue.Count < 1) { return false; }
            if (_selectedSong == null) { return false; }
            return true;
        }
        public void QueueListviewLeftDoubleClickCommand_ExecuteAsync(SongInfo song)
        {
            //_mpc.MpdPlaybackPlay(song.Id);
        }

        public ICommand QueueListviewClearCommand { get; }
        public bool QueueListviewClearCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            if (Queue.Count == 0) { return false; }
            return true;
        }
        public void QueueListviewClearCommand_ExecuteAsync()
        {
            // Clear queue here because "playlistinfo" ASYNC request returns nothing.
            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
            });

            //_mpc.MpdClear();
        }

        public ICommand QueueListviewDeleteCommand { get; }
        public bool QueueListviewDeleteCommand_CanExecute()
        {
            return true;
        }
        public void QueueListviewDeleteCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<SongInfo> selectedList = new List<SongInfo>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfo>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfo);
                }
            });

            List<string> deleteIdList = new List<string>();

            foreach (var item in selectedList)
            {
                deleteIdList.Add(item.Id);
            }

            // Clear queue here because "playlistinfo" ASYNC request returns nothing .
            // 非同期で結果が返ってくるので、キューが全部削除されて０になった場合、
            // 結果はOKしか返って来ないので分からない為、
            // ここでクリアする。

            if (deleteIdList.Count == Queue.Count)
            {
                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Queue.Clear();
                });
            }

            //_mpc.MpdDeleteId(deleteIdList);

        }

        public ICommand QueueListviewMoveUpCommand { get; }
        public bool QueueListviewMoveUpCommand_CanExecute()
        {
            return true;
        }
        public void QueueListviewMoveUpCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<SongInfo> selectedList = new List<SongInfo>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfo>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfo);
                }
            });

            Dictionary<string, string> IdToNewPos = new Dictionary<string, string>();

            foreach (var item in selectedList)
            {
                int i = 0;
                try
                {
                    i = Int32.Parse(item.Pos);

                    if (i == 0) return;

                    i -= 1;

                    IdToNewPos.Add(item.Id, i.ToString());
                }
                catch
                {
                    return;
                }
            }

            //_mpc.MpdMoveId(IdToNewPos);
        }

        public ICommand QueueListviewMoveDownCommand { get; }
        public bool QueueListviewMoveDownCommand_CanExecute()
        {
            return true;
        }
        public void QueueListviewMoveDownCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<SongInfo> selectedList = new List<SongInfo>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfo>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfo);
                }
            });

            Dictionary<string, string> IdToNewPos = new Dictionary<string, string>();

            foreach (var item in selectedList)
            {
                int i = 0;
                try
                {
                    i = Int32.Parse(item.Pos);

                    if (i >= Queue.Count-1) return;

                    i += 1;

                    IdToNewPos.Add(item.Id, i.ToString());
                }
                catch
                {
                    return;
                }
            }

            //_mpc.MpdMoveId(IdToNewPos);
        }

        public ICommand QueueListviewPlaylistAddCommand { get; }
        public bool QueueListviewPlaylistAddCommand_CanExecute()
        {
            return true;
        }
        public void QueueListviewPlaylistAddCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<SongInfo> selectedList = new List<SongInfo>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfo>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfo);
                }
            });

            List<string> fileUrisToAddList = new List<string>();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.file))
                    fileUrisToAddList.Add(item.file);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            ResetDialog();
            DialogTitle = MPDCtrl.Properties.Resources.Dialog_PlaylistSelect_SaveSelectedTo;
            DialogMessage = MPDCtrl.Properties.Resources.Dialog_PlaylistSelect_SelectPlaylist;

            DialogResultFunctionWith2ParamsWithObject = DoPlaylistAdd;
            DialogResultFunctionParamObject = fileUrisToAddList;

            PlaylistNamesWithNew.Clear();
            PlaylistNamesWithNew.Add("["+ MPDCtrl.Properties.Resources.Dialog_PlaylistSelect_NewPlaylistName + "]");

            foreach (var s in Playlists)
            {
                PlaylistNamesWithNew.Add(s);
            }

            IsPlaylistSelectDialogShow = true;
            //_mpc.MpdPlaylistAdd(playlistName, fileUrisToAddList);
        }

        public bool DoPlaylistAdd(String playlistName, List<string> fileUrisToAddList)
        {
            //_mpc.MpdPlaylistAdd(playlistName, fileUrisToAddList);

            return true;
        }


        public ICommand ScrollIntoNowPlayingCommand { get; }
        public bool ScrollIntoNowPlayingCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            if (Queue.Count == 0) { return false; }
            if (CurrentSong == null) { return false; }
            return true;
        }
        public void ScrollIntoNowPlayingCommand_Execute()
        {
            if (Queue.Count == 0) return;
            if (CurrentSong == null) return;
            if (Queue.Count < CurrentSong.Index + 1) return;

            // should I?
            //SelectedSong = CurrentSong;

            ScrollIntoView?.Invoke(this, CurrentSong.Index);

        }
        

        #endregion

        #region == Settings ==

        public ICommand ShowSettingsCommand { get; }
        public bool ShowSettingsCommand_CanExecute()
        {
            if (IsConnecting) return false;
            return true;
        }
        public void ShowSettingsCommand_Execute()
        {
            if (IsConnecting) return;

            //ConnectionStatusMessage = "";

            if (IsSettingsShow)
            {
                IsSettingsShow = false;
            }
            else
            {
                IsSettingsShow = true;
            }
        }

        public ICommand SettingsOKCommand { get; }
        public bool SettingsOKCommand_CanExecute()
        {
            return true;
        }
        public void SettingsOKCommand_Execute()
        {
            IsSettingsShow = false;
        }

        public ICommand NewProfileCommand { get; }
        public bool NewProfileCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            return true;
        }
        public void NewProfileCommand_Execute()
        {
            SelectedProfile = null;
        }

        public ICommand DeleteProfileCommand { get; }
        public bool DeleteProfileCommand_CanExecute()
        {
            if (Profiles.Count < 2) return false;
            if (SelectedProfile == null) return false;
            return true;
        }
        public void DeleteProfileCommand_Execute()
        {
            if (SelectedProfile == null) return;
            if (Profiles.Count < 2) return;

            var tmpNama = SelectedProfile.Name;
            var tmpIsDefault = SelectedProfile.IsDefault;

            if (Profiles.Remove(SelectedProfile))
            {
                SettingProfileEditMessage = MPDCtrl.Properties.Resources.Settings_ProfileDeleted + " (" + tmpNama + ")";

                SelectedProfile = Profiles[0];

                if (tmpIsDefault)
                    Profiles[0].IsDefault = tmpIsDefault;
            }
        }

        public ICommand SaveProfileCommand { get; }
        public bool SaveProfileCommand_CanExecute()
        {
            if (SelectedProfile != null) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (_port == 0) return false;
            return true;
        }
        public void SaveProfileCommand_Execute(object obj)
        {
            if (obj == null) return;
            if (SelectedProfile != null) return;
            if (String.IsNullOrEmpty(Host)) return;
            if (_port == 0) return;

            Profile pro = new Profile();
            pro.Host = _host;
            pro.Port = _port;

            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                Password = passwordBox.Password;
            }

            if (SetIsDefault)
            {
                foreach (var p in Profiles)
                {
                    p.IsDefault = false;
                }
                pro.IsDefault = true;
            }
            else
            {
                pro.IsDefault = false;
            }

            pro.Name = Host + ":" + _port.ToString();

            Profiles.Add(pro);

            SelectedProfile = pro;

            SettingProfileEditMessage = MPDCtrl.Properties.Resources.Settings_ProfileSaved;

            if (CurrentProfile == null)
            {
                SetIsDefault = true;
                pro.IsDefault = true;
                CurrentProfile = pro;
            }

        }

        public ICommand UpdateProfileCommand { get; }
        public bool UpdateProfileCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            return true;
        }
        public void UpdateProfileCommand_Execute(object obj)
        {
            if (obj == null) return;
            if (SelectedProfile == null) return;
            if (String.IsNullOrEmpty(Host)) return;
            if (_port == 0) return;

            SelectedProfile.Host = _host;
            SelectedProfile.Port = _port;
            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                SelectedProfile.Password = passwordBox.Password;
                Password = passwordBox.Password;

                if (SelectedProfile == CurrentProfile)
                {
                    //_mpc.MpdPassword = passwordBox.Password;
                }
            }

            if (SetIsDefault)
            {
                foreach (var p in Profiles)
                {
                    p.IsDefault = false;
                }
                SelectedProfile.IsDefault = true;
            }
            else
            {
                if (SelectedProfile.IsDefault)
                {
                    SelectedProfile.IsDefault = false;
                    Profiles[0].IsDefault = true;
                }
                else
                {
                    SelectedProfile.IsDefault = false;
                }
            }

            SelectedProfile.Name = Host + ":" + _port.ToString();

            SettingProfileEditMessage = MPDCtrl.Properties.Resources.Settings_ProfileUpdated;
        }

        public ICommand ChangeConnectionProfileCommand { get; }
        public bool ChangeConnectionProfileCommand_CanExecute()
        {
            //if (SelectedProfile == null) return false;
            if (string.IsNullOrWhiteSpace(Host)) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (IsConnecting) return false;
            return true;
        }
        public async void ChangeConnectionProfileCommand_Execute(object obj)
        {
            if (obj == null) return;
            //if (SelectedProfile == null) return;
            if (String.IsNullOrEmpty(Host)) return;
            if (string.IsNullOrWhiteSpace(Host)) return;
            if (_port == 0) return;
            if (IsConnecting) return;

            if (IsConnected)
            {
                _mpc.MpdStop = true;
                //_mpc.MpdDisconnect();
            }

            // Validate Host input.
            if (Host == "")
            {
                SetError("Host", "Error: Host must be epecified."); //TODO: translate
                NotifyPropertyChanged("Host");
                return;
            }
            else
            {
                if (Host == "localhost")
                {
                    Host = "127.0.0.1";
                }

                IPAddress ipAddress = null;
                try
                {
                    ipAddress = IPAddress.Parse(Host);
                    if (ipAddress != null)
                    {
                        ClearErrror("Host");
                    }
                }
                catch
                {
                    //System.FormatException
                    SetError("Host", "Error: Invalid address format."); //TODO: translate

                    return;
                }
            }

            if (_port == 0)
            {
                SetError("Port", "Error: Port must be epecified."); //TODO: translate.
                return;
            }

            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                Password = passwordBox.Password;
            }

            // Clear current...
            if (CurrentSong != null)
            {
                CurrentSong.IsPlaying = false;
                CurrentSong = null;
            }
            if (CurrentSong != null)
            {
                SelectedSong = null;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
                //LocalFiles.Clear();
                Playlists.Clear();
            });

            //
            //_mpc.MpdHost = _host;
            //_mpc.MpdPort = _port;
            //_mpc.MpdPassword = _password;

            _mpc.MpdStop = false;


            IsConnecting = true; 
            /*
            ConnectionResult r = await StartConnection(_host, _port, _password);

            if (r.IsSuccess)
            {
                IsConnectionSettingShow = false;
                IsSettingsShow = false;

                if (CurrentProfile == null)
                {
                    //
                    Profile prof = new Profile();
                    prof.Name = _host + ":" + _port.ToString();
                    prof.Host = _host;
                    prof.Port = _port;
                    prof.Password = _password;
                    prof.IsDefault = true;

                    CurrentProfile = prof;
                    SelectedProfile = prof;

                    Profiles.Add(prof);

                    // 初回だからUpdateしておく。
                    //_mpc.MpdSendUpdate();
                }
                else
                {
                    SelectedProfile.Host = _host;
                    SelectedProfile.Port = _port;
                    SelectedProfile.Password = _password;

                    if (SetIsDefault)
                    {
                        foreach (var p in Profiles)
                        {
                            p.IsDefault = false;
                        }
                        SelectedProfile.IsDefault = true;
                    }
                    else
                    {
                        SelectedProfile.IsDefault = false;
                    }

                    SelectedProfile.Name = Host + ":" + _port.ToString();

                    CurrentProfile = SelectedProfile;

                }


            }
            else
            {
                IsConnecting = false;
            }
            */
        }

        #endregion

        #region == Dialogs ==

        public ICommand ComfirmationDialogOKCommand { get; }
        public bool ComfirmationDialogOKCommand_CanExecute()
        {
            return true;
        }
        public void ComfirmationDialogOKCommand_Execute()
        {
            DialogResultFunction(DialogResultFunctionParamString);

            IsComfirmationDialogShow = false;
        }

        public ICommand ComfirmationDialogCancelCommand { get; }
        public bool ComfirmationDialogCancelCommand_CanExecute()
        {
            return true;
        }
        public void ComfirmationDialogCancelCommand_Execute()
        {
            IsComfirmationDialogShow = false;
        }

        public ICommand InformationDialogOKCommand { get; }
        public bool InformationDialogOKCommand_CanExecute()
        {
            return true;
        }
        public void InformationDialogOKCommand_Execute()
        {
            IsInformationDialogShow = false;
        }

        public ICommand InputDialogOKCommand { get; }
        public bool InputDialogOKCommand_CanExecute()
        {
            return true;
        }
        public void InputDialogOKCommand_Execute()
        {
            if (DialogResultFunctionParamString != "")
            {
                DialogResultFunctionWith2Params(DialogResultFunctionParamString, DialogInputText);
            }
            else
            {
                DialogResultFunction(DialogInputText);
            }

            IsInputDialogShow = false;
        }

        public ICommand InputDialogCancelCommand { get; }
        public bool InputDialogCancelCommand_CanExecute()
        {
            return true;
        }
        public void InputDialogCancelCommand_Execute()
        {
            IsInputDialogShow = false;
        }

        public ICommand ShowChangePasswordDialogCommand { get; }
        public bool ShowChangePasswordDialogCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (_port == 0) return false;
            return true;
        }
        public void ShowChangePasswordDialogCommand_Execute(object obj)
        {
            if (IsChangePasswordDialogShow)
            {
                IsChangePasswordDialogShow = false;
            }
            else
            {
                if (obj == null) return;
                // for Unbindable PasswordBox.
                var passwordBox = obj as PasswordBox;
                passwordBox.Password = "";

                ResetDialog();
                IsChangePasswordDialogShow = true;
            }
        }

        public ICommand ChangePasswordDialogOKCommand { get; }
        public bool ChangePasswordDialogOKCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (_port == 0) return false;
            return true;
        }
        public void ChangePasswordDialogOKCommand_Execute(object obj)
        {
            if (obj == null) return;

            // MultipleCommandParameterConverter で複数のパラメータを可能にしている。
            var values = (object[])obj;

            if ((values[0] is PasswordBox) && (values[1] is PasswordBox))
            {
                if ((values[0] as PasswordBox).Password == _password)
                {
                    SelectedProfile.Password = (values[1] as PasswordBox).Password; //allow empty string.

                    Password = SelectedProfile.Password;
                    NotifyPropertyChanged("IsPasswordSet");
                    NotifyPropertyChanged("IsNotPasswordSet");

                    (values[0] as PasswordBox).Password = "";
                    (values[1] as PasswordBox).Password = "";

                    if (SelectedProfile == CurrentProfile)
                    {
                        //_mpc.MpdPassword = SelectedProfile.Password;
                    }

                    SettingProfileEditMessage = MPDCtrl.Properties.Resources.ChangePasswordDialog_PasswordUpdated;

                }
                else
                {
                    DialogMessage = MPDCtrl.Properties.Resources.ChangePasswordDialog_CurrentPasswordMismatch;
                    return;
                }

                IsChangePasswordDialogShow = false;
            }
        }

        public ICommand ChangePasswordDialogCancelCommand { get; }
        public bool ChangePasswordDialogCancelCommand_CanExecute()
        {
            return true;
        }
        public void ChangePasswordDialogCancelCommand_Execute()
        {
            IsChangePasswordDialogShow = false;
        }

        public ICommand PlaylistSelectDialogOKCommand { get; }
        public bool PlaylistSelectDialogOKCommand_CanExecute()
        {
            return true;
        }
        public void PlaylistSelectDialogOKCommand_Execute(string playlistname)
        {
            if (string.IsNullOrEmpty(playlistname)) return;

            // PlaylistAdd
            DialogResultFunctionWith2ParamsWithObject(playlistname, DialogResultFunctionParamObject);

            IsPlaylistSelectDialogShow = false;
        }

        public ICommand PlaylistSelectDialogCancelCommand { get; }
        public bool PlaylistSelectDialogCancelCommand_CanExecute()
        {
            return true;
        }
        public void PlaylistSelectDialogCancelCommand_Execute()
        {
            IsPlaylistSelectDialogShow = false;
        }

        public ICommand ShowFindCommand { get; }
        public bool ShowFindCommand_CanExecute()
        {
            if (IsConnecting) return false;
            if (!IsConnected) return false;
            return true;
        }
        public void ShowFindCommand_Execute()
        {
            if (IsConnecting) return;
            if (!IsConnected) return;

            if (IsSettingsShow) return;
            if (IsComfirmationDialogShow) return;
            if (IsInformationDialogShow) return;
            if (IsInputDialogShow) return;
            if (IsChangePasswordDialogShow) return;
            if (IsPlaylistSelectDialogShow) return;


            if (IsFindDialogShow)
            {
                IsFindDialogShow = false;
            }
            else
            {
                IsFindDialogShow = true;
            }
        }

        public ICommand ShowFindCancelCommand { get; }
        public bool ShowFindCancelCommand_CanExecute()
        {
            return true;
        }
        public void ShowFindCancelCommand_Execute()
        {
            IsFindDialogShow = false;
        }

        public ICommand EscapeCommand { get; }
        public bool EscapeCommand_CanExecute()
        {
            return true;
        }
        public void EscapeCommand_ExecuteAsync()
        {
            // 検索画面の上に他のダイアログが被る事があるので、Esc一発で全部閉じないように。
            if (IsFindDialogShow)
            {
                if ((!IsComfirmationDialogShow) && (!IsInformationDialogShow) && (!IsInputDialogShow) && (!IsPlaylistSelectDialogShow))
                {
                    IsFindDialogShow = false;
                }
            }

            IsSettingsShow = false;
            IsComfirmationDialogShow = false;
            IsInformationDialogShow = false;
            IsInputDialogShow = false;
            IsChangePasswordDialogShow = false;
            IsPlaylistSelectDialogShow = false;


        }

        #endregion

        #region == QueueカラムヘッダーShow Hide ==

        public ICommand QueueColumnHeaderPositionShowHideCommand { get; }
        public bool QueueColumnHeaderPositionShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderPositionShowHideCommand_Execute()
        {
            if (QueueColumnHeaderPositionVisibility)
            {
                QueueColumnHeaderPositionVisibility = false;
                QueueColumnHeaderPositionWidth = 0;
            }
            else
            {
                QueueColumnHeaderPositionVisibility = true;
                QueueColumnHeaderPositionWidth = QueueColumnHeaderPositionWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderNowPlayingShowHideCommand { get; }
        public bool QueueColumnHeaderNowPlayingShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderNowPlayingShowHideCommand_Execute()
        {
            if (QueueColumnHeaderNowPlayingVisibility)
            {
                QueueColumnHeaderNowPlayingVisibility = false;
                QueueColumnHeaderNowPlayingWidth = 0;
            }
            else
            {
                QueueColumnHeaderNowPlayingVisibility = true;
                QueueColumnHeaderNowPlayingWidth = QueueColumnHeaderNowPlayingWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderTimeShowHideCommand { get; }
        public bool QueueColumnHeaderTimeShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderTimeShowHideCommand_Execute()
        {
            if (QueueColumnHeaderTimeVisibility)
            {

                QueueColumnHeaderTimeVisibility = false;
                QueueColumnHeaderTimeWidth = 0;
            }
            else
            {
                QueueColumnHeaderTimeVisibility = true;
                QueueColumnHeaderTimeWidth = QueueColumnHeaderTimeWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderArtistShowHideCommand { get; }
        public bool QueueColumnHeaderArtistShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderArtistShowHideCommand_Execute()
        {
            if (QueueColumnHeaderArtistVisibility)
            {
                QueueColumnHeaderArtistVisibility = false;
                QueueColumnHeaderArtistWidth = 0;
            }
            else
            {
                QueueColumnHeaderArtistVisibility = true;
                QueueColumnHeaderArtistWidth = QueueColumnHeaderArtistWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderAlbumShowHideCommand { get; }
        public bool QueueColumnHeaderAlbumShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderAlbumShowHideCommand_Execute()
        {
            if (QueueColumnHeaderAlbumVisibility)
            {
                QueueColumnHeaderAlbumVisibility = false;
                QueueColumnHeaderAlbumWidth = 0;
            }
            else
            {
                QueueColumnHeaderAlbumVisibility = true;
                QueueColumnHeaderAlbumWidth = QueueColumnHeaderAlbumWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderGenreShowHideCommand { get; }
        public bool QueueColumnHeaderGenreShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderGenreShowHideCommand_Execute()
        {
            if (QueueColumnHeaderGenreVisibility)
            {
                QueueColumnHeaderGenreVisibility = false;
                QueueColumnHeaderGenreWidth = 0;
            }
            else
            {
                QueueColumnHeaderGenreVisibility = true;
                QueueColumnHeaderGenreWidth = QueueColumnHeaderGenreWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderLastModifiedShowHideCommand { get; }
        public bool QueueColumnHeaderLastModifiedShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderLastModifiedShowHideCommand_Execute()
        {
            if (QueueColumnHeaderLastModifiedVisibility)
            {
                QueueColumnHeaderLastModifiedVisibility = false;
                QueueColumnHeaderLastModifiedWidth = 0;
            }
            else
            {
                QueueColumnHeaderLastModifiedVisibility = true;
                QueueColumnHeaderLastModifiedWidth = QueueColumnHeaderLastModifiedWidthRestore;
            }
        }


        #endregion

        #region == 検索画面 ==

        public ICommand SearchExecCommand { get; }
        public bool SearchExecCommand_CanExecute()
        {
            if (string.IsNullOrEmpty(SearchQuery)) 
                return false;
            return true;
        }
        public void SearchExecCommand_Execute()
        {
            if (string.IsNullOrEmpty(SearchQuery)) return;
            string queryShiki = "contains";//"==";

            //_mpc.MpdSearch(SelectedSearchTags.ToString(), queryShiki, SearchQuery);

        }

        public ICommand SongFilesListviewAddCommand { get; }
        public bool SongFilesListviewAddCommand_CanExecute()
        {
            return true;
        }
        public void SongFilesListviewAddCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 0)
            {
                var collection = items.Cast<NodeFile>();

                List<String> uriList = new List<String>();

                foreach (var item in collection)
                {
                    uriList.Add((item as NodeFile).OriginalFileUri);
                }

                //_mpc.MpdAdd(uriList);
            }
        }

        public ICommand SongsListviewAddCommand { get; }
        public bool SongsListviewAddCommand_CanExecute()
        {
            return true;
        }
        public void SongsListviewAddCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 0)
            {
                var collection = items.Cast<Song>();

                List<String> uriList = new List<String>();

                foreach (var item in collection)
                {
                    uriList.Add((item as Song).file);
                }

                //_mpc.MpdAdd(uriList);
            }
        }

        #endregion

        #region == DebugWindow and AckWindow ==

        public ICommand ClearDebugTextCommand { get; }
        public bool ClearDebugTextCommand_CanExecute()
        {
            return true;
        }
        public void ClearDebugTextCommand_Execute()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugWindowClear?.Invoke();
            });
        }

        public ICommand ShowDebugWindowCommand { get; }
        public bool ShowDebugWindowCommand_CanExecute()
        {
            return true;
        }
        public void ShowDebugWindowCommand_Execute()
        {
            /*
            if (IsShowDebugWindow)
                IsShowDebugWindow = false;
            else
                IsShowDebugWindow = true;
            */
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugWindowShowHide?.Invoke();
            });
        }

        public ICommand ClearAckTextCommand { get; }
        public bool ClearAckTextCommand_CanExecute()
        {
            return true;
        }
        public void ClearAckTextCommand_Execute()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AckWindowClear?.Invoke();
            });
        }

        public ICommand ShowAckWindowCommand { get; }
        public bool ShowAckWindowCommand_CanExecute()
        {
            return true;
        }
        public void ShowAckWindowCommand_Execute()
        {
            /*
            if (IsShowAckWindow)
                IsShowAckWindow = false;
            else
                IsShowAckWindow = true;
            */
            Application.Current.Dispatcher.Invoke(() =>
            {
                AckWindowShowHide?.Invoke();
            });
        }

        #endregion

        #endregion
    }

}