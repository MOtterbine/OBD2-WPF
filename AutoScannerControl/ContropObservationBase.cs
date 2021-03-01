using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
// number styles...
using System.Globalization;
using System.Net.Sockets;
using System.Net;
using FourDSecurity.Emulation.HardwareEmulation;

namespace FourDSecurity.Emulation.ContropObserverEmulation
{
	public enum ResponseType
	{
		Version,
		Switches,
		BuiltInTest,
		None
	}
	public class VersionInfo
	{
		static public System.Reflection.AssemblyName AssemblyVersion
		{
			get
			{
				return System.Reflection.Assembly.GetExecutingAssembly().GetName();
			}
		}
	}
	public class ContropObservationBase : FourDSecurity.Emulation.HardwareEmulation.DeviceEmulationBase
    {

        #region Properties and Fields

        public const int RIGHT_HARD_LIMIT   = 0xF23A;
        public const int LEFT_HARD_LIMIT    = 0x0E3A;
        public const int UP_HARD_LIMIT      = 0x98CC;
        public const int DOWN_HARD_LIMIT    = 0x58BE;

		public const int HighSpeedFactor	= 4;
		public const int LowSpeedFactor		= 1;
		public const int ManualZoomSpeed	= 0x20;

		protected int _RightPanLimit = 0xF23A;
		protected int _LeftPanLimit = 0x0E3A;
		protected int _UpperTiltLimit = 0x98CC;
		protected int _LowerTiltLimit = 0x58BE;

		protected int _IRZoomValue = 0x4000;

        protected AutoResetEvent autoEvent = new AutoResetEvent(false);
        protected int _GimbalMovementFactor = 2;
        protected MemoryStream messageStream = new MemoryStream();
        protected int _HorizontalPosition = 0x8000;
        protected int _HorizontalSpeed = 0x00;
        protected int _VerticalPosition = 0x8000;
        protected int _VerticalSpeed = 0x00;
        protected int _ZoomPosition = 0x0010;
        protected int _ZoomSpeed = 0x00;
        protected int _FocusPosition = 0x8000;
        //		protected int _FocusSpeed = 0x00;

        protected ResponseType _ResponseType = ResponseType.None;
        protected int _RequestedHorizontalPosition = 0x0000;
        protected int _RequestedVerticalPosition = 0x0000;
        protected int _RequestedZoomPosition = 0x0000;

        protected bool _ExecutingGoto = false;
        protected byte _ExecutingGotoStatus = 0x00;
        protected SwitchData _SwitchData = new SwitchData();

        protected bool _IsRunningSend = false;
        protected bool _IsRunningRcv = false;
        protected bool _IsInError = false;
        protected bool _Halt = true;
        protected System.Threading.Thread _IPSendThread = null;
        protected System.Threading.Thread _IPRcvThread = null;

		protected int _BuiltInTestCounter = 0x00;


        #endregion

        #region Base Overrides

