﻿using System;
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

namespace MPDCtrl.Models.Classes
{
    public class ConnectionResult
    {
        public bool isSuccess;
        public string errorMessage;
    }


    public class TCPC
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
        private ConnectionStatus _ConStat;
        private int _retryAttempt = 0;
        private class StateObject
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

        public delegate void delDataReceived(TCPC sender, object data);
        public event delDataReceived DataReceived;
        public delegate void delConnectionStatusChanged(TCPC sender, ConnectionStatus status);
        public event delConnectionStatusChanged ConnectionStatusChanged;
        public delegate void delDataSent(TCPC sender, object data);
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

        public async Task<ConnectionResult> Connect(IPAddress ip, int port)
        {
            ConnectionState = ConnectionStatus.Connecting;

            _ip = ip;
            _p = port;
            _retryAttempt = 0;

            _TCP = new TcpClient();
            _TCP.ReceiveTimeout = System.Threading.Timeout.Infinite; // or 0
            _TCP.SendTimeout = 5000;
            _TCP.Client.ReceiveTimeout = System.Threading.Timeout.Infinite;// 0;
            //_TCP.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            return await DoConnect(ip, port);
        }

        public async Task<bool> ReConnect()
        {
            System.Diagnostics.Debug.WriteLine("**ReConnecting...");

            ConnectionState = ConnectionStatus.AutoReconnecting;

            if (_retryAttempt > 1)
            {
                System.Diagnostics.Debug.WriteLine("**SendCommand@ReConnect() _retryAttempt > 1");

                ConnectionState = ConnectionStatus.DisconnectedByHost;

                return false;
            }

            _retryAttempt++;

            try
            {
                _TCP.Close();
            }
            catch { }

            _TCP = new TcpClient();
            _TCP.ReceiveTimeout = System.Threading.Timeout.Infinite;
            _TCP.SendTimeout = 5000;
            _TCP.Client.ReceiveTimeout = System.Threading.Timeout.Infinite;
            //_TCP.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

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

            ConnectionState = ConnectionStatus.Connected;

            Receive(_TCP.Client);

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
            // Retrieve the state object and the client socket from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;
            SocketError err = new SocketError();

            try
            {
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
                            DataReceived?.Invoke(this, state.sb.ToString().Trim());
                        }
                        state = new StateObject();
                        state.workSocket = client;
                    }

                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    //https://msdn.microsoft.com/en-us/library/ms145145(v=vs.110).aspx
                    System.Diagnostics.Debug.WriteLine("ReceiveCallback bytesRead 0. Disconnected By Host.");

                    ConnectionState = ConnectionStatus.DisconnectedByHost;

                    if (!await ReConnect())
                    {
                        ConnectionState = ConnectionStatus.DisconnectedByHost;

                        System.Diagnostics.Debug.WriteLine("**ReceiveCallback: bytesRead 0 - GIVING UP reconnect.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ReceiveCallback: " + err.ToString() + ". " + ex.ToString() + ". ");
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

                System.Diagnostics.Debug.WriteLine("**Error@Send: IOException - TRYING TO RECONNECT.");

                // Reconnect.
                if (await ReConnect())
                {
                    DoSend(_TCP.Client, cmd);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("**Error@Send: IOException - GIVING UP reconnect.");

                }

            }
            catch (SocketException)
            {
                //System.Net.Sockets.SocketException
                //An established connection was aborted by the software in your host machine
                System.Diagnostics.Debug.WriteLine("**Error@Send: SocketException - TRYING TO RECONNECT.");

                // Reconnect.
                if (await ReConnect())
                {
                    DoSend(_TCP.Client, cmd);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("**Error@Send: SocketException - GIVING UP reconnect.");

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("**Error@Send: " + ex.Message);

            }
        }

        private void DoSend(Socket client, String data)
        {
            DataSent?.Invoke(this, ">>" + data);

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
                _TCP.Client.Shutdown(SocketShutdown.Both);
                _TCP.Client.Close();
            }
            catch { }
        }
    }
}