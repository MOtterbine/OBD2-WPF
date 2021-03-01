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
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using OS.AutoScanner.Views;
using System.Security.Policy;
using Windows.Media.Capture;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace OS.AutoScanner.Models
{

    public class AutoScannerViewModel : IAppViewModel, INotifyPropertyChanged
    {
        #region Private members

        protected readonly ILog _logger = LogManager.GetLogger(typeof(AutoScannerViewModel));
        private DeviceEvent deviceEvent = null;
        private int expectedResponseCount = 0;
        private System.Timers.Timer portDiscoveryTimeout = new System.Timers.Timer(8000);
        private OBD2Device _OBD2Device = null;
        private SynchronizationContext syncContext;
        public System.Timers.Timer _timer = new System.Timers.Timer(10);
        private readonly Dictionary<DeviceRequestType, string> _initFunctionDictionary = new Dictionary<DeviceRequestType, string>()
        {  
            // {DeviceRequestType.DeviceReset, "ATZ" }, // this is done upon 'clientconnected' event
            {DeviceRequestType.DeviceDescription, "AT@1"  },
            {DeviceRequestType.SerialNumber, "AT@2"  },
            {DeviceRequestType.Protocol, "ATDP"  },
           // {DeviceRequestType.SupplyVoltage, "ATRV"  },
            {DeviceRequestType.EchoOff, "ATE0"  },
            {DeviceRequestType.AllowLongMessages, "ATAL"  },
            {DeviceRequestType.ProtocolSearch, "ATSP0"  }
        };
        public DataPlotModel dataPlotModel { get; private set; }




        private int _initStep = 0;
        private bool _initializingDevice = false;


        private double _pointIndex = 0;
      //  private LineSeries _lineSeries = new LineSeries();

        public void Elaps(object source, System.Timers.ElapsedEventArgs e)
        {

        }

        public AutoScannerViewModel()
        {

            this.dataPlotModel = new DataPlotModel { Title = "OBD2 Data", PlotType=PlotType.XY };
          //  this.dataPlotModel.YAxisMaxValue = 20;
            // for constant monitoring...
            this._timer.Elapsed += (object source, System.Timers.ElapsedEventArgs elapsed) => {
                this._timer.Stop();
                this._OBD2Device.Send($"{(char)0x0D}");
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



            // Auto connect...
        //    if (!string.IsNullOrEmpty(this.SelectedCommChannel))
        //    {
                // to let app know the status of this port...
                //...not finished
        //        _ = Connect();
        //    }
            UpdateDiagnosticsMainLabel();

            this.ELM327Commands = new System.Collections.ObjectModel.ReadOnlyCollection<ELM327Command>(OBD2Device.ELM327Commands.ToList());

            _logger.InfoFormat("Found {0} endpoints on the current machine.", epCount);
            this.SelectedELM327CommandCode = "ATDP";

            if (this._creationCompleteEvent != null) this._creationCompleteEvent();


            syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);

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
                    HardwareStatus += string.Format("Connecting to {0}...{1}", this.ComChannelName, Environment.NewLine);
                }
                catch(Exception e)
                {
                    string it = e.Message;
                }
                this._OBD2Device.CommunicationEvent += this.deviceEvent;
                if (!this._OBD2Device.Open())
                {
                    _DeviceIsIdle = true;
                    HardwareStatus += $"Unable to open device - {this._OBD2Device.MessageString}{Environment.NewLine}";
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
                HardwareStatus += $"{Environment.NewLine}No response...";
            }
        }
        private byte[] byteBuf = new byte [100];
        private int byteBufLen = 0;
        private char cBuf = ' ';
        private string lik = "";
        private string buildString = "";
        private bool _DeviceEchoOff = false;
        private bool _DeviceIsIdle = true;



    private void onDeviceEvent(object sender, ChannelEventArgs e)
        {
            string comChannelName = this.ComChannelName;//SelectedCommChannel;
            //if(this._UseIPSocket)con
            //{
            //    comChannelName = $"{this.IPAddress}:{this.IPPort}";
            //}
            //else
            //{
            //    comChannelName = SelectedCommChannel;
            //}

            ICommunicationDevice cDev = sender as ICommunicationDevice;
            if (cDev == null) return;
            switch (e.Event)
            {
                case CommunicationEvents.ConnectedAsClient:
                    portDiscoveryTimeout.Stop();
                    portDiscoveryTimeout.Elapsed -= PortResponseTimeout_Elapsed;
                    HardwareStatus = $"{comChannelName}: Open{Environment.NewLine}";
                    buildString = string.Empty;
                    DeviceOnOffCommandText = "Disconnect";
                    this.ChannelDisconnected = false;

                    switch(CurrentRequestType)
                    {
                        case DeviceRequestType.None:
                            this._initStep = 0;
                            this._initializingDevice = true;
                            //  CurrentRequestType = this._initFunctionList[0];
                            CurrentRequestType = DeviceRequestType.DeviceReset;
                            cDev.Send($"ATZ{(char)0x0D}"); // Reset the device
                            HardwareStatus += $"Scanning...{Environment.NewLine}";
                            break;
                    }

                    portDiscoveryTimeout.Elapsed += PortResponseTimeout_Elapsed;
                    portDiscoveryTimeout.Start();
                    expectedResponseCount++;
                    _DeviceIsIdle = true;

                    //StartButtonEnabled = true;


                    break;
                case CommunicationEvents.Receive:
                    //if(buidString.Length == 0)
                    //{
                    //    HardwareStatus = string.Empty;
                    //}
                    buildString += ASCIIEncoding.ASCII.GetString(e.data);
                    break;
                case CommunicationEvents.ReceiveEnd:
                    portDiscoveryTimeout.Stop();
                    portDiscoveryTimeout.Elapsed -= PortResponseTimeout_Elapsed;

                    buildString += ASCIIEncoding.ASCII.GetString(e.data);

                    string[] parsedData = System.Text.RegularExpressions.Regex.Replace(buildString,"(\r\r\r|\r\r|\r)", "\r").Split('\r');
                    string outData = parsedData[_DeviceEchoOff?0:1];

                    this._logger.InfoFormat("{0}: from device=> {1}", comChannelName, parsedData);
                    string [] newData = null;
                    //try
                    //{
                    switch (CurrentRequestType)
                    {
                        case DeviceRequestType.None:
                            HardwareStatus = "Idle";
                            break;

                        case DeviceRequestType.OBD2_GetEngineLoad:
                            newData = buildString.Split(' ');
                            if (newData.Length == 4)
                            {
                                HardwareStatus = $"Engine Load: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetEngineLoad].function(newData)}%";
                            }
                            else
                            {
                                HardwareStatus = buildString.Remove(buildString.Length - 3);
                            }
                            break;
                        case DeviceRequestType.OBD2_ShortTermFuelTrimBank1:
                            newData = buildString.Split(' ');
                            if (newData.Length == 4)
                            {
                                HardwareStatus = $"Short-Term Fuel Trim B1: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_ShortTermFuelTrimBank1].function(newData)}%";
                            }
                            else
                            {
                                HardwareStatus = buildString.Remove(buildString.Length - 3);
                            }
                            break;
                        case DeviceRequestType.OBD2_LongTermFuelTrimBank1:
                            newData = buildString.Split(' ');
                            if (newData.Length == 4)
                            {
                                HardwareStatus = $"Long-Term Fuel Trim B1: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_LongTermFuelTrimBank1].function(newData)}%";
                            }
                            else
                            {
                                HardwareStatus = buildString.Remove(buildString.Length - 3);
                            }
                            break;
                        case DeviceRequestType.OBD2_ShortTermFuelTrimBank2:
                            newData = buildString.Split(' ');
                            if (newData.Length == 4)
                            {
                                HardwareStatus = $"Short-Term Fuel Trim B2: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_ShortTermFuelTrimBank2].function(newData)}%";
                            }
                            else
                            {
                                HardwareStatus = buildString.Remove(buildString.Length - 3);
                            }
                            break;
                        case DeviceRequestType.OBD2_LongTermFuelTrimBank2:
                            newData = buildString.Split(' ');
                            if (newData.Length == 4)
                            {
                                HardwareStatus = $"Long-Term Fuel Trim B2: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_LongTermFuelTrimBank2].function(newData)}%";
                            }
                            else
                            {
                                HardwareStatus = buildString.Remove(buildString.Length - 3);
                            }
                            break;
                        case DeviceRequestType.SupplyVoltage:
                            HardwareStatus = " Monitoring Supply Voltage";
                            Properties.Settings.Default.CommChannel = this.SelectedCommChannel;

                            syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);

                            this.dataPlotModel.Title = $"Supply Voltage: {outData}";
                            this.dataPlotModel.Points.Add(new DataPoint(this._pointIndex, double.Parse(outData.Split(new char[] { 'V' })[0])));
                            this._pointIndex += 1;
                            if (this._pointIndex > this.dataPlotModel.XAxisMaxValue + 1)
                            {
                                this.ClearPlotData();
                            }
                            dataPlotModel.InvalidatePlot(true);
                            this.IsMonitoring = true;
                            buildString = string.Empty;
                            this._timer.Start();

                            break;
                        case DeviceRequestType.DeviceDescription:
                            if (this._initializingDevice)
                                HardwareStatus += "Device: " + outData + Environment.NewLine;
                            else
                                HardwareStatus = "Device: " + outData + Environment.NewLine;
                            break;

                        case DeviceRequestType.OBD2_ClearDTCs:
                            HardwareStatus = outData + Environment.NewLine;
                            // Properties.Settings.Default.CommChannel = this.SelectedCommChannel;

                            StartButtonEnabled = true;
                            syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);

                            break;

                        case DeviceRequestType.OBD2_GetPIDs:
                            newData = System.Text.RegularExpressions.Regex.Replace(buildString, "(\r\r\r|\r\r|\r|>)", "\r").Split('\r');
                            try
                            {
                                if (string.Compare(newData[0], "can error", true) == 0) HardwareStatus = newData[0];
                                string[] socl = newData[0].Substring(5).Split(' ');
                                lik = "";
                                int i = 0;
                                foreach (string str in socl)
                                {
                                    if (string.IsNullOrEmpty(str)) continue;

                                    byteBuf[i++] = byte.Parse(str, System.Globalization.NumberStyles.HexNumber);
                                }
                                byteBufLen = i;
                                // HardwareStatus = $"PIDS: {byteBuf[0]}-{byteBuf[1]}-{byteBuf[2]}-{byteBuf[3]}";


                                var x = 0;
                                uint mask = 0b10000000;
                                PIDCategory tempPIDCat = null;
                                StringBuilder sb = new StringBuilder($"Supported PIDs:{Environment.NewLine}");
                                for (i = 0; i < byteBufLen; i++)
                                {
                                    mask = 0b10000000;
                                    switch (i)
                                    {
                                        case 0:
                                            for (x = 1; x < 9; x++)
                                            {
                                                tempPIDCat = OBD2Device.OBD2PIDS[x];
                                                if (tempPIDCat.IsSupported = !((mask & byteBuf[i]) == 0))
                                                {
                                                    sb.Append($"{tempPIDCat.Description}{Environment.NewLine}");
                                                }
                                                mask >>= 1;
                                            }
                                            break;
                                        case 1:
                                            for (x = 9; x < 17; x++)
                                            {
                                                tempPIDCat = OBD2Device.OBD2PIDS[x];
                                                if (tempPIDCat.IsSupported = !((mask & byteBuf[i]) == 0))
                                                {
                                                    sb.Append($"{tempPIDCat.Description}{Environment.NewLine}");
                                                }
                                                mask >>= 1;
                                            }
                                            break;
                                        case 2:
                                            for (x = 17; x < 25; x++)
                                            {
                                                tempPIDCat = OBD2Device.OBD2PIDS[x];
                                                if (tempPIDCat.IsSupported = !((mask & byteBuf[i]) == 0))
                                                {
                                                    sb.Append($"{tempPIDCat.Description}{Environment.NewLine}");
                                                }
                                                mask >>= 1;
                                            }
                                            break;
                                        case 3:
                                            for (x = 25; x < 33; x++)
                                            {
                                                tempPIDCat = OBD2Device.OBD2PIDS[x];
                                                if (tempPIDCat.IsSupported = !((mask & byteBuf[i]) == 0))
                                                {
                                                    sb.Append($"{tempPIDCat.Description}{Environment.NewLine}");
                                                }
                                                mask >>= 1;
                                            }
                                            break;
                                    }
                                }
                                HardwareStatus = sb.ToString();
                            }
                            catch (Exception)
                            {
                                HardwareStatus = string.Empty;
                                foreach (var str in newData)
                                {
                                    HardwareStatus += $"{str}{Environment.NewLine}";
                                }

                            }
                            break;
                        case DeviceRequestType.OBD2_KmSinceDTCCleared:
                            newData = buildString.Split(' ');
                            if (newData.Length >= 5)
                            {
                                HardwareStatus = $"Distance traveled since DTCs cleared: {Math.Round(int.Parse(newData[2] + newData[3], System.Globalization.NumberStyles.HexNumber) / 1.60, 0, MidpointRounding.AwayFromZero)} miles";
                            }
                            else
                            {
                                HardwareStatus = buildString.Remove(buildString.Length - 3);
                            }
                            break;
                        case DeviceRequestType.OBD2_FuelLevel:

                            newData = buildString.Split(' ');

                            if (newData.Length == 4)
                            {
                                HardwareStatus = $"Fuel: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_FuelLevel].function(newData)}%";
                            }
                            else
                            {
                                HardwareStatus = buildString.Remove(buildString.Length - 3);
                            }
                            break;
                        case DeviceRequestType.OBD2_WarmUpsSinceDTCCleared:
                           // HardwareStatus = $"Warm-ups since DTC cleared: {int.Parse(outData.Substring(6), System.Globalization.NumberStyles.HexNumber)} warmups";
                            newData = buildString.Split(' ');
                            if (newData.Length >= 4)
                            {
                                HardwareStatus = $"Warm-ups since DTC cleared: {int.Parse(newData[2], System.Globalization.NumberStyles.HexNumber) } warmups";
                            }
                            else
                            {
                                HardwareStatus = buildString.Remove(buildString.Length - 3);
                            }
                            break;
                        case DeviceRequestType.OBD2_GetEngineCoolantTemp:
                            newData = buildString.Split(' ');
                            if (newData.Length == 4)
                            {
                                HardwareStatus = $"Coolant Temp: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetEngineCoolantTemp].function(newData)}° F";
                            }
                            else
                            {
                                HardwareStatus = buildString.Remove(buildString.Length - 3);
                            }
                            break;
                        case DeviceRequestType.OBD2_GetAmbientTemp:
                            newData = buildString.Split(' ');
                            if (newData.Length == 4)
                            {
                                HardwareStatus = $"Ambient Temp: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetAmbientTemp].function(newData)}° F";
                            }
                            else
                            {
                                HardwareStatus = buildString.Remove(buildString.Length - 3);
                            }
                            break;
                        case DeviceRequestType.OBD2_GetDTCs:
                            HardwareStatus = $"{outData}{Environment.NewLine}";
                            break;
                        case DeviceRequestType.OBD2_GetEngineRPM:
                            newData = buildString.Split(' ');
                            if (newData.Length == 5)
                            {
                                HardwareStatus = "Monitoring Engine RPM";

                                //var rpmVal = int.Parse(newData[2] + newData[3], System.Globalization.NumberStyles.HexNumber) / 4;
                                var rpmVal = OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetEngineRPM].function(newData);
                               // var rpmValStr = $"{rpmVal}";

                                this.dataPlotModel.Title = $"Engine RPM: {rpmVal}";
                                this.dataPlotModel.Points.Add(new DataPoint(this._pointIndex, Convert.ToDouble(rpmVal)));
                                this._pointIndex += 1;
                                if (this._pointIndex > this.dataPlotModel.XAxisMaxValue + 1)
                                {
                                    this.ClearPlotData();
                                }
                                dataPlotModel.InvalidatePlot(true);
                                this.IsMonitoring = true;
                                buildString = string.Empty;
                                this._timer.Start();
                            }
                            else
                            {
                                buildString = string.Empty;
                                if(newData.Count() < 2)
                                 {
                                    
                                    dataPlotModel.InvalidatePlot(true);
                                    this.IsMonitoring = true;
                                    this._timer.Start();
                                 }
                                else
                                {
                                    //HardwareStatus = string.Empty;
                                    //foreach (var str in newData)
                                    //{
                                    //    HardwareStatus += $"{str}{Environment.NewLine}";
                                    //}
                                   // HardwareStatus = buildString;


                                    HardwareStatus = System.Text.RegularExpressions.Regex.Replace(buildString, "(\r\r\r|\r\r|\r|>)", "\r");



                                }
                            }
                            
                            break;
                        case DeviceRequestType.OBD2_GetVIN:
                            newData = System.Text.RegularExpressions.Regex.Replace(buildString, "(\r\r\r|\r\r|\r)", "\r").Split('\r');
                            if (newData.Length == 5)
                            {
                                HardwareStatus = $"VIN: {OBD2Device.ELM327CommandDictionary[DeviceRequestType.OBD2_GetVIN].function(newData)}";
                            }
                            else
                            {
                                HardwareStatus = newData[0];
                            }
                            break;
                        case DeviceRequestType.SerialNumber:
                            if(this._initializingDevice)
                                HardwareStatus += $"Serial: {outData}{Environment.NewLine}";
                            else
                                HardwareStatus = $"Serial: {outData}{Environment.NewLine}";
                            break;
                        case DeviceRequestType.Protocol:
                            if (this._initializingDevice)
                                HardwareStatus += string.Format("Protocol: {0}{1}", outData, Environment.NewLine);
                            else
                                HardwareStatus = string.Format("Protocol: {0}{1}", outData, Environment.NewLine);
                        break;
                        case DeviceRequestType.EchoOff:
                            _DeviceEchoOff = true;
                            if (this._initializingDevice)
                                HardwareStatus += $"Echo Off{Environment.NewLine}";
                            else
                                HardwareStatus = $"Echo Off{Environment.NewLine}";
                        break;
                        case DeviceRequestType.DeviceReset:
                            HardwareStatus = $"Device Reset{Environment.NewLine}";
                            this._initializingDevice = true;
                            this._initStep = 0;
                            _DeviceEchoOff = false;
                            break;
                        case DeviceRequestType.AllowLongMessages:
                            if (this._initializingDevice)
                                HardwareStatus += $"Allow long messages{Environment.NewLine}";
                            else
                                HardwareStatus = $"Allow long messages{Environment.NewLine}";
                            break;
                        case DeviceRequestType.DeviceSetDefaults:
                            Properties.Settings.Default.CommChannel = comChannelName;
                            Properties.Settings.Default.Save();
                            break; 
                    }
                    // After the clientconnected event, run these initial functions
                    if (this._initializingDevice)
                    {
                        if (this._initStep < this._initFunctionDictionary.Count())
                        {
                            buildString = string.Empty;
                            CurrentRequestType = this._initFunctionDictionary.Keys.ElementAt(this._initStep);
                            this._OBD2Device.Send($"{this._initFunctionDictionary.Values.ElementAt(this._initStep)}{(char)0x0D}");
                            this._initStep++;
                        }
                        else
                        {
                            this._initializingDevice = false;
                            Properties.Settings.Default.CommChannel = comChannelName;
                            Properties.Settings.Default.Save();
                            StartButtonEnabled = true;
                            syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);
                        }
                    }
                    return;
                    //}
                    //catch(Exception)
                    //{
                    //    HardwareStatus = buildString.Remove(buildString.Length - 3);
                    //}
                    
                case CommunicationEvents.Disconnected:
                    this.ChannelDisconnected = true;

                    expectedResponseCount = 0;
                    DeviceOnOffCommandText = "Connect";
                    HardwareStatus = $"{comChannelName}: Closed";
                    this._logger.InfoFormat($"{comChannelName}: Disconnected");
                    OnPropertyChanged("ComChannelIsReady");
                    OnPropertyChanged("IpChannelIsReady");

                    break;
                case CommunicationEvents.Error:

                    this.ChannelDisconnected = true;
                    expectedResponseCount = 0;
                    DeviceOnOffCommandText = "Connect";
                    if (CurrentRequestType == DeviceRequestType.Connect)
                    {
                        HardwareStatus = $"Unable to connect to {comChannelName}";
                    }
                    else
                    {
                        HardwareStatus = $"{comChannelName}: Error";
                    }
                    this._logger.InfoFormat($"{comChannelName}: Error");
                    OnPropertyChanged("ComChannelIsReady");
                    OnPropertyChanged("IpChannelIsReady");
                    this._DeviceIsIdle = true;
                         

                    break;
                default:
                    HardwareStatus += "Unexpected Event Occurred..." + e.Event.ToString() + Environment.NewLine;
                    break;
            }
            syncContext.Post(delegate { CommandManager.InvalidateRequerySuggested(); }, null);
        }
        private DeviceRequestType CurrentRequestType = DeviceRequestType.DeviceDescription;

        #endregion Private members

        public async Task StartComm()
        {
            if (this._OBD2Device == null || !this._OBD2Device.IsOpen) return;
            //   ScanningPorts = false;
            HardwareStatus = "Communicating...";//string.Empty;
            buildString = string.Empty;

            this.CurrentRequestType = OBD2Device.ELM327Commands.First(x=>x.Code == this.SelectedELM327CommandCode).RequestType;

            this._OBD2Device.Send($"{this.SelectedELM327CommandCode}{(char)0x0D}");

            // Clear data plot
            this.ClearPlotData();


            portDiscoveryTimeout.Start();
            portDiscoveryTimeout.Elapsed += PortResponseTimeout_Elapsed;

            await Task.Delay(0);
        }

        private void ClearPlotData()
        {
            this.dataPlotModel.Points.Clear();
            this._pointIndex = 0.0;
            this.dataPlotModel.InvalidatePlot(true);
        }


        public async Task Connect()
        {
       //     ScanningPorts = false;
            HardwareStatus = string.Empty;
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
        static Task<int> shitaki(ICommunicationDevice comDevice)
        {
            return new Task<int>(() => 50);
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
                return this._ChannelDisconnected && this._UseIPSocket;
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
//                }

                OnPropertyChanged("UseIPSocket");
                OnPropertyChanged("ChannelDisconnected");
                OnPropertyChanged("ComChannelIsReady");
                OnPropertyChanged("IpChannelIsReady");
            }
        }

        private string _IPAddress = "";
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



        //public List<DataPoint> _Points = new List<DataPoint>();
        //public List<DataPoint> Points
        //{
        //    get { return _Points; }
        //    set {
        //        _Points = value;
        //        OnPropertyChanged("Points");
        //    }
        //}

        private bool _ErrorOcurred = false;
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

        private bool _ChannelDisconnected = false;
        public bool ChannelDisconnected
        {
            get
            {
                return this._ChannelDisconnected && this._DeviceIsIdle;
            }
            set
            {
               // if (value == this._ChannelDisconnected) return;
                this._ChannelDisconnected = value;



                OnPropertyChanged("ComChannelIsReady");
                OnPropertyChanged("ChannelDisconnected");
            }
        }

        
        private string _SelectedELM327CommandCode = "";
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


        private System.Collections.ObjectModel.ReadOnlyCollection<ELM327Command> _ELM327Commands = null;
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



        private string _HardwareStatus = "";
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

        //private bool _StartButtonEnabled = false;
        //public bool StartButtonEnabled
        //{
        //    get
        //    {
        //        return this._StartButtonEnabled;
        //    }
        //    set
        //    {
        //        this._StartButtonEnabled = value;
        //        OnPropertyChanged("StartButtonEnabled");
        //    }
        //}


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
                return this._StartButtonEnabled;
            }
            set
            {
                this._StartButtonEnabled = value;
                OnPropertyChanged("StartButtonEnabled");
            }
        }


        private string _SelectedCommChannel = "";
        public string SelectedCommChannel
        {
            get
            {
                return this._SelectedCommChannel;
            }
            set
            {
                this._SelectedCommChannel = value;

                CloseDevice();
                 StartButtonEnabled = false;

                Properties.Settings.Default.CommChannel = value;





                UpdateDiagnosticsMainLabel();
                OnPropertyChanged("SelectedCommChannel");
                this.DiagnosticsLabelText = HardwareStatus = string.IsNullOrEmpty(value)?"No channel":value + " selected";

            }
        }

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

        //#region ScanDevicesCommand

        //RelayCommand _ScanDevicesCommand;
        //public ICommand ScanDevicesCommand
        //{
        //    get
        //    {
        //        return _ScanDevicesCommand ?? (_ScanDevicesCommand = new RelayCommand(param => this.ScanDevices(), param => CanScanDevices));
        //    }
        //}

        //void ScanDevices()
        //{
        //   // _ = StartPortScan();
        //}

        //public bool CanScanDevices
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}

        //#endregion ScanDevicesCommand

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

        #region OpenDeviceCommand

        RelayCommand _OpenDeviceCommand;
        public ICommand OpenDeviceCommand
        {
            get
            {
                return _OpenDeviceCommand ?? (_OpenDeviceCommand = new RelayCommand(param => this.OpenDevice(), param => CanOpenDevice));
            }
        }


        void OpenDevice()
        {
            // This is a toggle operation
            if (this._OBD2Device == null || !this._OBD2Device.IsOpen)
            {
                _DeviceIsIdle = false;

                CurrentRequestType = DeviceRequestType.None;
                _ = Connect();
            }
            else
            {
                portDiscoveryTimeout.Stop();
                portDiscoveryTimeout.Elapsed -= PortResponseTimeout_Elapsed;
                StartButtonEnabled = false;
                if (this._OBD2Device == null) return;
                this._OBD2Device.Close();
                this._OBD2Device.CommunicationEvent -= this.deviceEvent;
            }
        }

        public bool CanOpenDevice
        {
            get
            {
                return _DeviceIsIdle && (!string.IsNullOrEmpty(SelectedCommChannel));
            }
        }

        #endregion OpenDeviceCommand

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
            _ = StartComm();

        }

        public bool CanStartCommunication
        {
            get
            {
                return StartButtonEnabled;
            }
        }

        #endregion StartCommunicationCommand

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
            CurrentRequestType =  DeviceRequestType.None;
            this.IsMonitoring = false;
        }

        public bool CanStopCommunication
        {
            get
            {
                return StartButtonEnabled && IsMonitoring;
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
