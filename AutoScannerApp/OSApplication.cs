using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using OS.Application;
using OS.Configuration;

namespace OS.AutoScannerApp
{
    partial class OSApplication : System.Windows.Application
    {
        protected readonly ILog _logger = LogManager.GetLogger(typeof(OSApplication));

        public OSApplication()
        {
            // Application actually starts in the "Main(...)" method below...
        }
        public void Initialize()
        {
            // SET THE INITIAL VIEW 
            // (*.xaml file name should be in app.config or specified in xml cmd line arguments)
            // The startup uri is required if any command line is used
            // Currently, the xaml's code behind is expected to load its own model
            // and assign that model to its 'DataContext' object for binding ect...
            // Here, we are only selecting the xaml (view) to load.
            // The view will load (via Unity DI) it's own model 

            // EXAMPLE STARTUP URI FROM SEPARATE ASSEMBLY: 
            // new System.Uri("pack://application:,,,/DSI.WPFModel.Sernivo;component/MainWindow.xaml");
            // in above string, 'application' and 'component' are specific designations (a path follows 'component')
            // ... where "OS.WPFModel.Sernivo.dll" is the library with the 'MainWIndow.xaml' having been in the root
            // of the project folder when compiled

            StringBuilder tmpString = null;
            bool fromcmdline = false;
            // ***FROM CONFIGURATION - Get the start uri (xaml) from the xml-formatted command line?
            if (SystemProperties.StartedWithValidArgs && !string.IsNullOrEmpty(SystemProperties.StartupUri))
            {
                fromcmdline = true;
                // pickup the url value
                tmpString = new StringBuilder(SystemProperties.StartupUri);
            }
            // ***FROM APP.CONFIG - Or, Get the start uri (xaml) from the app.config file under setting "ViewWindow" ?
            else
            {
                fromcmdline = false;
                string app_setting_name = "ViewWindow";
                try
                {
                    // pickup the url value
                    tmpString = new StringBuilder((String)ConfigurationManager.GetMainAppSetting(app_setting_name));
                    if (tmpString.Length < 1)
                    {
                        tmpString.AppendFormat("Unable to read application configuration setting '{0}'", app_setting_name);
                        this._logger.Error(tmpString.ToString());
                        throw new System.Configuration.ConfigurationErrorsException(tmpString.ToString());
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Error attempting to load view (xaml). Attempted to load '{0}' from {1}. - {2}", tmpString.ToString(), fromcmdline ? "command line" : "app configuration", OS.Helpers.ExceptionReader.GetFullExceptionMessage(ex)), ex);
                }

            }
            // Now, attempt to use the value we found - assumed to be an absolute uri
            try
            {
                this._logger.InfoFormat("Load startup uri '{0}' from {1}", tmpString, fromcmdline ? "command line" : "app configuration");
                this.StartupUri = new System.Uri(tmpString.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error attempting to load view (xaml). Attempted to load '{0}' from {1}. - {2}", tmpString.ToString(), fromcmdline ? "command line" : "app configuration", OS.Helpers.ExceptionReader.GetFullExceptionMessage(ex)), ex);
            }
        }

        private static void InitializeBaseComponents()
        {
            try
            {
                // CONFIGURE LOG4NET 
                log4net.Config.XmlConfigurator.Configure();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error attempting log4net configuration. - {0}", ex.Message), ex);
            }

        }

        /// <summary>
        /// Keeps us to only one instance..
        /// </summary>
        static Mutex mutex = new Mutex(true, "{94609910-1881-4D9B-AD8E-E381162FB5FF}");

        /// <summary>
        /// APPLICATION STARTS HERE - program entry point
        /// </summary>
        [System.STAThreadAttribute()]
        public static void Main(params string[] args)
        {
            // SecurityFunctions sf = new SecurityFunctions();
            // sf.GetCurrentWindowsUserInfo();


            OSApplication.InitializeBaseComponents();

            ILog Logger = LogManager.GetLogger("Program.Main()");


            //System.Diagnostics.Debugger.Break();
         //   SystemProperties.StartedWithValidArgs = false;
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                Logger.InfoFormat("{0} started by user: {1}\\{2}", VersionInfo.AppName, Environment.UserDomainName, Environment.UserName);
                if (args.Length > 0)// && args.Length > 1)// there's always a default argument passed here for startup/entry method
                {
                    StartupArgumentValidator validator = new StartupArgumentValidator(SystemProperties.ArgumentUsageHelpString);
                    // generic and passable, this object will be passed to classes unaware of how it was created..
                    IRuntimeArguments argHandler;
                    try
                    {
                        // Define the xml command line parameters that are required (there could be more ), but if 
                        // not in this list they are considered optional
                        String[] expectedArgs = new string[]
                        {
                            "StartupUri"
                            //"UserName",
                            //"Password",
                            //"ServerAddress",
                            //"Command",
                            //"WindowMode"     
                        };
                        // We take the entire xml command line as the only string - the first in the params list
                        argHandler = new XMLStartupArguments(args[0] as string, expectedArgs);

                    }
                    catch (Exception ex)
                    {
                        if (MessageBox.Show(string.Format("Unable to parse comman line parameters\n\r\n\r{0}\n\r\n\r\n\rDisplay usage?", ex.Message), "Validation Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                        {
                            validator.ShowArgumentUsageDialog();
                        }
                        Logger.Error("Unable to parse comman line parameters");
                        return;
                    }
                    if (string.IsNullOrEmpty(args[0] as string))
                    {
                        if (MessageBox.Show("Invalid command line parameters.", "Validation Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                        {
                            validator.ShowArgumentUsageDialog();
                        }
                        Logger.Error("Unable to parse comman line parameters");
                        return;
                    }

                    // VALIDATE ACTUAL PARAMETER VALUES
                    // User Name validation
                    if (!validator.ValidateParameterString(argHandler.GetValue("StartupUri"), "Invalid, or missing startup uri.", true))
                        return;
                    StartupCommands startupCommand;
                    if (!validator.ValidateEnumParameters<StartupCommands>(argHandler.GetValue("Command"), "Invalid, or missing startup command.", true, out startupCommand))
                        return;
                    // User Name validation
                    //if (!validator.ValidateParameterString(argHandler.GetValue("UserName"), "Invalid, or missing user name.", true))
                    //    return;
                    //// Password validation
                    //if (!validator.ValidateParameterString(argHandler.GetValue("Password"), "Bad, or missing password.", true, true))
                    //    return;
                    //// Server Address validation
                    //if (!validator.ValidateParameterString(argHandler.GetValue("ServerAddress"), "Invalid, or missing server address.", true))
                    //    return;
                    // Startup Command validation

                    //// Window Mode validation
                    //WindowModes windowMode;
                    //if (!validator.ValidateEnumParameters<WindowModes>(argHandler.GetValue("WindowMode"), "Invalid, or missing Window Mode.", false, out windowMode))
                    //{
                    //    Logger.Warn("Invalid, or missing Window mode specified.  Defaulting to 'Full' window mode.");
                    //    windowMode = WindowModes.Full;
                    //}

                    // These general values are validated now....we can just use them...(they may not work, but they're types are ok)
                    SystemProperties.StartupUri = argHandler.GetValue("StartupUri");
                    //SystemProperties.UserName = argHandler.GetValue("UserName");
                    //SystemProperties.Password = argHandler.GetValue("Password");
                    //SystemProperties.ServerAddress = argHandler.GetValue("ServerAddress");
                    SystemProperties.StartupCommand = startupCommand;
                    //   SystemProperties.WindowMode = windowMode;

                    SystemProperties.StartedWithValidArgs = true;
                    StringBuilder sb = new StringBuilder("Valid startup arguments were parsed:");
                    foreach (KeyValuePair<String, string> kvp in argHandler.ArgumentsFound)
                    {
                        sb.AppendFormat("{0}argument [{1}], [{2}]", Environment.NewLine, kvp.Key, kvp.Value);
                    }
                    Logger.Debug(sb.ToString());
                }

                OS.AutoScannerApp.OSApplication app = new OS.AutoScannerApp.OSApplication();
                app.Initialize();
                try
                {
                    app.Run();
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("Program Error - {0}{1}", ex.Message, ex.InnerException == null ? "" : ", " + ex.InnerException.Message);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            else
            {
                MessageBox.Show("Application is already running", "Startup", MessageBoxButton.OK, MessageBoxImage.Stop);
                // send our Win32 message to make the currently running instance
                // jump on top of all the other windows
                NativeMethods.PostMessage(
                    (IntPtr)NativeMethods.HWND_BROADCAST,
                    NativeMethods.WM_SHOWME,
                    (IntPtr)1234,
                    (IntPtr)5678);
                return;
            }

        }



    }
}
