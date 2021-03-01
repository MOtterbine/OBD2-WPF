using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OS.Communication;
using Windows.Media.Devices;
using Windows.Security.Authentication.Identity.Core;

namespace OS.AutoScanner.Models
{
    public enum DeviceRequestType
    {
        None = -1,
        Connect,
        AllowLongMessages,
        BufferDump,
        DeviceSetDefaults,
        DeviceDescription,
        DeviceReset,
        EchoOff,
        MonitorAll,
        Protocol,
        ProtocolSearch,
        SerialNumber,
        SupplyVoltage,
        OBD2_StatusSinceCodesLastCleared,
        OBD2_FreezeFrameCauseFault,
        OBD2_GetEngineRPM,
        OBD2_GetEngineCoolantTemp,
        OBD2_GetEngineLoad,
        OBD2_GetPIDs,
        OBD2_GetVIN,
        OBD2_FuelLevel,
        OBD2_ShortTermFuelTrimBank1,
        OBD2_LongTermFuelTrimBank1,
        OBD2_ShortTermFuelTrimBank2,
        OBD2_LongTermFuelTrimBank2,
        OBD2_FuelSystemStatus,
        OBD2_WarmUpsSinceDTCCleared,
        OBD2_KmSinceDTCCleared,
        OBD2_GetAmbientTemp,
        OBD2_GetDTCs,
        OBD2_ClearDTCs
    }

    public class PIDCategory
    {
        public PIDCategory()
        {
            this.IsSupported = false;
            this.Description = this.Code.ToString();
        }
        public int Code { get; set; }
        public ulong BitMask { get; set; }
        public string Description { get; set; }
        public bool IsSupported { get; set; }
    }

    public class OBD2Device
    {
        private ICommunicationDevice _comDevice = null;
        private bool _scanningPorts = false;

        public OBD2Device(ICommunicationDevice comDevice)
        {
            if (comDevice == null) throw new ArgumentNullException("comDevice");
            this._comDevice = comDevice;
            //      this._ELM327Commands.Add("DP", new ELM327Command { Code = "DP", Name = "Get Protocol" });
        }
        public event DeviceEvent CommunicationEvent
        {
            add
            {
                this._comDevice.CommunicationEvent += value;
            }
            remove
            {
                this._comDevice.CommunicationEvent -= value;
            }
        }
        public bool IsOpen
        {
            get
            {
                if (this._comDevice == null) return false;
                return this._comDevice.IsConnected;
            }
        }
        public bool Open()
        {
            return this._comDevice.Open();
        }
        public string MessageString
        {
            get { return this._comDevice.MessageString; }
        }
        public void Close()
        {
            if (this._comDevice == null) return;
            this._comDevice.Close();

        }

        public bool Send(string data)
        {
            if (this._comDevice == null || !this._comDevice.IsConnected) return false;
            return this._comDevice.Send(data);
        }


