using System;
using System.ComponentModel;
//using System.ComponentModel.Composition.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OS.Application;
using OS.WPF;
using System.Windows.Input;
using OS.Communication;
using log4net;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using OS.AutoScanner.Views;
using System.Security.Policy;
using Windows.Media.Capture;

namespace OS.AutoScanner.Models
{
    public class AutoScannerViewModel : IAppViewModel, INotifyPropertyChanged
    {
        #region Private members

        protected const string NotReadyString = "NO RESPONSE: Ensure vehicle is on and OBDII device is plugged in";
        protected readonly ILog _logger = LogManager.GetLogger(typeof(AutoScannerViewModel));
        private DeviceEvent deviceEvent = null;
        private int expectedResponseCount = 0;
        private System.Timers.Timer portDiscoveryTimeout = new System.Timers.Timer(8000);
        private OBD2Device _OBD2Device = null;
        private SynchronizationContext syncContext;
        public System.Timers.Timer _timer = new System.Timers.Timer(10);
        private readonly List<DeviceRequestType> _CANInitFunctionList = new List<DeviceRequestType>()
        {
              //DeviceRequestType.MemoryOff,
              DeviceRequestType.CANAutoFormatOn,
              DeviceRequestType.CANAddressMask,
              DeviceRequestType.CANAddressFilter,
              DeviceRequestType.MonitorAll
        };
        private readonly List<DeviceRequestType> _InitFunctionList = new List<DeviceRequestType>()
        {
              DeviceRequestType.MemoryOff,
              DeviceRequestType.EchoOff,
              DeviceRequestType.SpacesOff,
              DeviceRequestType.LineFeedsOff,
              DeviceRequestType.CANAutoAddress,
              DeviceRequestType.AllowLongMessages,
              DeviceRequestType.ForgetEvents,
              DeviceRequestType.ProtocolSearch,
              DeviceRequestType.OBD2_GetPIDs
        };
        public DataPlotModel dataPlotModel { get; private set; }

        private int _initStep = 0;
        private bool _initializingDevice = false;
        private bool _initializingDeviceCAN = false;

        private double _pointIndex = 0;

        public AutoScannerViewModel()
        {
            ActiveCANAddress = "Auto";
            this.dataPlotModel = new DataPlotModel("OBD2 Data");
            // for constant monitoring...
            this._timer.Elapsed += (object source, System.Timers.ElapsedEventArgs elapsed) => {
                this._timer.Stop();
                this._OBD2Device.Send($"{carriageReturn}");
                this.DeviceIsIdle = false;
            };

            DeviceOnOffCommandText = "Connect";
            this.ChannelDisconnected = true;

            CurrentRequestType = DeviceRequestType.None;
            syncContext = SynchronizationContext.Current;
            this._EndpointNames = ELM327.GetPortNames().ToList();
            _SelectedTab = 0;
            deviceEvent = onDeviceEvent;
            epCount = this._EndpointNames.Count();
            this.SelectedCommChannel = Properties.Settings.Default.CommChannel;
            this.UseIPSocket = Properties.Settings.Default.UseIPSocket;
            this.IPAddress = Properties.Settings.Default.IPAddress;
            this.IPPort = Properties.Settings.Default.IPPort;
            this.CANID = Properties.Settings.Default.CanAddress;
            this.CANMASK = Properties.Settings.Default.CanMask;


            UpdateDiagnosticsMainLabel();

            this.ELM327Commands = new System.Collections.ObjectModel.ReadOnlyCollection<ELM327Command>(OBD2Device.ELM327Commands.Where(u => u.IsUserFunction == true).ToList());

            _logger.InfoFormat("Found {0} endpoints on the current machine.", epCount);
            this.SelectedELM327CommandCode = "0300";

            if (this._creationCompleteEvent != null) this._creationCompleteEvent();
        }

        private void UpdateDiagnosticsMainLabel()
        { 
            if(string.IsNullOrEmpty(this.SelectedCommChannel) || !this.UseIPSocket)
            {
                DiagnosticsLabelText = "(no channel selected)";
                HardwareStatus = "";
                
            }
            else
            {
                DiagnosticsLabelText = $"{this.SelectedCommChannel}" ;
            }
        }

        private string _ComChannelName = "";
        public string ComChannelName
        {
            get 
            {
                return this._ComChannelName; 
            }
            set 
            {
                OnPropertyChanged("ComChannelName");
                this._ComChannelName = value;
            }
        }

        private int epIndex = 0;
        private int epCount = 0;
        
