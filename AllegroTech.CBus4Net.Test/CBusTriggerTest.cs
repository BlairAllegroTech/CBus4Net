using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace AllegroTech.CBus4Net.Test
{
    public class CBusTriggerTest
    {
        #region TRIGGER COMAMNDS
        [Test]
        public void Test_That_Reference_TriggerCommand_Can_Be_Parsed_Correctly()
        {
            //Take from 'C-Bus Quick Start Guide.pdf' Page 7

            var referenceMessageData = "05CA0002250109\r";

            var CBusAddressMap = new Protocol.CBusApplicationAddressMap();
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.LIGHTING, 0x38);
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.TRIGGER, 0xCA);

            var cbusProtocol = new Protocol.CBusProtcol(128);

            var _state = new Protocol.CBusProtcol.CBusStateMachine();
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
        public void Test_That_Reference_TriggerCommand_Can_Be_Interpreted_Correctly()
        {
            //Take from 'Trigger Control Application.pdf' Page 14
            byte[] referenceMessageData = { 0x05, 0xCA, 0x00, 0x02, 0x25, 0x01, 0x09 };

            var CBusAddressMap = new Protocol.CBusApplicationAddressMap();
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.LIGHTING, 0x38);
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.TRIGGER, 0xCA);

            Protocol.CBusSALCommand cmd;
            Assert.IsTrue(
                CBusAddressMap.TryParseCommand(referenceMessageData, referenceMessageData.Length, false, false, out cmd),
                "Failed to interpret reference lighting command"
                );

            Assert.IsTrue(
                cmd.ApplicationType == Atria.AVControl.Device.CBus.Protocol.CBusProtcol.ApplicationTypes.TRIGGER,
                "Expected Lighting Command Type"
                );
        }
        [Test]
        public void Test_That_Reference_TriggerCommand_Can_Be_Parsed_And_The_Result_Interpreted_Correctly()
        {
            //Take from 'C-Bus Quick Start Guide.pdf' Page 7
            var referenceMessageData = "05CA0002250109\r";
            //var referenceMessageData = "0505CA00020000\r";

            var CBusAddressMap = new Protocol.CBusApplicationAddressMap();
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.LIGHTING, 0x38);
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.TRIGGER, 0xCA);

            var cbusProtocol = new Protocol.CBusProtcol(128);

            var state = new Protocol.CBusProtcol.CBusStateMachine();
            var data = referenceMessageData.ToCharArray();
            int dataIndex = 0;
            Assert.IsTrue(
                    cbusProtocol.TryProcessReceivedBytes(
                        data, data.Length,
                        ref dataIndex, state
                        ),

                    "Failed to parse reference trigger command"
                    );

            var isMonitoredSAL = (state.MessageType == Protocol.CBusProtcol.CBusMessageType.MONITORED_SAL_MESSAGE_RECEIVED);
            Protocol.CBusSALCommand cmd;
            Assert.IsTrue(
                CBusAddressMap.TryParseCommand(state.CommandBytes, state.CommandLength, isMonitoredSAL ,true, out cmd),
                "Failed to interpret reference trigger command"
                );

            Assert.AreEqual(Protocol.CBusProtcol.ApplicationTypes.TRIGGER, cmd.ApplicationType, "Expected Trigger Command Type");

            Console.WriteLine("Parsed Command:{0}", cmd);
            foreach (var c in ((CBus.Protocol.CBusTriggerCommand)cmd).Commands())
            {
                Console.WriteLine("Child Command: **{0}**", c);
            }
        }

        #endregion
        [Test]
        public void Test_That_Command_Can_Be_Processed_When_Recieved_In_Multiple_Parts_From_The_Network()
        {
            //string referenceMessageData = "056438007922C4\r\n";
            string referenceMessageData1 = "05643800";
            string referenceMessageData2 = "7922C4\r\n";

            var CBusAddressMap = new Protocol.CBusApplicationAddressMap();
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.LIGHTING, 0x38);
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.TRIGGER, 0xCA);

            var cbusProtocol = new Protocol.CBusProtcol(128);
 
            var state = new Protocol.CBusProtcol.CBusStateMachine();
            var data = referenceMessageData1.ToCharArray();
            int dataIndex = 0;
            Assert.IsFalse(
                    cbusProtocol.TryProcessReceivedBytes(
                        data, data.Length,
                        ref dataIndex, state
                        ),


                "should not reuturn success when only partial command received"
                );

            //Process another segment of data

            data = referenceMessageData2.ToCharArray();
            dataIndex = 0;
            Assert.IsTrue(
                    cbusProtocol.TryProcessReceivedBytes(
                        data, data.Length,
                        ref dataIndex, state
                        ),

                    "Failed to parse reference lighting command"
                    );

            var isMonitoredSAL = (state.MessageType == Protocol.CBusProtcol.CBusMessageType.MONITORED_SAL_MESSAGE_RECEIVED);
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


        [Test]
        public void Test_Randomly_Selected_TriggerCommands_Can_Be_Parsed_And_The_Result_Interpreted_Correctly(
            [Values(
            "0505CA00020000\r\n",
            "0505CA00020002\r\n"
            )]
            string referenceMessageData
            )
        {
            
            var CBusAddressMap = new Protocol.CBusApplicationAddressMap();
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.LIGHTING, 0x38);
            CBusAddressMap.AddMapping(Protocol.CBusProtcol.ApplicationTypes.TRIGGER, 0xCA);

            var cbusProtocol = new Protocol.CBusProtcol(128);

            var state = new Protocol.CBusProtcol.CBusStateMachine();
            var data = referenceMessageData.ToCharArray();
            int dataIndex = 0;
            Assert.IsTrue(
                    cbusProtocol.TryProcessReceivedBytes(
                        data, data.Length,
                        ref dataIndex, state
                        ),

                    "Failed to parse reference trigger command"
                    );

            var isMonitoredSAL = (state.MessageType == Protocol.CBusProtcol.CBusMessageType.MONITORED_SAL_MESSAGE_RECEIVED);
            Protocol.CBusSALCommand cmd;
            Assert.IsTrue(
                CBusAddressMap.TryParseCommand(state.CommandBytes, state.CommandLength, isMonitoredSAL, false, out cmd),
                "Failed to interpret reference trigger command"
                );

            Assert.AreEqual(Protocol.CBusProtcol.ApplicationTypes.TRIGGER, cmd.ApplicationType, "Expected Trigger Command Type");

            Console.WriteLine("Parsed Command:{0}", cmd);
            foreach (var c in ((CBus.Protocol.CBusTriggerCommand)cmd).Commands())
            {
                Console.WriteLine("Child Command: **{0}**", c);
            }
        }



    }
}
