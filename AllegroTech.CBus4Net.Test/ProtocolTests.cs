using System;
using System.Collections.Generic;

using Xunit;

namespace AllegroTech.CBus4Net.Test
{
    /// <summary>
    /// There are still more CBus addressing issues to resolve
    /// For bridging etc....
    /// </summary> 
    [Trait("CBus", "Protocol")]
    public class CBusProtocolTests
    {        
        [Fact(DisplayName="Test_CheckSum_Calculation")]
        public void Test_CheckSum_Calculation()
        {
            //Take from 'C-Bus Quick Start Guide.pdf' Page 7
            byte[] referenceMessageData = {0x05, 0x38, 0x00, 0x79, 0x88};

            byte expectedChecksum = 0xC2;
            byte calculatedChecksum = Protocol.CBusSALCommand.CalculateChecksum(referenceMessageData, referenceMessageData.Length);

            Assert.Equal(
                expectedChecksum, 
                calculatedChecksum);
            //, "Expected Calculated and Reference check sum to be equal!");
        }

        [Fact(DisplayName = "Test_That_Message_CheckSum_Evalutates_To_Zero")]
        public void Test_That_Message_CheckSum_Evalutates_To_Zero()
        {
            //Take from 'C-Bus Quick Start Guide.pdf' Page 7
            byte[] referenceMessageData = { 0x05, 0x38, 0x00, 0x79, 0x88, 0xC2 };

            byte expectedChecksum = 0;
            byte calculatedChecksum = Protocol.CBusSALCommand.CalculateChecksum(referenceMessageData, referenceMessageData.Length);

            Assert.Equal(
                expectedChecksum, 
                calculatedChecksum);
            //"Check sum validadation failed 0 expected!");
        }
    }    
}
