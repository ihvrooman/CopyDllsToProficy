using CopyDllsToProficy.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CopyDllsToProficy.ValidationRules
{
    public class FolderPathValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!AppInfo.FolderExists(new DirectoryInfo(value.ToString())))
            {
                return new ValidationResult(false, $"Folder path not valid.");
            }
            return ValidationResult.ValidResult;
        }
    }
}
