using System;
using System.IO.Ports;

using System.Collections.Generic;

namespace AllegroTech.CBus4Net.Communication
{
    [System.Diagnostics.DebuggerDisplay("SerialPort :{SerialPortName} Settings:{_BaudRate}{_Parity}{_DataBits}{_StopBits}")]
    public class SerialCommunicationChannel : CommunicationChannelBase
    {
        SerialPort _serialPort;
        readonly string _SerialPortName;
        readonly int _BaudRate;
        readonly Parity _Parity;
        readonly int _DataBits;
        readonly StopBits _StopBits;

        public override bool IsOpen
        {
            get
            {
                if (_serialPort != null)
                    return _serialPort.IsOpen;
                else
                    return false;
            }
            protected set
            {                
                base.IsOpen = value;
            }
        }

        public SerialCommunicationChannel(string SerialPortName, int BaudRate, Parity Parity, int DataBits, StopBits StopBits) 
            : base()
        {
            this._SerialPortName = SerialPortName;
            this._BaudRate = BaudRate;
            this._Parity = Parity;
            this._DataBits = DataBits;
            this._StopBits = StopBits;


        }


        protected override void Dispose(bool IsDisposing)
        {
            if (IsDisposing)
            {
            }

            if (_serialPort != null)
                _serialPort.Dispose();        
        }

        public override System.IO.Stream CommunicationStream
        {
            get
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                    throw new CommunicationException();


                return _serialPort.BaseStream;
            }
        }

        public override void Close()
        {
            if (_serialPort!=null)
                _serialPort.Close();                        
        }

        public override bool Open()
        {
            if (_serialPort==null)
                _serialPort = new SerialPort(_SerialPortName, _BaudRate, _Parity, _DataBits, _StopBits);

            if (_serialPort != null)
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.WriteTimeout = 200;
                    _serialPort.ReadTimeout = 200;
                    _serialPort.Open();
                }
            }

            return _serialPort.IsOpen;
        }

        

        public override void Flush()
        {
            _serialPort.BaseStream.Flush();
        }

        public override int SendBytes(byte[] buffer, int Count)
        {
            _serialPort.Write(buffer, 0, Count);
            return Count;
        }

        public override int ReceiveBytes(byte[] buffer, int Count)
        {
            return ReceiveBytes(buffer, 0, Count);
        }
        public override int ReceiveBytes(byte[] buffer, int Offset, int Count)
        {
            if(_serialPort!=null)
            {
                int AvailableBytes = _serialPort.BytesToRead;
                if (AvailableBytes > 0)
                {
                    if (Count > AvailableBytes) Count = AvailableBytes;
                    return _serialPort.Read(buffer, Offset, Count);
                }
            }
            

            return 0;
        }
    }
}
