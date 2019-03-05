using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LrcToSrt
{
    [ValueConversion(typeof(String), typeof(String))]
    class StringHiddenConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var str = value as string;
            if (str != null && str.Length > 40)
                return str.Substring(0, 20) + " ... " + str.Substring(str.LastIndexOf('\\') - 5);
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
