using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ytplayer.data;

namespace ytplayer {
    public class MediaFlagToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool music = (parameter as string) == "music";

            if (Enum.IsDefined(value.GetType(), value) == false) {
                return DependencyProperty.UnsetValue;
            }
            switch((MediaFlag)value) {
                case MediaFlag.BOTH:
                    return Visibility.Visible;
                case MediaFlag.AUDIO:
                    return music ? Visibility.Visible : Visibility.Hidden;
                case MediaFlag.VIDEO:
                    return music ? Visibility.Hidden : Visibility.Visible;
                default:
                    return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
