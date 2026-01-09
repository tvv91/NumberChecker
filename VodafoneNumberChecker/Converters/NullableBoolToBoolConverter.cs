using System;
using System.Globalization;
using System.Windows.Data;

namespace VodafoneNumberChecker.Converters
{
    public class NullableBoolToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert null to false for display (unchecked)
            if (value is bool boolValue)
            {
                return boolValue;
            }
            // Handle nullable bool by checking if it's a bool? type
            if (value != null && value.GetType() == typeof(bool?))
            {
                return ((bool?)value) ?? false;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert back: false can be null or false, but we'll keep the original logic
            // The binding will handle the actual property update
            if (value is bool boolValue)
            {
                // If unchecked (false), we could return null, but the property setter will handle it
                // For now, return the bool value and let the property handle null logic
                return boolValue;
            }
            return null;
        }
    }
}
