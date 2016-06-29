using System;
using System.Collections.Generic;

using System.IO;
using System.Net;
using System.Net.Sockets;

namespace AllegroTech.CBus4Net.Communication
{

    [System.Diagnostics.DebuggerDisplay("Tcp IP:{_HostName} Port:{_Port}")]
    public class TcpCommunicationChannel : CommunicationChannelBase
    {
        TcpClient _TcpClient;
        readonly int _Port;
        readonly string _HostName;

        public TcpCommunicationChannel(string HostName, int Port)
        {
            _HostName = HostName;
            _Port = Port;
        }

        #region ICommunicationChannel Members

        public override int SendBytes(byte[] buffer, int Count)
        {            
            if (IsOpen)
                return _TcpClient.Client.Send(buffer, Count, SocketFlags.None);
            else
                return 0;
        }

        public override int ReceiveBytes(byte[] buffer, int Count)
        {
            if (IsOpen && _TcpClient.Available>0)
            {                
                return _TcpClient.Client.Receive(buffer, Count, SocketFlags.None);
            }
            else
                return 0;
        }
        public override int ReceiveBytes(byte[] buffer, int Offset, int Count)
        {
            if (IsOpen && _TcpClient.Available > 0)
                return _TcpClient.Client.Receive(buffer, Offset, Count, SocketFlags.None);
            else
                return 0;
        }


        public override Stream CommunicationStream
        {
            get { return _TcpClient.GetStream(); }
        }

        public override bool Open()
        {
            try
            {
                if (_TcpClient == null)
                {
                    _TcpClient = new TcpClient();

                    _TcpClient.Client.ReceiveTimeout = 200;
                    _TcpClient.Client.SendTimeout = 200;
                }

                _TcpClient.Connect(_HostName, _Port);

                System.Threading.Thread.Sleep(200);

                IsOpen = _TcpClient.Connected;
                return IsOpen;
            }
            catch (SocketException /*sockException*/)
            {
                //Logger.Warn("Failed to connect socket, {0}-{1}", sockException.SocketErrorCode, sockException.Message);
                if (_TcpClient != null)
                {
                    _TcpClient.Close();
                    _TcpClient = null;
                }
                return false;
            }
        }

        public override bool IsOpen
        {
            get
            {
                if (_TcpClient == null)
                    return false;
                else
                    return _TcpClient.Connected;                
            }
            protected set
            {
                base.IsOpen = value;
            }
        }

        public override void Close()
        {
            if (_TcpClient != null)
            {
                _TcpClient.Close();
                _TcpClient = null;
                IsOpen = false;
            }
        }

        public override void Flush()
        {
            if(IsOpen)
                _TcpClient.GetStream().Flush();
        }

        protected override void Dispose(bool IsDisposing)
        {
            if (IsDisposing)
            {
            }

            if (_TcpClient != null)
            {
                _TcpClient.Close();
                _TcpClient = null;
                IsOpen = false;                
            }           
        }

        #endregion

    }
}
