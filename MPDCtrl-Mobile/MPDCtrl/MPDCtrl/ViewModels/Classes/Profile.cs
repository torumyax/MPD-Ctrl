﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MPDCtrl.ViewModels
{
    /// <summary>
    /// Profile class for connection setting.
    /// </summary>
    public class Profile : ViewModelBase
    {
        private string _host;
        public string Host
        {
            get { return _host; }
            set
            {
                if (_host == value)
                    return;

                _host = value;
                NotifyPropertyChanged("Host");
            }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            set
            {
                if (_port == value)
                    return;

                _port = value;
                NotifyPropertyChanged("Port");
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                if (_password == value)
                    return;

                _password = value;
                NotifyPropertyChanged("Password");
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value)
                    return;

                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private bool _isDefault;
        public bool IsDefault
        {
            get { return _isDefault; }
            set
            {
                if (_isDefault == value)
                    return;

                _isDefault = value;

                NotifyPropertyChanged("IsDefault");
            }
        }

    }

}
