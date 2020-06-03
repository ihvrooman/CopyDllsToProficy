using CopyDllsToProficy.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CopyDllsToProficy.ValidationRules
{
    public class ServerNameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var serverPath = "\\\\" + value.ToString() + "\\c$";
            if (!AppInfo.FolderExists(new DirectoryInfo(serverPath)))
            {
                return new ValidationResult(false, $"Could not find server \"{serverPath}\".{Environment.NewLine}Ensure that you only put the name of the server (e.g. \"ServerName\" without any other characters).");
            }
            return ValidationResult.ValidResult;
        }
    }
}
