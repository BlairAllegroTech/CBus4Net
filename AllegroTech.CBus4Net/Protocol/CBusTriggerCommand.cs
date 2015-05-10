using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllegroTech.CBus4Net.Protocol
{
    /// <summary>
    /// CBusTriggerCommand
    /// </summary>
    /// <see cref="http://training.clipsal.com/downloads/OpenCBus/Trigger%20Control%20Application.pdf"/>    
    public class CBusTriggerCommand : CBusSALCommand
    {
        [System.Diagnostics.DebuggerDisplay("CBusTriggerCommand Cmd:{Command}, Group:{TriggerGroup}, Action:{Action}")]
        public class TriggerCommand
        {
            /// <summary>
            /// Lighting Command Content Header.
            /// Here the low 3 bits indicate the number of byte parameters
            /// </summary>
            public enum TriggerCommandId : byte
            {
                TRIGGER_MIN = 0x01,
                TRIGGER_MAX = 0x79,
                EVENT = 0x02,
                TRIGGER_KILL = 0x09,
                
                UNKNOWN = 0xFF
            }

            public readonly TriggerCommandId Command;
            public readonly byte TriggerGroup;
            public readonly byte Action;


            public TriggerCommand(TriggerCommandId Command, byte TriggerGroup)
            {
                this.Command = Command;
                this.TriggerGroup = TriggerGroup;
            }

            public TriggerCommand(byte TriggerGroup, byte Action)
            {
                Command = TriggerCommandId.EVENT;
                this.TriggerGroup = TriggerGroup;
                this.Action = Action;
            }

            public override string ToString()
            {
                switch (Command)
                {
                    case TriggerCommandId.EVENT:
                        return string.Format("CommandId:{0} Group:0x{1:x2}, Action:0x{2:x2}", Command, TriggerGroup, Action);

                    default:
                        return string.Format("CommandId:{0} TriggerGroup:0x{1:x2} Action:0x{2:x2}", Command, TriggerGroup, Action);
                }
                
            }
        }

        readonly List<TriggerCommand> CommandList;

        public CBusTriggerCommand(CBusHeader Header, byte AppAddress, IEnumerable<TriggerCommand> CommandList)
            : base(Header, AppAddress)
        {
            base.ApplicationType = CBusProtcol.ApplicationTypes.TRIGGER;
            this.CommandList = new List<TriggerCommand>(CommandList);
        }

        public IEnumerable<TriggerCommand> Commands()
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
        internal static bool TryParse(byte[] CommandBytes, int CommandLength, bool IsShortFormMessage, byte CBusApplicationAddress,
            ref int dataPointer, out CBusTriggerCommand Command)
        {
            Command = null;
            var triggerCommandList = new List<TriggerCommand>();

            CBusHeader header;
            CBusHeader.TryParse(CommandBytes[0], out header);

            //As message length includes checksum, where as we just want the payload length
            var messagePayloadLenght = (CommandLength - 1);

            //Process Contained Commands            
            while (dataPointer < messagePayloadLenght)
            {
                //Most Significant Bit indicates long or short format comamnd
                if ((CommandBytes[dataPointer] & 0x80) == 0)
                {
                    //Short form command
                    byte group = 0;
                    var triggerCommandType = TriggerCommand.TriggerCommandId.UNKNOWN;

                    if (!Enum.IsDefined(typeof(TriggerCommand.TriggerCommandId), CommandBytes[dataPointer]))
                        return false;

                    //Lower 3 bites of command
                    //var commandLength = (commandId & 0x07);

                    triggerCommandType = (TriggerCommand.TriggerCommandId)CommandBytes[dataPointer++];
                    group = CommandBytes[dataPointer++];


                    //Add the command
                    switch (triggerCommandType)
                    {
                        case TriggerCommand.TriggerCommandId.EVENT:
                        {
                            var triggerGroup = CommandBytes[dataPointer++];
                            var Action= CommandBytes[dataPointer++];
                            var cmd = new TriggerCommand(triggerGroup, Action);
                            triggerCommandList.Add(cmd);
                            break;
                        }

                        //Also contain level parameter
                        case TriggerCommand.TriggerCommandId.TRIGGER_MIN:
                        case TriggerCommand.TriggerCommandId.TRIGGER_MAX:
                        case TriggerCommand.TriggerCommandId.TRIGGER_KILL:                        
                        {
                            var triggerGroup = CommandBytes[dataPointer++];
                            var cmd = new TriggerCommand(triggerCommandType, triggerGroup);
                            triggerCommandList.Add(cmd);
                            break;
                        }

                        case TriggerCommand.TriggerCommandId.UNKNOWN:
                        default:
                            return false;
                    }                    
                }
                else
                {
                    //Long form command, not implemented                    
                    break;
                }
            }

            Command = new CBusTriggerCommand(header, CBusApplicationAddress, triggerCommandList);
            return true;
        }

        protected override byte[] GetRawCommandData()
        {
            int DataLength = 0;
            int TotalComandDataLength = CalculateRawDataLength();
            var Data = new byte[TotalComandDataLength];


            foreach (var cmd in CommandList)
            {
                switch (cmd.Command)
                {
                    case TriggerCommand.TriggerCommandId.UNKNOWN:
                        break;

                    case TriggerCommand.TriggerCommandId.TRIGGER_MIN:
                    case TriggerCommand.TriggerCommandId.TRIGGER_MAX:
                    case TriggerCommand.TriggerCommandId.TRIGGER_KILL:
                        Data[DataLength++] = (byte)cmd.Command;
                        Data[DataLength++] = (byte)cmd.TriggerGroup;
                        break;

                    case TriggerCommand.TriggerCommandId.EVENT:
                        Data[DataLength++] = (byte)cmd.Command;
                        Data[DataLength++] = (byte)cmd.TriggerGroup;
                        Data[DataLength++] = (byte)cmd.Action;
                        break;
                }
            }

            System.Diagnostics.Debug.Assert(TotalComandDataLength == DataLength, "Calculated and Actual Message Datalengths Vary!");
            return Data;
        }


        int CalculateRawDataLength()
        {
            int TotalComandDataLength = 1;
            foreach (var cmd in CommandList)
            {
                switch (cmd.Command)
                {
                    case TriggerCommand.TriggerCommandId.UNKNOWN:
                        break;

                    case TriggerCommand.TriggerCommandId.TRIGGER_MIN:
                    case TriggerCommand.TriggerCommandId.TRIGGER_MAX:
                    case TriggerCommand.TriggerCommandId.TRIGGER_KILL:
                        TotalComandDataLength += 1;
                        break;

                    case TriggerCommand.TriggerCommandId.EVENT:
                        TotalComandDataLength += 2;
                        break;
                }
            }
            return TotalComandDataLength;
        }

        public override string ToString()
        {
            return string.Format("CBus Command Type:{0}, Command Count:{1}", ApplicationType, this.CommandList.Count);
        }
    }
}
