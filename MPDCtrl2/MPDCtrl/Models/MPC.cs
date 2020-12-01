﻿/// 
/// 
/// MPD Ctrl
/// https://github.com/torum/MPDCtrl
/// 
/// TODO:
///  MPD password test.
///
/// Known issue:
/// 
/// Mopidy related:
///  Mopidy does not accept command_list_begin + password
///   https://github.com/mopidy/mopidy/issues/1661
///    command_list_begin
///    password hoge
///    status
///    command_list_end
///    
///  Mopidy has some issue with M3U and UTF-8, Ext M3Us.
///    https://github.com/mopidy/mopidy/issues/1370
///    


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace MPDCtrl
{
    public class ConnectionResult
    {
        public bool isSuccess;
        public string errorMessage;
    }

    public class MPC
    {
        /// <summary>
        /// Song Class for ObservableCollection. 
        /// </summary>
        public class Song
        {
            public string ID { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }

            public bool IsPlaying { get; set; }
        }

        /// <summary>
        /// Status Class. It holds current MPD "status" information.
        /// </summary>
        public class Status
        {
            public enum MpdPlayState
            {
                Play, Pause, Stop
            };

            private MpdPlayState _ps;
            private int _volume;
            private bool _repeat;
            private bool _random;
            private string _songID = "";
            private double _songTime = 0;
            private double _songElapsed = 0;

            public MpdPlayState MpdState
            {
                get { return _ps; }
                set { _ps = value; }
            }

            public int MpdVolume
            {
                get { return _volume; }
                set
                {
                    _volume = value;
                }
            }

            public bool MpdRepeat
            {
                get { return _repeat; }
                set
                {
                    _repeat = value;
                }
            }

            public bool MpdRandom
            {
                get { return _random; }
                set
                {
                    _random = value;
                }
            }

            public string MpdSongID
            {
                get { return _songID; }
                set
                {
                    _songID = value;
                }
            }

            public double MpdSongTime
            {
                get { return _songTime; }
                set
                {
                    _songTime = value;
                }
            }

            public double MpdSongElapsed
            {
                get { return _songElapsed; }
                set
                {
                    _songElapsed = value;
                }
            }

            public Status()
            {
                //constructor
            }
        }

        /// <summary>
        /// Main MPC (MPD Client) Class. 
        /// </summary>
        #region MPC PRIVATE FIELD declaration

        private bool _isBusy;
        private string _h;
        private int _p;
        private string _a;
        private Status _st;
        private Song _currentSong;
        private ObservableCollection<Song> _songs = new ObservableCollection<Song>();
        private ObservableCollection<String> _playLists = new ObservableCollection<String>();
        private AsynchronousTCPClient _asyncClient;

        #endregion END of MPC PRIVATE FIELD declaration

        #region MPC PUBLIC PROPERTY and EVENT FIELD

        public string MpdHost
        {
            get { return _h; }
            set
            {
                _h = value;
            }
        }

        public int MpdPort
        {
            get { return _p; }
            set
            {
                _p = value;
            }
        }

        public Status MpdStatus
        {
            get { return _st; }
        }

        public Song MpdCurrentSong
        {
            // The Song object is currectly set only if
            // Playlist is received and song id is matched.
            get
            {
                return _currentSong;
            }
            set
            {
                _currentSong = value;
            }
        }

        public ObservableCollection<Song> CurrentQueue
        {
            get { return this._songs; }
        }

        public ObservableCollection<String> Playlists
        {
            get { return this._playLists; }
        }

        public delegate void MpdStatusChanged(MPC sender, object data);
        public event MpdStatusChanged StatusChanged;

        public delegate void MpdError(MPC sender, object data);
        public event MpdError ErrorReturned;

        public delegate void MpdConnected(MPC sender);
        public event MpdConnected Connected;

        public delegate void MpdConnectionError(MPC sender, object data);
        public event MpdConnectionError ErrorConnected;

        public delegate void MpdStatusUpdate(MPC sender, object data);
        public event MpdStatusUpdate StatusUpdate;

        public delegate void MpdDataReceived(MPC sender, object data);
        public event MpdDataReceived DataReceived;

        public delegate void MpdDataSent(MPC sender, object data);
        public event MpdDataSent DataSent;

        public delegate void MpdConnectionStatusChanged(MPC sender, AsynchronousTCPClient.ConnectionStatus status);
        public event MpdConnectionStatusChanged ConnectionStatusChanged;

        public bool MpdStop { get; set; }

        #endregion END of MPC PUBLIC PROPERTY and EVENT FIELD

        public MPC(string h, int p, string a)
        {
            _h = h;
            _p = p;
            _a = a;
            _st = new Status();

            _asyncClient = new AsynchronousTCPClient();
            _asyncClient.DataReceived += new AsynchronousTCPClient.delDataReceived(AsynchronousClient_DataReceived);
            _asyncClient.DataSent += new AsynchronousTCPClient.delDataSent(AsynchronousClient_DataSent);
            _asyncClient.ConnectionStatusChanged += new AsynchronousTCPClient.delConnectionStatusChanged(AsynchronousClient_ConnectionStatusChanged);
        }

        #region MPC METHODS

        public async Task<ConnectionResult> MpdConnect()
        {
            ConnectionResult isDone = await _asyncClient.Connect(IPAddress.Parse(this._h), this._p, this._a);

            if (!isDone.isSuccess)
            {
                ErrorConnected?.Invoke(this, isDone.errorMessage);
            }
            return isDone;
        }

        public void MpdDisconnect()
        {
            _asyncClient.DisConnect();
        }

        public void MpdSendPassword()
        {
            try
            {
                if (!string.IsNullOrEmpty(_a))
                {
                    string mpdCommand = "password " + _a.Trim() + "\n";
                    _asyncClient.Send(mpdCommand);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdSendPassword(): " + ex.Message);
            }
        }

        public void MpdQueryStatus()
        {
            try
            {
                string mpdCommand = "status" + "\n";

                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    mpdCommand = mpdCommand + "status" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryStatus(): " + ex.Message);
            }
        }

        private bool ParseStatusResponse(List<string> sl)
        {
            if (this.MpdStop) { return false; }
            if (sl == null)
            {
                System.Diagnostics.Debug.WriteLine("ParseStatusResponse sl==null");

                // TODO:
                return false;
            }
            if (sl.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("ParseStatusResponse slCount == 0");
                return false;
            }

            Dictionary<string, string> MpdStatusValues = new Dictionary<string, string>();

            try
            {
                foreach (string value in sl)
                {
                    //System.Diagnostics.Debug.WriteLine("ParseStatusResponse loop: " + value);

                    if (value.StartsWith("ACK"))
                    {
                        System.Diagnostics.Debug.WriteLine("ACK@ParseStatusResponse: " + value);
                        ErrorReturned?.Invoke(this, "MPD Error (@C1): " + value);
                        return false;
                    }

                    string[] StatusValuePair = value.Split(':');
                    if (StatusValuePair.Length > 1)
                    {
                        if (MpdStatusValues.ContainsKey(StatusValuePair[0].Trim()))
                        {
                            if (StatusValuePair.Length == 2)
                            {
                                MpdStatusValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim();
                            }
                            else if (StatusValuePair.Length == 3)
                            {
                                MpdStatusValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim() + ':' + StatusValuePair[2].Trim();
                            }

                        }
                        else
                        {
                            if (StatusValuePair.Length == 2)
                            {
                                MpdStatusValues.Add(StatusValuePair[0].Trim(), StatusValuePair[1].Trim());
                            }
                            else if (StatusValuePair.Length == 3)
                            {
                                MpdStatusValues.Add(StatusValuePair[0].Trim(), (StatusValuePair[1].Trim() + ":" + StatusValuePair[2].Trim()));
                            }
                        }
                    }
                }

                // Play state
                if (MpdStatusValues.ContainsKey("state"))
                {
                    switch (MpdStatusValues["state"])
                    {
                        case "play":
                            {
                                _st.MpdState = Status.MpdPlayState.Play;
                                break;
                            }
                        case "pause":
                            {
                                _st.MpdState = Status.MpdPlayState.Pause;
                                break;
                            }
                        case "stop":
                            {
                                _st.MpdState = Status.MpdPlayState.Stop;
                                break;
                            }
                    }
                }

                // Volume
                if (MpdStatusValues.ContainsKey("volume"))
                {
                    try
                    {
                        _st.MpdVolume = Int32.Parse(MpdStatusValues["volume"]);
                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // songID
                _st.MpdSongID = "";
                if (MpdStatusValues.ContainsKey("songid"))
                {
                    _st.MpdSongID = MpdStatusValues["songid"];
                    //System.Diagnostics.Debug.WriteLine("StatusResponse songid:"+ _st.MpdSongID);
                    /*
                    if (_st.MpdSongID != "") {
                        // Not good when multithreading.

                        // Set currentSong.
                        try
                        {
                            lock (_objLock)
                            {
                                var item = _songs.FirstOrDefault(i => i.ID == _st.MpdSongID);
                                if (item != null)
                                {
                                    this._currentSong = (item as Song);
                                }
                            }

                            //var listItem = _songs.Where(i => i.ID == _st.MpdSongID);
                            //if (listItem != null)
                            //{
                            //    foreach (var item in listItem)
                            //    {
                            //        this._currentSong = item as Song;
                            //        break;
                            //        //System.Diagnostics.Debug.WriteLine("StatusResponse linq: _songs.Where?="+ _currentSong.Title);
                            //    }
                            //}

                        }
                        catch (Exception ex)
                        {
                            // System.NullReferenceException
                            System.Diagnostics.Debug.WriteLine("Error@StatusResponse linq: _songs.Where?: " + ex.Message);
                        }
                    }
                    */
                }

                // Repeat opt bool.
                if (MpdStatusValues.ContainsKey("repeat"))
                {
                    try
                    {
                        if (MpdStatusValues["repeat"] == "1")
                        {
                            _st.MpdRepeat = true;
                        }
                        else
                        {
                            _st.MpdRepeat = false;
                        }

                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // Random opt bool.
                if (MpdStatusValues.ContainsKey("random"))
                {
                    try
                    {
                        if (Int32.Parse(MpdStatusValues["random"]) > 0)
                        {
                            _st.MpdRandom = true;
                        }
                        else
                        {
                            _st.MpdRandom = false;
                        }

                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // Song time.
                if (MpdStatusValues.ContainsKey("time"))
                {
                    //System.Diagnostics.Debug.WriteLine(MpdStatusValues["time"]);
                    try
                    {
                        if (MpdStatusValues["time"].Split(':').Length > 1)
                        {
                            _st.MpdSongTime = Double.Parse(MpdStatusValues["time"].Split(':')[1].Trim());
                            _st.MpdSongElapsed = Double.Parse(MpdStatusValues["time"].Split(':')[0].Trim());
                        }
                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // Song time elapsed.
                if (MpdStatusValues.ContainsKey("elapsed"))
                {
                    try
                    {
                        _st.MpdSongElapsed = Double.Parse(MpdStatusValues["elapsed"]);
                    }
                    catch { }
                }

                // Song duration.
                if (MpdStatusValues.ContainsKey("duration"))
                {
                    try
                    {
                        _st.MpdSongTime = Double.Parse(MpdStatusValues["duration"]);
                    }
                    catch { }
                }

                //TODO: more?
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParseStatusResponse:" + ex.Message);
            }

            return true;
        }

        public void MpdQueryCurrentPlaylist()
        {
            try
            {
                string mpdCommand = "playlistinfo" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";

                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";

                    mpdCommand = mpdCommand + "playlistinfo" + "\n";

                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryCurrentPlaylist(): " + ex.Message);
            }
        }

        private async Task<bool> ParsePlaylistInfoResponse(List<string> sl)
        {
            if (this.MpdStop) { return false; }
            if (sl == null)
            {
                System.Diagnostics.Debug.WriteLine("ConError@ParsePlaylistInfoResponse: null");

                // TODO:
                ErrorReturned?.Invoke(this, "Connection Error: (@C2)");
                return false;
            }
            /*
            file: Creedence Clearwater Revival/Chronicle, Vol. 1/11 Who'll Stop the Rain.mp3
            Last-Modified: 2011-08-03T16:08:06Z
            Artist: Creedence Clearwater Revival
            AlbumArtist: Creedence Clearwater Revival
            Title: Who'll Stop the Rain
            Album: Chronicle, Vol. 1
            Track: 11
            Date: 1976
            Genre: Rock
            Composer: John Fogerty
            Time: 149
            duration: 149.149
            Pos: 5
            Id: 22637
            */
            _isBusy = true;
            try
            {
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //songs.Clear();
                });

                Dictionary<string, string> SongValues = new Dictionary<string, string>();
                foreach (string value in sl)
                {
                    //System.Diagnostics.Debug.WriteLine("@ParsePlaylistInfoResponse() loop: " + value);

                    if (value.StartsWith("ACK"))
                    {
                        System.Diagnostics.Debug.WriteLine("ACK@ParsePlaylistInfoResponse: " + value);
                        ErrorReturned?.Invoke(this, "MPD Error (@C2): " + value);
                        return false;
                    }

                    try
                    {
                        string[] StatusValuePair = value.Split(':');
                        if (StatusValuePair.Length > 1)
                        {
                            if (SongValues.ContainsKey(StatusValuePair[0].Trim()))
                            {
                                SongValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim();
                            }
                            else
                            {
                                SongValues.Add(StatusValuePair[0].Trim(), StatusValuePair[1].Trim());
                            }
                        }

                        if (SongValues.ContainsKey("Id"))
                        {
                            Song sng = new Song();
                            sng.ID = SongValues["Id"];

                            if (SongValues.ContainsKey("Title"))
                            {
                                sng.Title = SongValues["Title"];
                            }
                            else
                            {
                                sng.Title = "- no title";
                                if (SongValues.ContainsKey("file"))
                                {
                                    sng.Title = Path.GetFileName(SongValues["file"]);
                                }
                            }

                            if (SongValues.ContainsKey("Artist"))
                            {
                                sng.Artist = SongValues["Artist"];
                            }
                            else
                            {
                                if (SongValues.ContainsKey("AlbumArtist"))
                                {
                                    sng.Artist = SongValues["AlbumArtist"];
                                }
                                else
                                {
                                    sng.Artist = "...";
                                }
                            }

                            if (sng.ID == _st.MpdSongID)
                            {
                                this._currentSong = sng;
                                //System.Diagnostics.Debug.WriteLine(sng.ID + ":" + sng.Title + " - is current.");
                            }

                            SongValues.Clear();

                            try
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    _songs.Add(sng);
                                });

                                // This will significantly slows down the load but gives more ui responsiveness.
                                await Task.Delay(10);
                            }
                            catch (Exception ex)
                            {
                                _isBusy = false;
                                System.Diagnostics.Debug.WriteLine("Error@ParsePlaylistInfoResponse _songs.Add: " + ex.Message);
                                return false;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _isBusy = false;
                        System.Diagnostics.Debug.WriteLine("Error@ParsePlaylistInfoResponse: " + ex.Message);
                        return false;
                    }
                }
                _isBusy = false;
                return true;
            }
            catch (Exception ex)
            {
                _isBusy = false;
                System.Diagnostics.Debug.WriteLine("Error@ParsePlaylistInfoResponse: " + ex.Message);
                return false;
            }
        }

        public void MpdQueryPlaylists()
        {
            try
            {
                string mpdCommand = "listplaylists" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";

                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";

                    mpdCommand = mpdCommand + "listplaylists" + "\n";

                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryPlaylists(): " + ex.Message);
            }
        }

        private bool ParsePlaylistsResponse(List<string> sl)
        {
            if (this.MpdStop) { return false; }
            if (sl == null)
            {
                System.Diagnostics.Debug.WriteLine("Connected response@ParsePlaylistsResponse: null");
                ErrorReturned?.Invoke(this, "Connection Error: (C3)");
                return false;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _playLists.Clear();
            });

            // Tmp list for sorting.
            List<string> slTmp = new List<string>();

            foreach (string value in sl)
            {
                //System.Diagnostics.Debug.WriteLine("@ParsePlaylistsResponse() loop: " + value + "");

                if (value.StartsWith("ACK"))
                {
                    System.Diagnostics.Debug.WriteLine("ACK@ParsePlaylistsResponse: " + value);
                    ErrorReturned?.Invoke(this, "MPD Error (@C3): " + value);
                    return false;
                }

                if (value.StartsWith("playlist:"))
                {
                    if (value.Split(':').Length > 1)
                    {
                        //_playLists.Add(value.Split(':')[1].Trim()); // need sort.
                        slTmp.Add(value.Split(':')[1].Trim());
                    }
                }
                else if (value.StartsWith("Last-Modified: ") || (value.StartsWith("OK")))
                {
                    // Ignoring for now.
                }
            }

            // Sort.
            slTmp.Sort();
            foreach (string v in slTmp)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _playLists.Add(v);
                });
            }

            return true;
        }

        public void MpdPlaybackPlay(string songID = "")
        {
            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    if (songID != "")
                    {
                        mpdCommand = mpdCommand + "playid " + songID + "\n";
                    }
                    else
                    {
                        mpdCommand = mpdCommand + "play" + "\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    if (songID != "")
                    {
                        mpdCommand = "playid " + songID + "\n";
                    }
                    else
                    {
                        mpdCommand = "play" + "\n";
                    }
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPlay: " + ex.Message);
            }
        }

        public void MpdPlaybackSeek(string songID, int seekTime)
        {
            if ((songID == "") || (seekTime == 0)) { return; }

            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    mpdCommand = mpdCommand + "seekid " + songID + " " + seekTime.ToString() + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "seekid " + songID + " " + seekTime.ToString() + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdPlaybackSeek: " + ex.Message);
            }
        }

        public void MpdPlaybackPause()
        {
            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    mpdCommand = mpdCommand + "pause 1" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "pause 1" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPause: " + ex.Message);
            }
        }

        public void MpdPlaybackResume()
        {
            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    mpdCommand = mpdCommand + "pause 0" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "pause 0" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackResume: " + ex.Message);
            }
        }

        public void MpdPlaybackStop()
        {
            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    mpdCommand = mpdCommand + "stop" + "\n";
                    mpdCommand = mpdCommand + "command_list_end";
                }
                else
                {
                    mpdCommand = "stop" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackStop: " + ex.Message);
            }
        }

        public void MpdPlaybackNext()
        {
            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    mpdCommand = mpdCommand + "next" + "\n";
                    mpdCommand = mpdCommand + "command_list_end\n";
                }
                else
                {
                    mpdCommand = "next\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackNext: " + ex.Message);
            }
        }

        public void MpdPlaybackPrev()
        {
            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    mpdCommand = mpdCommand + "previous" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "previous" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPrev: " + ex.Message);
            }
        }

        public void MpdSetVolume(int v)
        {
            if (v == _st.MpdVolume) { return; }

            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    mpdCommand = mpdCommand + "setvol " + v.ToString() + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "setvol " + v.ToString() + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetVol: " + ex.Message);
            }
        }

        public void MpdSetRepeat(bool on)
        {
            if (_st.MpdRepeat == on) { return; }

            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    if (on)
                    {
                        mpdCommand = mpdCommand + "repeat 1" + "\n";
                    }
                    else
                    {
                        mpdCommand = mpdCommand + "repeat 0" + "\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    if (on)
                    {
                        mpdCommand = "repeat 1" + "\n";
                    }
                    else
                    {
                        mpdCommand = "repeat 0" + "\n";
                    }
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetRepeat: " + ex.Message);
            }
        }

        public void MpdSetRandom(bool on)
        {
            if (_st.MpdRandom == on) { return; }

            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    if (on)
                    {
                        mpdCommand = mpdCommand + "random 1" + "\n";
                    }
                    else
                    {
                        mpdCommand = mpdCommand + "random 0" + "\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    if (on)
                    {
                        mpdCommand = "random 1" + "\n";
                    }
                    else
                    {
                        mpdCommand = "random 0" + "\n";
                    }
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetRandom: " + ex.Message);
            }
        }

        public void MpdListAll()
        {
            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                    mpdCommand = mpdCommand + "listall" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "listall" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdMpdListAll: " + ex.Message);
            }
        }

        public void MpdChangePlaylist(string playlistName)
        {
            if (playlistName.Trim() != "")
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }
                mpdCommand = mpdCommand + "clear" + "\n";

                mpdCommand = mpdCommand + "load " + playlistName.Trim() + "\n";

                //mpdCommand = mpdCommand + "play" + "\n";

                mpdCommand = mpdCommand + "command_list_end" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
        }

        private bool ParseVoidResponse(List<string> sl)
        {
            if (this.MpdStop) { return false; }
            if (sl == null)
            {
                System.Diagnostics.Debug.WriteLine("ConError@ParseVoidResponse: null");
                ErrorReturned?.Invoke(this, "Connection Error: (@C)");
                return false;
            }

            try
            {
                foreach (string value in sl)
                {
                    //System.Diagnostics.Debug.WriteLine("@ParseVoidResponse() loop: " + value);

                    if (value.StartsWith("ACK"))
                    {
                        System.Diagnostics.Debug.WriteLine("ACK@ParseVoidResponse: " + value);
                        ErrorReturned?.Invoke(this, "MPD Error (@C): " + value);
                        return false;
                    }

                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParseVoidResponse: " + ex.Message);
                return false;
            }
        }

        private async void AsynchronousClient_ConnectionStatusChanged(AsynchronousTCPClient sender, AsynchronousTCPClient.ConnectionStatus status)
        {
            //System.Diagnostics.Debug.WriteLine("--AsynchronousClient_ConnectionStatusChanged: " + status.ToString());

            await Task.Run(() => { ConnectionStatusChanged?.Invoke(this, status); });

            if (status == AsynchronousTCPClient.ConnectionStatus.Connected)
            {
                //sender.Send("idle player mixer options playlist stored_playlist\n");
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.MpdOK)
            {
                //Connected?.Invoke(this);
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.MpdAck)
            {
                //
            }

        }

        private async void AsynchronousClient_DataSent(AsynchronousTCPClient sender, object data)
        {
            await Task.Run(() => { DataSent?.Invoke(this, (data as string)); });
        }

        private async void AsynchronousClient_DataReceived(AsynchronousTCPClient sender, object data)
        {
            //System.Diagnostics.Debug.WriteLine("AsynchronousClient_DataReceived:\n" + (data as string));
            await Task.Run(() => { DataReceived?.Invoke(this, (data as string)); });

            if ((data as string).StartsWith("OK MPD"))
            {
                // Connected.

                //TODO: Needs to be tested.
                MpdSendPassword();

                //
                await Task.Run(() => { Connected?.Invoke(this); });

            }
            else
            {
                Dispatch((data as string));
            }

        }

        private void Dispatch(string str)
        {
            List<string> reLines = str.Split('\n').ToList();

            string d = "";
            foreach (string value in reLines)
            {
                if (value == "OK")
                {
                    if (d != "")
                    {
                        ParseData(d);
                        d = "";
                    }
                }
                else
                {
                    d = d + value + "\n";
                }
            }
        }

        private async void ParseData(string str)
        {
            if (str.StartsWith("changed:"))
            {
                //System.Diagnostics.Debug.WriteLine("IdleConnection DataReceived Dispa ParseData changed: " + str);

                await Task.Run(() => { StatusChanged?.Invoke(this, str); });

                List<string> SubSystems = str.Split('\n').ToList();

                try
                {
                    bool isPlayer = false;
                    bool isPlaylist = false;
                    bool isStoredPlaylist = false;
                    foreach (string line in SubSystems)
                    {
                        if (line == "changed: playlist")
                        {
                            isPlaylist = true;
                        }
                        if (line == "changed: player")
                        {
                            isPlayer = true;
                        }
                        if (line == "changed: options")
                        {
                            isPlayer = true;
                        }
                        if (line == "changed: mixer")
                        {
                            isPlayer = true;
                        }
                        if (line == "changed: stored_playlist")
                        {
                            isStoredPlaylist = true;
                        }
                    }

                    if (isPlayer)
                    {
                        this.MpdQueryStatus();
                    }

                    if (isPlaylist)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.CurrentQueue.Clear();
                        });
                        this.MpdQueryCurrentPlaylist();
                    }

                    if (isStoredPlaylist)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.Playlists.Clear();
                        });
                        this.MpdQueryPlaylists();
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("--Error@IdleConnection DataReceived ParseData: " + str);
                }

            }
            else if (str.StartsWith("volume:") ||
                   str.StartsWith("repeat:") ||
                   str.StartsWith("random:") ||
                   str.StartsWith("state:") ||
                   str.StartsWith("song:") ||
                   str.StartsWith("songid:") ||
                   str.StartsWith("time:") ||
                   str.StartsWith("elapsed:") ||
                   str.StartsWith("duration:"))
            {
                // "status"

                /*
                volume: 78
                repeat: 1
                random: 1
                single: 0
                consume: 0
                playlist: 2708
                playlistlength: 107
                mixrampdb: 0.000000
                state: play
                song: 6
                songid: 26569
                time: 35:138
                elapsed: 35.038
                bitrate: 192
                duration: 137.648
                audio: 44100:24:2
                nextsong: 39
                nextsongid: 26602
                OK
                */

                List<string> reLines = str.Split('\n').ToList();

                ParseStatusResponse(reLines);

                await Task.Run(() => { StatusUpdate?.Invoke(this, "isPlayer"); });

            }
            else if (str.StartsWith("file:") ||
                str.StartsWith("Modified:") ||
                str.StartsWith("Artist:") ||
                str.StartsWith("AlbumArtist:") ||
                str.StartsWith("Title:") ||
                str.StartsWith("Album:"))
            {

                // "playlistinfo"
                //System.Diagnostics.Debug.WriteLine("Got playlistinfo");
                // System.Diagnostics.Debug.WriteLine("IdleConnection DataReceived Dispa playlistinfo: " + str);
                /*

file: Creedence Clearwater Revival/Chronicle, Vol. 1/11 Who'll Stop the Rain.mp3
Last-Modified: 2011-08-03T16:08:06Z
Artist: Creedence Clearwater Revival
AlbumArtist: Creedence Clearwater Revival
Title: Who'll Stop the Rain
Album: Chronicle, Vol. 1
Track: 11
Date: 1976
Genre: Rock
Composer: John Fogerty
Time: 149
duration: 149.149
Pos: 5
Id: 22637
                */
                int c = 0;
                while (_isBusy)
                {
                    c++;
                    await Task.Delay(100);
                    if (c > (100 * 100))
                    {
                        System.Diagnostics.Debug.WriteLine("IdleConnection DataReceived Dispa ParseData: TIME OUT");
                        _isBusy = false;
                    }
                }


                List<string> reLines = str.Split('\n').ToList();
                await ParsePlaylistInfoResponse(reLines);

                //Task.Run(() => { StatusUpdate?.Invoke(this, "isPlaylist"); });
                await Task.Run(() => { StatusUpdate?.Invoke(this, "isPlaylist"); });
            }
            else if (str.StartsWith("playlist:"))
            {
                /*
                playlist: Blues
                Last-Modified: 2018-01-26T12:12:10Z
                playlist: Jazz
                Last-Modified: 2018-01-26T12:12:37Z
                OK
                */

                List<string> reLines = str.Split('\n').ToList();
                ParsePlaylistsResponse(reLines);

                await Task.Run(() => { StatusUpdate?.Invoke(this, "isStoredPlaylist"); });
            }
            else if (str.StartsWith("updating_db:"))
            {
                //System.Diagnostics.Debug.WriteLine();
            }
             else
            {
                System.Diagnostics.Debug.WriteLine("IdleConnection DataReceived Dispa ParseData NON: " + str);

            }
        }

        #endregion END of MPD METHODS

    }

    // State object for receiving data.  
    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 5000;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();

    }

    public class AsynchronousTCPClient
    {
        public enum ConnectionStatus
        {
            NeverConnected,
            Connecting,
            Connected,
            MpdOK,
            MpdAck,
            AutoReconnecting,
            DisconnectedByUser,
            DisconnectedByHost,
            ConnectFail_Timeout,
            ReceiveFail_Timeout,
            SendFail_Timeout,
            SendFail_NotConnected,
            Error
        }

        private TcpClient _TCP;
        private IPAddress _ip = IPAddress.None;
        private int _p = 0;
        private string _a = "";
        private ConnectionStatus _ConStat;
        private int _retryAttempt = 0;
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        public delegate void delDataReceived(AsynchronousTCPClient sender, object data);
        public event delDataReceived DataReceived;
        public delegate void delConnectionStatusChanged(AsynchronousTCPClient sender, ConnectionStatus status);
        public event delConnectionStatusChanged ConnectionStatusChanged;
        public delegate void delDataSent(AsynchronousTCPClient sender, object data);
        public event delDataSent DataSent;

        public ConnectionStatus ConnectionState
        {
            get
            {
                return _ConStat;
            }
            private set
            {
                bool raiseEvent = value != _ConStat;
                _ConStat = value;

                if (raiseEvent)
                    Task.Run(() => { ConnectionStatusChanged?.Invoke(this, _ConStat); });
            }
        }

        static AsynchronousTCPClient()
        {

        }

        public async Task<ConnectionResult> Connect(IPAddress ip, int port, string auth)
        {
            _ip = ip;
            _p = port;
            _a = auth;
            _retryAttempt = 0;

            this._TCP = new TcpClient();
            // This will crash on iPhone.
            //this._TCP.ReceiveTimeout = System.Threading.Timeout.Infinite;
            this._TCP.ReceiveTimeout = 0;
            this._TCP.SendTimeout = 5000;
            this._TCP.Client.ReceiveTimeout = 0;
            //this._TCP.Client.ReceiveTimeout = System.Threading.Timeout.Infinite;
            //this._TCP.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            return await DoConnect(ip, port);
        }

        public async Task<bool> ReConnect()
        {
            System.Diagnostics.Debug.WriteLine("**ReConnecting...");

            this.ConnectionState = ConnectionStatus.AutoReconnecting;

            if (_retryAttempt > 1)
            {
                System.Diagnostics.Debug.WriteLine("**SendCommand@ReConnect() _retryAttempt > 1");
                
                ConnectionState = ConnectionStatus.DisconnectedByHost;

                return false;
            }

            _retryAttempt++;

            try
            {
                this._TCP.Close();
            }
            catch { }

            await Task.Delay(500);

            this._TCP = new TcpClient();
            this._TCP.ReceiveTimeout = 0;//System.Threading.Timeout.Infinite;
            this._TCP.SendTimeout = 5000;
            this._TCP.Client.ReceiveTimeout = 0;//System.Threading.Timeout.Infinite;
            //this._TCP.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            ConnectionResult r = await DoConnect(_ip, _p);

            return r.isSuccess;
        }

        public async Task<ConnectionResult> DoConnect(IPAddress ip, int port)
        {
            ConnectionResult r = new ConnectionResult();

            try
            {
                await _TCP.ConnectAsync(ip, port);
            }
            catch (SocketException ex)
            {
                System.Diagnostics.Debug.WriteLine("**Error@DoConnect: SocketException " + ex.Message);
                ConnectionState = ConnectionStatus.Error;

                r.isSuccess = false;
                r.errorMessage = ex.Message + " (SocketException)";
                return r;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("**Error@DoConnect: Exception " + ex.Message);
                ConnectionState = ConnectionStatus.Error;

                r.isSuccess = false;
                r.errorMessage = ex.Message + " (Exception)";
                return r;
            }

            Receive(_TCP.Client);

            ConnectionState = ConnectionStatus.Connected;
            _retryAttempt = 0;

            r.isSuccess = true;
            r.errorMessage = "";
            return r;
        }

        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@Receive" + ex.ToString());
                ConnectionState = ConnectionStatus.Error;
            }
        }

        private async void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                SocketError err = new SocketError();
                int bytesRead = client.EndReceive(ar, out err);

                if (bytesRead > 0)
                {
                    string res = Encoding.Default.GetString(state.buffer, 0, bytesRead);
                    state.sb.Append(res);

                    if (res.EndsWith("OK\n") || res.StartsWith("OK MPD") || res.StartsWith("ACK"))
                    //if (client.Available == 0)
                    {
                        if (!string.IsNullOrEmpty(state.sb.ToString().Trim()))
                        {
                            await Task.Run(() => { DataReceived?.Invoke(this, state.sb.ToString()); });
                        }
                        //receiveDone.Set();

                        state = new StateObject();
                        state.workSocket = client;
                    }

                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

                }
                else
                {
                    //if (err != SocketError.Success)
                    System.Diagnostics.Debug.WriteLine("ReceiveCallback bytesRead 0");

                    this.ConnectionState = ConnectionStatus.DisconnectedByHost;
                    //https://msdn.microsoft.com/en-us/library/ms145145(v=vs.110).aspx
                    /*
                    if (!string.IsNullOrEmpty(state.sb.ToString().Trim()))
                    {
                        await Task.Run(() => { DataReceived?.Invoke(this, state.sb.ToString()); });
                    }
                    */
                    if (!await ReConnect())
                    {
                        System.Diagnostics.Debug.WriteLine("**ReceiveCallback: bytesRead 0 - GIVING UP reconnect.");
                        ConnectionState = ConnectionStatus.DisconnectedByHost;
                    }
                    
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ReceiveCallback" + ex.ToString());
                ConnectionState = ConnectionStatus.Error;
            }
        }

        public async void Send(string cmd)
        {
            if (ConnectionState != ConnectionStatus.Connected) { return; }
            
            try
            {
                DoSend(_TCP.Client, cmd);
            }
            catch (IOException)
            {
                //System.IO.IOException
                //Unable to transfer data on the transport connection: An established connection was aborted by the software in your host machine.

                System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: IOException - TRYING TO RECONNECT.");

                // Reconnect.
                if (await ReConnect())
                {
                    DoSend(_TCP.Client, cmd);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: IOException - GIVING UP reconnect.");

                }

            }
            catch (SocketException)
            {
                //System.Net.Sockets.SocketException
                //An established connection was aborted by the software in your host machine
                System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: SocketException - TRYING TO RECONNECT.");

                // Reconnect.
                if (await ReConnect())
                {
                    DoSend(_TCP.Client, cmd);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: SocketException - GIVING UP reconnect.");

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: " + ex.Message);

            }


            sendDone.WaitOne();
        }

        private void DoSend(Socket client, String data)
        {
            //System.Diagnostics.Debug.WriteLine("Sending... :" + data);

            //await Task.Run(() => {  });
            DataSent?.Invoke(this, Environment.NewLine + "Sending...: " + data + Environment.NewLine);

            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.Default.GetBytes(data);
            try
            {
                // Begin sending the data to the remote device.  
                client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@DoSend" + ex.ToString());
                ConnectionState = ConnectionStatus.Error;
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                //System.Diagnostics.Debug.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@SendCallback" + ex.ToString());
                ConnectionState = ConnectionStatus.Error;
            }
        }

        public void DisConnect()
        {
            // Release the socket.  
            try
            {
                this._TCP.Client.Shutdown(SocketShutdown.Both);
                this._TCP.Client.Close();
            }
            catch { }
        }
    }

}