        public static readonly Dictionary<DeviceRequestType, ELM327Command> ELM327CommandDictionary = new Dictionary<DeviceRequestType, ELM327Command>
        {
            {DeviceRequestType.OBD2_ClearDTCs, new ELM327Command{ Code = "0400", Name = "Clear DTCs", Description = "Clear OBD2 DTCs", RequestType = DeviceRequestType.OBD2_ClearDTCs } },
            {DeviceRequestType.DeviceReset,  new ELM327Command{ Code = "ATZ", Name = "Reset Device", Description = "Resets the device", RequestType = DeviceRequestType.DeviceReset } },
            {DeviceRequestType.Protocol, new ELM327Command{ Code = "ATDP", Name = "Get Protocol", Description = "Gets the currently active OBD2 protocol", RequestType = DeviceRequestType.Protocol } },
            {DeviceRequestType.DeviceDescription, new ELM327Command{ Code = "AT@1", Name = "Get Description", Description = "Gets a description from the device", RequestType = DeviceRequestType.DeviceDescription } },
            {DeviceRequestType.AllowLongMessages, new ELM327Command{ Code = "ATAL", Name = "Allow Long Messages", Description = "Allows messages longer than 7 bytes", RequestType = DeviceRequestType.AllowLongMessages } },
            {DeviceRequestType.BufferDump, new ELM327Command{ Code = "ATBD", Name = "Buffer Dump", Description = "Perform a buffer dump", RequestType = DeviceRequestType.BufferDump } },
            {DeviceRequestType.SerialNumber, new ELM327Command{ Code = "AT@2", Name = "Get Serial Number", Description = "Gets the permanent serial number (if programmed)", RequestType = DeviceRequestType.SerialNumber } },
            {DeviceRequestType.MonitorAll, new ELM327Command{ Code = "ATMA", Name = "Monitor All", Description = "Monitors data flow", RequestType = DeviceRequestType.MonitorAll } },
            {DeviceRequestType.SupplyVoltage, new ELM327Command{ Code = "ATRV", Name = "Get Voltage", Description = "Gets the voltage of the connected vehicle", RequestType = DeviceRequestType.SupplyVoltage } },
            {DeviceRequestType.ProtocolSearch, new ELM327Command{ Code = "ATSP0", Name = "Search for protocol", Description = "Sets device to search for appropriate protocol with vehicle", RequestType = DeviceRequestType.ProtocolSearch } },
            {DeviceRequestType.EchoOff, new ELM327Command{ Code = "ATE0", Name = "Set Echo Off", Description = "Turns off response echo" } },
            {DeviceRequestType.OBD2_GetPIDs, new ELM327Command{ Code = "0100", Name = "Report PIDs", Description = "Ask vehicle for supported PIDS", RequestType = DeviceRequestType.OBD2_GetPIDs } },
            {DeviceRequestType.OBD2_StatusSinceCodesLastCleared, new ELM327Command{ Code = "0101", Name = "Status Since Last Fault Clearing", Description = "Gets status since codes last cleared", RequestType = DeviceRequestType.OBD2_StatusSinceCodesLastCleared } },
            {DeviceRequestType.OBD2_FreezeFrameCauseFault, new ELM327Command{ Code = "0102", Name = "Freeze Frame Cause Fault", Description = "Gets freeze frame cause fault", RequestType = DeviceRequestType.OBD2_FreezeFrameCauseFault } },
            {DeviceRequestType.OBD2_FuelSystemStatus, new ELM327Command{ Code = "0103", Name = "Fuel System Status", Description = "Gets vehicle fuel system status", RequestType = DeviceRequestType.OBD2_FuelSystemStatus } },
            {DeviceRequestType.OBD2_GetEngineLoad, new ELM327Command{ Code = "0104", Name = "Engine Load Calculated %", Description = "Gets vehicle engine calculated load %", RequestType = DeviceRequestType.OBD2_GetEngineLoad,
                                function = (str)=>
                                {
                                    return (int.Parse(str[2], System.Globalization.NumberStyles.HexNumber) * 100) / 255;
                                } } },
            {DeviceRequestType.OBD2_GetEngineCoolantTemp, new ELM327Command{ Code = "0105", Name = "Get Coolant Temperature", Description = "Ask vehicle for cooant temperature", RequestType = DeviceRequestType.OBD2_GetEngineCoolantTemp,
                                function = (str)=>
                                {
                                    return ((int.Parse(str[2], System.Globalization.NumberStyles.HexNumber) - 40) * 1.8) + 32;
                                } } },
            {DeviceRequestType.OBD2_ShortTermFuelTrimBank1, new ELM327Command{ Code = "0106", Name = "Short-Term FT B1 %", Description = "Short-term fuel trim for bank 1 (%)", RequestType = DeviceRequestType.OBD2_ShortTermFuelTrimBank1,
                                function = (str)=>
                                {
                                    return (int.Parse(str[2], System.Globalization.NumberStyles.HexNumber) * 100) / 255;
                                } } },
            {DeviceRequestType.OBD2_LongTermFuelTrimBank1, new ELM327Command{ Code = "0107", Name = "Long-Term FT B1 %", Description = "Long-term fuel trim for bank 1 (%)", RequestType = DeviceRequestType.OBD2_LongTermFuelTrimBank1,
                                function = (str)=>
                                {
                                    return (int.Parse(str[2], System.Globalization.NumberStyles.HexNumber) * 100) / 255;
                                } } },
            {DeviceRequestType.OBD2_ShortTermFuelTrimBank2, new ELM327Command{ Code = "0108", Name = "Short-Term FT B2 %", Description = "Short-term fuel trim for bank 2 (%)", RequestType = DeviceRequestType.OBD2_ShortTermFuelTrimBank2,
                                function = (str)=>
                                {
                                    return (int.Parse(str[2], System.Globalization.NumberStyles.HexNumber) * 100) / 255;
                                } } },
            {DeviceRequestType.OBD2_LongTermFuelTrimBank2, new ELM327Command{ Code = "0109", Name = "Long-Term FT B2 %", Description = "Long-term fuel trim for bank 2 (%)", RequestType = DeviceRequestType.OBD2_LongTermFuelTrimBank2,
                                function = (str)=>
                                {
                                    return (int.Parse(str[2], System.Globalization.NumberStyles.HexNumber) * 100) / 255;
                                } } },
            {DeviceRequestType.OBD2_GetAmbientTemp, new ELM327Command{ Code = "0146", Name = "Get Ambient Temperature", Description = "Ask vehicle for ambient air temperature", RequestType = DeviceRequestType.OBD2_GetAmbientTemp,
                                function = (str)=>
                                {
                                    return ((int.Parse(str[2], System.Globalization.NumberStyles.HexNumber) - 40) * 1.8) + 32;
                                } } },
            {DeviceRequestType.OBD2_GetEngineRPM, new ELM327Command{ Code = "010C", Name = "Get Engine RPM", Description = "Ask vehicle for Engine RPM", RequestType = DeviceRequestType.OBD2_GetEngineRPM,
                                function = (str)=>
                                {
                                    return int.Parse(str[2] + str[3], System.Globalization.NumberStyles.HexNumber) / 4;
                                } } },
            {DeviceRequestType.OBD2_FuelLevel, new ELM327Command{ Code = "012F", Name = "Fuel Level %", Description = "Ask vehicle for fuel level in percent", RequestType = DeviceRequestType.OBD2_FuelLevel ,
                                function = (str)=>
                                { 
                                    return (int.Parse(str[2], System.Globalization.NumberStyles.HexNumber) * 100) / 255; 
                                } } },
            {DeviceRequestType.OBD2_WarmUpsSinceDTCCleared, new ELM327Command{ Code = "0130", Name = "Warmups since DTC Cleared", Description = "Ask vehicle for number of warmups since DTC Cleared", RequestType = DeviceRequestType.OBD2_WarmUpsSinceDTCCleared } },
            {DeviceRequestType.OBD2_KmSinceDTCCleared, new ELM327Command{ Code = "0131", Name = "Miles since DTC Cleared", Description = "Ask vehicle for distance in Km since DTC Cleared", RequestType = DeviceRequestType.OBD2_KmSinceDTCCleared } },
            {DeviceRequestType.OBD2_GetDTCs, new ELM327Command{ Code = "0300", Name = "Get Fault Codes", Description = "Ask vehicle for error codes", RequestType = DeviceRequestType.OBD2_GetDTCs } },
            {DeviceRequestType.OBD2_GetVIN, new ELM327Command{ Code = "0902", Name = "Get VIN", Description = "Ask vehicle for its Vehice Identification Number (VIN)", RequestType = DeviceRequestType.OBD2_GetVIN,
                                function=(str)=>
                                {
                                    char cBuf;
                                    string lik = "";
                                    foreach (string s in str)
                                    {
                                        if (s.Length < 5) continue;
                                        lik += s.Substring(3);
                                    }
                                    str = lik.Substring(9).Split(' ');
                                    lik = "";
                                    foreach (string s in str)
                                    {
                                        if (string.IsNullOrEmpty(s)) continue;
                                        cBuf = (char)byte.Parse(s, System.Globalization.NumberStyles.HexNumber);
                                        lik += cBuf;
                                    }
                                    return lik.Trim();
                                } } }
        };






