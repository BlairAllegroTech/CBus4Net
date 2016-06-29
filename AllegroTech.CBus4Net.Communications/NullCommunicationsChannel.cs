using System;
using System.IO;

namespace AllegroTech.CBus4Net.Communication
{
    [System.Diagnostics.DebuggerDisplay("Null :{_HostName} Port:{_Port}")]
    public class NullCommunicationsChannel : CommunicationChannelBase
    {
        class NullStream : Stream
        {
            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override void Flush()
            {
                
            }

            public override long Length
            {
                get { return 0; }
            }

            public override long Position
            {
                get
                {
                    return 0;
                }
                set
                {
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return 0;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return 0;
            }

            public override void SetLength(long value)
            {
                
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                
            }
        }
        readonly NullStream _Stream;        

        public NullCommunicationsChannel() 
            : base()
        {
            _Stream = new NullStream();
        }

        protected override void Dispose(bool IsDisposing)
        {
            IsOpen = false;
            if(IsDisposing)            
                _Stream.Dispose();
        }

        public override System.IO.Stream CommunicationStream
        {
            get { return _Stream; }
        }

        public override void Close()
        {
            IsOpen = false;
        }

        public override bool Open()
        {
            IsOpen = true;
            return IsOpen;
        }

        public override void Flush()
        {
        }


        public override int SendBytes(byte[] buffer, int Count)
        {
            return Count;
        }

        public override int ReceiveBytes(byte[] buffer, int Count)
        {
            return 0;
        }

        public override int ReceiveBytes(byte[] buffer, int Offset, int Count)
        {
            return 0;
        }
    }
}
