using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atria.AVControl.Device.CBus.Protocol
{
    public class CBusCALCommandBuilder
    {
        public byte Application1 { get; protected set; }
        public byte Application2 { get; protected set; }
        public CBusProtcol.Interface_Options_1 Options_1 { get; protected set; }
        public CBusProtcol.Interface_Options_3 Options_3 { get; protected set; }

        public CBusCALCommandBuilder()
        {
            Reset();
        }

        public static ResetCommand Build_ResetCommand()
        {
            return new ResetCommand();
        }

        public static CBusCALCommand Build_RegisterApplication1Monitor(byte CBusApplicationId)
        {
            return Set_CAL_Parameter(CBusProtcol.CBusConfigurationParameters.App_Address_1, CBusApplicationId, false);
        }

        public static CBusCALCommand Build_RegisterApplication1Monitor(Protocol.CBusProtcol.CBusApplicationId CBusApplicationId)
        {
            return Set_CAL_Parameter(CBusProtcol.CBusConfigurationParameters.App_Address_1, Convert.ToByte(CBusApplicationId), false);
        }

        public static CBusCALCommand Build_RegisterApplication2Monitor(byte CBusApplicationId)
        {
            return Set_CAL_Parameter(CBusProtcol.CBusConfigurationParameters.App_Address_2, CBusApplicationId, false);
        }

        public static CBusCALCommand Build_RegisterApplication2Monitor(Protocol.CBusProtcol.CBusApplicationId CBusApplicationId)
        {
            return Set_CAL_Parameter(CBusProtcol.CBusConfigurationParameters.App_Address_2, Convert.ToByte(CBusApplicationId), false);
        }

        public static CBusCALCommand Build_SetCAL_Options1(CBusProtcol.Interface_Options_1 Options1)
        {
            return Set_CAL_Parameter(
                CBusProtcol.CBusConfigurationParameters.Interface_Options_1,
                Convert.ToByte(Options1),
                false
                );
        }


        public ResetCommand ResetCommand() 
        {
            Reset();
            return Build_ResetCommand(); 
        }

        private void Reset()
        {
            Application1 = Application2 = 0xFF;
            Options_1 = 0x00;
            Options_3 = 0x00;
        }

        public CBusCALCommand RegisterApplication1Monitor(byte CBusApplicationId)
        {
            Application1 = CBusApplicationId;
            return Build_RegisterApplication1Monitor(CBusApplicationId);
        }

        public CBusCALCommand RegisterApplication2Monitor(byte CBusApplicationId)
        {
            Application2 = CBusApplicationId;
            return Build_RegisterApplication2Monitor(CBusApplicationId);
        }

        public CBusCALCommand Set_CAL_Options1(CBusProtcol.Interface_Options_1 Options1)
        {
            this.Options_1 = Options1;
            return Set_CAL_Parameter(
                CBusProtcol.CBusConfigurationParameters.Interface_Options_1,
                Convert.ToByte(Options1),
                false
                );
        }

        public CBusCALCommand Set_CAL_Options3(CBusProtcol.Interface_Options_3 Options3)
        {
            this.Options_3 = Options3;
            return Set_CAL_Parameter(
                CBusProtcol.CBusConfigurationParameters.Interface_Options_3,
                Convert.ToByte(Options3),
                false
                );
        }

        static CBusCALCommand Set_CAL_Parameter(CBusProtcol.CBusConfigurationParameters Parameter, byte ParameterValue, bool IncludeChecksum)
        {
            return new CBusCALCommand(Parameter, ParameterValue, IncludeChecksum);
        }

       
    }
}
