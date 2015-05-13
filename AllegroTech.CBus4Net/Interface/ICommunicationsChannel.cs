using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllegroTech.CBus4Net.Interface
{
    public interface ICommunicationChannel
    {
        void SendBytes(byte[] commandBytes, int MessageLength);
        int ReceiveBytes(byte[] rxBuffer, int BufferLength);
    }
}
