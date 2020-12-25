using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ytplayer.common {
    public static class ColorUtil {

        private static Regex colorReg = new Regex(@"#*(?<a>[0-9a-f]{2})*(?<r>[0-9a-f]{2})(?<g>[0-9a-f]{2})(?<b>[0-9a-f]{2})", RegexOptions.IgnoreCase);
        public static Color Parse(string colorText) {
            var m = colorReg.Match(colorText);
            if(m.Success) {
                var a = m.Groups["a"].Value;
                var r = m.Groups["r"].Value;
                var g = m.Groups["g"].Value;
                var b = m.Groups["b"].Value;
                if(!string.IsNullOrEmpty(a)) {
                    return Color.FromArgb(Convert.ToByte(a,16), Convert.ToByte(r,16), Convert.ToByte(g,16), Convert.ToByte(b,16));
                } else {
                    return Color.FromRgb(Convert.ToByte(r, 16), Convert.ToByte(g, 16), Convert.ToByte(b, 16));
                }
            }
            return Colors.Gray;
        }
    }
}
