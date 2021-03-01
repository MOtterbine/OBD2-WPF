using System;
using System.IO;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;
using FourDSecurity.Emulation.HardwareEmulation;
// REGISTRY KEY ACCESS
using Microsoft.Win32;
using System.Security.Permissions;
using System.Globalization;


[assembly: RegistryPermissionAttribute(SecurityAction.RequestMinimum,
ViewAndModify =  "HKEY_LOCAL_MACHINE")]

namespace FourDSecurity.Emulation.ContropObserverEmulation
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class ContropObservationCamA : ContropObservationBase// FourDSecurity.Emulation.HardwareEmulation.DeviceEmulationBase
	{
		#region Properties and Fields
     
//        AutoResetEvent autoEvent = new AutoResetEvent(false);
//        private int _GimbalMovementFactor = 2;
//        MemoryStream messageStream	= new MemoryStream();
//        private int _HorizontalPosition = 0x8000;
//        private int _HorizontalSpeed = 0x00;
//        private int _VerticalPosition = 0x8000;
//        private int _VerticalSpeed = 0x00;
//        private int _ZoomPosition = 0x0010;
//        private int _ZoomSpeed = 0x00;
//        private int _FocusPosition = 0x8000;
////		private int _FocusSpeed = 0x00;

        //private int _RequestedHorizontalPosition = 0x0000;
        //private int _RequestedVerticalPosition = 0x0000;
        //private int _RequestedZoomPosition = 0x0000;

        //private bool _ExecutingGoto = false;
        //private byte _ExecutingGotoStatus = 0x00;
        //private SwitchData _SwitchData = new SwitchData();

        //private bool _IsRunningSend = false;
        //private bool _IsRunningRcv = false;
        //private bool _IsInError = false;

		protected const int BuiltInTestCount = 30; // 3 seconds of 100ms intervals (emulation loop time)
		public override bool IsInError
		{
			get 
			{
				return this._IsInError;
			}
		}
		public override string StatusString
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("CO30A: {0}:{1}\n\n\r", this.SourceAddress, this.SourcePort.ToString());
				if (this._IsInError == true)
				{
					sb.Append("Error - See Event Log.");
				}
				else
				{
					if (this._SwitchData.IsCCD == true)
					{
						sb.Append("Day Mode\n\r");
					}
					else
					{
						sb.Append("Night Mode\n\r");

					}
					sb.Append("Switches: ");
					sb.Append(this._SwitchData.ToString());
					sb.Append("\n\rPan: ");
					sb.Append(this._HorizontalPosition.ToString("X4"));
					sb.Append("\n\r");
					sb.Append("Tilt: ");
					sb.Append(this._VerticalPosition.ToString("X4"));
					sb.Append("\n\r");
					sb.Append("Zoom: ");
					if (this._SwitchData.IsCCD)
					{
						sb.Append(this._ZoomPosition.ToString("X4"));
					}
					else
					{
						sb.Append("0000");
					}
					sb.Append("\n\r");
					sb.Append("Focus: ");
					sb.Append(this._FocusPosition.ToString("X4"));
					sb.Append("\n\r");
				}
				return sb.ToString();
			}
		}

        //private bool _Halt = true;
        //System.Threading.Thread _IPSendThread = null;
        //System.Threading.Thread _IPRcvThread = null;
		public static Form _PropertiesForm = null;
	
		public override string Description
		{
			get
			{
				return  "Vendor1 CO30A, ver. " + VersionInfo.AssemblyVersion.Version.ToString();
			}
		}

        public override int EngineTimerInterval
        {
            get
            {
                return Properties.ContropObservationCamA.Default.EngineTimerInterval;
            }
            set
            {
                Properties.ContropObservationCamA.Default.EngineTimerInterval = value;
            }
        }
        public override bool IsRunning
        {
            get
            {
                return (this._IsRunningRcv || this._IsRunningSend);
            }
        }
        public override string SourceAddress
        {
            get
            {
                return this._SourceAddress;
            }
            set
            {
                this._SourceAddress = value;
            }
        }
        private string _SourceAddress = "";
        public override int SourcePort
        {
            get
            {
                return this._SourcePort;
            }
            set
            {
                this._SourcePort = value;
            }
        }
        private int _SourcePort = 0;
        public override string DestinationAddress
        {
            get
            {
                return this._DestinationAddress;
            }
            set
            {
                this._DestinationAddress = value;
            }
        }
        private string _DestinationAddress = "";
        public override int ? DestinationPort
        {
            get
            {
                return this._DestinationPort;
            }
            set
            {
				if (value != null)
				{
					this._DestinationPort = value.Value;
				}
            }
        }
        private int _DestinationPort = 0;
        public override int DeviceCount
        {
            get
            {
                return this._DeviceCount;
            }
            set
            {
                this._DeviceCount = value;
            }
        }
        private int _DeviceCount = 0;


		#endregion

		#region Overriden Properties and Fields

		protected override int GotoSpeed
		{
			get
			{
				return 0x50;
			}
		}

		#endregion

		#region Overriden Methods

		public override void Halt()
		{
			this._Halt = true;
			this._IsRunningRcv = false;
			this._IsRunningSend = false;
//			if(this._IPRcvThread != null)
//			{
//				this._IPRcvThread.Abort();
//			}
		}
		public override void RunEmulation()
		{
			this._Halt = false;

			this._IPSendThread = new Thread(new ThreadStart(this.Emulate));
			this._IPSendThread.IsBackground = true;
			this._IPSendThread.Start();

			this._IPRcvThread = new Thread(new ThreadStart(this.ReceiveData));
			this._IPRcvThread.IsBackground = true;
			this._IPRcvThread.Start();
		}
		public override void ShowPropertiesForm()
		{
			using (ContropObservationOptions optForm = new ContropObservationOptions())
			{
				optForm.ShowDialog();
			}
			
		}
        public override void SaveConfiguration()
        {
            Properties.ContropObservationCamA.Default.LocalIPAddress = this._SourceAddress;
            Properties.ContropObservationCamA.Default.LocalPort = this._SourcePort;
            Properties.ContropObservationCamA.Default.RemoteIPAddress = this._DestinationAddress;
            Properties.ContropObservationCamA.Default.RemotePort = this._DestinationPort;
            Properties.ContropObservationCamA.Default.DeviceCount = this._DeviceCount;
            Properties.ContropObservationCamA.Default.Save();
        }

		#endregion

		#region Standard Methods

		public ContropObservationCamA(string CamIPAddress, int CamPort, string DestAddress, int DestPort )
		{
			this.SourceAddress = CamIPAddress;
			this.SourcePort = CamPort;
			this.DestinationAddress = DestAddress;
			this.DestinationPort = DestPort;
		}
		public ContropObservationCamA()
		{
            Properties.ContropObservationCamA.Default.Reload();
            this._SourceAddress = Properties.ContropObservationCamA.Default.LocalIPAddress;
            this._SourcePort = Properties.ContropObservationCamA.Default.LocalPort;
            this._DestinationAddress = Properties.ContropObservationCamA.Default.RemoteIPAddress;
            this._DestinationPort = Properties.ContropObservationCamA.Default.RemotePort;
            this._DeviceCount = Properties.ContropObservationCamA.Default.DeviceCount;
        }
		//protected override ResponseType ParseData()
		//{
		//    try
		//    {
		//        byte rcvdChecksum = 0;
		//        bool packetReady = false;
		//        string strMessage = "";
		//        byte [] tBuf = new byte[32];
		//        int startIndex = -1;
		//        int endIndex = -1;
		//        string txtBuf = "";
		//        if(this.messageStream.Length >= 3)
		//        {
		//            if(this.messageStream.Length > 41)
		//            {
		//                //	Debug.WriteLine(string.Format("!!!! Bad Message Stream !!!! {0}", strMessage));
		//                messageStream = new MemoryStream();
		//                //this.messageStream.SetLength(0);
		//                return ResponseType.None; ;
		//            }
		//            this.messageStream.Position = 0;
		//            this.messageStream.Read(tBuf,0,(int)(this.messageStream.Length));
		//            txtBuf = System.Text.Encoding.ASCII.GetString(tBuf,0,(int)(this.messageStream.Length));
		//            startIndex = txtBuf.IndexOf("$");
		//            endIndex = -1;
		//            if(startIndex >= 0)
		//            {
		//                endIndex = txtBuf.IndexOf("\r\n",startIndex);
		//                if(endIndex > startIndex)
		//                {
		//                    if(this.messageStream.Length > endIndex + 1)
		//                    {
		//                        rcvdChecksum = tBuf[endIndex - 1];
		//                        packetReady = true;
		//                    }
		//                    else // Missing Checksum
		//                    {
		//                        //		Debug.WriteLine(string.Format("!!!! Missing checksum !!!! {0}", strMessage));
		//                        return ResponseType.None; ;
		//                    }
		//                }
		//                else // Missing (or invalid) endindex
		//                {
		//                    //		Debug.WriteLine(string.Format("!!!! Bad EndIndex !!!! {0}", strMessage));
		//                    return ResponseType.None;
		//                }
		//            }
		//            else // Missing a start index
		//            {
		//                this.messageStream.SetLength(0);
		//                return ResponseType.None;
		//            }
		//        }
		//        if(packetReady == false)
		//        {
		//            //	Debug.WriteLine(string.Format("!!!! Packet NOT Ready !!!! {0}", strMessage));
		//            return ResponseType.None;
		//        }

		//        // ENSURE THE CHECKSUM AGREES...
		//        strMessage = txtBuf.Substring(startIndex,endIndex-startIndex);
		//        byte chkSum = 0;
		//        if(this.CalculateChecksum(strMessage,1,strMessage.Length-1,out chkSum) == false)
		//        {
		//            this.messageStream.SetLength(0);
		//            return ResponseType.None;	
		//        }
		//        // Checksum Failure
		//        if(chkSum != rcvdChecksum)
		//        {
		//            this.messageStream.SetLength(0);
		//            return ResponseType.None;	
		//        }
		//        return this.ProcessResponse(strMessage);
		//    }
		//    catch(Exception ex)
		//    {
		//        string errtxt = ex.Message;
		//    }
		//    return ResponseType.None;
		//}
		//protected override ResponseType ProcessResponse(string responseData)
		//{
		//    this.messageStream.Seek(1, SeekOrigin.Begin);
		//    byte reqChar = (byte)this.messageStream.ReadByte();
		//    switch ((char)reqChar)
		//    {
		//        case 'R':
		//            switch (responseData.Substring(3, 1).ToUpper())
		//            {
		//                case "V":
		//                    return ResponseType.Version;
		//                case "W":
		//                    return ResponseType.Switches;
		//            }
		//            break;
		//        default:
		//            break;
		//    }
		//    return base.ProcessResponse(responseData);
		//}
		public void Emulate()
		{
			this._IsInError = false;

			StringBuilder sb = new StringBuilder();
			System.Random rGen = new Random(DateTime.Now.Millisecond);
			int rCount = rGen.Next(30);
			byte chSum = 0x00;
			this._IsRunningSend = true;
			this.ThreadRunning.Reset();
			UdpClient udpclient = new UdpClient(0);
			System.Net.IPEndPoint LocalEP = null;
			try
			{
				try
				{
					if (this.DestinationPort == null)
					{
						this._EventLog.WriteToEventLog("No Destination (EOMC) Port Specified", System.Diagnostics.EventLogEntryType.Error);
						return;
					}
					LocalEP = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(this.DestinationAddress), this.DestinationPort.Value);
				}
				catch(Exception ex)
				{
					this.Halt();
					udpclient.Close();
					this._IsRunningSend = false;
					this.ThreadRunning.Set();
					sb.Remove(0, sb.Length);
					sb.AppendFormat("Initialization Error: {0} - {1}:{2} ", ex.Message , this.DestinationAddress, this.DestinationPort);
					this._EventLog.WriteToEventLog(sb.ToString(), System.Diagnostics.EventLogEntryType.Error);
					using (EmulatorEventArgs evt = new EmulatorEventArgs())
					{
						evt.EmulatorKey = this.SourcePort.ToString();
						evt.StatusEventType = EmulatorEventTypes.ThreadFailure;
						evt.Description = sb.ToString();
						this.FireEmulatorEvent(evt);
					}
					this._IsInError = true;
					return;
				}
         
				while(true)
				{
					if(this._Halt == true)
					{
						udpclient.Close();
						//sock.Close();
						this._IsRunningSend = false;
						this.ThreadRunning.Set();
						return;
					}
                    // M.O. - Each packet was measured at 403 milliseconds with firmware ver 3.28 on 10/30/2008
                    System.Threading.Thread.Sleep(100);
                    sb.Remove(0, sb.Length);
					try
					{
                        switch (this._ResponseType)
                        {
							case ResponseType.BuiltInTest:
								if (base._BuiltInTestCounter++ < BuiltInTestCount)
								{
									continue;
								}
								sb.Append("$b=FF"); // Full Pass of self test...(all bits high)
								break;
							case ResponseType.Version:
								sb.Append("$v=LO2H V1.59B 29/07/2009");
								this._ResponseType = ResponseType.None;
								break;
							case ResponseType.Switches:
								sb.Append("$w=");
								sb.Append(this._SwitchData.ToString());
                                this._ResponseType = ResponseType.None;
                                break;
                            default:
								// Position/zoom
								sb.Append("$H=");
								sb.Append(_HorizontalPosition.ToString("X4"));
								sb.Append(",");
								sb.Append(_VerticalPosition.ToString("X4"));
								sb.Append(",");
								// Fault Injection
								if (this._SwitchData.IsCCD == true)
								{
									sb.Append(_ZoomPosition.ToString("X4"));
								}
								else
								{
									sb.Append(this._IRZoomValue.ToString("X4"));
								}
                                break;
                        }
						chSum = this.CalculateChecksum(sb.ToString(), 1);
						udpclient.Send(Encoding.ASCII.GetBytes(sb.ToString()), sb.Length, LocalEP);
						udpclient.Send(new byte[] { chSum, (byte)'\r', (byte)'\n' }, 3, LocalEP);
                        using (EmulatorEventArgs evt = new EmulatorEventArgs())
                        {
                            evt.EmulatorKey = this.SourcePort.ToString();
                            evt.StatusEventType = EmulatorEventTypes.StatusUpdate;
                            this.FireEmulatorEvent(evt);
                        }
                    }
					catch (Exception ex)
					{
						this.Halt();
						udpclient.Close();
						this._IsRunningSend = false;
						this.ThreadRunning.Set();
						sb.Remove(0, sb.Length);
						sb.AppendFormat("Send error on {0} - {1}", LocalEP.ToString(), ex.Message);
						this._EventLog.WriteToEventLog(sb.ToString(), System.Diagnostics.EventLogEntryType.Error);
						using (EmulatorEventArgs evt = new EmulatorEventArgs())
						{
							evt.EmulatorKey = this.SourcePort.ToString();
							evt.StatusEventType = EmulatorEventTypes.ThreadFailure;
							evt.Description = sb.ToString();
							this.FireEmulatorEvent(evt);
						}
						//	MessageBox.Show("Send Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						this._IsInError = true;
						return;
					}
				}
			}
			catch(Exception ex)
			{
				this.Halt();
				udpclient.Close();
				this._IsRunningSend = false;
				this.ThreadRunning.Set();
				sb.Remove(0, sb.Length);
				sb.AppendFormat("Send error on {0} - {1}", LocalEP.ToString(), ex.Message);
				this._EventLog.WriteToEventLog(sb.ToString(), System.Diagnostics.EventLogEntryType.Error);
				using (EmulatorEventArgs evt = new EmulatorEventArgs())
				{
					evt.EmulatorKey = this.SourcePort.ToString();
					evt.StatusEventType = EmulatorEventTypes.ThreadFailure;
					evt.Description = sb.ToString();
					this.FireEmulatorEvent(evt);
				}
				this._IsInError = true;
				return;
			}
		}

		#endregion

	}
}
