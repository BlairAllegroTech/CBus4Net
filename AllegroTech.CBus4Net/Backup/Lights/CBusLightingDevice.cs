using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

using System.Threading;

namespace Atria.AVControl.Device.CBus
{   
    using Tools;
    using Interface.Common;
    using Atria.AVControl.Command.Device;
    using Protocol;

    [MetaData.DeviceClassificationAttribute(MetaData.DeviceClassification.LIGHTING)]
    public class CBusLightingDevice : BaseThreadedDevice
    {
        public List<byte> _ActiveApplications = new List<byte>();
        class PresetMap
        {
            public enum CommandType
            {
                Lighting,
                Trigger
            }

            public string PresetName { get; protected set; }
            public byte CBusApplicationId { get; protected set; }
            public byte CBusGroupAddress { get; protected set; }
            public byte CBusLevelOrAction { get; protected set; }
            public string Description { get; protected set; }
            public PresetMap.CommandType CBusCommandType { get; set; }

            public bool? Active { get; set; }

            public PresetMap(string PresetName, string Description, byte CBusApplicationId, byte CBusLightGroupAddress)
            {
                this.PresetName = PresetName.ToUpperInvariant();
                this.Description = Description;

                this.CBusApplicationId = CBusApplicationId;
                this.CBusGroupAddress = CBusLightGroupAddress;
                if (this.CBusApplicationId == (byte)CBusProtcol.CBusApplicationId.TRIGGER)
                {
                    this.CBusCommandType = PresetMap.CommandType.Trigger;
                    this.CBusLevelOrAction = 0xFF; // Default action is All
                }
                else
                {
                    if ((this.CBusApplicationId >= (byte)CBusProtcol.CBusApplicationId.LIGHTING_BASE) && (this.CBusApplicationId <= (byte)CBusProtcol.CBusApplicationId.LIGHTING_MAX))
                    {
                        this.CBusCommandType = PresetMap.CommandType.Lighting;
                        this.CBusLevelOrAction = 100; // Default On at 100%
                    }
                }
            }


            public override string ToString()
            {
                return Description;
            }

            
        }
                
        readonly Protocol.CBusApplicationAddressMap _CBusAddressMap;

        readonly Tools.Interface.ILightingDefinition _LightingDefinition;
        readonly Interface.Communication.ICommunicationChannel _CommunicationChannel;
        readonly ThreadSafeQueue<ICommand> _commandQueue = new ThreadSafeQueue<ICommand>();
        readonly IEnumerable<PresetMap> _presetMap;
        readonly CBusCALCommandBuilder _configBuilder;

        public CBusLightingDevice(string Address, Tools.Interface.ILightingDefinition LightingDefinition, Interface.Communication.ICommunicationChannel CommunicationChannel, byte CBusLightControlApplication, byte CBusPresetApplication)
            : base(Address)
        {
            _ActiveApplications.Add((byte)CBusProtcol.CBusApplicationId.LIGHTING_DEFAULT);
            _ActiveApplications.Add((byte)CBusProtcol.CBusApplicationId.TRIGGER);
            _LightingDefinition = LightingDefinition;
            _CommunicationChannel = CommunicationChannel;
            if (!_ActiveApplications.Contains(CBusLightControlApplication))
                _ActiveApplications.Add(CBusLightControlApplication);
            if (!_ActiveApplications.Contains(CBusPresetApplication))
                _ActiveApplications.Add(CBusPresetApplication);
            _configBuilder = new CBusCALCommandBuilder();

            _CBusAddressMap = new CBusApplicationAddressMap();

            var presetMapTemp = new List<PresetMap>();
            foreach (var item in LightingDefinition.PresetMapping)
            {
                byte applicationID = 0;
                byte groupAddress = 0;
                byte triggerGroupAddress = 0;

                bool overrideDefaultApplication = (item.Application.TryParseHexOrDecimal(out applicationID));
                bool lightGroupSpecified = (item.PresetData.TryParseHexOrDecimal(out groupAddress));
                bool triggerGroupSpecified = (item.PresetValue.TryParseHexOrDecimal(out triggerGroupAddress));

                if ((!overrideDefaultApplication) || (applicationID == 0))
                {
                    if (lightGroupSpecified)
                        applicationID = (byte)CBusProtcol.CBusApplicationId.LIGHTING_DEFAULT;
                    else if (triggerGroupSpecified)
                        applicationID = (byte)CBusProtcol.CBusApplicationId.TRIGGER;
                }
                if (applicationID != 0)
                {
                    if (!_ActiveApplications.Contains(applicationID))
                        _ActiveApplications.Add(applicationID);
                }
                if (lightGroupSpecified || triggerGroupSpecified)
                {
                    if (applicationID == ((byte)CBusProtcol.CBusApplicationId.TRIGGER))
                    {
                        if (!triggerGroupSpecified)
                            triggerGroupAddress = groupAddress;
                        presetMapTemp.Add(
                            new PresetMap(
                                item.PresetName, 
                                item.Description, 
                                applicationID, 
                                triggerGroupAddress)
                        );
                        _CBusAddressMap.AddMapping(CBusProtcol.ApplicationTypes.TRIGGER, applicationID);
                    }
                    else if ((applicationID >= ((byte)CBusProtcol.CBusApplicationId.LIGHTING_BASE) && (applicationID <= ((byte)CBusProtcol.CBusApplicationId.LIGHTING_MAX))))
                    {
                        if (!lightGroupSpecified)
                            groupAddress = triggerGroupAddress;
                        presetMapTemp.Add(
                            new PresetMap(
                                item.PresetName, 
                                item.Description, 
                                applicationID, 
                                groupAddress)
                        );
                        _CBusAddressMap.AddMapping(CBusProtcol.ApplicationTypes.LIGHTING, applicationID);
                    }
                }
            }

            _presetMap = presetMapTemp;
        }

