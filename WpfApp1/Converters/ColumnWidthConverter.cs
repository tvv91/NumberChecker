using System;
using System.Globalization;
using System.Windows.Data;

namespace VodafoneLogin.Converters
{
    public class ColumnWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return 0.0;

            if (values[0] is bool showAllFields && values[1] is double width)
            {
                return showAllFields ? width : 0.0;
            }
            
            // Handle case where second value might be a string representation of a number
            if (values[0] is bool showAllFields2 && values[1] != null)
            {
                if (double.TryParse(values[1].ToString(), out double widthValue))
                {
                    return showAllFields2 ? widthValue : 0.0;
                }
            }
            
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

