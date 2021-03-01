#define DEBUG_4Dh

using System;

namespace FourDSecurity.Drivers.Baz
{
	public abstract class StateMachine : IDisposable
	{

		#region Properties and Fields

		private object syncObject = new object();
		protected bool _InOverrideMode = false;
		public bool CurrentStateIsLastState
		{
			get
			{
				return (this._StateIndex == (this._StateMachine.Length - 1));
			}
		}
		public HardwareStates PreviousState
		{
			get
			{
				return this._PreviousState;
			}
		}
		protected HardwareStates _PreviousState = HardwareStates.Undefined;
		public HardwareStates CurrentState
		{
			get
			{
				if(this._InOverrideMode == true)
				{
					return this._OverrideState;
				}
				else
				{
					return this._StateMachine[this._StateIndex];
				}
			}
			set
			{
				lock(this.syncObject)
				{

					if(this._InOverrideMode == true)
					{
						this._PreviousState = this._OverrideState;
					}
					else
					{
						this._PreviousState = this._StateMachine[this._StateIndex];
					}
					this._InOverrideMode = true;
					this._OverrideState = value;
				}
			}
		}

		private HardwareStates _OverrideState = HardwareStates.Idle;
		private int _StateIndex = 0;
		// THIS ITERATION OF STATES WILL GET ZOOM, FOCUS PAN, TILT AND OTHER STATUS INFO
		//		private HardwareStates [] _StateMachine = new HardwareStates[]
		//			{
		//				HardwareStates.PollCameraZoom, HardwareStates.PollStatus,
		//				HardwareStates.PollCameraFocus, HardwareStates.PollStatus
		//			};
		protected HardwareStates [] _StateMachine = new HardwareStates[]{ HardwareStates.PollStatus };

		#endregion

		#region Standard Methods

		/// <summary>
		/// This function reverts the current state to the previous value as an
		/// overridden state without affecting the current state index.
		/// </summary>
		public void RevertToPreviousState()
		{
			this._InOverrideMode = true;
			this._OverrideState = this._PreviousState;
		}
		/// <summary>
		/// Resets this state machine to the first state
		/// </summary>
		public void Reset()
		{
			this._StateIndex = 0;
			this._InOverrideMode = false;

		}
		/// <summary>
		/// Resets this state machine to the state before the override was set
		/// </summary>
		public void Release()
		{
			this._InOverrideMode = false;
		}
		public static StateMachine operator ++(StateMachine c1) 
		{
			// CANCEL ANY OVERRIDDEN STATE
			c1._InOverrideMode = false;
			c1._PreviousState = c1.CurrentState;
			if((c1._StateIndex + 1) >= c1._StateMachine.Length)
			{
				c1._StateIndex = 0;
			}
			else
			{
				c1._StateIndex++;
			}
			#if DEBUG_4D
			Et.EOMCCommon.Utils.Instance.WriteColorLine("SM++ to " + c1.CurrentState.ToString(), Et.EOMCCommon.ConsoleColor.Grey, true);	
			#endif
			return c1;
		}
		public static StateMachine operator --(StateMachine c1) 
		{
			// CANCEL ANY OVERRIDDEN STATE
			c1._InOverrideMode = false;
			c1._PreviousState = c1.CurrentState;
			if(c1._StateIndex == 0)
			{
				c1._StateIndex = c1._StateMachine.Length - 1;
			}
			else
			{
				c1._StateIndex--;
			}
#if DEBUG_4D
			Et.EOMCCommon.Utils.Instance.WriteColorLine("SM-- to " + c1.CurrentState.ToString(), Et.EOMCCommon.ConsoleColor.Grey, true);	
#endif
			return c1;
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
		~StateMachine()
		{
			// Do not re-create Dispose clean-up code here.
			// Calling Dispose(false) is optimal in terms of
			// readability and maintainability.
			Dispose(false);
		}

		#endregion


	};

	public class SelfTestingStates : StateMachine
	{

		#region Overridden Properties and Fields


		#endregion

		#region Properties and Fields


		#endregion

		#region Standard Methods
		
		public SelfTestingStates()
		{
			this._StateMachine = new HardwareStates []
			{
				HardwareStates.BeginProcess,
				HardwareStates.AwaitingResponse
			};
		}

		#endregion

	};

	public class SoftLimitClearingStates : StateMachine
	{

		#region Overridden Properties and Fields


		#endregion

		#region Properties and Fields


		#endregion

		#region Standard Methods
		
		public SoftLimitClearingStates()
		{
			this._StateMachine = new HardwareStates []
			{
				HardwareStates.SettingCWSoftLimit,
				HardwareStates.SettingCCWSoftLimit,
				HardwareStates.SettingUpwardSoftLimit,
				HardwareStates.SettingDownwardSoftLimit,
			};
		}

		#endregion

	};

	public class InitStates : StateMachine
	{

		#region Overridden Properties and Fields


		#endregion

		#region Properties and Fields


		#endregion

		#region Standard Methods
		
		public InitStates()
		{
			this._StateMachine = new HardwareStates []
			{
				HardwareStates.Initializing
			};
		}

		#endregion
        
	};

}