        public CBusLightingDevice(string Address, Tools.Interface.ILightingDefinition LightingDefinition, Interface.Communication.ICommunicationChannel CommunicationChannel)
            : this(
                Address, 
                LightingDefinition, 
                CommunicationChannel, 
                Convert.ToByte(CBus.Protocol.CBusProtcol.CBusApplicationId.LIGHTING_DEFAULT), 
                Convert.ToByte(CBus.Protocol.CBusProtcol.CBusApplicationId.TRIGGER)
            )
        {
        }


        protected override BaseThreadedDevice.ThreadContext GetThreadContext()
        {
            return new BaseThreadedDevice.ThreadContext("CBus Lighting", Address);
        }

        private bool PerformLogon(Interface.Communication.ICommunicationChannel communicationChannel)
        {

            var rxBuffer = new byte[128];
            string rxString = string.Empty;
            var resetCount = 3;
            var resetSuccess = false;
            do
            {
                ICBusCommand calCommand = _configBuilder.ResetCommand();
                Logger.DebugFormat("TX {0}:{1}", calCommand, calCommand.ToCBusString());

                CBus.Protocol.CBusProtcol.SendCommand(communicationChannel, calCommand);
                CBus.Protocol.CBusProtcol.SendCommand(communicationChannel, calCommand);


                //Wait for response
                Thread.Sleep(200);

                var rxLen = communicationChannel.ReceiveBytes(rxBuffer, rxBuffer.Length);
                rxString = GetReceivedString(rxBuffer, rxLen);
                Logger.DebugFormat("RX:{0}", rxString);
                resetCount--;
                resetSuccess = rxString.Contains(CBus.Protocol.CBusProtcol.MODE_RESET_CHAR);
            } while (!resetSuccess && resetCount > 0);

            if (resetSuccess)
            {
                _ActiveApplications.Sort();
                for (int i = 0; i < _ActiveApplications.Count(); i++)
                {
                    var calCommand_App = _configBuilder.RegisterApplication1Monitor(_ActiveApplications[i]);
                    if (!DoCALCommsTransaction(communicationChannel, calCommand_App))
                        return false;
                }
                var calCommand = _configBuilder.Set_CAL_Options3(CBusProtcol.Interface_Options_3.LOCAL_SAL);
                if (!DoCALCommsTransaction(communicationChannel, calCommand))
                    return false;

                //See 4.3.3.1 CAL Reply, to under stand response (Long Form response as SMART selected)
                //Tx:@A3300059
                //Response: 86FAFA0032300024
                calCommand = _configBuilder.Set_CAL_Options1(
                    CBusProtcol.Interface_Options_1.CONNECT |
                    CBusProtcol.Interface_Options_1.SRCHK |
                    CBusProtcol.Interface_Options_1.SMART |
                    CBusProtcol.Interface_Options_1.IDIOM
                    );

                if (!DoCALCommsTransaction(communicationChannel, calCommand))
                    return false;


                return true;
            }


            return false;
        }

