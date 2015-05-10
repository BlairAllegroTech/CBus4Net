using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atria.AVControl.Device.CBus.Protocol
{
    public interface ICBusCommand
    {
        string ToCBusString();
    }
    /// <summary>
    /// CBus application command, Specific Application Language (SAL) Command
    /// </summary>
    public abstract class CBusSALCommand : ICBusCommand
    {
        /// <summary>
        /// See Serial Interface Guide Page.11-13
        /// </summary>
        public class CBusHeader
        {
            //Makes up top 2 bits of header
            public enum CBusHeader_Priority : byte
            {
                Lowest_Class4 = 0x00,
                Medium_Class3 = 0x01,
                MediumHigh_Class4 = 0x02,
                Highest_Class1 = 0x03
            }

            
            public enum CBusHeader_AddressType : byte
            {
                /// <summary>
                /// Hex 0x03, 2 Byte Address
                /// </summary>
                Point_To_Point_Multi = 0x03,
                
                /// <summary>
                /// Hex 0x05, No Address
                /// </summary>
                Point_To_Multi = 0x05,

                /// <summary>
                /// Hex 0x06, 1 byte Address
                /// </summary>
                Point_To_Point = 0x06
            }

            public static readonly CBusHeader PPM;
            public static readonly CBusHeader PM;
            public static readonly CBusHeader PP;

            public CBusHeader_Priority Priority { get; protected set; }
            public CBusHeader_AddressType AddressType { get; protected set; }

            static CBusHeader()
            {
                PPM = new CBusHeader(CBusHeader_Priority.Lowest_Class4, CBusHeader_AddressType.Point_To_Point_Multi);
                PM = new CBusHeader(CBusHeader_Priority.Lowest_Class4, CBusHeader_AddressType.Point_To_Multi);
                PP = new CBusHeader(CBusHeader_Priority.Lowest_Class4, CBusHeader_AddressType.Point_To_Point);
            }

            public CBusHeader(CBusHeader_Priority Priority, CBusHeader_AddressType AddressType)
            {
                this.Priority = Priority;
                this.AddressType = AddressType;
            }


            public static implicit operator byte(CBusHeader header)
            {
                var result = ((byte)header.Priority << 6) + (byte)header.AddressType;
                return Convert.ToByte(result);
            }

            internal static bool TryParse(byte p, out CBusHeader Header)
            {
                var addressType = Convert.ToByte(p & 0x07);
                if (Enum.IsDefined(typeof(CBusHeader_AddressType), addressType))
                    Header = new CBusHeader(CBusHeader_Priority.Lowest_Class4, (CBusHeader_AddressType)addressType);
                else
                    Header = PP;

                return true;
            }
        }

        protected readonly CBusHeader Header;
        protected readonly byte ApplicationAddress;

        public CBusProtcol.ApplicationTypes ApplicationType { get; protected set; }

        public char? AckCharacter { get; set; }

        protected CBusSALCommand(CBusHeader Header, byte ApplicationAddress)
        {
            this.Header = Header;
            this.ApplicationAddress = ApplicationAddress;          
        }

        

        /// <summary>
        /// Convert command data to a byte array
        /// </summary>
        /// <returns></returns>
        protected abstract byte[] GetRawCommandData();

        public byte[] ToByteArray()
        {
            int i = 0;
            var commandData = GetRawCommandData();

            var data = new byte[commandData.Length + 4];
            data[i++] = this.Header;
            data[i++] = this.ApplicationAddress;
            data[i++] = 0x00;

            //Add data
            commandData.CopyTo(data,i);
            i+=commandData.Length;

            data[i] = CalculateChecksum(data, i);
            return data;
        }


        public static byte CalculateChecksum(byte[] RawCommand, int Length)
        {
            int CheckSumAccumulator = 0;
           
            for(int i=0; i< Length; ++i)
            {
                CheckSumAccumulator += Convert.ToInt32(RawCommand[i]);
            }

            CheckSumAccumulator %= 0x100;
            
            //2's complement + 1
            CheckSumAccumulator *= -1;
            
            CheckSumAccumulator &= 0xFF;

            return Convert.ToByte(CheckSumAccumulator);
        }

        public string ToCBusString()
        {
            var sb = new StringBuilder();
            var data = ToByteArray();
            sb.Append( CBus.Protocol.CBusProtcol.STX_CHAR);
            foreach (var dat in data)
            {
                sb.AppendFormat("{0:X2}", dat);
            }

            if (this.AckCharacter.HasValue)
                sb.Append(AckCharacter.Value);            

            sb.Append(CBus.Protocol.CBusProtcol.ETX_CHAR_1);                        
            return sb.ToString();
        }



        /// <summary>
        /// When receiving a command from the wire parse using this method into a CBus Command
        /// </summary>
        /// <param name="CommandBytes"></param>
        /// <param name="Command"></param>
        /// <returns></returns>
        internal static bool TryParseApplicationId(byte[] CommandBytes, int CommandLength, bool IsMonitoredSALReply, bool IsShortFormMessage, out int SALDataPointer, out byte  ApplicationId)
        {
            SALDataPointer = 0;
            ApplicationId = 0;

            if (IsMonitoredSALReply)
            {
                if (IsShortFormMessage)
                {
                    if (CommandBytes[0] == 0)
                    {
                        //No Bridge, next byte is Application Id
                        ApplicationId = CommandBytes[1];
                        SALDataPointer = 2;
                    }
                    else
                    {
                        //No Bridge, next byte is Application Id
                        ApplicationId = CommandBytes[2];
                        SALDataPointer = 3;
                    }
                }
                else
                {
                    if (CommandBytes[0] == 0x05)
                    {
                        ApplicationId = CommandBytes[2];

                        var NumberOfRoutes = CommandBytes[3] / CBusProtcol.ROUTE_HEADER_DIVISOR;

                        SALDataPointer = 3 + NumberOfRoutes;
                        SALDataPointer++;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                CBusHeader header;
                if (CBusHeader.TryParse(CommandBytes[0], out header))
                {
                    switch (header.AddressType)
                    {
                        case CBusHeader.CBusHeader_AddressType.Point_To_Point:
                            //NOT suported CAL data only
                            return false;

                        case CBusHeader.CBusHeader_AddressType.Point_To_Multi:
                            ApplicationId = CommandBytes[1];
                            SALDataPointer = 2;
                            SALDataPointer++; //Skip trailing 0
                            break;

                        case CBusHeader.CBusHeader_AddressType.Point_To_Point_Multi:
                            {
                                var firstHopTarget = CommandBytes[1];
                                var NumberOfRoutes = CommandBytes[2] / CBusProtcol.ROUTE_HEADER_DIVISOR;

                                ApplicationId = CommandBytes[2 + NumberOfRoutes];
                                SALDataPointer = 3 + NumberOfRoutes;
                                SALDataPointer++;
                                break;
                            }

                        default:
                            return false;

                    }
                }
                else
                {
                    return false;
                }

                return true;
            }

         
        }

        public static string BinaryArrayToHexString(byte[] Data, int Length)
        {
            var sb = new StringBuilder();
                        
            for(int i=0; i < Length; ++i)
            {
                sb.AppendFormat("0x{0:X2},", Data[i]);
            }

            if(sb.Length > 1)
                sb.Remove(sb.Length-1,1);

            return sb.ToString();
        }

    }
}