        public override int EngineTimerInterval
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public override bool IsRunning
        {
            get { throw new NotImplementedException(); }
        }
        public override bool IsInError
        {
            get { throw new NotImplementedException(); }
        }
        public override string Description
        {
            get { throw new NotImplementedException(); }
        }
        public override string StatusString
        {
            get { throw new NotImplementedException(); }
        }
		public override string SourceAddress
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}
		public override int SourcePort
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}
		public override string DestinationAddress
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}
		public override int? DestinationPort
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}
		public override string BaseSourceAddress
		{
			get
			{
				return Properties.ContropObservationCam.Default.IPAddress;
			}
			set
			{
				Properties.ContropObservationCam.Default.IPAddress = value;
				Properties.ContropObservationCam.Default.Save();
			}
		}
		public override int BaseSourcePort
		{
			get
			{
				return Properties.ContropObservationCam.Default.Port;
			}
			set
			{
				Properties.ContropObservationCam.Default.Port = value;
				Properties.ContropObservationCam.Default.Save();
			}
		}
		public override string BaseDestinationAddress
		{
			get
			{
				return null;
			}
			set
			{
			}
		}
		public override int? BaseDestinationPort
		{
			get
			{
				return null;
			}
			set
			{
			}
		}
		public override int DeviceCount
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public override void RunEmulation()
        {
        }
        public override void Halt()
        {
        }
        public override void ShowPropertiesForm()
        {
        }
        public override void DoPeriodicFunction()
        {
            if (this._SwitchData.LowSpeed == true)
            {
				this._GimbalMovementFactor = LowSpeedFactor;
            }
            else
            {
				this._GimbalMovementFactor = HighSpeedFactor;
            }
            if (this._ExecutingGoto == true)
            {
                if (this._HorizontalSpeed > 0)
                {
                    if ((this._HorizontalPosition + (this._HorizontalSpeed + this._GimbalMovementFactor)) >= this._RequestedHorizontalPosition)
                    {
                        this._HorizontalPosition = this._RequestedHorizontalPosition;
                        this._HorizontalSpeed = 0x00;
                        this._ExecutingGotoStatus |= 0x01;
                    }
                    else
                    {
                        this._HorizontalPosition += (this._HorizontalSpeed * this._GimbalMovementFactor);
                    }
                }
                else
                {
                    if ((this._HorizontalPosition + (this._HorizontalSpeed + this._GimbalMovementFactor)) <= this._RequestedHorizontalPosition)
                    {
                        this._HorizontalPosition = this._RequestedHorizontalPosition;
                        this._HorizontalSpeed = 0x00;
                        this._ExecutingGotoStatus |= 0x01;
                    }
                    else
                    {
                        this._HorizontalPosition += (this._HorizontalSpeed * this._GimbalMovementFactor);
                    }
                }

                // TILT
                if (this._VerticalSpeed > 0)
                {
                    if ((this._VerticalPosition + (this._VerticalSpeed * this._GimbalMovementFactor)) >= this._RequestedVerticalPosition)
                    {
                        this._VerticalPosition = this._RequestedVerticalPosition;
                        this._VerticalSpeed = 0x00;
                        this._ExecutingGotoStatus |= 0x02;
                    }
                    else
                    {
                        this._VerticalPosition += (this._VerticalSpeed * this._GimbalMovementFactor);
                    }
                }
                else
                {
                    if ((this._VerticalPosition + (this._VerticalSpeed * this._GimbalMovementFactor)) <= this._RequestedVerticalPosition)
                    {
                        this._VerticalPosition = this._RequestedVerticalPosition;
                        this._VerticalSpeed = 0x00;
                        this._ExecutingGotoStatus |= 0x02;
                    }
                    else
                    {
                        this._VerticalPosition += (this._VerticalSpeed * this._GimbalMovementFactor);
                    }
                }
                // ZOOM
                if (this._SwitchData.IsCCD == true)
                {
                    if (this._ZoomSpeed > 0)
                    {
                        if ((this._ZoomPosition + (this._ZoomSpeed * this._GimbalMovementFactor)) >= this._RequestedZoomPosition)
                        {
                            this._ZoomPosition = this._RequestedZoomPosition;
                            this._ZoomSpeed = 0x00;
                            this._ExecutingGotoStatus |= 0x04;
                        }
                        else
                        {
                            this._ZoomPosition += (this._ZoomSpeed * this._GimbalMovementFactor);
                        }
                    }
                    else
                    {
                        if ((this._ZoomPosition + (this._ZoomSpeed * this._GimbalMovementFactor)) <= this._RequestedZoomPosition)
                        {
                            this._ZoomPosition = this._RequestedZoomPosition;
                            this._ZoomSpeed = 0x00;
                            this._ExecutingGotoStatus |= 0x04;
                        }
                        else
                        {
                            this._ZoomPosition += (this._ZoomSpeed * this._GimbalMovementFactor);
                        }
                    }
                }
                else
                {
                    this._ExecutingGotoStatus |= 0x04;
                }
                // KILL THE GOTO OPERATION ONCE EVERYTHING IS WHERE IT IS SUPPOSED TO BE
                if ((this._ExecutingGotoStatus & 0x07) == 0x07)
                {
                    this._ExecutingGoto = false;
                    this._ExecutingGotoStatus = 0x00;
                }

            }
            else
            {
                // PAN
                if ((this._HorizontalPosition + (this._HorizontalSpeed * this._GimbalMovementFactor)) > this._RightPanLimit)
                {
                    this._HorizontalPosition = this._RightPanLimit;
                    this._HorizontalSpeed = 0x00;
                }
                else if ((this._HorizontalPosition + (this._HorizontalSpeed * this._GimbalMovementFactor)) < this._LeftPanLimit)
                {
                    this._HorizontalPosition = this._LeftPanLimit;
                    this._HorizontalSpeed = 0x00;
                }
                else
                {
                    this._HorizontalPosition += (this._HorizontalSpeed * this._GimbalMovementFactor);
                }
                // TILT
                if ((this._VerticalPosition + (this._VerticalSpeed * this._GimbalMovementFactor)) > this._UpperTiltLimit)
                {
                    this._VerticalPosition = this._UpperTiltLimit;
                    this._VerticalSpeed = 0x00;
                }
                else if ((this._VerticalPosition + (this._VerticalSpeed * this._GimbalMovementFactor)) < this._LowerTiltLimit)
                {
                    this._VerticalPosition = this._LowerTiltLimit;
                    this._VerticalSpeed = 0x00;
                }
                else
                {
                    this._VerticalPosition += (this._VerticalSpeed * this._GimbalMovementFactor);
                }
                // ZOOM
                if (this._SwitchData.ToString().Substring(4, 1) == "8")
                {
                    this._ZoomSpeed = -1 * ManualZoomSpeed;
                }
                else if (this._SwitchData.ToString().Substring(4, 1) == "4")
                {
                    this._ZoomSpeed = ManualZoomSpeed;
                }
                else
                {
                    this._ZoomSpeed = 0x00;
                }
                if ((this._ZoomPosition + (this._ZoomSpeed * this._GimbalMovementFactor)) > 0x4000)
                {
                    this._ZoomPosition = 0x4000;
                    this._ZoomSpeed = 0x00;
                }
                else if ((this._ZoomPosition + (this._ZoomSpeed * this._GimbalMovementFactor)) < 0x0000)
                {
                    this._ZoomPosition = 0x0000;
                    this._ZoomSpeed = 0x00;
                }
                else
                {
                    this._ZoomPosition += (this._ZoomSpeed * this._GimbalMovementFactor);
                }

            }
        }
        public override void SaveConfiguration()
        {
        }

        #endregion

        #region Virtual Members

		protected virtual int GotoSpeed
		{
			get
			{
				return 0x30;
			}
		}
		protected virtual ResponseType ProcessResponse(string responseData)
        {
			this.messageStream.Seek(1, SeekOrigin.Begin);
			byte reqChar = (byte)this.messageStream.ReadByte();
			string txt = "";
			int dataValue = 0;
			switch ((char)reqChar)
			{
				case 'J': // pan/tilt speed
					if (this._ExecutingGoto == false)
					{
						txt = responseData.Substring(3, 2);
						this._HorizontalSpeed = int.Parse(txt, NumberStyles.AllowHexSpecifier);
						if (txt == "80")
						{
							this._HorizontalSpeed = 0x00;
						}
						else
						{
							this._HorizontalSpeed -= 0x80;
						}
						txt = responseData.Substring(6, 2);
						this._VerticalSpeed = int.Parse(txt, NumberStyles.AllowHexSpecifier);
						if (txt == "80")
						{
							this._VerticalSpeed = 0x00;
						}
						else
						{
							this._VerticalSpeed -= 0x80;
						}
						this._HorizontalSpeed /= 3;
						this._VerticalSpeed /= 3;
					}
					break;
				case 'P': // GO TO POSITION AND ZOOM
					txt = responseData.Substring(3, 4);
					this._RequestedHorizontalPosition = int.Parse(txt, NumberStyles.AllowHexSpecifier);
					txt = responseData.Substring(8, 4);
					this._RequestedVerticalPosition = int.Parse(txt, NumberStyles.AllowHexSpecifier);
					txt = responseData.Substring(13, 4);
					//	"$w=102602000";
					if (this._SwitchData.IsCCD)
					{
						this._RequestedZoomPosition = int.Parse(txt, NumberStyles.AllowHexSpecifier);
					}
					this._ZoomSpeed = this.GotoSpeed;
					this._HorizontalSpeed = this.GotoSpeed;
					this._VerticalSpeed = this.GotoSpeed;
					if (this._VerticalPosition > this._RequestedVerticalPosition)
					{
						this._VerticalSpeed *= -1;
					}
					if (this._HorizontalPosition > this._RequestedHorizontalPosition)
					{
						this._HorizontalSpeed *= -1;
					}
					if (this._SwitchData.IsCCD)
					{
						if (this._ZoomPosition > this._RequestedZoomPosition)
						{
							this._ZoomSpeed *= -1;
						}
					}
					this._ExecutingGoto = true;
					this._ExecutingGotoStatus = 0x00;
					break;
				case 'W':
					txt = responseData.Substring(3, 9);
					this._SwitchData.SetSwitchesFromString(txt);
					break;
				case 'R':
					switch (responseData.Substring(3, 1).ToUpper())
					{
						case "V":
							return ResponseType.Version;
						case "W":
							return ResponseType.Switches;
						case "B":
							this._BuiltInTestCounter = 0x00;
							return ResponseType.BuiltInTest;
						default:
							break;
					}
					break;
				case 'S':
					dataValue = int.Parse(responseData.Substring(5, 4), NumberStyles.AllowHexSpecifier);
					switch (responseData.Substring(3, 1).ToUpper())
					{
						case "U":
							if (dataValue <= UP_HARD_LIMIT && dataValue >= DOWN_HARD_LIMIT)
							{
								this._UpperTiltLimit = dataValue;
							}
							else
							{
								this._UpperTiltLimit = UP_HARD_LIMIT;
							}
							break;
						case "D":
							if (dataValue <= UP_HARD_LIMIT && dataValue >= DOWN_HARD_LIMIT)
							{
								this._LowerTiltLimit = dataValue;
							}
							else
							{
								this._LowerTiltLimit = DOWN_HARD_LIMIT;
							}
							break;
						case "L":
							if (dataValue >= LEFT_HARD_LIMIT && dataValue <= RIGHT_HARD_LIMIT)
							{
								this._LeftPanLimit = dataValue;
							}
							else
							{
								this._LeftPanLimit = LEFT_HARD_LIMIT;
							}
							break;
						case "R":
							if (dataValue >= LEFT_HARD_LIMIT && dataValue <= RIGHT_HARD_LIMIT)
							{
								this._RightPanLimit = dataValue;
							}
							else
							{
								this._RightPanLimit = RIGHT_HARD_LIMIT;
							}
							break;
					}
					break;
				default:
					break;
			}

			return ResponseType.None;
        }
		protected virtual ResponseType ParseData()
		{
			try
			{
				byte rcvdChecksum = 0;
				bool packetReady = false;
				string strMessage = "";
				byte[] tBuf = new byte[32];
				int startIndex = -1;
				int endIndex = -1;
				string txtBuf = "";
				if (this.messageStream.Length >= 3)
				{
					if (this.messageStream.Length > 41)
					{
						//	Debug.WriteLine(string.Format("!!!! Bad Message Stream !!!! {0}", strMessage));
						messageStream = new MemoryStream();
						//this.messageStream.SetLength(0);
						return ResponseType.None; ;
					}
					this.messageStream.Position = 0;
					this.messageStream.Read(tBuf, 0, (int)(this.messageStream.Length));
					txtBuf = System.Text.Encoding.ASCII.GetString(tBuf, 0, (int)(this.messageStream.Length));
					startIndex = txtBuf.IndexOf("$");
					endIndex = -1;
					if (startIndex >= 0)
					{
						endIndex = txtBuf.IndexOf("\r\n", startIndex);
						if (endIndex > startIndex)
						{
							if (this.messageStream.Length > endIndex + 1)
							{
								rcvdChecksum = tBuf[endIndex - 1];
								packetReady = true;
							}
							else // Missing Checksum
							{
								//		Debug.WriteLine(string.Format("!!!! Missing checksum !!!! {0}", strMessage));
								return ResponseType.None; ;
							}
						}
						else // Missing (or invalid) endindex
						{
							//		Debug.WriteLine(string.Format("!!!! Bad EndIndex !!!! {0}", strMessage));
							return ResponseType.None;
						}
					}
					else // Missing a start index
					{
						this.messageStream.SetLength(0);
						return ResponseType.None;
					}
				}
				if (packetReady == false)
				{
					//	Debug.WriteLine(string.Format("!!!! Packet NOT Ready !!!! {0}", strMessage));
					return ResponseType.None;
				}

				// ENSURE THE CHECKSUM AGREES...
				strMessage = txtBuf.Substring(startIndex, endIndex - startIndex);
				byte chkSum = 0;
				if (this.CalculateChecksum(strMessage, 1, strMessage.Length - 1, out chkSum) == false)
				{
					this.messageStream.SetLength(0);
					return ResponseType.None;
				}
				// Checksum Failure
				if (chkSum != rcvdChecksum)
				{
					this.messageStream.SetLength(0);
					return ResponseType.None;
				}
				return this.ProcessResponse(strMessage);
			}
			catch (Exception ex)
			{
				string errtxt = ex.Message;
			}
			return ResponseType.None;
		}

        #endregion

		#region Standard Methods

		public ContropObservationBase()
		{
			this._SwitchData.SwitchEvent += new SwitchDataEvent(OnSwitchEvent);
		}

		void OnSwitchEvent(object sender, SwitchEventArgs e)
		{
			if (this._SwitchData.IsCCD == false)
			{
				switch (this._IRZoomValue)
				{
					case 0x4000:
						if ((bool)e.dataObject)
						{
							this._IRZoomValue = 0x6000;
						}
						else
						{
						}
						break;
					case 0x6000:
						if ((bool)e.dataObject)
						{
							this._IRZoomValue = 0x7000;
						}
						else
						{
							this._IRZoomValue = 0x4000;
						}
						break;
					case 0x7000:
						if ((bool)e.dataObject)
						{

						}
						else
						{
							this._IRZoomValue = 0x6000;
						}
						break;
					default:
						if ((bool)e.dataObject)
						{
							this._IRZoomValue = 0x7000;
						}
						else
						{
							this._IRZoomValue = 0x4000;
						}
						break;

				}
			}
			throw new NotImplementedException();
		}
		protected bool SendData(byte[] data)
		{
			if (this.DestinationPort == null)
			{
				this._EventLog.WriteToEventLog("No Destination (EOMC) Port Specified to send to", System.Diagnostics.EventLogEntryType.Error);
				return false;
			}
			UdpClient client = new UdpClient(this.DestinationPort.Value);
			try
			{
				if (this.DestinationPort == null)
				{
					this._EventLog.WriteToEventLog("No Destination (EOMC) Port Specified to send to", System.Diagnostics.EventLogEntryType.Error);
					return false;
				}
				System.Net.IPEndPoint LocalEP = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(this.DestinationAddress), this.DestinationPort.Value);
				//       Byte[] byteData = System.Text.Encoding.ASCII.GetBytes(data);
				client.Send(data, data.Length, LocalEP);
				client.Close();
			}
			catch (Exception ex)
			{
				client.Close();
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("Send error on {0} - {1}", client.ToString(), ex.Message);
				this._EventLog.WriteToEventLog(sb.ToString(), System.Diagnostics.EventLogEntryType.Error);
				using (EmulatorEventArgs evt = new EmulatorEventArgs())
				{
					evt.EmulatorKey = this.SourcePort.ToString();
					evt.StatusEventType = EmulatorEventTypes.ThreadFailure;
					evt.Description = sb.ToString();
				}
				return false;
			}

			return true;

		}
		public void ReceiveData()
		{
			StringBuilder sb = new StringBuilder();
			Socket server = null;
			System.Net.IPEndPoint LocalEP = null;
			System.Net.EndPoint remEndPoint = null;
			try
			{
				if (this.DestinationPort == null)
				{
					this._EventLog.WriteToEventLog("No Destination (EOMC) Port Specified to receive from", System.Diagnostics.EventLogEntryType.Error);
					return;
				}
				server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				remEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(this.DestinationAddress), this.DestinationPort.Value);
				LocalEP = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(this.SourceAddress), this.SourcePort);
				server.Bind(LocalEP);
				byte[] rcvData = new byte[32];
				this._IsRunningRcv = true;
				while (true)
				{
					if (this._Halt == true)
					{
						server.Close();
						this._IsRunningRcv = false;
						return;
					}
					if (server.Poll(10, SelectMode.SelectRead) == true)
					{
						if (server.ReceiveFrom(rcvData, 0, rcvData.Length, SocketFlags.None, ref remEndPoint) > 0)
						{
							this.messageStream.SetLength(0);//Seek(0,SeekOrigin.Begin);
							this.messageStream.Seek(0, SeekOrigin.Begin);
							this.messageStream.Write(rcvData, 0, rcvData.Length);
							this._ResponseType = this.ParseData();
						}
					}
					for (int i = 0; i < rcvData.Length; i++)
					{
						rcvData[i] = 0x00;
					}
				}
			}
			catch (Exception ex)
			{
				this.Halt();
				server.Close();
				this._IsRunningRcv = false;
				this._IsInError = true;
				sb.Remove(0, sb.Length);
				sb.AppendFormat("Receive error on {0} - {1}", LocalEP.ToString(), ex.Message);
				this._EventLog.WriteToEventLog(sb.ToString(), System.Diagnostics.EventLogEntryType.Error);
				using (EmulatorEventArgs evt = new EmulatorEventArgs())
				{
					evt.EmulatorKey = this.SourcePort.ToString();
					evt.StatusEventType = EmulatorEventTypes.ThreadFailure;
					evt.Description = sb.ToString();
					this.FireEmulatorEvent(evt);
				}
				//	MessageBox.Show("Receive Thread Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
		}
		protected bool CalculateChecksum(string data, int start, int end, out byte chkSum)
		{
			byte[] bData = System.Text.Encoding.ASCII.GetBytes(data);

			return this.CalculateChecksum(bData, start, end, out chkSum);
		}
		protected bool CalculateChecksum(byte[] data, int start, int end, out byte chkSum)
		{
			chkSum = 0;
			if (end < start)
			{
				return false;
			}
			if (end > data.Length - 1)
			{
				return false;
			}
			for (int i = start; i < end; i++)
			{
				chkSum += data[i];
			}
			return true;
		}
		protected byte CalculateChecksum(string data, int start)
		{
			return this.CalculateChecksum(Encoding.ASCII.GetBytes(data), start);
		}
		protected byte CalculateChecksum(byte[] data, int start)
		{
			byte chSum = 0x00;
			for (int i = start; i < data.Length; i++)
			{
				chSum += data[i];
			}
			return chSum;
		}

		#endregion

	}

