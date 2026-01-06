using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Converters
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

                // Processed with error - Red (more saturated and contrasted)
                if (offer.IsError)
                    return new SolidColorBrush(Color.FromRgb(255, 120, 120)); // Saturated red

                // Processed, found propositions - Green (more saturated and contrasted)
                if (offer.DiscountPercent > 0 || offer.GiftAmount > 0)
                    return new SolidColorBrush(Color.FromRgb(80, 200, 80)); // Saturated green

                // Processed, propositions not suitable - Blue (more saturated and contrasted)
                if (offer.IsPropositionsNotSuitable)
                    return new SolidColorBrush(Color.FromRgb(120, 120, 255)); // Saturated blue

                // Processed, no propositions - Yellow (more saturated and contrasted)
                return new SolidColorBrush(Color.FromRgb(255, 255, 100)); // Saturated yellow
            }

            return Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