        private async Task<bool> CreateDevice(string channelName)
        {
            Task<bool> retTask = Task.Run(() => {
            
                StartButtonEnabled = false;
                expectedResponseCount = 0;
                try
                {
                    if (this.UseIPSocket)
                    {
                        this.ComChannelName = $"{this.IPAddress}:{this.IPPort}";
                        this._OBD2Device = new OBD2Device(new OS.Communication.TCPSocket("192.168.0.10", 35000, ConnectMethods.Client));
                    }
                    else
                    {
                        
                        this.ComChannelName = this.SelectedCommChannel;
                        this._OBD2Device = new OBD2Device(new ELM327(this.ComChannelName, 38400));
                    }
                    HardwareStatus += DiagnosticsLabelText = string.Format("Connecting to {0}...{1}", this.ComChannelName, Environment.NewLine);
                                    }
                catch(Exception e)
                {
                    string it = e.Message;
                }
                this._OBD2Device.CommunicationEvent += this.deviceEvent;
                if (!this._OBD2Device.Open())
                {
                    DeviceIsIdle = true;
                    HardwareStatus += DiagnosticsLabelText = $"Unable to open device - {this._OBD2Device.MessageString}{Environment.NewLine}";
                    syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);
                    return false;
                }
                syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);
                return true;
            
            });
            await retTask;
            return retTask.Result;
        }
        private void PortResponseTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            portDiscoveryTimeout.Stop();
            portDiscoveryTimeout.Elapsed -= PortResponseTimeout_Elapsed;
            if (epIndex < epCount)
            {
                //CloseDevice();
                DiagnosticsLabelText = NotReadyString;
                HardwareStatus += $"{Environment.NewLine}{DiagnosticsLabelText}";

                switch (this.CurrentRequestType)
                {
                    case DeviceRequestType.MonitorAll:
                        // reset can address filters - also stops monitoring
                        this._OBD2Device.Send($"{carriageReturn}");
                      //  this._OBD2Device.Send($"{OBD2Device.ELM327CommandDictionary[DeviceRequestType.CANSetAddressFilters].Code}{carriageReturn}");
                        break;
                }
                this.DeviceIsIdle = true;
                this.IsMonitoring = false;
                //  this.ChannelDisconnected = true;
                syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);
            }
        }
        private System.Text.StringBuilder buildString = new StringBuilder();
        private bool _DeviceEchoOff = false;
        private double plotYVal = 0.0;

        private char[] dataEndTrimChars = new char[] { '\n', '\r', '>' };
        private char carriageReturn = (char)0x0D;

        private void onDeviceEvent(object sender, ChannelEventArgs e)
        {
            string comChannelName = this.ComChannelName;//SelectedCommChannel;

            ICommunicationDevice cDev = sender as ICommunicationDevice;
            if (cDev == null) return;
            switch (e.Event)
            {
                case CommunicationEvents.ConnectedAsClient:
                    portDiscoveryTimeout.Stop();
                    portDiscoveryTimeout.Elapsed -= PortResponseTimeout_Elapsed;
                    HardwareStatus = DiagnosticsLabelText = $"{comChannelName}: Open{Environment.NewLine}";
                    buildString.Clear();
                    DeviceOnOffCommandText = "Disconnect";
                    this.ChannelDisconnected = false;

                    switch(CurrentRequestType)
                    {
                        case DeviceRequestType.None:
                            this._initStep = 0;
                            this._initializingDevice = true;
                            CurrentRequestType = DeviceRequestType.DeviceReset;
                            cDev.Send($"ATZ{carriageReturn}"); // Reset the device
                            this.DeviceIsIdle = false;
                            DiagnosticsLabelText = "Scanning...";
                            HardwareStatus += DiagnosticsLabelText + Environment.NewLine;
                            break;
                    }

                    portDiscoveryTimeout.Elapsed += PortResponseTimeout_Elapsed;
                    portDiscoveryTimeout.Start();
                    expectedResponseCount++;
                    DeviceIsIdle = true;

                    //StartButtonEnabled = true;

                    this._logger.InfoFormat("{0}: Device Connected", comChannelName);

                    break;
                case CommunicationEvents.Receive:
                    buildString.Append(ASCIIEncoding.ASCII.GetString(e.data));
                    break;
                case CommunicationEvents.ReceiveEnd:
                    portDiscoveryTimeout.Stop();
                    portDiscoveryTimeout.Elapsed -= PortResponseTimeout_Elapsed;

                    buildString.Append(ASCIIEncoding.ASCII.GetString(e.data));

                    // all complete OBD2 messages end with '>' character according to ELM327 data sheet
                    if (!buildString.ToString().EndsWith(">")) return;
                    this.DeviceIsIdle = true;
                    syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);


                    string outData = "";
                    string[] parsedData = System.Text.RegularExpressions.Regex.Replace(buildString.ToString().TrimEnd(dataEndTrimChars), "(\r\r\r|\r\r|\r)", "\r").Split('\r');
                    try
                    {
                        outData = parsedData[_DeviceEchoOff ? 0 : 1];
                    }
                    catch(Exception)
                    {
                        return;
                    }

                    //this._logger.InfoFormat("{0}: from device=> {1}", comChannelName, parsedData);
                    //string [] newData = null;


                    if (string.Compare(parsedData[0], "NO DATA", true) == 0)
                    {
                        HardwareStatus = string.Empty;
                        foreach (var str in parsedData)
                        {
                            HardwareStatus += $"{str}{Environment.NewLine}";
                            DiagnosticsLabelText = "No Data";
                        }
                        return;
                    }
                    if (string.Compare(parsedData[0], "CAN ERROR", true) == 0)
                    {
                        HardwareStatus = string.Empty;
                        foreach (var str in parsedData)
                        {
                            HardwareStatus += $"{str}{Environment.NewLine}";
                            DiagnosticsLabelText = "CAN Error";
                        }
                        return;
                    }
                    try
                    {
                        switch (CurrentRequestType)
                        {
                            case DeviceRequestType.None:
                                HardwareStatus = DiagnosticsLabelText = "Idle";
                                break;

                            case DeviceRequestType.OBD2_GetEngineLoad:
                                //newData = parsedData.Split('\r');
                                if (parsedData.Length >= 1)
                                {

                                    this.plotYVal = Convert.ToDouble(OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetEngineLoad].function(parsedData));
                                    HardwareStatus = DiagnosticsLabelText = $"Engine Load: {plotYVal:F2}%";
                                    //HardwareStatus = DiagnosticsLabelText = $"Engine Load: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetEngineLoad].function(newData)}%";
                               
                                    this.dataPlotModel.Title = DiagnosticsLabelText;

                                    this.dataPlotModel.AddDataPoint(this._pointIndex, this.plotYVal);

                                    this._pointIndex += 1;
                                    if (this._pointIndex > this.dataPlotModel.XAxisMaxValue + 1)
                                    {
                                        this.ClearPlotData();
                                    }
                                    dataPlotModel.InvalidatePlot(true);
                                    this.IsMonitoring = true;
                                    buildString.Clear();
                                    this._timer.Start();
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_ShortTermFuelTrimBank1:
                                if (parsedData.Length >= 1)
                                {
                                    //HardwareStatus = DiagnosticsLabelText = $"STFT Bank 1: {Convert.ToDouble(OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_ShortTermFuelTrimBank1].function(parsedData)):F2}%";
                                    if (!this.IsMonitoring)
                                    {
                                        this.dataPlotModel.YAxisMaxValue = 30;
                                        this.dataPlotModel.YAxisMinValue = -30;
                                    }
                                    this.plotYVal = Convert.ToDouble(OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_ShortTermFuelTrimBank1].function(parsedData));
                                    HardwareStatus = DiagnosticsLabelText = $"STFT Bank 1: {plotYVal:F2}%";

                                    this.dataPlotModel.Title = DiagnosticsLabelText;

                                    this.dataPlotModel.AddDataPoint(this._pointIndex, this.plotYVal);

                                    this._pointIndex += 1;
                                    if (this._pointIndex > this.dataPlotModel.XAxisMaxValue + 1)
                                    {
                                        this.ClearPlotData();
                                    }
                                    dataPlotModel.InvalidatePlot(true);
                                    this.IsMonitoring = true;
                                    buildString.Clear();
                                    this._timer.Start();
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }

                                }
                                break;
                            case DeviceRequestType.OBD2_LongTermFuelTrimBank1:
                                if (parsedData.Length >= 1)
                                {
                                    //HardwareStatus = DiagnosticsLabelText = $"LTFT Bank 1: {Convert.ToDouble(OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_LongTermFuelTrimBank1].function(parsedData)):F2}%";
                                    if (!this.IsMonitoring)
                                    {
                                        this.dataPlotModel.YAxisMaxValue = 30;
                                        this.dataPlotModel.YAxisMinValue = -30;
                                    }
                                    this.plotYVal = Convert.ToDouble(OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_LongTermFuelTrimBank1].function(parsedData));
                                    HardwareStatus = DiagnosticsLabelText = $"LTFT Bank 1: {plotYVal:F2}%";

                                    this.dataPlotModel.Title = DiagnosticsLabelText;

                                    this.dataPlotModel.AddDataPoint(this._pointIndex, this.plotYVal);

                                    this._pointIndex += 1;
                                    if (this._pointIndex > this.dataPlotModel.XAxisMaxValue + 1)
                                    {
                                        this.ClearPlotData();
                                    }
                                    dataPlotModel.InvalidatePlot(true);
                                    this.IsMonitoring = true;
                                    buildString.Clear();
                                    this._timer.Start();
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_ShortTermFuelTrimBank2:
                                if (parsedData.Length >= 1)
                                {
                                  //  HardwareStatus = DiagnosticsLabelText = $"STFT Bank 2: {Convert.ToDouble(OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_ShortTermFuelTrimBank2].function(parsedData)):F2}%";
                                    if (!this.IsMonitoring)
                                    {
                                        this.dataPlotModel.YAxisMaxValue = 30;
                                        this.dataPlotModel.YAxisMinValue = -30;
                                    }
                                    this.plotYVal = Convert.ToDouble(OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_ShortTermFuelTrimBank2].function(parsedData));
                                    HardwareStatus = DiagnosticsLabelText = $"STFT Bank 2: {plotYVal:F2}%";

                                    this.dataPlotModel.Title = DiagnosticsLabelText;

                                    this.dataPlotModel.AddDataPoint(this._pointIndex, this.plotYVal);

                                    this._pointIndex += 1;
                                    if (this._pointIndex > this.dataPlotModel.XAxisMaxValue + 1)
                                    {
                                        this.ClearPlotData();
                                    }
                                    dataPlotModel.InvalidatePlot(true);
                                    this.IsMonitoring = true;
                                    buildString.Clear();
                                    this._timer.Start();
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_LongTermFuelTrimBank2:
                                if (parsedData.Length >= 1)
                                {
                                    //HardwareStatus = DiagnosticsLabelText = $"LTFT Bank 2 : {Convert.ToDouble(OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_LongTermFuelTrimBank2].function(parsedData)):F2}%";
                                    if (!this.IsMonitoring)
                                    {
                                        this.dataPlotModel.YAxisMaxValue = 30;
                                        this.dataPlotModel.YAxisMinValue = -30;
                                    }
                                    this.plotYVal = Convert.ToDouble(OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_LongTermFuelTrimBank2].function(parsedData));
                                    HardwareStatus = DiagnosticsLabelText = $"LTFT Bank 2: {plotYVal:F2}%";


                                    this.dataPlotModel.Title = DiagnosticsLabelText;

                                    this.dataPlotModel.AddDataPoint(this._pointIndex, this.plotYVal);

                                    this._pointIndex += 1;
                                    if (this._pointIndex > this.dataPlotModel.XAxisMaxValue + 1)
                                    {
                                        this.ClearPlotData();
                                    }
                                    dataPlotModel.InvalidatePlot(true);
                                    this.IsMonitoring = true;
                                    buildString.Clear();
                                    this._timer.Start();
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_StatusSinceCodesLastCleared:
                               // newData = outData.Split(' ');
                                if (parsedData.Length >= 1)
                                {
                                    HardwareStatus = $"Status since DTCs cleared: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_StatusSinceCodesLastCleared].function(parsedData)}";
                                    DiagnosticsLabelText = "Status since DTCs cleared:(see details tab)";
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_FuelSystemStatus:
                               // newData = outData.Split(' ');
                                if (parsedData.Length == 1)
                                {
                                    HardwareStatus = DiagnosticsLabelText = $"Fuel System: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_FuelSystemStatus].function(parsedData)}";
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_FreezeFrameCauseFault:
                                if (parsedData.Length >= 1)
                                {
                                    HardwareStatus = DiagnosticsLabelText = $"Freeze Frame Fault: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_FreezeFrameCauseFault].function(parsedData)}";
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.SupplyVoltage:
                                HardwareStatus = DiagnosticsLabelText = " Monitoring Supply Voltage";
                                Properties.Settings.Default.CommChannel = this.SelectedCommChannel;

                                syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);

                                this.dataPlotModel.Title = $"Supply Voltage: {outData}";

                                this.plotYVal = double.Parse(outData.Split(new char[] { 'V' })[0]);

                                this.dataPlotModel.AddDataPoint(this._pointIndex, this.plotYVal);

                                this._pointIndex += 1;
                                if (this._pointIndex > this.dataPlotModel.XAxisMaxValue + 1)
                                {
                                    this.ClearPlotData();
                                }
                                dataPlotModel.InvalidatePlot(true);
                                this.IsMonitoring = true;
                                buildString.Clear();
                                this._timer.Start();

                                break;
                            case DeviceRequestType.DeviceDescription:
                                if (this._initializingDevice)
                                    HardwareStatus += "Device: " + outData + Environment.NewLine;
                                else
                                    HardwareStatus = DiagnosticsLabelText = "Device: " + outData + Environment.NewLine;

                                break;

                            case DeviceRequestType.OBD2_ClearDTCs:
                                HardwareStatus = DiagnosticsLabelText = $"{parsedData[parsedData.Length - 1]}{Environment.NewLine}";
                                break;

                            case DeviceRequestType.OBD2_GetPIDs:
                                if (this._initializingDevice)
                                {
                                    DiagnosticsLabelText = "Ready";
                                }
                                else
                                {
                                    try
                                    {
                                        HardwareStatus =  $"Supported PIDS : {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetPIDs].function(parsedData)}";
                                        DiagnosticsLabelText = "Supported PIDs (see details tab)";
                                    }
                                    catch (Exception)
                                    {
                                        // HardwareStatus = DiagnosticsLabelText = NotReadyString;
                                        HardwareStatus = string.Empty;
                                        foreach (var str in parsedData)
                                        {
                                            HardwareStatus += $"{str}{Environment.NewLine}";
                                            DiagnosticsLabelText = NotReadyString;
                                        }
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_KmSinceDTCCleared:
                                //newData = buildString.Split(' ');
                                if (parsedData.Length >= 1)
                                {
                                  //  HardwareStatus = DiagnosticsLabelText = $"Distance traveled since DTCs cleared: {Math.Round(int.Parse(newData[2] + newData[3], System.Globalization.NumberStyles.HexNumber) / 1.60, 0, MidpointRounding.AwayFromZero)} miles";
                                    HardwareStatus = DiagnosticsLabelText = $"Distance traveled since DTCs cleared: {Math.Round((int)OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_KmSinceDTCCleared].function(parsedData) / 1.60, 0, MidpointRounding.AwayFromZero)} miles";
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_KmWithMilOn:
                                //newData = buildString.Split(' ');
                                if (parsedData.Length >= 1)
                                {
                                    //  HardwareStatus = DiagnosticsLabelText = $"Distance traveled since DTCs cleared: {Math.Round(int.Parse(newData[2] + newData[3], System.Globalization.NumberStyles.HexNumber) / 1.60, 0, MidpointRounding.AwayFromZero)} miles";
                                    HardwareStatus = DiagnosticsLabelText = $"Distance traveled with MIL on: {Math.Round((int)OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_KmSinceDTCCleared].function(parsedData) / 1.60, 0, MidpointRounding.AwayFromZero)} miles";
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_FuelLevel:
                                if (parsedData.Length >= 1)
                                {
                                    HardwareStatus = DiagnosticsLabelText = $"Fuel: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_FuelLevel].function(parsedData)}%";
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_WarmUpsSinceDTCCleared:
                                if (parsedData.Length >= 1)
                                {
                                    HardwareStatus = DiagnosticsLabelText = $"Warm-ups since DTC cleared: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_WarmUpsSinceDTCCleared].function(parsedData)} warmups";
                                  //  HardwareStatus = DiagnosticsLabelText = $"Warm-ups since DTC cleared: {int.Parse(parsedData[_DeviceEchoOff ? 2 : 3], System.Globalization.NumberStyles.HexNumber) } warmups";
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_GetEngineCoolantTemp:
                                //newData = buildString.Split(' ');
                                if (parsedData.Length >= 1)
                                {
                                    HardwareStatus = DiagnosticsLabelText = $"Coolant Temp: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetEngineCoolantTemp].function(parsedData)}° F";
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_GetAmbientTemp:
                                //newData = buildString.Split(' ');
                                if (parsedData.Length >= 1)
                                {
                                    HardwareStatus = DiagnosticsLabelText = $"Ambient Temp: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetAmbientTemp].function(parsedData)}° F";
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.OBD2_GetDTCs:
                                
                                DiagnosticsLabelText = "DTC Report (see details tab)";
                                HardwareStatus = $"Current DTCs:{Environment.NewLine}";
                                foreach (string str in parsedData)
                                {
                                    HardwareStatus += $"{str}{Environment.NewLine}";
                                }

                                buildString.Clear();
                                CurrentRequestType = DeviceRequestType.OBD2_GetPendingDTCs;
                                this._OBD2Device.Send($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{carriageReturn}");
                                this.DeviceIsIdle = false;
                                break;
                            case DeviceRequestType.OBD2_GetPendingDTCs:
                                HardwareStatus = $"Pending DTCs:{Environment.NewLine}";
                                foreach (string str in parsedData)
                                {
                                    HardwareStatus += $"{str}{Environment.NewLine}";
                                }

                                break;
                            case DeviceRequestType.OBD2_GetEngineRPM:
                                //newData = buildString.Split(' ');
                                if (parsedData.Length >= 1)
                                {
                                    HardwareStatus = DiagnosticsLabelText = "Monitoring Engine RPM";

                                    this.plotYVal = Convert.ToDouble(OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetEngineRPM].function(parsedData));

                                    this.dataPlotModel.Title = $"Engine RPM: {this.plotYVal}";

                                    this.dataPlotModel.AddDataPoint(this._pointIndex, Convert.ToDouble(this.plotYVal));

                                    this._pointIndex += 1;
                                    if (this._pointIndex > this.dataPlotModel.XAxisMaxValue + 1)
                                    {
                                        this.ClearPlotData();
                                    }
                                    dataPlotModel.InvalidatePlot(true);
                                    this.IsMonitoring = true;
                                    buildString.Clear();
                                    this._timer.Start();
                                }
                                else
                                {
                                    buildString.Clear();
                                    if(parsedData.Count() < 2)
                                     {
                                        dataPlotModel.InvalidatePlot(true);
                                        this.IsMonitoring = true;
                                        this._timer.Start();
                                     }
                                    else
                                    {
                                        HardwareStatus = string.Empty;
                                        foreach (var str in parsedData)
                                        {
                                            HardwareStatus += $"{str}{Environment.NewLine}";
                                            DiagnosticsLabelText = NotReadyString;
                                        }
                                    }
                                }
                            
                                break;
                            case DeviceRequestType.OBD2_GetVIN:
                                //newData = System.Text.RegularExpressions.Regex.Replace(buildString, "(\r\r\r|\r\r|\r)", "\r").Split('\r');
                                if (parsedData.Length >= 3)
                                {
                                    HardwareStatus = DiagnosticsLabelText = $"VIN: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetVIN].function(parsedData)}";
                                }
                                else
                                {
                                    HardwareStatus = string.Empty;
                                    foreach (var str in parsedData)
                                    {
                                        HardwareStatus += $"{str}{Environment.NewLine}";
                                        DiagnosticsLabelText = NotReadyString;
                                    }
                                }
                                break;
                            case DeviceRequestType.SerialNumber:
                                if(this._initializingDevice)
                                    HardwareStatus += $"Serial: {outData}{Environment.NewLine}";
                                else
                                    HardwareStatus = DiagnosticsLabelText = $"Serial: {outData}{Environment.NewLine}";
                                break;
                            case DeviceRequestType.Protocol:
                                if (this._initializingDevice)
                                    HardwareStatus += string.Format("Protocol: {0}{1}", outData, Environment.NewLine);
                                else
                                    HardwareStatus = DiagnosticsLabelText = string.Format("Protocol: {0}{1}", outData, Environment.NewLine);
                                break;
                            case DeviceRequestType.DeviceReset:
                                HardwareStatus = DiagnosticsLabelText = $"Device Resetting...{Environment.NewLine}";
                                this._initializingDevice = true;
                                this._initStep = 0;
                                _DeviceEchoOff = false;
                                break;
                            case DeviceRequestType.ProtocolSearch:
                                if (this._initializingDevice)
                                    HardwareStatus += $"Protocol Search: {outData}{Environment.NewLine}";
                                else
                                    HardwareStatus = DiagnosticsLabelText = $"Protocol Search: {outData}";
                                break;
                            case DeviceRequestType.DeviceSetDefaults:
                                Properties.Settings.Default.CommChannel = comChannelName;
                                Properties.Settings.Default.Save();
                                break;
                            case DeviceRequestType.EchoOff:
                                _DeviceEchoOff = true;
                                if (this._initializingDevice)
                                    HardwareStatus += $"Echo Off{Environment.NewLine}";
                                else
                                    HardwareStatus = DiagnosticsLabelText = $"Echo Off{Environment.NewLine}";
                                break;
                            case DeviceRequestType.MonitorAll:
                                MonitorText += $"{buildString.ToString().TrimEnd(dataEndTrimChars)}{Environment.NewLine}";
                                if(string.Compare(parsedData[parsedData.Length-1],"STOPPED",true) == 0)
                                {
                           //         this._OBD2Device.Send($"{OBD2Device.ELM327CommandDictionary[DeviceRequestType.CANSetAddressFilters].Code}{carriageReturn}");
                                }
                                buildString.Clear();
                                break;
                            //case DeviceRequestType.CANAutoFormatOn:
                            //    buildString.Clear();
                            //    break;
                            default:
                                var obdFunction = OBD2Device.ELM327CommandDictionary[CurrentRequestType];
                                if (this._initializingDevice || this._initializingDeviceCAN)
                                {
                                    switch (CurrentRequestType)
                                    {
                                        case DeviceRequestType.CANAddressMask:
                                            if (this._initializingDeviceCAN) HardwareStatus += $"Set CAN address mask: {CANMASK}{Environment.NewLine}";
                                            break;
                                        case DeviceRequestType.CANAddressFilter:
                                            if (this._initializingDeviceCAN) HardwareStatus += $"Set CAN address filter: {CANID}{Environment.NewLine}";
                                            break;
                                        case DeviceRequestType.CANAutoAddress:
                                            HardwareStatus += $"Reset CAN address filters:{Environment.NewLine}";
                                            break;
                                        default:
                                            HardwareStatus += $"{obdFunction.Name}{Environment.NewLine}";
                                            break;
                                    }
                                }
                                else
                                {
                                    // only show on diagnostic screen if this is a user function
                                  //  if (obdFunction.IsUserFunction) 
                                 //   {
                                        HardwareStatus = DiagnosticsLabelText = $"{obdFunction.Name}";
                                 //   }
                                }
                                break;
                        }
                        // After the clientconnected event, run these initial functions
                        if (this._initializingDevice)
                        {
                            if (this._initStep < this._InitFunctionList.Count())
                            {
                                buildString.Clear();
                                CurrentRequestType = this._InitFunctionList[this._initStep];
                                //this._OBD2Device.Send($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{carriageReturn}");
                                _ = StartComm($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{carriageReturn}");
                                this.DeviceIsIdle = false;
                                this._initStep++;
                            }
                            else
                            {
                                this._initializingDevice = false;
                                Properties.Settings.Default.CommChannel = comChannelName;
                                Properties.Settings.Default.Save();
                                StartButtonEnabled = true;
                                syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);
                                this.DeviceIsIdle = true;

                            }
                        }
                        if (this._initializingDeviceCAN)
                        {
                            if (this._initStep < this._CANInitFunctionList.Count())
                            {
                                buildString.Clear();
                                CurrentRequestType = this._CANInitFunctionList[this._initStep];
                                switch(CurrentRequestType)
                                {
                                    case DeviceRequestType.MonitorAll:
                                        HardwareStatus += DiagnosticsLabelText = $"Monitoring CAN...{Environment.NewLine}";
                                        _ = StartComm($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{carriageReturn}");
                                        break;
                                    case DeviceRequestType.CANAddressMask:
                                        _ = StartComm($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{CANMASK}{carriageReturn}");
                                        break;
                                    case DeviceRequestType.CANAddressFilter:
                                        this.ActiveCANAddress = CANID;
                                        _ = StartComm($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{CANID}{carriageReturn}");
                                        break;
                                    default:
                                        _ = StartComm($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{carriageReturn}");
                                        break;
                                }
                                this.DeviceIsIdle = false;
                                this._initStep++;
                            }
                            else
                            {
                                this._initializingDeviceCAN = false;
                                Properties.Settings.Default.CommChannel = comChannelName;
                                Properties.Settings.Default.Save();
                                StartButtonEnabled = true;
                                syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);
                                this.DeviceIsIdle = true;

                            }
                        }
                        return;
                    }
                    catch (Exception)
                    {
                        HardwareStatus = DiagnosticsLabelText = NotReadyString;
                    }
                    break;

                case CommunicationEvents.Disconnected:
                    this.ChannelDisconnected = true;
                    this._OBD2Device.CommunicationEvent -= this.deviceEvent;

                    expectedResponseCount = 0;
                    DeviceOnOffCommandText = "Connect";
                    HardwareStatus = DiagnosticsLabelText = $"{comChannelName}: Closed";
                    this._logger.InfoFormat($"{comChannelName}: Disconnected");
                    this.DeviceIsIdle = true;
                    OnPropertyChanged("ComChannelIsReady");
                    OnPropertyChanged("IpChannelIsReady");

                    break;
                case CommunicationEvents.Error:

                    this.ChannelDisconnected = true;
                    expectedResponseCount = 0;
                    DeviceOnOffCommandText = "Connect";
                    if (CurrentRequestType == DeviceRequestType.Connect)
                    {
                        HardwareStatus = DiagnosticsLabelText = $"Unable to connect to {comChannelName}";
                    }
                    else
                    {
                        HardwareStatus = DiagnosticsLabelText = $"{comChannelName}: Error";
                    }
                    this._logger.InfoFormat($"{comChannelName}: Error");
                    OnPropertyChanged("ComChannelIsReady");
                    OnPropertyChanged("IpChannelIsReady");
                    this.DeviceIsIdle = true;
                         

                    break;
                default:
                    HardwareStatus += DiagnosticsLabelText = "Unexpected Event Occurred..." + e.Event.ToString() + Environment.NewLine;
                    break;
            }
            syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);
        }
        private DeviceRequestType CurrentRequestType = DeviceRequestType.DeviceDescription;

        #endregion Private members

        public async Task StartComm(string data)
        {
            if (this._OBD2Device == null || !this._OBD2Device.IsOpen) return;
            //   ScanningPorts = false;
            //HardwareStatus = DiagnosticsLabelText = "Communicating...";//string.Empty;
            buildString.Clear();



            this._OBD2Device.Send(data);
            this.DeviceIsIdle = false;

            // Clear data plot
            this.ClearPlotData();

            portDiscoveryTimeout.Start();
            portDiscoveryTimeout.Elapsed += PortResponseTimeout_Elapsed;

            await Task.Delay(0);
        }
        public async Task StartComm()
        {
            if (this._OBD2Device == null || !this._OBD2Device.IsOpen) return;
            //   ScanningPorts = false;
            HardwareStatus = DiagnosticsLabelText = "Communicating...";//string.Empty;
            buildString.Clear();



            this._OBD2Device.Send($"{this.SelectedELM327CommandCode}{carriageReturn}");
            this.DeviceIsIdle = false;

            // Clear data plot
            this.ClearPlotData();

            portDiscoveryTimeout.Start();
            portDiscoveryTimeout.Elapsed += PortResponseTimeout_Elapsed;

            await Task.Delay(0);
        }

        private void ClearPlotData()
        {
            // preseve plot range if data is just cycling
            if (!this.IsMonitoring)
            {
                this.dataPlotModel.ResetVerticalRange();

            }
            this.dataPlotModel.ClearDataPoints();
            this._pointIndex = 0.0;
            this.dataPlotModel.InvalidatePlot(true);
        }


        public async Task Connect()
        {
       //     ScanningPorts = false;
            HardwareStatus = DiagnosticsLabelText = string.Empty;
            syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);
            await CreateDevice(SelectedCommChannel);
            syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);
        }

        static Task<byte[]> ReadDevice(ICommunicationDevice comDevice)
        {
            
            return new Task<byte[]>(() => {
                System.Threading.Thread.Sleep(1500);
                return new byte[] { 10, 23 };
            });
        }

        #region IInitialize
        private static bool _ReadyForUse = false;
        bool IInitialize.ReadyForUse
        {
            get
            {
                return _ReadyForUse;
            }
        }

        private object objectLock = new object();
        private event CreationComplete _creationCompleteEvent = ()=>{
            _ReadyForUse = true;
        };
        event CreationComplete IInitialize.Created
        {
            add
            {
                lock (objectLock)
                {
                    _creationCompleteEvent += value;
                }
            }
            remove
            {
                lock (objectLock)
                {
                    _creationCompleteEvent -= value;
                }
            }
        }

        void IInitialize.Initialize()
        {
            this._ErrorOcurred = true;
        }

        #endregion IInitialize

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propName)
        {
            try
            { 
                if(PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
            catch(Exception e)
            {
                string ext = e.Message;
            }
        }
        
        #endregion INotifyPropertyChanged

        #region Events

        // Subscribed to from the outside
        public event ModelEvent ModelEvent;

        private void SendStringEvent(ModelEventTypes eventType, string text, params object[] args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(text, args);
            using (ModelEventArgs evt = new ModelEventArgs(null, eventType, sb.ToString()))
            {
                if (this.ModelEvent != null) this.ModelEvent(this, evt);
            }
        }
        private object FireModelEvent(ModelEventArgs e)
        {
            if (this.ModelEvent == null) return false;
            return this.ModelEvent(this, e);
        }

        #endregion Events

        #region Properties
        /// <summary>
        /// Abreviated message on diagnostics control
        /// </summary>
        public string DiagnosticsMessage
        {
            get
            {
                return this._DiagnosticsMessage;
            }
            set
            {
                if (value == this._DiagnosticsMessage) return;
                this._DiagnosticsMessage = value;
                OnPropertyChanged("DiagnosticsMessage");
            }
        }
        private string _DiagnosticsMessage = "";
        //ChannelDisconnected
        public bool ComChannelIsReady
        {
            get
            {
                 return this._ChannelDisconnected && !this._UseIPSocket && (this._OBD2Device==null?true:!this._OBD2Device.IsOpen);
            }
        }
        public bool IpChannelIsReady
        {
            get
            {
                return this._ChannelDisconnected && this._UseIPSocket && this._DeviceIsIdle;
            }
        }

        private bool _UseIPSocket = false;
        public bool UseIPSocket
        {
            get
            {
                return this._UseIPSocket;
            }
            set
            {
                if (value == this._UseIPSocket) return;
                this._UseIPSocket = value;

                Properties.Settings.Default.UseIPSocket = value;
                Properties.Settings.Default.Save();
                //if (this._UseIPSocket)
                //{

                //}
                //else
                //{
                DiagnosticsLabelText = "";
                    HardwareStatus = "";
//              }

                OnPropertyChanged("UseIPSocket");
                OnPropertyChanged("ChannelDisconnected");
                OnPropertyChanged("ComChannelIsReady");
                OnPropertyChanged("IpChannelIsReady");
            }
        }
        public string CANID
        {
            get
            {
                return this._CANID;
            }
            set
            {
                if (value == this._CANID) return;
                this._CANID = value;
                OnPropertyChanged("CANID");
            }
        }
        private string _CANID = "";
        public string CANMASK
        {
            get
            {
                return this._CANMASK;
            }
            set
            {
                if (value == this._CANMASK) return;
                this._CANMASK = value;
                OnPropertyChanged("CANMASK");
            }
        }
        private string _CANMASK = "";

        public string IPAddress
        {
            get
            {
                return this._IPAddress;
            }
            set
            {
                if (value == this._IPAddress) return;
                this._IPAddress = value;
                OnPropertyChanged("IPAddress");
            }
        }
        private string _IPAddress = "";

        private int _IPPort = 0;
        public int IPPort
        {
            get
            {
                return this._IPPort;
            }
            set
            {
                if (value == this._IPPort) return;
                this._IPPort = value;
                OnPropertyChanged("IPPort");
            }
        }

        private List<string> _EndpointNames = new List<string>();
        public List<string> EndpointNames
        {
            get
            {
                return this._EndpointNames;
            }
            set
            {
                _EndpointNames = value;
                OnPropertyChanged("EndpointNames");
            }
        }

        private int _SelectedTab = 0;
        public int SelectedTab
        {
            get
            {
                return this._SelectedTab;
            }
            set
            {
                _SelectedTab = value;
                OnPropertyChanged("SelectedTab");
            }
        }

        public bool ErrorOcurred
        {
            get
            {
                return this._ErrorOcurred;
            }
            set
            {
                if (value == this._ErrorOcurred) return;
                this._ErrorOcurred = value;
                OnPropertyChanged("ErrorOcurred");
            }
        }
        private bool _ErrorOcurred = false;

        /// <summary>
        /// Applies to both com ports and sockets
        /// </summary>
        public bool ChannelDisconnected
        {
            get
            {
                return this._ChannelDisconnected && this.DeviceIsIdle;
            }
            set
            {
               // if (value == this._ChannelDisconnected) return;
                this._ChannelDisconnected = value;



                OnPropertyChanged("ComChannelIsReady");
                OnPropertyChanged("ChannelDisconnected");
            }
        }
        private bool _ChannelDisconnected = false;

        /// <summary>
        /// Device is not idle if it is connected or in the process of attempting a connection
        /// </summary>
        public bool DeviceIsIdle
        {
            get
            {
                return this._DeviceIsIdle;
            }
            set
            {
                this._DeviceIsIdle = value;
                OnPropertyChanged("DeviceIsIdle");
                OnPropertyChanged("IpChannelIsReady");
                OnPropertyChanged("ChannelDisconnected");
            }
        }
        private bool _DeviceIsIdle = true;



        public string SelectedELM327CommandCode
        {
            get
            {
                return this._SelectedELM327CommandCode;
            }
            set
            {
                if (value == this._SelectedELM327CommandCode) return;
                this._SelectedELM327CommandCode = value;
                OnPropertyChanged("SelectedELM327CommandCode");
            }
        }
        private string _SelectedELM327CommandCode = "";



        public System.Collections.ObjectModel.ReadOnlyCollection<ELM327Command> ELM327Commands
        {
            get
            {
                return this._ELM327Commands;
            }
            set
            {
                this._ELM327Commands = value;
                OnPropertyChanged("ELM327Commands");
            }
        }
        private System.Collections.ObjectModel.ReadOnlyCollection<ELM327Command> _ELM327Commands = null;



        public string MonitorText
        {
            get
            {
                return this._MonitorText;
            }
            set
            {
                //if (string.Compare(value, this._HardwareStatus) != 0)
                //{
                this._MonitorText = value;
                OnPropertyChanged("MonitorText");

                //}
            }
        }
        private string _MonitorText = "";

        public string HardwareStatus
        {
            get
            {
                return this._HardwareStatus;
            }
            set
            {
                //if (string.Compare(value, this._HardwareStatus) != 0)
                //{
                this._HardwareStatus = value;
                OnPropertyChanged("HardwareStatus");

                //}
            }
        }
        private string _HardwareStatus = "";

        private string _StartButtonDescription = "Go";
        public string StartButtonDescription
        {
            get
            {
                return this._StartButtonDescription;
            }
            set
            {
                this._StartButtonDescription = value;
                OnPropertyChanged("StartButtonDescription");
            }
        }


        private string _StopButtonDescription = "Stop";
        public string StopButtonDescription
        {
            get
            {
                return this._StopButtonDescription;
            }
            set
            {
                this._StopButtonDescription = value;
                OnPropertyChanged("StopButtonDescription");
            }
        }




        private string _DeviceOnOffCommandText = "";
        public string DeviceOnOffCommandText
        {
            get
            {
                return this._DeviceOnOffCommandText;
            }
            set
            {
                this._DeviceOnOffCommandText = value;
                OnPropertyChanged("DeviceOnOffCommandText");
            }
        }


        private string _DiagnosticsLabelText = "Diagnostics";
        public string DiagnosticsLabelText
        {
            get
            {
                return this._DiagnosticsLabelText;
            }
            set
            {
                this._DiagnosticsLabelText = value;
                OnPropertyChanged("DiagnosticsLabelText");
            }
        }


        private bool _StartButtonEnabled = false;
        public bool StartButtonEnabled
        {
            get
            {
                return this._StartButtonEnabled && DeviceIsIdle;
            }
            set
            {
                this._StartButtonEnabled = value;
                OnPropertyChanged("StartButtonEnabled");
            }
        }


        public string ActiveCANAddress
        {
            get
            {
                return this._ActiveCANAddress;
            }
            set
            {
                this._ActiveCANAddress = value;
                OnPropertyChanged("ActiveCANAddress");
            }
        }
        private string _ActiveCANAddress = "";



        public string SelectedCommChannel
        {
            get
            {
                return this._SelectedCommChannel;
            }
            set
            {
                this._SelectedCommChannel = value;
                StartButtonEnabled = false;

                Properties.Settings.Default.CommChannel = value;
                UpdateDiagnosticsMainLabel();
                OnPropertyChanged("SelectedCommChannel");
                this.DiagnosticsLabelText = HardwareStatus = DiagnosticsLabelText = string.IsNullOrEmpty(value)?"No channel":value + " selected";
            }
        }
        private string _SelectedCommChannel = "";

        private string _DiagnosticDescription = "";
        public string DiagnosticDescription
        {
            get
            {
                return this._DiagnosticDescription;
            }
            set
            {
                this._DiagnosticDescription = value;
                OnPropertyChanged("DiagnosticDescription");
            }
        }


        private bool _IsMonitoring = false;

        public bool IsMonitoring
        {
            get
            {
                return this._IsMonitoring;
            }
            set
            {
                this._IsMonitoring = value;
                OnPropertyChanged("IsMonitoring");
            }
        }
        #endregion Properties

        #region GetAllDevicesCommand

        RelayCommand _GetAllDevicesCommand;
        public ICommand GetAllDevicesCommand
        {
            get
            {
                return _GetAllDevicesCommand ?? (_GetAllDevicesCommand = new RelayCommand(param => this.GetAllDevices(), param => CanGetAllDevices));
            }
        }

        void GetAllDevices()
        {
            this._EndpointNames = ELM327.GetPortNames().ToList();
        }

        public bool CanGetAllDevices
        {
            get
            {
                return true;
            }
        }

        #endregion GetAllDevicesCommand

        #region ScanDeviceCommand

        RelayCommand _ScanDeviceCommand;
        public ICommand ScanDeviceCommand
        {
            get
            {
                return _ScanDeviceCommand ?? (_ScanDeviceCommand = new RelayCommand(param => this.ScanDevice(), param => CanScanDevice));
            }
        }


        void ScanDevice()
        {
            CurrentRequestType = DeviceRequestType.None;
            _ = Connect();
        }

        public bool CanScanDevice
        {
            get
            {
                return !string.IsNullOrEmpty(SelectedCommChannel);
            }
        }

        #endregion ScanDeviceCommand

        #region OpenCloseDeviceCommand

        RelayCommand _OpenCloseDeviceCommand;
        public ICommand OpenCloseDeviceCommand
        {
            get
            {
                return _OpenCloseDeviceCommand ?? (_OpenCloseDeviceCommand = new RelayCommand(param => this.OpenCloseDevice(), param => CanOpenCloseDevice));
            }
        }


        void OpenCloseDevice()
        {
            // This is a toggle operation
            if (this._OBD2Device == null || !this._OBD2Device.IsOpen)
            {

                if (MessageBox.Show($"*Ensure OBDII hardware is connected{Environment.NewLine}*Vehicle ignition is ON{Environment.NewLine}*Engine is NOT running.", "Connect To Vehicle", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.Cancel)
                {
                    return;
                }


                DeviceIsIdle = false;

                CurrentRequestType = DeviceRequestType.None;



                _ = Connect();
            }
            else
            {

                this._timer.Stop();
                portDiscoveryTimeout.Stop();

                portDiscoveryTimeout.Elapsed -= PortResponseTimeout_Elapsed;
                StartButtonEnabled = false;
                if (this._OBD2Device == null) return;
                this._OBD2Device.Close();
            }
        }

        public bool CanOpenCloseDevice
        {
            get
            {
                return !IsMonitoring && DeviceIsIdle && (!string.IsNullOrEmpty(SelectedCommChannel) || UseIPSocket);
            }
        }

        #endregion OpenCloseDeviceCommand

        void CloseDevice()
        {
         //   StartButtonEnabled = false;
            if (this._OBD2Device == null) return;
            this._OBD2Device.Close();
            this._OBD2Device.CommunicationEvent -= this.deviceEvent;
        }

        #region StartCommunicationCommand

        RelayCommand _StartCommunicationCommand;
        public ICommand StartCommunicationCommand
        {
            get
            {
                return _StartCommunicationCommand ?? (_StartCommunicationCommand = new RelayCommand(param => this.StartCommunication(), param => CanStartCommunication));
            }
        }


        void StartCommunication()
        {
            this.CurrentRequestType = OBD2Device.ELM327Commands.First(x => x.Code == this.SelectedELM327CommandCode).RequestType;
            
            switch(this.CurrentRequestType)
            {
                case DeviceRequestType.OBD2_FuelLevel:
                case DeviceRequestType.OBD2_GetEngineCoolantTemp:
                case DeviceRequestType.OBD2_GetEngineLoad:
                case DeviceRequestType.OBD2_GetEngineRPM:
                case DeviceRequestType.OBD2_KmSinceDTCCleared:
                case DeviceRequestType.OBD2_KmWithMilOn:
                case DeviceRequestType.OBD2_LongTermFuelTrimBank1:
                case DeviceRequestType.OBD2_LongTermFuelTrimBank2:
                case DeviceRequestType.OBD2_ShortTermFuelTrimBank1:
                case DeviceRequestType.OBD2_ShortTermFuelTrimBank2:
                case DeviceRequestType.SupplyVoltage:
                    this.SelectedTab = 0;
                    break;
                case DeviceRequestType.OBD2_ClearDTCs:
                    if (MessageBox.Show("Clear all vehicle fault codes.", "Clear All Vehicle DTCs", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    {
                        return;
                    }
                    this.SelectedTab = 1;
                    break;
                case DeviceRequestType.OBD2_FuelSystemStatus:
                case DeviceRequestType.OBD2_StatusSinceCodesLastCleared:
                case DeviceRequestType.OBD2_GetPIDs:
                case DeviceRequestType.OBD2_GetDTCs:
                case DeviceRequestType.OBD2_GetPendingDTCs:
                    this.SelectedTab = 1;
                    break;
                //case DeviceRequestType.MonitorAll:
                //    this._initStep = 0;
                //    HardwareStatus = DiagnosticsLabelText = $"Communicating...{Environment.NewLine}";
                //    this._initializingDeviceCAN = true;
                //    CurrentRequestType = DeviceRequestType.MemoryOff;
                //    MonitorText = string.Empty;
                //    this.IsMonitoring = true;
                //    this.SelectedTab = 2;
                //    Properties.Settings.Default.CanAddress = CANID;
                //    Properties.Settings.Default.Save();
                //    //this._OBD2Device.Send($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{carriageReturn}");
                //    _ = StartComm($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{carriageReturn}");
                //    return;
            }
            _ = StartComm();

        }

        public bool CanStartCommunication
        {
            get
            {
                return StartButtonEnabled && !IsMonitoring;
            }
        }

        #endregion StartCommunicationCommand

        #region ResetCANAddressCommand

        RelayCommand _ResetCANAddressCommand;
        public ICommand ResetCANAddressCommand
        {
            get
            {

                return _ResetCANAddressCommand ?? (_ResetCANAddressCommand = new RelayCommand(param => this.ResetCANAddress(), param => CanStartCommunication));
            }
        }


        void ResetCANAddress()
        {
            CurrentRequestType = DeviceRequestType.CANAutoAddress;
            _ = StartComm();
            this.ActiveCANAddress = "Auto";


        }

        //public bool CanResetCANAddress
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}

        #endregion ResetCANAddressCommand



        #region StartCANMonitoringCommand

        RelayCommand _StartCANMonitoringCommand;
        public ICommand StartCANMonitoringCommand
        {
            get
            {
                return _StartCANMonitoringCommand ?? (_StartCANMonitoringCommand = new RelayCommand(param => this.StartCANMonitoring(), param => CanStartCommunication));
            }
        }

        void StartCANMonitoring()
        {

            this._initStep = 0;
            HardwareStatus = DiagnosticsLabelText = $"Communicating...{Environment.NewLine}";
            MonitorText = string.Empty;
            this._initializingDeviceCAN = true;
            MonitorText = string.Empty;
            this.IsMonitoring = true;
            this.SelectedTab = 2;
            Properties.Settings.Default.CanAddress = CANID;
            Properties.Settings.Default.Save();
            //this._OBD2Device.Send($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{carriageReturn}");
            CurrentRequestType = DeviceRequestType.MemoryOff;
            _ = StartComm($"{OBD2Device.ELM327CommandDictionary[CurrentRequestType].Code}{carriageReturn}");











            //CurrentRequestType = DeviceRequestType.MonitorAll;
            //_ = StartComm();

        }

        //public bool CanStartCANMonitoring
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}

        #endregion StartCANMonitoringCommand

        #region StopCANMonitoringCommand

        RelayCommand _StopCANMonitoringCommand;
        public ICommand StopCANMonitoringCommand
        {
            get
            {

                return _StopCANMonitoringCommand ?? (_StopCANMonitoringCommand = new RelayCommand(param => this.StopCANMonitoring(), param => CanStopCommunication));
            }
        }


        void StopCANMonitoring()
        {
            HardwareStatus = DiagnosticsLabelText = "Bus Monitor Stopped";
            this.IsMonitoring = false;
            this.DeviceIsIdle = true;
            // CurrentRequestType = DeviceRequestType.None;
            this._OBD2Device.Send($"{carriageReturn}");
         //   _ = StartComm($"{Environment.NewLine}");
            this.IsMonitoring = false;
            this.DeviceIsIdle = true;
            syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);

        }

        //public bool CanStopCANMonitoring
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}

        #endregion ResetCANAddressCommand

        #region StopCommunicationCommand

        RelayCommand _StopCommunicationCommand;
        public ICommand StopCommunicationCommand
        {
            get
            {
                return _StopCommunicationCommand ?? (_StopCommunicationCommand = new RelayCommand(param => this.StopCommunication(), param => CanStopCommunication));
            }
        }


        void StopCommunication()
        {

            switch (this.CurrentRequestType)
            {
                case DeviceRequestType.MonitorAll:
                    // reset CAN address filters and also stops monitoring
                    this._OBD2Device.Send($"{carriageReturn}");
                   // this._OBD2Device.Send($"{OBD2Device.ELM327CommandDictionary[DeviceRequestType.CANSetAddressFilters].Code}{carriageReturn}");
                    HardwareStatus = DiagnosticsLabelText = "Bus Monitor Stopped";
                    break;
                default:
                    CurrentRequestType =  DeviceRequestType.None;
                    break;
            }


            this.IsMonitoring = false;
            this.DeviceIsIdle = true;
            syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);

        }

        public bool CanStopCommunication
        {
            get
            {
                return IsMonitoring;
            }
        }

        #endregion StartCommunicationCommand

        #region CloseCommand

        RelayCommand _closeCommand;
        public ICommand CloseCommand
        {
            get
            {
                if (_closeCommand == null)
                    _closeCommand = new RelayCommand(param => this.OnRequestClose());

                return _closeCommand;
            }
        }

        /// <summary>
        /// Tell anyone who's listening that we want to close...
        /// </summary>
        public event EventHandler RequestClose;

        void OnRequestClose()
        {
          //  this.InfoText = "Closing...";

            //this._ConversionRunningWait.WaitOne();

            // We're closing...join the threads and await exit
            //if (this._importThread != null) this._importThread.Join();
            EventHandler handler = this.RequestClose;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        #endregion CloseCommand

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.333333333333333333322222222222222222222

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AutoScannerViewModel() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion


    }
}
