using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OS.Communication;
using System.Xml.Serialization;
using System.IO.Ports;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace OS.AutoScanner.Models
{

	// EXAMPLE CONSUMER CODE....

//	OSBluetoothLEAdvertisementWatcher blueToothWatcher = new OSBluetoothLEAdvertisementWatcher();
//	blueToothWatcher.StartedListening += () => {
//             //   HardwareStatus = "Bluetooth connecting...";
//            };
//        blueToothWatcher.StoppedListening += () => {
//            //    HardwareStatus = "Bluetooth disconnecting...";
//            };
//            blueToothWatcher.NewDeviceDiscovered += (device) => {
//            //    HardwareStatus = string.Format("new device - name: [{0}], address: [{1}]", device.Name, device.Address) ;
//            }; 
//            blueToothWatcher.DeviceNameChanged += (device) => {
//           //     HardwareStatus = string.Format("Device name changed - name: [{0}], address: [{1}]", device.Name, device.Address);
//            };

//         //
//            blueToothWatcher.StartListening();



















	[Serializable]
	[XmlRootAttribute("CommunicationDevice", Namespace = "", IsNullable = false)]
	public class OSBluetoothLEAdvertisementWatcher : CommunicationDevice, ICloneable, ICommunicationDevice
	{
		#region Event Sources

		public override event OnSelfTestEvent SelfTestEvent;
		public event Action StoppedListening = () => {
		};
		public event Action StartedListening = () => {
		};
		public event Action<OSBluetoothLEDevice> DeviceDiscovered = (device) => {
			Console.WriteLine($"Device discovered - name: [{device.Name}], address: [{device.Address}]");
		};
		public event Action<OSBluetoothLEDevice> NewDeviceDiscovered = (device) => {
			Console.WriteLine($"New device - name: [{device.Name}], address: [{device.Address}]");
		};
		public event Action<OSBluetoothLEDevice> DeviceNameChanged = (device) => {
			Console.WriteLine($"Device Name Changed - name: [{device.Name}], address: [{device.Address}]");
		};

		#endregion

		#region Properties

		private readonly BluetoothLEAdvertisementWatcher mWatcher = null;
		private readonly Dictionary<ulong, OSBluetoothLEDevice> mDiscoveredDevices = new Dictionary<ulong, OSBluetoothLEDevice>();

		[XmlIgnoreAttribute()]
		public string MessageString { get; set; }
		private SerialPort _SerialPort = null;
		public int Port
		{
			get
			{
				return this._Port;
			}
			set
			{
				if (this._SerialPort.IsOpen == false)
				{
					this._Port = value;
					this._SerialPort.PortName = "Com" + value;
				}
			}
		}
		private int _Port = -1;
		public StopBits StopBits
		{
			get
			{
				return this._SerialPort.StopBits;
			}
			set
			{
				if (this._SerialPort.IsOpen == false)
				{
					this._SerialPort.StopBits = value;
				}
			}
		}
		protected int _StopBits = 0;
		public Parity Parity
		{
			get
			{
				return this._SerialPort.Parity;
			}
			set
			{
				if (this._SerialPort.IsOpen == false)
				{
					this._SerialPort.Parity = value;
				}
			}
		}
		protected int _DataBits = 8;
		public int DataBits
		{
			get
			{
				return this._SerialPort.DataBits;
			}
			set
			{
				if (this._SerialPort.IsOpen == false)
				{
					this._SerialPort.DataBits = value;
				}
			}
		}
		protected int _BaudRate = 9600;
		public int BaudRate
		{
			get
			{
				return this._SerialPort.BaudRate;
			}
			set
			{
				if (this._SerialPort.IsOpen == false)
				{
					this._SerialPort.BaudRate = value;
				}
			}
		}

		#endregion
		private readonly object mThreadLock = new object();
		public IReadOnlyCollection<OSBluetoothLEDevice> DiscoveredDevices
        {
            get
            {
				lock(mThreadLock)
                {
					return mDiscoveredDevices.Values.ToList().AsReadOnly();
                }
            }
        }

		#region Methods

		public static string[] GetPortNames()
		{

//
			return SerialPort.GetPortNames();
		}

		// Copy Constructor
		public OSBluetoothLEAdvertisementWatcher(CommunicationDevice commDevice) : base(commDevice)
		{
			this.Initialize();
			this._SerialPort = new SerialPort();
		}
		public OSBluetoothLEAdvertisementWatcher() // Required for serialization
		{
			mWatcher = new BluetoothLEAdvertisementWatcher
			{
				ScanningMode = BluetoothLEScanningMode.Active

			};

            mWatcher.Received += WatcherAdvertisementRecieved;
            mWatcher.Stopped += (watcher, e) => {
				this.StoppedListening();
			};

			//this.Initialize();
		//	this._SerialPort = new SerialPort();
		}

		public void StartListening()
		{
			if (Listening) return;
			mWatcher.Start();
			this.StartedListening();
		}
		public void StoptListening()
		{
			if (!Listening) return;
			mWatcher.Stop();
		}

		public bool Listening => mWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        private void WatcherAdvertisementRecieved(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {

			OSBluetoothLEDevice dev = null;

			var newDiscovery = !mDiscoveredDevices.ContainsKey(args.BluetoothAddress);
			var nameChanged = !newDiscovery && !string.IsNullOrEmpty(args.Advertisement.LocalName) && mDiscoveredDevices[args.BluetoothAddress].Name != args.Advertisement.LocalName;


			lock (mThreadLock)
			{

				var name = args.Advertisement.LocalName;
				dev = new OSBluetoothLEDevice(
					address: args.BluetoothAddress,
					name: name,
					broadcastTime: args.Timestamp,
					rssi: args.RawSignalStrengthInDBm
				);

				mDiscoveredDevices[args.BluetoothAddress] = dev;
            }

			DeviceDiscovered(dev);
			if (newDiscovery) NewDeviceDiscovered(dev);
			if (nameChanged) DeviceNameChanged(dev);

		}

		public OSBluetoothLEAdvertisementWatcher(string portname)
		{
			this.Initialize();
			this._SerialPort = new SerialPort(portname);
		}
		public OSBluetoothLEAdvertisementWatcher(string portname, int baud)
		{
			this.Initialize();
			this._SerialPort = new SerialPort(portname, baud);
		}
		public OSBluetoothLEAdvertisementWatcher(string portname, int baud, int dataBits)
		{
			this.Initialize();
			this._SerialPort = new SerialPort(portname, baud, Parity.None, dataBits);
		}
		public OSBluetoothLEAdvertisementWatcher(string portname, int baud, int dataBits, StopBits stopBits)
		{
			this.Initialize();
			this._SerialPort = new SerialPort(portname, baud, Parity.None, 8, stopBits);
		}
		public OSBluetoothLEAdvertisementWatcher(string portname, int baud, int dataBits, StopBits stopBits, Parity parity)
		{
			this.Initialize();
			this._SerialPort = new SerialPort(portname, baud, Parity.None, dataBits, stopBits);
		}

		public OSBluetoothLEAdvertisementWatcher(int port, int baud)
		{
			this.Initialize();
			string portname = "Com" + port.ToString();
			this._SerialPort = new SerialPort(portname, baud);
		}
		public OSBluetoothLEAdvertisementWatcher(int port, int baud, int dataBits)
		{
			this.Initialize();
			string portname = "Com" + port.ToString();
			this._SerialPort = new SerialPort(portname, baud, Parity.None, dataBits);
		}
		public OSBluetoothLEAdvertisementWatcher(int port, int baud, int dataBits, StopBits stopBits)
		{
			this.Initialize();
			string portname = "Com" + port.ToString();
			this._SerialPort = new SerialPort(portname, baud, Parity.None, dataBits, stopBits);
		}
		public OSBluetoothLEAdvertisementWatcher(int port, int baud, int dataBits, StopBits stopBits, Parity parity)
		{
			this.Initialize();
			string portname = "Com" + port.ToString();
			this._SerialPort = new SerialPort(portname, baud, Parity.None, dataBits, stopBits);
		}
		public bool Reset()
		{
			if (this._SerialPort.IsOpen)
			{
				this._SerialPort.Close();
				return this.Open();
			}
			return false;
		}
		protected void FireStatusMessage(RS232EventArgs channelArgs)
		{
			channelArgs.iType = this.iType;
			if (this._SuspendDelegateNotifications == false)
			{
				this.FireCommunicationEvent(channelArgs);
			}
			if (this.InLoopBackMode == true)
			{
				if (this.SelfTestEvent != null)
				{
					this.SelfTestEvent(this, channelArgs);
				}
			}
		}

		#endregion

		#region ICloneable Members

		object ICloneable.Clone()
		{
			RS232 deviceClone = new Communication.RS232(
				this._SerialPort.PortName
				, this.BaudRate
				, this.DataBits
				, this.StopBits
				, this.Parity);

			deviceClone.DeviceName = this._DeviceName;

			deviceClone.DeviceID = this._guid;
			deviceClone.ConnectMethod = this._ConnectMethod;
			deviceClone.DeviceType = this._DeviceType;
			deviceClone.PayloadDefinition = this._PayloadDefinition;
			deviceClone.PayloadCriteria = this._PayloadCriteria;
			deviceClone.ParentObject = this.ParentObject;
			deviceClone.CommunicationType = this._CommunicationType;

			return deviceClone;
		}

		#endregion

		#region Inherited Properties

		[XmlIgnoreAttribute()] // [XmlElementAttribute("Name", typeof(string))]
		public override string DeviceName
		{
			get
			{
				return this._DeviceName;
			}
			set
			{
				this._DeviceName = value;
			}
		}
		[XmlIgnoreAttribute()]
		public override string Description
		{
			get
			{
				return "RS232: " + this._SerialPort.PortName;
			}
			set
			{

			}
		}
		[XmlIgnoreAttribute()]
		public override bool IsConnected
		{
			get
			{
				return this._SerialPort.IsOpen;
			}
		}
		protected bool _IsListening = false;
		[XmlIgnoreAttribute()]
		public override bool IsListening
		{
			get
			{
				if (this._SerialPort == null)
				{
					return false;
				}
				if (this.ConnectMethod == ConnectMethods.Client)
				{
					return false;
				}
				return this._IsListening;
			}
		}

		#endregion

		#region Inherited Methods

		public bool Initialize()
		{
			this._CommunicationType = CommunicationTypes.Bluetooth;



			//	this._SerialPort.Handshake = Handshake.RequestToSend;
			return true;
		}

		void _SerialPort_PinChanged(object sender, SerialPinChangedEventArgs e)
		{
			//throw new NotImplementedException();
		}
		public bool Open()
		{
			try
			{

				//	this._SerialPort.Handshake = Handshake.RequestToSend;
				this._SerialPort.Open();
				this._SerialPort.ReadTimeout = 300;
				this._SerialPort.WriteTimeout = 300;
				this._SerialPort.DiscardInBuffer();
				this._SerialPort.DiscardOutBuffer();
				this._SerialPort.DataReceived += new SerialDataReceivedEventHandler(_SerialPort_DataReceived);
				this._SerialPort.PinChanged += new SerialPinChangedEventHandler(_SerialPort_PinChanged);
				//			this._SerialPort.ReadChar();
			}
			catch (Exception ex)
			{
				this.MessageString = ex.Message;
				return false;
			}
			using (RS232EventArgs evt = new RS232EventArgs(this._SerialPort))
			{
				evt.Event = CommunicationEvents.ConnectedAsClient;
				evt.Description = "Connected...";
				FireStatusMessage(evt);
			}
			return true;
		}

		void _SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			using (RS232EventArgs evt = new RS232EventArgs(this._SerialPort))
			{
				evt.Event = CommunicationEvents.ReceiveEnd;
				evt.Description = this._SerialPort.ReadExisting();
				evt.data = Encoding.ASCII.GetBytes(evt.Description);
				FireStatusMessage(evt);
			}
		}
		/// <summary>
		/// Synchronous
		/// </summary>
		/// <returns> byte[]</returns>
		public byte[] Read()
		{
			byte[] retData = null;
			while (this._SerialPort.BytesToRead > 0)
			{
				retData = new byte[this._SerialPort.BytesToRead];
				//  this._SerialPort.
				this._SerialPort.Read(retData, 0, this._SerialPort.BytesToRead);
			}

			return retData;
		}
		public bool Send(string data)
		{
			if ((data == null) || (data.Length == 0))
			{
				return false;
			}
			return this.Send(Encoding.ASCII.GetBytes(data), 0, data.Length);
		}
		public bool Send(byte[] buffer, int offset, int count)
		{
			try
			{
				this._SerialPort.DiscardOutBuffer();
				this._SerialPort.Write(buffer, offset, count);
			}
			catch (Exception ex)
			{
				this.MessageString = ex.Message;
				return false;
			}
			return true;
		}
		public bool Close()
		{
			if (this._ConnectMethod == ConnectMethods.Listener)
			{

			}
			this._SerialPort.Close();
			using (RS232EventArgs evt = new RS232EventArgs(this._SerialPort))
			{
				evt.Event = CommunicationEvents.Disconnected;
				FireStatusMessage(evt);
			}
			return true;
		}
		public void RunEditForm()
		{

		}
		#endregion
	}


}