//////////////////////////////////////////////////////////////////////////////////////////
	
	
	public delegate void SwitchDataEvent(object sender, SwitchEventArgs e);

    public class SwitchData
    {

		#region Event Sourcing

//		public ManualResetEvent ThreadRunning = new ManualResetEvent(true);
		public event SwitchDataEvent SwitchEvent = null;
		protected void FireSwitchEvent(SwitchEventArgs e)
		{
			if (this.SwitchEvent != null)
			{
				this.SwitchEvent(this, e);
			}
		}

		#endregion


        #region Properties and Fields

        public override string ToString()
        {
            return this._SwitchString;
        }
        protected string _SwitchString = "012602000";
        public bool IsCCD
        {
            get
            {
                int i = System.Int32.Parse(this._SwitchString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                return Convert.ToBoolean(i & 0x20);
            }
        }
		public bool LowSpeed
		{
			get
			{
				int i = System.Int32.Parse(this._SwitchString.Substring(2, 1), NumberStyles.AllowHexSpecifier);
				return Convert.ToBoolean(i & 0x1);
			}
			set
			{
				byte bt = byte.Parse(_SwitchString.Substring(2, 1), NumberStyles.AllowHexSpecifier);
				bt ^= 1;
				string text = _SwitchString.Remove(2, 1);
				text = text.Insert(2, bt.ToString("X"));
				this._SwitchString = text;
			}
		}
		public bool IRZoomIn
		{
			get
			{
				int i = System.Int32.Parse(this._SwitchString.Substring(2, 1), NumberStyles.AllowHexSpecifier);
				return Convert.ToBoolean(i & 0x1);
			}
			set
			{
				byte bt = byte.Parse(_SwitchString.Substring(2, 1), NumberStyles.AllowHexSpecifier);
				bt ^= 1;
				string text = _SwitchString.Remove(2, 1);
				text = text.Insert(2, bt.ToString("X"));
				this._SwitchString = text;
			}
		}

        #endregion

        #region Standard Methods

        public void SetSwitchesFromString(string data)
        {
            if ((data == null) || (data.Length == 0))
            {
                return;
            }

            byte bt = byte.Parse(data.Substring(2, 1), NumberStyles.AllowHexSpecifier);
            //    if ((bt & 0x8) == 0x8) bt ^ 8;
            bt ^= 8;
            string text = data.Remove(2, 1);
            text = text.Insert(2, bt.ToString("X"));
            this._SwitchString = text;
            bt = byte.Parse(data.Substring(7, 1), NumberStyles.AllowHexSpecifier);
			if((bt & 0x03) != 0x00) // either IR zoom is on
			{
				using (SwitchEventArgs evt = new SwitchEventArgs())
				{
					if ((bt & 0x02) != 0x00)
					{
						evt.dataObject = (bool)true; // zoom in
					}
					else
					{
						evt.dataObject = (bool)false; // zoom out

					}
					evt.StatusEventType = SwitchEventTypes.IRZoom;
					this.FireSwitchEvent(evt);
				}
			}
        }

        #endregion

    }
	public enum SwitchEventTypes
	{
		IRZoom,
		Undefined
	}
	public class SwitchEventArgs : EventArgs, IDisposable
	{

		#region Properties and Fields
		/// <summary>
		/// General string usage
		/// </summary>
		public string Description = "";
		/// <summary>
		/// General object usage
		/// </summary>
		public object dataObject = null;
		/// <summary>
		/// Instantiation of the EventType enum
		/// </summary>
		public SwitchEventTypes StatusEventType = SwitchEventTypes.Undefined;

		#endregion

		#region Standard Methods

		public SwitchEventArgs()
		{
		}

		#endregion

		#region IDisposable Members

		private bool disposed = false;
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
		// Dispose(bool disposing) executes in two distinct scenarios.
		// If disposing equals true, the method has been called directly
		// or indirectly by a user's code. Managed and unmanaged resources
		// can be disposed.
		// If disposing equals false, the method has been called by the
		// runtime from inside the finalizer and you should not reference
		// other objects. Only unmanaged resources can be disposed.
		private void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.disposed)
			{
				// If disposing equals true, dispose all managed
				// and unmanaged resources.
				if (disposing)
				{

					// Dispose managed resources.
					//		GC.Collect();
					//		GC.GetTotalMemory(true);
					//	component.Dispose();
				}
				//				System.Runtime.InteropServices.Marshal.(ComObject(this.TSEngine.getaddr
				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.

				//	CloseHandle(handle);
				//	handle = IntPtr.Zero;

				// Note disposing has been done.
				disposed = true;
			}
		}
		~SwitchEventArgs()
		{
			// Do not re-create Dispose clean-up code here.
			// Calling Dispose(false) is optimal in terms of
			// readability and maintainability.
			Dispose(false);
		}
		#endregion

	}




}
