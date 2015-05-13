using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllegroTech.CBus4Net.Protocol
{
    public class ResetCommand : ICBusCommand
    {
        public ResetCommand()
        {
        }

        public string ToCBusString()
        {
            return Protocol.CBusProtcol.MODE_RESET_CHAR.ToString();
        }

    }

    /// <summary>
    /// CBus Device Management Command, Common Application Language (CAL) Command
    /// </summary>
    /// <remarks>
    /// Command Format:
    ///  @A3pp00vv(CR)
    ///  --pp parameter number
    ///  --vv paramater value to set
    ///  --cc checksum
    /// </remarks>
    public class CBusCALCommand : ICBusCommand
    {
        public readonly bool IncludeChecksum;
        readonly byte[] rawCommand;
        readonly CBusProtcol.CBusConfigurationParameters Parameter;

        public CBusCALCommand(CBusProtcol.CBusConfigurationParameters Parameter, byte ParameterValue)
            : this(Parameter, ParameterValue, true)
        {

        }

        public CBusCALCommand(CBusProtcol.CBusConfigurationParameters Parameter, byte ParameterValue, bool IncludeChecksum)
        {
            this.IncludeChecksum = IncludeChecksum;
            this.Parameter = Parameter;
            rawCommand = new byte[] { 0xA3, 0x00, 0x00, 0x00, 0x00 };

            rawCommand[1] = Convert.ToByte(Parameter);
            rawCommand[3] = ParameterValue;
            rawCommand[4] = CBusSALCommand.CalculateChecksum(rawCommand, rawCommand.Length - 1);
        }

        public byte[] ToByteArray()
        {
            return (byte[])rawCommand.Clone();
        }

        public string ToCBusString()
        {
            var sb = new StringBuilder();
            var bytesToProcess = rawCommand.Length;

            //sb.Append(CBusProtcol.STX_CHAR);

            sb.Append(CBusProtcol.DIRECT_COMMAND_ACCESS);
            if (!IncludeChecksum)
                bytesToProcess = rawCommand.Length - 1;

            for (var cnt = 0; cnt < bytesToProcess; ++cnt)
            {
                sb.AppendFormat("{0:X2}", rawCommand[cnt]);
            }

            sb.Append(CBusProtcol.ETX_CHAR_1);
            return sb.ToString();
        }

        public override string ToString()
        {
            switch (Parameter)
            {
                case CBusProtcol.CBusConfigurationParameters.Interface_Options_1:
                    return string.Format("CAL Command [{0}] Value[{1}]", Parameter, (CBusProtcol.Interface_Options_1)rawCommand[3]);

                default:
                    return string.Format("CAL Command [{0}] Value[0x{1:X2}]", Parameter, rawCommand[3]);
            }


        }


    }
}