        /// <summary>
        /// Do CAL Management  comms transaction, expecting local echo
        /// </summary>
        /// <param name="communicationChannel"></param>
        /// <param name="calCommand"></param>
        /// <returns></returns>
        bool DoCALCommsTransaction(Interface.Communication.ICommunicationChannel communicationChannel, ICBusCommand calCommand)
        {
            var rxBuffer = new byte[128];
            var commandString = calCommand.ToCBusString();
            Logger.DebugFormat("TX {0}:{1}", calCommand, commandString);
            CBus.Protocol.CBusProtcol.SendCommand(communicationChannel, calCommand);

            Thread.Sleep(200);

            var rxLen = communicationChannel.ReceiveBytes(rxBuffer, rxBuffer.Length);
            var rxDataString = GetReceivedString(rxBuffer, rxLen);

            if(rxDataString.StartsWith(commandString))
            {
                Logger.DebugFormat("RX:{0}", rxDataString.Substring(commandString.Length));
                return true;
            }
            else
            {
                Logger.WarnFormat("RX:{0}", rxDataString);
                return false;
            }
            
        }



        protected override void OnThreadEntry(object ThreadContext)
        {
            var context = ThreadContext as BaseThreadedDevice.ThreadContext;
            var protocol = new CBusProtcol(256);
            var rxBuffer = new byte[256];

            var keepAliveTimeout = DateTime.Now;
            var keepAliveInterval = TimeSpan.FromSeconds(10);
            
            CBusSALCommand _pendingCommand=null;

            var _state = new CBusProtcol.CBusStateMachine();

            while (context.KeepRunning)
            {
                var clock = DateTime.Now;
                try
                {
                    if (!_CommunicationChannel.IsOpen)
                    {
                        if (!_CommunicationChannel.Open())
                        {
                            Thread.Sleep(500);
                        }
                        else
                        {
                            if (!PerformLogon(_CommunicationChannel))
                            {
                                Logger.WarnFormat("Logon Failed");
                                _CommunicationChannel.Close();
                            }
                            else
                            {
                                Logger.Info("Logon Success");
                            }
                        }
                    }

                    if (_CommunicationChannel.IsOpen)
                    {
                        #region Process Received Messages
                        int bytesReceived = _CommunicationChannel.ReceiveBytes(rxBuffer, rxBuffer.Length);

                        if (bytesReceived > 0)
                        {
                            var rxCharacterBufferPointer = 0;
                            var receviedCharacters = Protocol.CBusProtcol.CharacterEncoding.GetChars(rxBuffer, 0, bytesReceived);

                            while (rxCharacterBufferPointer < receviedCharacters.Length)
                            {
                                if (protocol.TryProcessReceivedBytes(receviedCharacters, receviedCharacters.Length, ref rxCharacterBufferPointer, _state))
                                {
                                    //Log received messages
                                    if (Logger.IsDebugEnabled)
                                    {
                                        Logger.DebugFormat("State:{0}",_state);
                                        var cmdString = CBusSALCommand.BinaryArrayToHexString(_state.CommandBytes, _state.CommandLength);

                                        Logger.DebugFormat(
                                           "Rx Message:{0}",cmdString);
                                    }

                                    switch (_state.MessageType)
                                    {
                                        case CBusProtcol.CBusMessageType.ACK:
                                            {
                                                if (_state.ACK_Character.HasValue && _pendingCommand!=null && _pendingCommand.AckCharacter.HasValue)
                                                {
                                                    //ACK Received
                                                    if (_state.ACK_Character.Value == _pendingCommand.AckCharacter.Value)
                                                    {
                                                        Logger.DebugFormat("Positive ACK Received");
                                                        if (_pendingCommand != null)
                                                        {
                                                            //When command acked, same as receiving an unsolicited message
                                                            switch (_pendingCommand.ApplicationType)
                                                            {
                                                                case CBusProtcol.ApplicationTypes.LIGHTING:
                                                                    ProcessLightingMessageResponse((CBusLightingCommand)_pendingCommand);
                                                                    break;

                                                                case CBusProtcol.ApplicationTypes.TRIGGER:
                                                                    ProcessTriggerMessageResponse((CBusTriggerCommand)_pendingCommand);
                                                                    break;
                                                            }
                                                            _pendingCommand = null;
                                                        }
                                                    }
                                                }
                                                break;
                                            }

                                        case CBusProtcol.CBusMessageType.NAK:
                                        case CBusProtcol.CBusMessageType.NAK_CORRUPTED:
                                        case CBusProtcol.CBusMessageType.NAK_NO_CLOCK:
                                        case CBusProtcol.CBusMessageType.NAK_MESSAGE_TOO_LONG:
                                            Logger.WarnFormat("NAK Received: {0}", _state.MessageType);
                                            _pendingCommand = null;
                                            break;

                                        case CBusProtcol.CBusMessageType.SAL_MESSAGE_RECEIVED:
                                        case CBusProtcol.CBusMessageType.MONITORED_SAL_MESSAGE_RECEIVED:
                                            {
                                                CBusSALCommand cmd;

                                                var isMonitoredSAL = (_state.MessageType == CBusProtcol.CBusMessageType.MONITORED_SAL_MESSAGE_RECEIVED);
                                                
                                                //In smart mode all messages are received in Long format
                                                var isSmartMode = (_configBuilder.Options_1 & CBusProtcol.Interface_Options_1.SMART)!=0;

                                                if (_CBusAddressMap.TryParseCommand(_state.CommandBytes, _state.CommandLength, isMonitoredSAL, !isSmartMode, out cmd))
                                                {
                                                    switch (cmd.ApplicationType)
                                                    {
                                                        case CBusProtcol.ApplicationTypes.LIGHTING:
                                                        {
                                                            //Process Command
                                                            var lightingCommand = (CBus.Protocol.CBusLightingCommand)cmd;
                                                            ProcessLightingMessageResponse(lightingCommand);
                                                            break;
                                                        }

                                                        case CBusProtcol.ApplicationTypes.TRIGGER:
                                                        {
                                                            var triggerCommand = (CBus.Protocol.CBusTriggerCommand)cmd;
                                                            ProcessTriggerMessageResponse(triggerCommand);
                                                            break;
                                                        }

                                                        default:
                                                            Logger.WarnFormat("Unknown CBus application Type (Discarding), {0}", cmd.ApplicationType);
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    if(Logger.IsWarnEnabled)
                                                    {
                                                        var cmdString = CBusSALCommand.BinaryArrayToHexString(_state.CommandBytes, _state.CommandLength);
                                                        Logger.WarnFormat("Failed to process command, {0}", cmdString);
                                                        Logger.WarnFormat("State:{0}", _state);
                                                    }
                                                }

                                                break;
                                            }
                                    }//END: Switch 
                                   
                                    //Reset state after a positive outcome
                                    _state.Reset();
                                }
                                else
                                {
                                    //No message received
                                }                                
                            }
                        }
                        #endregion


                        #region Tx Command
                        ICommand command = _commandQueue.Dequeue();
                        if (command != null)
                        {
                            if (command is Command.Device.LightingDeviceCommand)
                            {
                                var cmd = (Command.Device.LightingDeviceCommand)command;
                                var preset = _presetMap.SingleOrDefault(x => x.PresetName == cmd.Preset);

                                if (preset != null)
                                {
                                    if (preset.CBusCommandType == PresetMap.CommandType.Lighting)
                                    {
                                        //byte ApplicationLightingAddress = preset.CBusApplicationId;
                                        //if (_CBusAddressMap.TryGetApplicationAddress(CBusProtcol.ApplicationTypes.LIGHTING, out ApplicationLightingAddress))
                                        {
                                            _pendingCommand = new CBus.Protocol.CBusLightingCommand(
                                                CBus.Protocol.CBusSALCommand.CBusHeader.PPM,
                                                preset.CBusApplicationId,
                                                new CBusLightingCommand.LightingCommand(
                                                    cmd.TargetState ?
                                                        CBusLightingCommand.LightingCommand.LightingCommandId.ON :
                                                        CBusLightingCommand.LightingCommand.LightingCommandId.OFF,
                                                    preset.CBusGroupAddress)
                                                );
                                        }
                                    }
                                    else if (preset.CBusCommandType == PresetMap.CommandType.Trigger)
                                    {
                                        //byte ApplicationTriggerAddress = preset.CBusApplicationId;                                        
                                        //if (_CBusAddressMap.TryGetApplicationAddress(CBusProtcol.ApplicationTypes.TRIGGER, out ApplicationTriggerAddress))
                                        {
                                            _pendingCommand = new CBus.Protocol.CBusTriggerCommand(
                                                CBus.Protocol.CBusSALCommand.CBusHeader.PPM,
                                                preset.CBusApplicationId,

                                                new[] { new CBusTriggerCommand.TriggerCommand(preset.CBusGroupAddress, preset.CBusLevelOrAction) }

                                                );
                                        }
                                    }


                                    if (_pendingCommand != null)
                                    {
                                        _pendingCommand.AckCharacter = protocol.NextConfirmationCharacter();

                                        Logger.DebugFormat("TX {0}:{1}", _pendingCommand, _pendingCommand.ToCBusString());
                                        CBusProtcol.SendCommand(_CommunicationChannel, _pendingCommand);
                                        keepAliveTimeout = clock.Add(keepAliveInterval);
                                    }
                                }
                                else
                                {
                                    //Preset not recognised
                                }

                            }
                            else
                            {
                                //Un Supported Command
                            }
                            
                            
                            
                        }
                        #endregion Tx Command
                        else
                        {
                            if (keepAliveTimeout < clock)
                            {
                                keepAliveTimeout = clock.Add(keepAliveInterval);
                                var ping = Protocol.CBusProtcol.CharacterEncoding.GetBytes("\r\n");
                                _CommunicationChannel.SendBytes(ping, ping.Length);
                                Logger.DebugFormat("Lighting Keep Alive Sent");
                            }
                            else
                            {
                                _commandQueue.Handle.WaitOne(500);
                            }
                        }

                    }
                    else
                    {
                        //Comms not open 
                        Thread.Sleep(1000);
                    }
                }
                catch (ThreadInterruptedException)
                {
                    Logger.Debug("Thread Interrupted");
                }
                catch (Exception ex)
                {
                    if (ex is ThreadAbortException)
                        throw;
                    else
                    {
                        Logger.Error("Thread Error", ex);
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        

        

        protected override bool OnProcessCommand(Atria.AVControl.Interface.Common.ICommand Command)
        {
            if (Command is LightingDeviceCommand)
            {
                _commandQueue.Enqueue(Command);
            }

            return true;
        }

        protected override void Dispose(bool IsDisposing)
        {            
            base.Dispose(IsDisposing);

            if (IsDisposing)
                _commandQueue.Dispose();
        }



        void ProcessTriggerMessageResponse(CBusTriggerCommand triggerCommand)
        {
            Logger.InfoFormat("Command:{0}", triggerCommand);

            var PresetStateChangedList = new List<PresetMap>();
            foreach (var subCmd in triggerCommand.Commands())
            {
                //TODO : Process each command
                Logger.InfoFormat("SubCommand:{0}", subCmd);
                var preset = _presetMap.FirstOrDefault(c => c.CBusGroupAddress == subCmd.TriggerGroup && c.CBusLevelOrAction == subCmd.Action);
                if (preset != null)
                {
                    //Triggers are always active
                    preset.Active = true;
                    PresetStateChangedList.Add(preset);
                }
            }

            foreach (var preset in PresetStateChangedList)
            {
                //TODO : Raise and event back to Logic device
                DispatchEventCommand(preset.PresetName, preset.Active.ToString());
            }
        }

        void ProcessLightingMessageResponse(CBusLightingCommand lightingCommand)
        {
            Logger.InfoFormat("Command:{0}", lightingCommand);

            var  PresetStateChangedList = new List<PresetMap>();
            foreach (var subCmd in lightingCommand.Commands())
            {
                //TODO : Process each command
                Logger.InfoFormat("SubCommand:{0}", subCmd);
                var preset = _presetMap.Where(c=>c.CBusGroupAddress == subCmd.Group);

                
                switch (subCmd.Command)
                {
                    case CBusLightingCommand.LightingCommand.LightingCommandId.ON:
                    case CBusLightingCommand.LightingCommand.LightingCommandId.OFF:
                        foreach (var p in preset)
                        {
                            var targetState = (subCmd.Command== CBusLightingCommand.LightingCommand.LightingCommandId.ON);
                            if (!p.Active.HasValue || targetState != p.Active.Value)
                            {
                                p.Active = targetState;
                                PresetStateChangedList.Add(p);
                            }
                        }
                        break;

                    case CBusLightingCommand.LightingCommand.LightingCommandId.RAMP_TERMINATE:
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
                    {
                        foreach (var p in preset)
                        {
                            var targetState = (subCmd.RampTargetLevel > 0);
                            if (!p.Active.HasValue || targetState != p.Active.Value)
                            {
                                p.Active = targetState;
                                PresetStateChangedList.Add(p);
                            }
                        }
                        break;
                    }

                }
            }

            foreach(var preset in PresetStateChangedList)
            {
                //TODO : Raise and event back to Logic device
                DispatchEventCommand(preset.PresetName, preset.Active.ToString());
            }
        }

        string GetReceivedString(byte[] buffer, int length)
        {
            if (length > 0)
                return CBus.Protocol.CBusProtcol.CharacterEncoding.GetString(buffer, 0, length);
            else
                return "(Empty)";
        }

        bool DispatchEventCommand(string Preset, string PresetState)
        {
            var dispatchAddress = _commandDispatch as IAddressable;
            if (dispatchAddress != null)
            {
                return DispatchEvent(
                 new Event.StateParameterChangeEventCommand(
                     Address,
                     dispatchAddress.Address,
                     Preset,
                     PresetState
                 ));
            }
            else
            {
                return false;
            }
        }

    }

}

