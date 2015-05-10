using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atria.AVControl.Device.CBus.Protocol
{
    public class CBusProtcol
    {
        public enum ApplicationTypes
        {
            LIGHTING,
            TRIGGER,
            //Add more application types as they are implemented here
        }

        /// <summary>
        /// CBus Application Id
        /// </summary>
        /// <see cref="http://training.clipsal.com/downloads/OpenCBus/C-Bus%20Applications.pdf"/>
        public enum CBusApplicationId : byte
        {
            /// <summary>
            /// 56 Decimal Clipsal Lighitng applications are 0x30 thtough 0x5F with 0x38 the "default"
            /// </summary> 
            LIGHTING_BASE = 0x30,
            LIGHTING_DEFAULT = 0x38, // 0x30 through 0x5F
            LIGHTING_MAX = 0x5F,

            /// <summary>
            /// (202 Decimal)
            /// </summary>
            TRIGGER = 0xCA,

            HEATING = 0x88,
            ROOM_CONTROL_SYSTEM = 0x26,
            SECURITY = 0xD0,
            METERING = 0xD1,

            TEMPERATURE_BROADCAST = 0x19,
            NETWORK_MANAGEMENT = 0xFF
        }

        

        public enum CBusConfigurationParameters : byte
        {
            /// <summary>
            /// Application addresses 1 and 2 can be used to define either 1 or 2 Applications to be monitored,
            /// in the range from 0x00 to 0xFE. The MONITOR option needs to be also set to be active.
            /// 
            /// Setting Application 1 address 0xFF acts as a wild card and will respond to all activity, in this mode Application 2 should be set to 0xFF also.
            /// This is the DEFAULT setting
            /// </summary>
            App_Address_1 = 0x21,
            
            /// <summary>
            /// See application Address 1
            /// </summary>
            App_Address_2 = 0x22,

            /// <summary>
            /// Interface Options 1 is used to select various Serial Interface communications options.
            /// </summary>
            Interface_Options_1 = 0x30,

            /// <summary>
            /// Serial Interface Baud selection
            /// </summary>
            Baud_Selector = 0x3D,

            /// <summary>
            /// 10.3.4 Select C-Bus communications options. 
            /// This parameter should not be chnaged by the serial interface and are normally configured 
            /// by the C-Bus installation software.
            /// </summary>
            Interface_Options_2 = 0x3E,
            
            /// <summary>
            /// Same as Options 1 but is copied to options 1 on power up. 
            /// </summary>
            Interface_Options_1_PowerUp = 0x41,

            /// <summary>
            /// 10.3.6 Selects varios serial interface optios.
            /// Can only be set via the serial interface
            /// </summary>
            Interface_Options_3 = 0x42,

            /// <summary>
            /// Reserved range from 0xFB -- 0xF2
            /// </summary>
            Custom_Manufacturer = 0xEB,

            /// <summary>
            /// Reserved Range from 0xF3 -- 0xF6
            /// </summary>
            Serial_Number = 0xF3,

            /// <summary>
            /// Reservved Range from F7 -- FE
            /// </summary>
            Management = 0xF7
        }

        public enum BaudRate : byte
        {
            Baud_9600 = 0xFF,
            Baud_4800 = 0x01,
            Baud_2400 = 0x02,
            
            //Other values should not be used as they are too slow!
        }

        /// <summary>
        /// 10.3.2 Interface Options 1 (Parameter 0x30)
        /// </summary>
        /// <remarks>
        /// Interface Options 1 is used to select various Serial Interface communications options.
        /// On power up, it is loaded from Interface Options Power Up Settings
        /// 
        /// This parameter can only be modified from the serial port.
        /// This parameter is made up of the following bits
        /// </remarks>
        [Flags]
        public enum Interface_Options_1 : byte
        {
            /// <summary>
            /// When set, the serial interface makes a logical connection 
            /// between C-Bus and the RS-232 port, for the Applications 
            /// configured in parameter 0x21 and 0x22 (0x01)
            /// </summary>
            CONNECT     = 0x01,

            /// <summary>
            /// Reserved (0x02)
            /// </summary>
            RESERVED_1    = 0x02,

            /// <summary>
            /// When set, switches on the use of XON/XOFF 
            /// handshaking for serial communications (0x04)
            /// </summary>
            XON_XOF     = 0x04,

            /// <summary>
            /// When set, forces the Serial Interface to expect a checksum 
            /// on all serial communications it receives. (0x08)
            /// </summary>
            SRCHK       = 0x08,

            /// <summary>
            /// When set the serial interface will NOT echo serial data it receives, 
            /// and will include all path information and source addresses in 
            /// monitoring SAL messages and someCAL messages. (0x10)
            /// </summary>
            SMART       = 0x10,

            /// <summary>
            /// When st, the serial interface will relay all status reports 
            /// for Applications matching its parameter numbers 0x21 and 0x22. (0x20)
            /// </summary>
            MONITOR     = 0x20,

            /// <summary>
            /// When set, all messages returned from the serial interface in response 
            /// to a command sent to the serial interface are given a format consistent 
            /// with SMART Mode for all other messages. (0x40)
            /// </summary>
            IDIOM       = 0x40,

            /// <summary>
            /// Reserved (0x08)
            /// </summary>
            RESERVED_2  = 0x80
        }

        /// <summary>
        /// Clipsal recomment setting 'LOCAL_SAL'
        /// </summary>
        [Flags]
        public enum Interface_Options_3 : byte
        {
            /// <summary>
            /// Raise notifications when configurations changed via C-Bus (0x01)
            /// </summary>
            PCN = 0x01,

            /// <summary>
            /// (0x02)
            /// </summary>
            LOCAL_SAL = 0x02,


            /// <summary>
            /// Power up notification, sent when unit power up and ready to accept characters (0x04)
            /// </summary>
            PUN = 0x04,

            /// <summary>
            /// When set (0x08):
            ///  -- Switched Status replies are presented in a format compatable with a Level Status
            ///  -- All status replies are presented in long form with addressing information
            ///  
            /// </summary>
            EXSTAT = 0x08

        }

        /// <summary>
        /// Between the \ and the CR max characters are 45
        /// </summary>
        const int MAX_MESSAGE_LENGTH = 45;

        public const char MODE_RESET_CHAR = '~';

        /// <summary>
        /// This special character can be used to send a Device Management (CAL) command 
        /// directly to the serial interface, bypassing all addressing and irrespective 
        /// of any other options or modes
        /// </summary>
        public const char DIRECT_COMMAND_ACCESS = '@';

        const char CANCEL_CHAR = '?';

        public const char STX_CHAR = '\\';

        /// <summary>
        /// (LF), (0x0A), (\n)
        /// </summary>
        public const char ETX_CHAR_2 = '\n';

        /// <summary>
        /// (CR), (0x0D), (\r)
        /// </summary>
        public const char ETX_CHAR_1 = '\r'; 

        /// <summary>
        /// Positive acknowledgmen
        /// </summary>
        const char ACK_CHAR = '.';
        /// <summary>
        /// Negative acknowledgment – too many retries
        /// </summary>
        const char NAK = '#';
        /// <summary>
        /// Negative acknowledgment – corruption
        /// </summary>
        const char NAK_CORRUPTED = '$';
        /// <summary>
        /// Negative acknowledgment – loss of C-Bus clock
        /// </summary>
        const char NAK_NO_CLOCK = '%';

        /// <summary>
        /// CBus maximum limit exceeded
        /// </summary>
        const char NAK_MSG_TOO_LONG = '\'';


        public const byte ROUTE_HEADER_DIVISOR = 0x09;

        
        public enum CBusMessageType : byte
        {
            /// <summary>
            /// No message received
            /// </summary>
            NONE,

            /// <summary>
            /// ACK (.)
            /// </summary>
            ACK,

            /// <summary>
            /// NAK (#)
            /// </summary>
            NAK,

            /// <summary>
            /// CORRUPTED ($)
            /// </summary>
            NAK_CORRUPTED,

            /// <summary>
            /// NAK_NO_CLOCK (%)
            /// </summary>
            NAK_NO_CLOCK,

            /// <summary>
            /// Max message length exceeded (')
            /// </summary>
            NAK_MESSAGE_TOO_LONG,

            /// <summary>
            /// Complete Monitored SAL message received
            /// </summary>
            MONITORED_SAL_MESSAGE_RECEIVED,

            /// <summary>
            /// Complete SAL message received
            /// </summary>
            SAL_MESSAGE_RECEIVED,



            /// <summary>
            /// Message partialy received pending more data
            /// </summary>
            MESSAGE_PENDING

        }
        
        //int rxBufferPointer;
        //bool STX_Found = false;
        //bool ETX1_Found = false;
        //bool ETX2_Found = false;        
        //public char? ACK_Character = null;

        readonly byte[] rxBuffer;

        int _ConfirmationCharIndex = 0;
        readonly char[] confimationChracterSet;

        

        public static Encoding CharacterEncoding { get { return ASCIIEncoding.ASCII; } }

        public class CBusStateMachine
        {
            internal enum StateMachineState
            {
                NONE,
                ACK_RECEIVED,
                STX_FOUND,
                DATA1, DATA2,                
                MESSAGE_COMPLETE
            }

            internal StateMachineState ParserState;

            

            //public bool STX_Found = false;
            //public bool ETX1_Found = false;
            //public bool ETX2_Found = false;            

            public char? ACK_Character;
            public CBusMessageType MessageType;

            public int BytesProcessed{get; set;}

            //public int DataPointer;
            public int CommandLength;
            public readonly byte[] CommandBytes;

            public byte CalculatedChecksum;
            internal char PreviousRxCharacter;

            public CBusStateMachine()
            {
                CommandBytes = new byte[128];
                Reset();
            }

            public void Reset()
            {
                ParserState = StateMachineState.NONE;
                ACK_Character = null;
                CalculatedChecksum = 0xFF;
                MessageType = CBusMessageType.NONE;
                
                //STX_Found = false;
                //ETX1_Found = ETX2_Found = false;
                //DataPointer = 0;

                CommandLength = 0;                
                PreviousRxCharacter = '\0';
            }

            public override string ToString()
            {
                return string.Format("ParserState:{0}, MessageType:{1}, CheckSum:{2} ACK_Char:{3}, Command Length{4}",
                    ParserState, 
                    MessageType, 
                    CalculatedChecksum, 
                    ACK_Character, 
                    CommandLength
                    );
            }
        }


        public CBusProtcol(int RxBufferSize)
        {
            confimationChracterSet = new char[] { 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'x', 'y', 'z' };
            rxBuffer = new byte[RxBufferSize];
            

            for (int i = 0; i < rxBuffer.Length; ++i)
                rxBuffer[i] = 0x00;
        }

        

        //void Reset()
        //{            
        //    ACK_Character = null;
        //    STX_Found = false;
        //    ETX1_Found = ETX2_Found = false;
        //    rxBufferPointer = 0;
        //}

        /// <summary>
        /// All digits an hex characters are valid
        /// </summary>
        /// <param name="CBusChar"></param>
        /// <returns></returns>
        bool IsValidCharacter(char CBusChar)
        {
            if (char.IsDigit(CBusChar))
                return true;

            var lowerChar = char.ToLower(CBusChar);

            switch (lowerChar)
            {
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                    return true;
            }

            return false;
        }

        public bool TryProcessReceivedBytes(char[] RxData, int rxDataLength, ref int DataPositionPointer, CBusStateMachine state)
        {
            state.BytesProcessed = 0;

            for (int i = DataPositionPointer; i < rxDataLength; ++i)
            {                
                var currentCharacter = RxData[i];
                state.BytesProcessed++;
                DataPositionPointer++;

                var result = ProcessReceivedCharacter(currentCharacter, state);
                state.PreviousRxCharacter = currentCharacter;

                

                if (result)
                {
                    //If we have a valid check sum and message terminated by ETX_CHAR_1 & ETX_CHAR_2 then trat as a monitored SAL
                    if (/*state.CalculatedChecksum == 0 &&*/ state.MessageType == CBusMessageType.SAL_MESSAGE_RECEIVED)
                    {
                        //Has Valid check sum so could be a monitored SAL message
                        if ((i + 1) < rxDataLength)
                        {
                            if (RxData[i + 1] == ETX_CHAR_2)
                            {
                                DataPositionPointer++;
                                state.BytesProcessed++;
                                state.MessageType = CBusMessageType.MONITORED_SAL_MESSAGE_RECEIVED;
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        bool ProcessReceivedCharacter(char currentCharacter, CBusStateMachine state)
        {
            switch (state.ParserState)
            {
                case CBusStateMachine.StateMachineState.MESSAGE_COMPLETE:
                case CBusStateMachine.StateMachineState.NONE:
                    {
                        if (currentCharacter != STX_CHAR)
                        {
                            //Serial interface seems to strip leading '\' character 
                            //so just assume that the STX was received and that this is the first data character.
                            //So the next received character will be data 2
                            if (IsValidCharacter(currentCharacter))
                                state.ParserState = CBusStateMachine.StateMachineState.DATA2;
                            else
                                state.ParserState = CBusStateMachine.StateMachineState.ACK_RECEIVED;
                        }
                        else
                        {
                            state.ParserState = CBusStateMachine.StateMachineState.STX_FOUND;
                        }
                        break;
                    }

                case CBusStateMachine.StateMachineState.ACK_RECEIVED:
                {
                    switch (currentCharacter)
                    {
                        case ACK_CHAR:
                            //Store the ack character...                                
                            state.ACK_Character = state.PreviousRxCharacter;
                            state.MessageType = CBusMessageType.ACK;
                            return true;

                        //NAK Responses
                        case NAK:
                            state.MessageType = CBusMessageType.NAK;
                            return true;

                        case NAK_CORRUPTED:
                            state.MessageType = CBusMessageType.NAK_CORRUPTED;
                            return true;

                        case NAK_NO_CLOCK:
                            state.MessageType = CBusMessageType.NAK_NO_CLOCK;
                            return true;

                        case NAK_MSG_TOO_LONG:
                            state.MessageType = CBusMessageType.NAK_MESSAGE_TOO_LONG;
                            return true;

                        default:
                            //Unexpected character
                            state.Reset();
                            break;

                    }
                    break;
                }

                case CBusStateMachine.StateMachineState.DATA1:
                    {
                        if (currentCharacter == ETX_CHAR_1)
                        {
                            state.ParserState = CBusStateMachine.StateMachineState.MESSAGE_COMPLETE;
                            state.CalculatedChecksum = CBusSALCommand.CalculateChecksum(state.CommandBytes, state.CommandLength);
                            state.MessageType = CBusMessageType.SAL_MESSAGE_RECEIVED;
                            return true;     
                        }
                        else
                        {
                            if (IsValidCharacter(currentCharacter))
                                state.ParserState = CBusStateMachine.StateMachineState.DATA2;
                            else
                                state.Reset();
                        }
                        break;
                    }

                case CBusStateMachine.StateMachineState.DATA2:
                    {
                        var hex = string.Format("{0}{1}", state.PreviousRxCharacter, currentCharacter);

                        byte data;
                        if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out data))
                        {
                            state.CommandBytes[state.CommandLength++] = data;
                            state.ParserState = CBusStateMachine.StateMachineState.DATA1;
                        }
                        else
                        {
                            state.Reset();
                        }

                        break;
                    }
            }

            if (state.ParserState != CBusStateMachine.StateMachineState.NONE)
                state.MessageType = CBusMessageType.MESSAGE_PENDING;

            return false;
        }

        public static void SendCommand(Interface.Communication.ICommunicationChannel CommsChannel, ICBusCommand Command)
        {
            //Send Data, APPEND Telnet Flush characters
            var command = Command.ToCBusString() + "\r\n";            
            var commandBytes = CharacterEncoding.GetBytes(command);
            CommsChannel.SendBytes(commandBytes, commandBytes.Length);            
        }

        public char NextConfirmationCharacter()
        {
            _ConfirmationCharIndex++;
            _ConfirmationCharIndex %= confimationChracterSet.Length;
            return confimationChracterSet[_ConfirmationCharIndex];
        }
    }


    

}
