using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace gg_file_organizer.Tools
{
    public class DirectoryExistsRule : ValidationRule
    {
        public static DirectoryExistsRule Instance = new DirectoryExistsRule();

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            try
            {
                if (value is not string stringValue)
                    return new ValidationResult(false, "InvalidPath");

                if (!Directory.Exists(stringValue))
                    return new ValidationResult(false, "Path Not Found");
            }
            catch (ArgumentException)
            {
                return new ValidationResult(false, "Invalid Path");
            }
            catch (IOException)
            {
                return new ValidationResult(false, "Invalid Path");
            }

            return new ValidationResult(true, null);
        }
    }
}
