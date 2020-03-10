using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace RRemote.Common
{
    public class BoolToRedTransparentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string s)
        {
            Brush ret = new SolidColorBrush(Colors.Transparent);
            if (value != null && (bool) value)
                ret = new SolidColorBrush(Colors.Red);
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string s)
        {
            throw new NotImplementedException();
        }
    }

    public class NegativeBoolToRedTransparentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string s)
        {
            Brush ret = new SolidColorBrush(Colors.Transparent);
            if (value != null && !(bool) value)
                ret = new SolidColorBrush(Colors.Red);
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string s)
        {
            throw new NotImplementedException();
        }
    }
}