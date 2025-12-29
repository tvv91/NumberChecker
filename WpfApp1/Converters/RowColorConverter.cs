using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using VodafoneLogin.Models;

namespace VodafoneLogin.Converters
{
    public class RowColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Brushes.Transparent;

            if (values[0] is PhoneOffer offer && values[1] is bool useColors)
            {
                if (!useColors)
                    return Brushes.Transparent;

                // Not processed - default (transparent)
                if (!offer.IsProcessed)
                    return Brushes.Transparent;

                // Processed with error - Red
                if (offer.IsError)
                    return new SolidColorBrush(Color.FromRgb(255, 224, 224)); // Light red

                // Processed, found propositions - Green
                if (offer.DiscountPercent > 0 || offer.GiftAmount > 0)
                    return new SolidColorBrush(Color.FromRgb(224, 255, 224)); // Light green

                // Processed, no propositions - Grey
                return new SolidColorBrush(Color.FromRgb(255, 255, 224)); // Yello
            }

            return Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

