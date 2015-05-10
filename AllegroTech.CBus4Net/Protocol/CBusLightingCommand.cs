using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllegroTech.CBus4Net.Protocol
{
    
    public class CBusLightingCommand : CBusSALCommand
    {
        [System.Diagnostics.DebuggerDisplay("CBusLightingCommand Cmd:{Command}, Group:{Group}, Ramp:{RampTargetLevel}")]
        public class LightingCommand
        {
            /// <summary>
            /// Lighting Command Content Header.
            /// Here the low 3 bits indicate the number of byte parameters
            /// </summary>
            public enum LightingCommandId : byte
            {
                OFF = 0x01,
                ON = 0x79,

                //Ramp Command Rates
                RAMP_TERMINATE = 0x09,

                //2 params group and level
                RAMP_INSTANT = 0x02,
                RAMP_4SEC = 0x0A,
                RAMP_8SEC = 0x12,
                RAMP_12SEC = 0x1A,
                RAMP_20SEC = 0x22,
                RAMP_30SEC = 0x2A,
                RAMP_40SEC = 0x32,
                RAMP_1MIN = 0x3A,
                RAMP_2MIN = 0x4A,
                RAMP_5MIN = 0x5A,
                RAMP_10MIN = 0x6A,

                UNKNOWN = 0xFF
            }

            public readonly LightingCommandId Command;
            public readonly byte Group;
            public byte RampTargetLevel;

            public LightingCommand(LightingCommandId Command, byte Group)
            {
                this.Command = Command;
                this.Group = Group;
            }

            public override string ToString()
            {
                switch(Command)
                {
                    case LightingCommandId.UNKNOWN:
                    case LightingCommandId.OFF:
                    case LightingCommandId.ON:
                    case LightingCommandId.RAMP_INSTANT:
                    case LightingCommandId.RAMP_TERMINATE:
                        return string.Format("CommandId:{0} Group:0x{1:x2}", Command, Group);

                    default:
                        return string.Format("CommandId:{0} Group:0x{1:x2}, RampLevel:0x{2:x2}", Command, Group, RampTargetLevel);
                }
                
            }

        }

        readonly List<LightingCommand> CommandList;

        /// <summary>
        /// Create a CBus lighting command
        /// </summary>
        /// <param name="Header">Indicates the type of message e.g. Multi or point to point etc..</param>
        /// <param name="AppAddress">Typically 0x38 but can differ</param>
        /// <param name="Command">ON or OFF etc...</param>
        /// <param name="Group"></param>
        public CBusLightingCommand(CBusHeader Header, byte AppAddress, LightingCommand.LightingCommandId Command, byte Group)
            : this(Header, AppAddress, new LightingCommand(Command, Group))
        {            
        }


        public CBusLightingCommand(CBusHeader Header, byte AppAddress, IEnumerable<LightingCommand> CommandList)
            : base(Header, AppAddress)
        {
            base.ApplicationType = CBusProtcol.ApplicationTypes.LIGHTING;
            this.CommandList = new List<LightingCommand>(CommandList);            
        }

        /// <summary>
        /// Multiple commands can bs strung togeather
        /// </summary>
        /// <param name="Header"></param>
        /// <param name="AppAddress"></param>
        /// <param name="Command"></param>
        public CBusLightingCommand(CBusHeader Header, byte AppAddress, params LightingCommand[] Command)
            : this(Header, AppAddress, Command.AsEnumerable())
        {

        }

        public IEnumerable<LightingCommand> Commands()
        {
            foreach (var cmd in CommandList)
                yield return cmd;
        }

        /// <summary>
        /// When receiving a command from the wire parse using this method into a CBus Command
        /// </summary>
        /// <param name="CommandBytes"></param>
        /// <param name="Command"></param>
        /// <returns></returns>
        internal static bool TryParseReply(
            byte[] CommandBytes, int CommandLength, bool IsShortFormMessage, byte CBusApplicationAddress,
            ref int dataPointer, out CBusLightingCommand Command)
        {            
            //Process Contained Commands
            var lightingCommandList = new List<LightingCommand>();
            if (TryProcessApplicationData(CommandBytes, CommandLength, lightingCommandList, ref dataPointer))
            {
                //Use any old header not part of reply
                Command = new CBusLightingCommand(CBusHeader.PPM, CBusApplicationAddress, lightingCommandList);
                return true;
            }

            Command = null;
            return false;
        }


        internal static bool TryParseCommand(byte[] CommandBytes, int CommandLength, out CBusLightingCommand Command)
        {
            int dataPointer = 0;
            Command = null;
            var lightingCommandList = new List<LightingCommand>();

            CBusHeader header = null;
            
            //Datapointer=0
            if (!CBusHeader.TryParse(CommandBytes[dataPointer++], out header))
                return false;

            

            //Datapointer=1
            var appAddress = CommandBytes[dataPointer++];

            switch (header.AddressType)
            {
                case CBusHeader.CBusHeader_AddressType.Point_To_Multi:
                    break;

                case CBusHeader.CBusHeader_AddressType.Point_To_Point:
                    //Skip 1 byte address
                    dataPointer++;
                    break;

                case CBusHeader.CBusHeader_AddressType.Point_To_Point_Multi:
                    //Skip 2 byte address
                    dataPointer += 2;
                    break;
            }


            //Datapointer=3
            if (CommandBytes[dataPointer] != 0x01 && CommandBytes[dataPointer] != 0x00)
                return false;

            //If this byte 01 then next byte must be 00
            if (CommandBytes[dataPointer++] == 0x01)
            {
                if (CommandBytes[dataPointer++] != 0x00)
                    return false;
            }

           

            //Process Contained Commands

            //Subtract 1 as this is the payload length excluding checksum
            if (TryProcessApplicationData(CommandBytes, (CommandLength-1), lightingCommandList, ref dataPointer))
            {
                Command = new CBusLightingCommand(header, appAddress, lightingCommandList);
                return true;
            }
            else
            {
                return false;
            }            
        }

        static bool TryProcessApplicationData(byte[] CommandBytes, int CommandLength, IList<LightingCommand> lightingCommandList, ref int dataPointer)
        {
            //As message length includes checksum
            while (dataPointer < (CommandLength-1))
            {
                //Most Significant Bit indicates long or short format comamnd
                if ((CommandBytes[dataPointer] & 0x80) == 0)
                {
                    //Short form command
                    byte group = 0;
                    var lightingCommandType = LightingCommand.LightingCommandId.UNKNOWN;

                    //Lower 3 bits of command, See Lighting application Document Page.8
                    var subCommandLength = (CommandBytes[dataPointer] & 0x07);

                    //Check that reported data length not too long.(Add one as does not include command id its self)
                    if ((dataPointer + subCommandLength+1) > CommandLength)
                        return false;

                    if (!Enum.IsDefined(typeof(LightingCommand.LightingCommandId), CommandBytes[dataPointer]))
                        return false;


                    lightingCommandType = (LightingCommand.LightingCommandId)CommandBytes[dataPointer++];
                    group = CommandBytes[dataPointer++];


                    //Add the command
                    var cmd = new LightingCommand(lightingCommandType, group);

                    switch (lightingCommandType)
                    {
                        case LightingCommand.LightingCommandId.UNKNOWN:
                            return false;
                           
                        case LightingCommand.LightingCommandId.RAMP_TERMINATE:
                        case LightingCommand.LightingCommandId.ON:
                        case LightingCommand.LightingCommandId.OFF:
                            break;

                        //Also contain level parameter
                        default:
                            cmd.RampTargetLevel = CommandBytes[dataPointer++];
                            break;
                    }

                    lightingCommandList.Add(cmd);
                }
                else
                {
                    //Long form command, not implemented

                    //Lower 3 bits of command, See Lighting application Document Page.8
                    var commandLength = (CommandBytes[dataPointer++] & 0x1F);

                    //Skip this command
                    dataPointer += commandLength;
                }
            }

            return true;
        }

        protected override byte[] GetRawCommandData()
        {
            int DataLength = 0;
            int TotalComandDataLength = CalculateRawDataLength();
            //Add one for group address
            var Data = new byte[TotalComandDataLength];

            
            foreach (var cmd in CommandList)
            {
                switch (cmd.Command)
                {
                    case LightingCommand.LightingCommandId.ON:
                    case LightingCommand.LightingCommandId.OFF:
                    case LightingCommand.LightingCommandId.RAMP_TERMINATE:
                        Data[DataLength++] = (byte)cmd.Command;
                        Data[DataLength++] = (byte)cmd.Group;
                        break;

                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_INSTANT:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_4SEC:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_8SEC:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_10MIN:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_12SEC:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_30SEC:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_40SEC:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_1MIN:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_5MIN:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_20SEC:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_2MIN:
                        Data[DataLength++] = (byte)cmd.Command;
                        Data[DataLength++] = (byte)cmd.Group;
                        Data[DataLength++] = cmd.RampTargetLevel;
                        break;
                }
            }

            System.Diagnostics.Debug.Assert(TotalComandDataLength==DataLength, "Calculated and Actual Message Data lengths Vary!");
            return Data;
        }

        int CalculateRawDataLength()
        {
            int TotalComandDataLength = 1;
            foreach (var cmd in CommandList)
            {
                switch (cmd.Command)
                {
                    case LightingCommand.LightingCommandId.UNKNOWN:
                        break;

                    case LightingCommand.LightingCommandId.ON:
                    case LightingCommand.LightingCommandId.OFF:
                    case LightingCommand.LightingCommandId.RAMP_TERMINATE:
                        //TotalComandDataLength += 0;
                        break;

                    default:
                        TotalComandDataLength += 1;
                        break;

                }
            }
            return TotalComandDataLength+1;
        }


        public override string ToString()
        {
            return string.Format("CBus Command Type:{0}, Command Count:{1}", ApplicationType, this.CommandList.Count);
        }
    }
}

