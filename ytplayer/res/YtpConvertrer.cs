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
    public class MBSizeTextConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            try {
                var v = System.Convert.ToInt64(value);
                if (v == 0) {
                    return "0";
                }
                var low = (v / 1000) % 1000;
                var hi = v / 1_000_000;
                return $"{hi:N0}.{low:D3}";
            } catch (Exception) {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class MediaFlagToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool audio = (parameter as string) == "audio";

            if (Enum.IsDefined(value.GetType(), value) == false) {
                return DependencyProperty.UnsetValue;
            }
            switch((MediaFlag)value) {
                case MediaFlag.BOTH:
                    return Visibility.Visible;
                case MediaFlag.AUDIO:
                    return audio ? Visibility.Visible : Visibility.Hidden;
                case MediaFlag.VIDEO:
                    return audio ? Visibility.Hidden : Visibility.Visible;
                default:
                    return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
