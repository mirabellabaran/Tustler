using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;

namespace Tustler.Helpers
{
    public class FractionConverter : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            var total = System.Convert.ToDouble(value, culture.NumberFormat);
            var fraction = System.Convert.ToDouble(parameter, culture.NumberFormat);
            return total * fraction;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
