using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using log4net;

namespace OS.AutoScannerApp
{
    public class StartupArgumentValidator
    {
        private readonly ILog Logger = LogManager.GetLogger(typeof(StartupArgumentValidator));

        private string _ArgumentUsageString = null;
        public void ShowArgumentUsageDialog()
        {
            MessageBox.Show(this._ArgumentUsageString, "Command Line Usage", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.Cancel, MessageBoxOptions.DefaultDesktopOnly);
        }
        public StartupArgumentValidator(string argUsageString)
        {
            // Is the parameter not good?
            if (string.IsNullOrEmpty(argUsageString))
            {
                this._ArgumentUsageString = "Error in the command line. Contact manufacturer for details";
            }
            this._ArgumentUsageString = argUsageString;
        }
        public bool ValidateParameterString(string value, string errorText, bool showDialog, bool allowEmpty)
        {
            string dummy = "";
            return this.ValidateParameterString(value, errorText, showDialog, allowEmpty, out dummy);
        }
        public bool ValidateParameterString(string value, string errorText, bool showDialog, out string messageString)
        {
            return this.ValidateParameterString(value, errorText, showDialog, false, out messageString);
        }
        /// <summary>
        /// Validates a string, returning false for null or empty. Also displays custom message with optional help dialog.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="errorText"></param>
        /// <param name="showDialog"></param>
        /// <returns></returns>
        public bool ValidateParameterString(string value, string errorText, bool showDialog)
        {
            string dummy = "";
            return this.ValidateParameterString(value, errorText, showDialog, false, out dummy);
        }
        /// <summary>
        /// Validates a string, returning false for null or (optionally) empty. Also displays custom message with optional help dialog.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="errorText"></param>
        /// <param name="showDialog"></param>
        /// <returns></returns>
        public bool ValidateParameterString(string value, string errorText, bool showDialog, bool allowEmpty, out string messageString)
        {
            messageString = "";
            if (allowEmpty && value != null) return true;
            if (string.IsNullOrEmpty(value))
            {
                if (string.IsNullOrEmpty(errorText)) errorText = "Validation Error Detected";
                messageString = string.Format("{0}\n\n\n\rDisplay command line usage?", errorText);
                if (showDialog)
                {
                    if (MessageBox.Show(messageString, "Validation Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    {
                        ShowArgumentUsageDialog();
                    }
                }
                Logger.Error(errorText);
                return false;
            }
            return true;
        }
        /// <summary>
        /// Intended to compare strings that correspond to enum types. The out parameter is invalid if returning false. T must be an enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="errorText"></param>
        /// <param name="showDialog"></param>
        /// <param name="enumObject"></param>
        /// <returns></returns>
        public bool ValidateEnumParameters<T>(string value, string errorText, bool showDialog, out T enumObject)
        {
            string dummy = "";
            return ValidateEnumParameters<T>(value, errorText, showDialog, out enumObject, out dummy);
        }
        public bool ValidateEnumParameters<T>(string value, string errorText, bool showDialog, out T enumObject, out string messageString)
        {
            messageString = "";
            if (typeof(T).IsValueType)
            {
                enumObject = (T)(object)0; //Works ONLY if T is valuetype: int, otherwise you get a "must be less than infinity"-error.
            }
            else
            {
                enumObject = (T)(object)null; //Null ref exception in the cast...
            }

            StringBuilder HelpString = new StringBuilder();
            T enumClass;
            if (string.IsNullOrEmpty(value))
            {
                HelpString.Clear();
                string[] cmdList = Enum.GetNames(typeof(T));
                foreach (string str in cmdList) HelpString.AppendFormat("\"{0}\" ", str);
                messageString = string.Format("{0} - \n\n\rAvailable: {1}\n\n\n\rDisplay command line usage?", errorText, HelpString);
                if (showDialog)
                {
                    if (MessageBox.Show(messageString, "Validation Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    {
                        ShowArgumentUsageDialog();
                    }
                }
                Logger.Error(errorText + ", Available: " + HelpString);
                return false;
            }
            try
            {
                enumClass = (T)Enum.Parse(typeof(T), value, true);
            }
            catch (Exception ex)
            {
                HelpString.Clear();
                string[] cmdList = Enum.GetNames(typeof(T));
                foreach (string str in cmdList) HelpString.AppendFormat("\"{0}\" ", str);
                messageString = string.Format("{0} - {1}\n\n\rAvailable: {2}\n\n\n\rDisplay command line usage?", errorText, ex.Message, HelpString);
                if (showDialog)
                {
                    if (MessageBox.Show(messageString, "Validation Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    {
                        ShowArgumentUsageDialog();
                    }
                }
                Logger.Error(errorText + " " + HelpString);
                return false;
            }
            enumObject = enumClass;
            return true;
        }
        public string MessageText { get; private set; }

    }
}
