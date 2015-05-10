using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllegroTech.CBus4Net.Protocol
{
    public class CBusApplicationAddressMap
    {
        class Mapping
        {
            public readonly CBusProtcol.ApplicationTypes ApplicationType;
            public readonly byte Address;

            public Mapping(CBusProtcol.ApplicationTypes ApplicationType, byte Address)
            {
                this.ApplicationType = ApplicationType;
                this.Address = Address;                                
            }            

            public override int GetHashCode()
            {
                return (Convert.ToInt32(ApplicationType) * 256) + Address;
            }
        }

        HashSet<Mapping> AddressMap = new HashSet<Mapping>();

        public void AddMapping(CBusProtcol.ApplicationTypes ApplicationType, byte Address)
        {
            var findByAddress = AddressMap.Where(m => m.Address == Address);

            var mappingAlreadyAdded = false;
            foreach (var mapping in findByAddress)
            {
                if (mapping.ApplicationType != ApplicationType)
                    throw new ApplicationException("Duplicate Addresses for Different Application Types Detected");

                if (mapping.Address == Address && mapping.ApplicationType == ApplicationType)
                    mappingAlreadyAdded = true;
            }


            if (!mappingAlreadyAdded)
                AddressMap.Add(new Mapping(ApplicationType, Address));
        }

        [Obsolete("Not realy required any more", true)]
        bool TryGetApplicationAddress(CBusProtcol.ApplicationTypes ApplicationType, out byte Address)
        {
            var mapping = AddressMap.SingleOrDefault(x => x.ApplicationType == ApplicationType);
            if (mapping != null)
            {
                Address = mapping.Address;
                return true;
            }
            else
            {
                Address = 0;
                return false;
            }
        }

        public IEnumerable<byte> EnumerateMappings()
        {
            foreach (var item in AddressMap)
                yield return item.Address;
        }

        /// <summary>
        /// Expects CBus message bytes with ETX and STX stripped and check sum already verified
        /// </summary>
        /// <param name="CommandBytes"></param>
        /// <param name="Command"></param>
        /// <returns></returns>
        public bool TryParseCommand(byte[] CommandBytes, int CommandLength, bool IsMonitoredSAL, bool IsShortFormMessage, out CBusSALCommand Command)
        {
            int dataPointer;
            byte CBusApplicationAddress;

            if (CBusSALCommand.TryParseApplicationId(CommandBytes, CommandLength, IsMonitoredSAL, IsShortFormMessage, out dataPointer, out CBusApplicationAddress))
            {
                //var appAddress = IsShortFormMessage ? CommandBytes[1] : CommandBytes[2];

                var maping = AddressMap.SingleOrDefault(m => m.Address == CBusApplicationAddress);
                if (maping != null)
                {
                    switch (maping.ApplicationType)
                    {
                        case CBusProtcol.ApplicationTypes.LIGHTING:
                            {
                                CBusLightingCommand lightingCommand;
                                if (CBusLightingCommand.TryParseReply(CommandBytes, CommandLength, IsShortFormMessage, CBusApplicationAddress, ref dataPointer, out lightingCommand))
                                {
                                    Command = lightingCommand;
                                    return true;
                                }
                                break;
                            }

                        case CBusProtcol.ApplicationTypes.TRIGGER:
                            {
                                CBusTriggerCommand triggerCommand;
                                if (CBusTriggerCommand.TryParse(CommandBytes, CommandLength, IsShortFormMessage, CBusApplicationAddress, ref dataPointer, out triggerCommand))
                                {
                                    Command = triggerCommand;
                                    return true;
                                }
                                break;
                            }

                        default:
                            break;
                    }
                }
                else
                {
                    //Valid message but unknown application type

                }
            }
            else
            {
                //Could not get application address
            }

            Command = null;
            return false;
        }

    }
}
