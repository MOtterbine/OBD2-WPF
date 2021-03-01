using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OS.AutoScannerApp
{
    /// <summary>
    /// For parsing commands passed in via command line parameters
    /// </summary>
    public enum WindowModes
    {
        Full,
        Minimal
    }
    public enum StartupCommands
    {
        None,
        LogIn,
        RunTask,
        ImportFile,
        ExportFile,
        ConvertFile
    }

    #region System Properties

    public static class SystemProperties
    {
        //Persisted Properties
        public static bool AutoClose
        {
            get
            {
                return Properties.Settings.Default.AutoClose;
            }

            set
            {
                if (Properties.Settings.Default.AutoClose != value)
                {
                    Properties.Settings.Default.AutoClose = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        //public const string VideoProcessorAppName = "DataExchangeControl.exe";
        public static string ArgumentUsageHelpString
        {
            get
            {
                return Properties.Settings.Default.ArgumentUsageHelpString;
            }
        }
        //public static string ServiceControlAppPath
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.ServiceControlAppPath;
        //    }
        //    set
        //    {
        //        // This property is usually set once during installation after the target install directory is known
        //        if (Properties.Settings.Default.ServiceControlAppPath != value)
        //        {
        //            Properties.Settings.Default.ServiceControlAppPath = value;
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}
        //public static string VideoProcessorServiceName
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.VideoProcessorServiceName;
        //    }
        //}
        //public static string WCFServiceURIString
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.WCFServiceURIString;
        //    }
        //    set
        //    {
        //        // This property is usually set once during installation after the target install directory is known
        //        if (Properties.Settings.Default.WCFServiceURIString != value)
        //        {
        //            Properties.Settings.Default.WCFServiceURIString = value;
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}
        //public static bool AutoStartService
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.AutoStartService;
        //    }

        //    set
        //    {
        //        if (Properties.Settings.Default.AutoStartService != value)
        //        {
        //            Properties.Settings.Default.AutoStartService = value;
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}
        //public static bool DisplayIntermediateDialogs
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.DisplayIntermediateDialogs;
        //    }

        //    set
        //    {
        //        if (Properties.Settings.Default.DisplayIntermediateDialogs != value)
        //        {
        //            Properties.Settings.Default.DisplayIntermediateDialogs = value;
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}
        //public static string UserName
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.UserName;
        //    }
        //    set
        //    {
        //        // This property is usually set once during installation after the target install directory is known
        //        if (Properties.Settings.Default.UserName != value)
        //        {
        //            Properties.Settings.Default.UserName = value;
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}
        //public static string Password
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.Password;
        //    }
        //    set
        //    {
        //        // This property is usually set once during installation after the target install directory is known
        //        if (Properties.Settings.Default.Password != value)
        //        {
        //            Properties.Settings.Default.Password = value;
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}
        //public static string ServerAddress
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.Gateway;
        //    }
        //    set
        //    {
        //        // This property is usually set once during installation after the target install directory is known
        //        if (Properties.Settings.Default.Gateway != value)
        //        {
        //            Properties.Settings.Default.Gateway = value;
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}
        //public static string ConversionFileName
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.ConversionFileName;
        //    }
        //    set
        //    {
        //        // This property is usually set once during installation after the target install directory is known
        //        if (Properties.Settings.Default.ConversionFileName != value)
        //        {
        //            Properties.Settings.Default.ConversionFileName = value;
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}
        //public static string CameraID
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.CameraID;
        //    }
        //    set
        //    {
        //        // This property is usually set once during installation after the target install directory is known
        //        if (Properties.Settings.Default.CameraID != value)
        //        {
        //            Properties.Settings.Default.CameraID = value;
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}
        //public static string LogFileName
        //{
        //    get
        //    {
        //        return Properties.Settings.Default.LogFileName;
        //    }
        //    set
        //    {
        //        // This property is usually set once during installation after the target install directory is known
        //        if (Properties.Settings.Default.LogFileName != value)
        //        {
        //            Properties.Settings.Default.LogFileName = value;
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}
        //public static DateTime ConversionStartTime { get; set; }
        //public static DateTime ConversionStopTime { get; set; }
        //public static ExportFileTypes ConversionFileType
        //{
        //    get
        //    {
        //        try
        //        {
        //            return (ExportFileTypes)Enum.Parse(typeof(ExportFileTypes), Properties.Settings.Default.ConversionFileType);
        //        }
        //        catch (Exception)
        //        {
        //            return ExportFileTypes.G64;
        //        }
        //    }
        //    set
        //    {
        //        if (ConversionFileType != value)
        //        {
        //            Properties.Settings.Default.ConversionFileType = value.ToString();
        //            Properties.Settings.Default.Save();
        //        }
        //    }
        //}

        // Per-Session properties 


        public static WindowModes WindowMode { get; set; }
        public static string StartupUri { get; set; }
        //     public static VMSTypes VMSType { get; set; }
        public static StartupCommands StartupCommand { get; set; }
        public static bool StartedWithValidArgs { get; set; }
        //        public static bool ExportAndConvert { get; set; }

    }

    #endregion System Properties

}
