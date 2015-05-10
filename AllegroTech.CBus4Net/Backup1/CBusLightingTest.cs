using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Atria.AVControl.Device.CBus.Test
{
    public class CBusLightingTests
    {
        [Test]
        public void Test_That_Reference_LightingCommand_Can_Be_Interpreted_Correctly()
        {
            //Take from 'C-Bus Quick Start Guide.pdf' Page 7
            byte[] referenceMessageData = { 0x05, 0x38, 0x00, 0x79, 0x88, 0xC2 };

            var CBusAddressMap = new Protocol.CBusApplicationAddressMap();
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.LIGHTING, 0x38);
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.TRIGGER, 0xCA);

            Protocol.CBusSALCommand cmd;
            Assert.IsTrue(
                CBusAddressMap.TryParseCommand(referenceMessageData, referenceMessageData.Length, false, false, out cmd),
                "Failed to interpret reference lighting command");

            Assert.IsTrue(
                cmd.ApplicationType == Atria.AVControl.Device.CBus.Protocol.CBusProtcol.ApplicationTypes.LIGHTING,
                "Expected Lighting Command Type");
        }

        [Test]
        public void Test_That_Reference_LightingCommand_Can_Be_Parsed_Correctly()
        {
            //Take from 'C-Bus Quick Start Guide.pdf' Page 7
            //var referenceMessageData = "0538007988C2\r";

            var referenceMessageData = "0503380002473D3DFD\r";

            var CBusAddressMap = new Protocol.CBusApplicationAddressMap();
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.LIGHTING, 0x38);
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.TRIGGER, 0xCA);

            var cbusProtocol = new Protocol.CBusProtcol(128);

            var _state = new Protocol.CBusProtcol.CBusStateMachine();
            //int rxDataPointer = 0;
            //int rxCommandLength;
            //byte[] CommandBytes;
            //Protocol.CBusProtcol.CBusMessageType rxMessageType;

            var data = referenceMessageData.ToCharArray();
            int dataIndex = 0;
            Assert.IsTrue(
                    cbusProtocol.TryProcessReceivedBytes(
                        data, data.Length,
                        ref dataIndex, _state
                        ),

                    "Failed to parse reference lighting command"
                    );
        }

        [Test]
        public void Test_That_Reference_LightingCommand_Can_Be_Parsed_And_The_Result_Interpreted_Correctly(
            [Values(
            //"0503380002473D3DFD\r", 
            //"050338000249FF79FD\r", 
            //"050338000248FF7AFD\r",
            //"0503380002462E4DFD\r",

            "050138000A07000A06000A05000A040084\r\n",
            //"056438007922C4\r\n",

            "05013800794679c2793379346F\r\n"
            //"0"
            )]
            string referenceMessageData

            )
        {
            //Take from 'C-Bus Quick Start Guide.pdf' Page 7
            //var referenceMessageData = "0538007988C2\r";


            //05 - Long Form Reply
            //03 - Unit/Bridge Address
            //38 - Application Address
            //00 - Network 
            //SAL Data (02,47,3D,3D)
            //FD - Checksum

            //SAL Data
            //02 - Ramp Instant (Short Form)
            //47 - Group Address
            //3D - Level
            //var referenceMessageData = "0503380002473D3DFD\r";


            var CBusAddressMap = new Protocol.CBusApplicationAddressMap();
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.LIGHTING, 0x38);
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.TRIGGER, 0xCA);

            var cbusProtocol = new Protocol.CBusProtcol(128);


            //int rxDataPointer = 0;
            //int rxCommandLength;
            //byte[] CommandBytes;
            //Protocol.CBusProtcol.CBusMessageType rxMessageType;
            var state = new Protocol.CBusProtcol.CBusStateMachine();
            var data = referenceMessageData.ToCharArray();
            int dataIndex = 0;
            Assert.IsTrue(
                    cbusProtocol.TryProcessReceivedBytes(
                        data, data.Length,
                        ref dataIndex, state
                        ),

                    "Failed to parse reference lighting command"
                    );


            var isMonitoredSAL = (state.MessageType== Protocol.CBusProtcol.CBusMessageType.MONITORED_SAL_MESSAGE_RECEIVED);
            Protocol.CBusSALCommand cmd;
            Assert.IsTrue(
                CBusAddressMap.TryParseCommand(state.CommandBytes, state.CommandLength, isMonitoredSAL, false, out cmd),
                "Failed to interpret reference lighting command"
                );

            Assert.AreEqual(Protocol.CBusProtcol.ApplicationTypes.LIGHTING, cmd.ApplicationType, "Expected lighting Command Type");

            Console.WriteLine("Parsed Command:{0}", cmd);
            foreach (var c in ((CBus.Protocol.CBusLightingCommand)cmd).Commands())
            {
                Console.WriteLine("Child Command: **{0}**", c);
            }
        }
    }
}
