﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Linq;
using System.Text;
using System.IO;

namespace MPDCtrl
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 二重起動防止 on/offフラグ
        private bool _mutexOn = true;

        /// <summary>The event mutex name.</summary>
        private const string UniqueEventName = "{ff3032a5-315d-40f5-a729-43b8a310f09a}";

        /// <summary>The unique mutex name.</summary>
        private const string UniqueMutexName = "{fcf95901-d7f3-42da-bcbe-87a3dbd13b8b}";

        /// <summary>The event wait handle.</summary>
        private EventWaitHandle eventWaitHandle;

        /// <summary>The mutex.</summary>
        private Mutex mutex;

        /// <summary> Check and bring to front if already exists.</summary>
        private void AppOnStartup(object sender, StartupEventArgs e)
        {
            // テスト用
            //ChangeTheme("DefaultTheme");
            //ChangeTheme("LightTheme");

            if (_mutexOn)
            {
                this.mutex = new Mutex(true, UniqueMutexName, out bool isOwned);
                this.eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

                // So, R# would not give a warning that this variable is not used.
                GC.KeepAlive(this.mutex);

                if (isOwned)
                {
                    // Spawn a thread which will be waiting for our event
                    var thread = new Thread(
                        () =>
                        {
                            while (this.eventWaitHandle.WaitOne())
                            {
                                Current.Dispatcher.BeginInvoke(
                                    (Action)(() => ((MainWindow)Current.MainWindow).BringToForeground()));
                            }
                        });

                    // It is important mark it as background otherwise it will prevent app from exiting.
                    thread.IsBackground = true;

                    thread.Start();
                    return;
                }

                // Notify other instance so it could bring itself to foreground.
                this.eventWaitHandle.Set();

                // Terminate this instance.
                this.Shutdown();

            }


        }

        // テーマ切替メソッド
        public void ChangeTheme(string themeName)
        {
            //System.Diagnostics.Debug.WriteLine(themeName);

            ResourceDictionary _themeDict = Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source == new Uri("pack://application:,,,/Themes/DefaultTheme.xaml"));
            if (_themeDict != null)
            {
                _themeDict.Clear();
            }
            else
            {
                // 新しいリソース・ディクショナリを追加
                _themeDict = new ResourceDictionary();
                Application.Current.Resources.MergedDictionaries.Add(_themeDict);
            }

            // テーマをリソース・ディクショナリのソースに指定
            string themeUri = String.Format("pack://application:,,,/Themes/{0}.xaml", themeName);
            _themeDict.Source = new Uri(themeUri);

        }
    }
}
