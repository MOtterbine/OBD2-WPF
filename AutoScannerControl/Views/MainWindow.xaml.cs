using System.Windows;
using log4net;
using System;
using OS.Unity;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Interop;
using OS.WPF;
using OS.Application;
using OS.Security;
using OS.Configuration;


namespace OS.AutoScanner.Views
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected readonly ILog _logger = LogManager.GetLogger(typeof(MainWindow));

      //  private UnityResolver _dependencyResolver = null;
        public System.Windows.Controls.UserControl MainTabContent {get; private set;}

        
        public MainWindow()
        {
            string message = "";
             if (!ResolveDependencies(out message))
            {
                MessageBox.Show(message
                                , "Startup Error"
                                , MessageBoxButton.OK
                                , MessageBoxImage.Error);
                this.Close();
            }
            
           InitializeComponent();
            this.SetTitle();
        }
        private string _DBUser = "";
        private void SetTitle()
        {
            this.Title = $"OBD2 Interface - Ver {VersionInfo.AppVersion}";
        }
        private IAppViewModel _appViewModel = null;
        /// <summary>
        /// Gets this window's unity-mapped model and connection string
        /// </summary>
        /// <returns></returns>
        private bool ResolveDependencies(out string message)
        {
            message = "";
            try
            {
                // CONFIGURE UNITY (DEPENDENCY INJECTION)
               // _dependencyResolver = new UnityResolver();

                // THE POINT HERE IS TO INSTANTIATE THE MODEL (IMPLEMENTING 'IAppViewModel') FROM UNITY DI

                // ADD ANY RUNTIME ARGUMENTS TO THE CONSTRUCTOR OF THE INJECTED OBJECT
                // THE 'disp' CONSTRUCTOR IS NOT USED ANYMORE, BUT THIS OPERATIONAL CODE IS LEFT IN 
                // PLACE FOR WHEN A CONSTUCTOR ARGUMENT IS NEEDED FOR THE INJECTED TYPE
                // ANY ARGUMENTS NOT USED ARE IGNORED
                Dictionary<string, object> modelConstructorArguments = new Dictionary<string, object>();
                // key is the actual constructor parameter name, value is the value to pass in.
                modelConstructorArguments.Add("disp", this.Dispatcher);

                // GET THE UNITY MAPPING NAME USED TO REFERENCE THE IMPLEMENTATION OF THE MODEL FROM 
                // THIS DLL'S CONFIG FILE - NOT THE EXECUTING APP.CONFIG
                // THE dll.config SETTING IS DESIGNATED BY  "<WINDOW CLASS NAME> + '_model'" 
                // <!-- Unity Mapping name for Model class used for view 'MainWindow.xaml' -->
                //  <appSettings>
                //    <add key="MainWindow_model" value="ImporterViewModel"/>
                //  </appSettings>

                object modelUnityMappingName = null;
                ConfigurationManager.GetDllConfigAppSetting(this.GetType().Name + "_model", out modelUnityMappingName);
                if (string.IsNullOrEmpty((string)modelUnityMappingName))
                {
                    throw new Exception("Could not find dll configuration setting '" + this.Name + "_model' which is used to specify the Unity DI mapping for the view model object");
                }

                // ASSIGN THE INJECTED MODEL TO THIS WINOW'S DataContext - 
                this._appViewModel = UnityResolver.Instance.GetObjectByAlias<IAppViewModel>((string)modelUnityMappingName, modelConstructorArguments);
                if (this._appViewModel == null)
                {
                    _logger.Error("Unable to create view model - check unity log and configuration");
                }
                else
                {
                    // ASSIGN THE MODEL TO THIS WINOW'S DataContext
                    this._appViewModel.ModelEvent += OnViewModelEvent;
                    if (this._appViewModel.ReadyForUse)
                    {
                        this.DataContext = this._appViewModel;
                        this._appViewModel.Initialize();
                    }
                    else
                    {
                        this._appViewModel.Created += new CreationComplete(delegate()
                            {
                                this.DataContext = this._appViewModel;
                                this._appViewModel.Initialize();
                            });
                    }
                    _logger.InfoFormat("DataContext assigned to {0}.", this._appViewModel.GetType().FullName);
                    this._appViewModel.ModelEvent -= OnViewModelEvent;

                    return true;
                }
            }
            catch (System.Security.Authentication.AuthenticationException)
            {

                message = "Login Failure - see system administrator";
                this._logger.ErrorFormat("Login Failure while running under {0}\\{1}", Environment.UserDomainName, Environment.UserName);
                this._appViewModel.Dispose();
            }
            catch (Exception ex)
            {
                this._logger.ErrorFormat("Unity resolver error - {0}{1}", ex.Message, ex.InnerException == null ? "" : ", " + ex.InnerException.Message);

                message = "Error resolving dependencies. Please check log and make necessary adjustments to 'Unity.config' file";

            }
            return false;
        }

        object OnViewModelEvent(object sender, ModelEventArgs e)
        {
            switch(e.EventType)
            {
                case ModelEventTypes.Confirmation:
                    if (MessageBox.Show(e.Messages[0], e.Messages[1], MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                    {
                        return false;
                    }
                    return true;
                case ModelEventTypes.Authenticate:
                    // Here, we pass in a viewmodel (passed via the delegate callback event args) to handle authentication logic
                    //if (!(e.WorkerObject is IAuthenticate))
                    //{
                    //    throw new InvalidCastException("Unable to authenticate. ModelEventArgs.WorkerObject must be an 'IAuthenticate' object.");
                    //}
                    //LoginWindow logWindow = new LoginWindow(e.WorkerObject as IAuthenticate, this.IsLoaded ? this : null);
                    //if(this.IsLoaded)
                    //{
                    //    logWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    //}
                    //else
                    //{
                    //    logWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                    //}
                    //retVal = logWindow.ShowDialog();
                    //this._DBUser = (e.WorkerObject as IAuthenticate).UserName;
                    //if ((bool)retVal == true)
                    //{
                    //    this._logger.InfoFormat("User {0} logged in", this._DBUser);
                    //}
                    //return retVal;
                    break;
                case ModelEventTypes.ChangePassword:
                    // Here, we pass in a viewmodel (passed via the delegate callback event args) to handle authentication logic
                    //if (!(e.WorkerObject is IAuthenticate))
                    //{
                    //    throw new InvalidCastException("Unable to authenticate. ModelEventArgs.WorkerObject must be an 'IAuthenticate' object.");
                    //}
                    //PasswordChangeWindow pswdChgWindow = new PasswordChangeWindow(e.WorkerObject as IAuthenticate, this.IsLoaded?this:null);
                    //if(this.IsLoaded)
                    //{
                    //    pswdChgWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    //}
                    //else
                    //{
                    //    pswdChgWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                    //}
                    //retVal = pswdChgWindow.ShowDialog();
                    //this._DBUser = (e.WorkerObject as IAuthenticate).UserName;
                    //if ((bool)retVal == true)
                    //{
                    //    this._logger.InfoFormat("User {0} logged in", this._DBUser);
                    //}
                    //return retVal;
                    break;
                case ModelEventTypes.FilePathRequest:
                    // return FileBrowser.BrowseForFile(true, e.Messages[0], e.Messages[1], (string[])e.WorkerObject);
                    break;
                case ModelEventTypes.InfoLog:
                    _logger.Info(e.Messages[0]);
                    break;
                case ModelEventTypes.ErrorLog:
                    _logger.Error(e.Messages[0]);
                    break;
                case ModelEventTypes.WarnLog:
                    _logger.Warn(e.Messages[0]);
                    break;
                case ModelEventTypes.DebugLog:
                    _logger.Debug(e.Messages[0]);
                    break;
                case ModelEventTypes.ExitApplication:
                    (this.DataContext as IAppViewModel).Dispose();
                    this.Close();
                    break;
                  
            }
            return false;
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if(this.DataContext != null)
            {
                (this.DataContext as IAppViewModel).Dispose();
            }
            base.OnClosing(e);
        }


        #region Form Messages

        private void OnClosed(object sender, EventArgs e)
        {
            _logger.InfoFormat("{0} is closing\r\n", VersionInfo.AppName);
            // Get the current view model's implementation of close...
            ICommand Closed = (this.DataContext as IAppViewModel).CloseCommand;
            Closed.Execute(null);
        }
        //private void OnLoadForm(object sender, RoutedEventArgs e)
        //{
        //    if (SystemProperties.StartedWithValidArgs)
        //    {
        //        switch (SystemProperties.StartupCommand)
        //        {
        //            case StartupCommands.ExportFile:
        //                ICommand ExportVideoFileCommand = (Application.Current as FourDSecurity.App).viewModel.ExportVideoFileCommand;
        //                ExportVideoFileCommand.Execute(null);
        //                break;
        //            case StartupCommands.LogIn:
        //                ICommand LoginCommand = (Application.Current as FourDSecurity.App).viewModel.LoginCommand;
        //                LoginCommand.Execute(null);
        //                break;
        //            case StartupCommands.ConvertFile:
        //                break;
        //        }
        //    }
        //}

        #endregion Form Messages

        #region Support for Win32 messages

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;

            // This moves the current instance (of this single-instance app) to the top when the application is invoked
            if (Convert.ToInt32(msg) != NativeMethods.WM_SHOWME) return IntPtr.Zero;
            this.Topmost = true;
            this.BringIntoView();
            this.Topmost = false;
            handled = true;

            return IntPtr.Zero;
        }

        #endregion Support for Win32 messages

        private void OnRowEditEnding(object sender, System.Windows.Controls.DataGridRowEditEndingEventArgs e)
        {
            

        }

        private void OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            
        }

        private void DataGrid_OnBeginningEdit(object sender, System.Windows.Controls.DataGridBeginningEditEventArgs e)
        {
            // Only allows edit of the first row displayed. - Cheap, I know it.....
            if (e.Row.GetIndex() != 0)
            {
                e.Cancel = true;
            }

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Call initialize on the component after this is loaded
            this.DataContext = this._appViewModel;
            if (this._appViewModel == null)
            {
                this._logger.Error("The assigned DataContext is not an 'IAppViewModel' object");
                return;
            }

            if (this._appViewModel.ReadyForUse)
            {
               this._appViewModel.ModelEvent += OnViewModelEvent;
               this._appViewModel.Initialize();
               this._appViewModel.ModelEvent += OnViewModelEvent;
            }
            else
            {
                this._appViewModel.Created += new CreationComplete(delegate()
                {
                    this._appViewModel.ModelEvent += OnViewModelEvent;
                    this._appViewModel.Initialize();
                    this._appViewModel.ModelEvent += OnViewModelEvent;
                });
            }

        }


    }
}
