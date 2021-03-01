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
	public class ContropObservationCam : ContropObservationBase //FourDSecurity.Emulation.HardwareEmulation.DeviceEmulationBase
	{
		#region Properties and Fields

		protected const int BuiltInTestCount = 9; // 3.6 seconds of 400ms intervals (emulation loop time)

		public override int EngineTimerInterval
        {
            get
            {
                return Properties.ContropObservationCam.Default.EngineTimerInterval;	
            }
            set
            {
                Properties.ContropObservationCam.Default.EngineTimerInterval = value;
            }
        }
        public override bool IsRunning
        {
            get
            {
                return (this._IsRunningRcv || this._IsRunningSend);
            }
        }

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
				sb.AppendFormat("CO30: {0}:{1}\n\n\r", this.SourceAddress, this.SourcePort.ToString());
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

		public static Form _PropertiesForm = null;
	
		public override string Description
		{
			get
			{
				return  "Vendor1 CO30, ver. " + VersionInfo.AssemblyVersion.Version.ToString();
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
                return null;
            }
            set
            {
            }
        }
        public override int ? DestinationPort
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
                return this._DeviceCount;
            }
            set
            {
                this._DeviceCount = value;
            }
        }
        private int _DeviceCount = 0;
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


		#endregion

		#region Overriden Methods

		public override void Halt()
		{
			this._Halt = true;
			this._IsRunningRcv = false;
			this._IsRunningSend = false;
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
        public override void SaveConfiguration()
        {
            Properties.ContropObservationCam.Default.IPAddress = this._SourceAddress;
            Properties.ContropObservationCam.Default.Port = this._SourcePort;
			//Properties.ContropObservationCam.Default.RemoteIPAddress = this._DestinationAddress;
			//Properties.ContropObservationCam.Default.RemotePort = this._DestinationPort;
            Properties.ContropObservationCam.Default.DeviceCount = this._DeviceCount;

            Properties.ContropObservationCam.Default.Save();
        }
		public override void ShowPropertiesForm()
		{
			using (ContropObservationOptions optForm = new ContropObservationOptions())
			{
				optForm.ShowDialog();
			}
			
		}
        //public override void DoPeriodicFunction()
        //{
        //    base.DoPeriodicFunction();
        //}


		#endregion

		#region Standard Methods

		public ContropObservationCam(string CamIPAddress, int CamPort, string DestAddress, int DestPort )
		{
			this.SourceAddress = CamIPAddress;
			this.SourcePort = CamPort;
			this.DestinationAddress = DestAddress;
			this.DestinationPort = DestPort;
		}
		public ContropObservationCam()
		{
            Properties.ContropObservationCamA.Default.Reload();
            this._SourceAddress = Properties.ContropObservationCam.Default.IPAddress;
            this._SourcePort = Properties.ContropObservationCam.Default.Port;
			//this._DestinationAddress = Properties.ContropObservationCam.Default.RemoteIPAddress;
			//this._DestinationPort = Properties.ContropObservationCam.Default.RemotePort;
            this._DeviceCount = Properties.ContropObservationCam.Default.DeviceCount;
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
		//        if(this.messageStream.Length >= 15)
		//        {
		//            if(this.messageStream.Length > 41)
		//            {
		//                //	Debug.WriteLine(string.Format("!!!! Bad Message Stream !!!! {0}", strMessage));
		//                messageStream = new MemoryStream();
		//                //this.messageStream.SetLength(0);
		//                return ResponseType.None;
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
					// M.O. - Each packet was measured at 403 milliseconds with firmware ver 1.58 on 10/30/2008
					System.Threading.Thread.Sleep(43);
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
								this._ResponseType = ResponseType.None;
								break;
							case ResponseType.Switches:
								sb.Append("$w=");
								sb.Append(this._SwitchData.ToString());
								this._ResponseType = ResponseType.None;
								break;
							case ResponseType.Version:
								sb.Append("$v=uDTVLO2H V1.59 29/10/2008");
                                this._ResponseType = ResponseType.None;
                                break;
                            default:
								sb.Append("$H=");
								sb.Append(_HorizontalPosition.ToString("X4"));
								sb.Append(",");
								sb.Append(_VerticalPosition.ToString("X4"));
								sb.Append(",");
								if (this._SwitchData.IsCCD == true)
								{
									sb.Append(_ZoomPosition.ToString("X4"));
								}
								else
								{
									sb.Append("0000");
								}
								this._ResponseType = ResponseType.Switches; // for alternating data
								break;
                        }
						chSum = this.CalculateChecksum(sb.ToString(),1);
				        udpclient.Send(Encoding.ASCII.GetBytes(sb.ToString()), sb.Length, LocalEP);
				        udpclient.Send(new byte[]{chSum,(byte)'\r',(byte)'\n'}, 3, LocalEP);
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
					try
					{
						using(EmulatorEventArgs evt = new EmulatorEventArgs())
						{
							evt.EmulatorKey = this.SourcePort.ToString();
							evt.StatusEventType = EmulatorEventTypes.StatusUpdate;
							this.FireEmulatorEvent(evt);
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