        // int is the actual PID value, long is the bit value
        static readonly Dictionary<int, PIDCategory> _OBD2PIDS = new Dictionary<int, PIDCategory>
        {
            {0x01,  new PIDCategory {Code=0x01, BitMask = (ulong)0x80000000, Description = "Current Data" } },
            {0x02,  new PIDCategory {Code=0x02, BitMask = (ulong)0x40000000, Description = "Freeze Frame Data" } },
            {0x03,  new PIDCategory {Code=0x03, BitMask = (ulong)0x20000000, Description = "Stored Codes" } },
            {0x04,  new PIDCategory {Code=0x04, BitMask = (ulong)0x10000000, Description = "Clear Codes" } },
            {0x05,  new PIDCategory {Code=0x05, BitMask = (ulong)0x08000000, Description = "Test Results and 02 Monitoring" } },
            {0x06,  new PIDCategory {Code=0x06, BitMask = (ulong)0x04000000, Description = "Test Results and Other Monitoring" } },
            {0x07,  new PIDCategory {Code=0x07, BitMask = (ulong)0x02000000, Description = "Pending Codes" } },
            {0x08,  new PIDCategory {Code=0x08, BitMask = (ulong)0x01000000, Description = "On-Board System Control" } },
            {0x09,  new PIDCategory {Code=0x09, BitMask = (ulong)0x00800000, Description = "Vehicle Information" } },
            {0x0A,  new PIDCategory {Code=0x0A, BitMask = (ulong)0x00400000, Description = "Permanent Codes" } },
            {0x0B,  new PIDCategory {Code=0x0B, BitMask = (ulong)0x00200000, Description = "x0B" } },
            {0x0C,  new PIDCategory {Code=0x0C, BitMask = (ulong)0x00100000, Description = "x0C" } },
            {0x0D,  new PIDCategory {Code=0x0D, BitMask = (ulong)0x00080000, Description = "x0D" } },
            {0x0E,  new PIDCategory {Code=0x0E, BitMask = (ulong)0x00040000, Description = "x0E" } },
            {0x0F,  new PIDCategory {Code=0x0F, BitMask = (ulong)0x00020000, Description = "x0F" } },
            {0x10,  new PIDCategory {Code=0x10, BitMask = (ulong)0x00010000, Description = "x10" } },
            {0x11,  new PIDCategory {Code=0x11, BitMask = (ulong)0x00008000, Description = "x11" } },
            {0x12,  new PIDCategory {Code=0x12, BitMask = (ulong)0x00004000, Description = "x12" } },
            {0x13,  new PIDCategory {Code=0x13, BitMask = (ulong)0x00002000, Description = "x13" } },
            {0x14,  new PIDCategory {Code=0x14, BitMask = (ulong)0x00001000, Description = "x14" } },
            {0x15,  new PIDCategory {Code=0x15, BitMask = (ulong)0x00000800, Description = "x15" } },
            {0x16,  new PIDCategory {Code=0x16, BitMask = (ulong)0x00000400, Description = "x16" } },
            {0x17,  new PIDCategory {Code=0x17, BitMask = (ulong)0x00000200, Description = "x17" } },
            {0x18,  new PIDCategory {Code=0x18, BitMask = (ulong)0x00000100, Description = "x18" } },
            {0x19,  new PIDCategory {Code=0x19, BitMask = (ulong)0x00000080, Description = "x19" } },
            {0x1A,  new PIDCategory {Code=0x1A, BitMask = (ulong)0x00000040, Description = "x1A" } },
            {0x1B,  new PIDCategory {Code=0x1B, BitMask = (ulong)0x00000020, Description = "x1B" } },
            {0x1C,  new PIDCategory {Code=0x1C, BitMask = (ulong)0x00000010, Description = "x1C" } },
            {0x1D,  new PIDCategory {Code=0x1D, BitMask = (ulong)0x00000008, Description = "x1D" } },
            {0x1E,  new PIDCategory {Code=0x1E, BitMask = (ulong)0x00000004, Description = "x1E" } },
            {0x1F,  new PIDCategory {Code=0x1F, BitMask = (ulong)0x00000002, Description = "x1F" } },
            {0x20,  new PIDCategory {Code=0x20, BitMask = (ulong)0x00000001, Description = "x20" } }
        };

        public static Dictionary<int, PIDCategory> OBD2PIDS
        {
            get
            {
                return _OBD2PIDS;
            }
        }

        public static ICollection<ELM327Command> ELM327Commands
        {
            get
            {
             return ELM327CommandDictionary.Values;
            }
        }




    }
}
