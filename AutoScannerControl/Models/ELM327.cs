using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OS.Communication;
using System.Xml.Serialization;
using System.IO.Ports;
using Windows.System.Update;
using Windows.Networking.Sockets;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace OS.AutoScanner.Models
{


	public class ELM327Command
	{
		public DeviceRequestType RequestType { get; set; }
		public string Code { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public Func<string[], object> function { get; set; }

	}


	[Serializable]
	[XmlRootAttribute("CommunicationDevice", Namespace = "", IsNullable = false)]
	public class ELM327 : CommunicationDevice, ICloneable, ICommunicationDevice
	{
        #region Event Sources

        public override event OnSelfTestEvent SelfTestEvent;

        #endregion

        #region Properties

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

		#region Methods

		public static string[] GetPortNames()
		{
			return SerialPort.GetPortNames();
		}

		public ELM327(string portname, int baud)
		{
			this.Initialize();
			this._SerialPort = new SerialPort(portname, baud);
			this._SerialPort.Handshake = Handshake.None;
			
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

		#endregion ICloneable Members

		#region Inherited Properties
		[XmlIgnoreAttribute()]
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
				return "ELM327: " + this._SerialPort.PortName;
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
			this._CommunicationType = CommunicationTypes.RS232;
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

				evt.Description = this._SerialPort.ReadExisting();
				evt.data = Encoding.ASCII.GetBytes(evt.Description);

				//if (evt.data[evt.data.Length - 1] == '\r')
				if (evt.Description.IndexOf('>') > -1)
				{
					evt.Event = CommunicationEvents.ReceiveEnd;
				}
                else
                {
					evt.Event = CommunicationEvents.Receive;

                }
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
