using System;
using System.IO;
using System.Collections.Generic;

namespace AllegroTech.CBus4Net.Communication
{
    using Interface;

    public abstract class CommunicationChannelBase :
        ICommunicationChannel
    {
        private bool IsDisposed { get; set; }

        protected CommunicationChannelBase()
        {
            IsDisposed = false;
        }

        ~CommunicationChannelBase()
        {
            Dispose(false);
        }

        protected void LogMessage(string Message, params object[] Args)
        {
            string msg = string.Format(Message, Args);
            System.Diagnostics.Debug.WriteLine(msg);
        }

       
        public void Dispose()
        {
            if (!IsDisposed)
            {
                Dispose(true);
                System.GC.SuppressFinalize(this);
                IsDisposed = true;
                IsOpen = false;
            }
        }

        public virtual bool IsOpen { get; protected set; }

        protected abstract void Dispose(bool IsDisposing);

        #region ICommunicationChannel Members

        //public abstract System.IO.Stream CommunicationStream    {get;}
        public abstract void Close();
        public abstract bool Open();
        public abstract void Flush();
        

        public abstract int SendBytes(byte[] buffer, int Count);
        public abstract int ReceiveBytes(byte[] buffer, int Count);
        public abstract int ReceiveBytes(byte[] buffer, int Offset, int Count);

        public abstract Stream CommunicationStream { get; }

        #endregion
    }
}
