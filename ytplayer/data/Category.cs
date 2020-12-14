using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq.Mapping;
using System.Text.RegularExpressions;
using common;
//using System.Drawing;
using System.Windows.Media;

namespace ytplayer.data {
    public class Category : MicPropertyChangeNotifier {
        private string label = null;
        private string svgPath = null;
        //private int color = 0;
        private Color color = Colors.Blue;

        public string Label {
            get => label;
            set => setProp(callerName(), ref label, value);
        }
        public string SvgPath {
            get => svgPath;
            set => setProp(callerName(), ref svgPath, value);
        }

        //private static byte A(uint v) {
        //    return (byte)((v & 0xFF000000) >> 24);
        //}
        //private static byte R(uint v) {
        //    return (byte)((v & 0x00FF0000) >> 16);
        //}
        //private static byte G(uint v) {
        //    return (byte)((v & 0x0000FF00) >> 8);
        //}
        //private static byte B(uint v) {
        //    return (byte)(v & 0x000000FF);
        //}
        //private static uint IntColor(byte a, byte r, byte g, byte b) {
        //    return ((((uint)a) << 24) & 0xFF000000) | ((((uint)r) << 16) & 0x00FF0000) | ((((uint)g) << 8) & 0x0000FF00) | (((uint)b) & 0x000000FF);
        //}
        //private static uint IntColor(Color c) {
        //    return IntColor(c.A, c.R, c.G, c.B);
        //}

        public Color Color {
            get => color; // Color.FromArgb(A(color), R(color), G(color), B(color));
            set => setProp(callerName(), ref color, value, "Brush");
        }

        [System.Xml.Serialization.XmlIgnore]
        public SolidColorBrush Brush => new SolidColorBrush(Color);

        public Category() {

        }

        public Category(string label, Color color, string svgPath) {
            this.label = label;
            this.svgPath = svgPath;
            this.Color = color;
        }
    }

    public class CategoryList {
        public List<Category> List { get; } = new List<Category>();

        public Category Get(string label) {
            return List.Find((c) => c.Label.Equals(label, StringComparison.CurrentCultureIgnoreCase));
        }
        public Category Create(string label, string svgPath, Color color) {
            var c = Get(label);
            if(c!=null) {
                c.SvgPath = svgPath;
                c.Color = color;
            } else {
                c = new Category(label, svgPath, color);
                List.Add(c);
            }
            return c;
        }
        
        public void Initialize() {
            if(List.Count==0) {
                List.Add(new Category("Music", Colors.Orange, "M13 11V7.5L15.2 5.29C16 4.5 16.15 3.24 15.59 2.26C15.14 1.47 14.32 1 13.45 1C13.24 1 13 1.03 12.81 1.09C11.73 1.38 11 2.38 11 3.5V6.74L7.86 9.91C6.2 11.6 5.7 14.13 6.61 16.34C7.38 18.24 9.06 19.55 11 19.89V20.5C11 20.76 10.77 21 10.5 21H9V23H10.5C11.85 23 13 21.89 13 20.5V20C15.03 20 17.16 18.08 17.16 15.25C17.16 12.95 15.24 11 13 11M13 3.5C13 3.27 13.11 3.09 13.32 3.03C13.54 2.97 13.77 3.06 13.88 3.26C14 3.46 13.96 3.71 13.8 3.87L13 4.73V3.5M11 11.5C10.03 12.14 9.3 13.24 9.04 14.26L11 14.78V17.83C9.87 17.53 8.9 16.71 8.43 15.57C7.84 14.11 8.16 12.45 9.26 11.33L11 9.5V11.5M13 18V12.94C14.17 12.94 15.18 14.04 15.18 15.25C15.18 17 13.91 18 13 18Z"));
                List.Add(new Category("Cats & Dogs", Colors.OliveDrab, "M12,8L10.67,8.09C9.81,7.07 7.4,4.5 5,4.5C5,4.5 3.03,7.46 4.96,11.41C4.41,12.24 4.07,12.67 4,13.66L2.07,13.95L2.28,14.93L4.04,14.67L4.18,15.38L2.61,16.32L3.08,17.21L4.53,16.32C5.68,18.76 8.59,20 12,20C15.41,20 18.32,18.76 19.47,16.32L20.92,17.21L21.39,16.32L19.82,15.38L19.96,14.67L21.72,14.93L21.93,13.95L20,13.66C19.93,12.67 19.59,12.24 19.04,11.41C20.97,7.46 19,4.5 19,4.5C16.6,4.5 14.19,7.07 13.33,8.09L12,8M9,11A1,1 0 0,1 10,12A1,1 0 0,1 9,13A1,1 0 0,1 8,12A1,1 0 0,1 9,11M15,11A1,1 0 0,1 16,12A1,1 0 0,1 15,13A1,1 0 0,1 14,12A1,1 0 0,1 15,11M11,14H13L12.3,15.39C12.5,16.03 13.06,16.5 13.75,16.5A1.5,1.5 0 0,0 15.25,15H15.75A2,2 0 0,1 13.75,17C13,17 12.35,16.59 12,16V16H12C11.65,16.59 11,17 10.25,17A2,2 0 0,1 8.25,15H8.75A1.5,1.5 0 0,0 10.25,16.5C10.94,16.5 11.5,16.03 11.7,15.39L11,14Z");
                List.Add(new Category("Funny", Colors.OliveDrab, "M12,8L10.67,8.09C9.81,7.07 7.4,4.5 5,4.5C5,4.5 3.03,7.46 4.96,11.41C4.41,12.24 4.07,12.67 4,13.66L2.07,13.95L2.28,14.93L4.04,14.67L4.18,15.38L2.61,16.32L3.08,17.21L4.53,16.32C5.68,18.76 8.59,20 12,20C15.41,20 18.32,18.76 19.47,16.32L20.92,17.21L21.39,16.32L19.82,15.38L19.96,14.67L21.72,14.93L21.93,13.95L20,13.66C19.93,12.67 19.59,12.24 19.04,11.41C20.97,7.46 19,4.5 19,4.5C16.6,4.5 14.19,7.07 13.33,8.09L12,8M9,11A1,1 0 0,1 10,12A1,1 0 0,1 9,13A1,1 0 0,1 8,12A1,1 0 0,1 9,11M15,11A1,1 0 0,1 16,12A1,1 0 0,1 15,13A1,1 0 0,1 14,12A1,1 0 0,1 15,11M11,14H13L12.3,15.39C12.5,16.03 13.06,16.5 13.75,16.5A1.5,1.5 0 0,0 15.25,15H15.75A2,2 0 0,1 13.75,17C13,17 12.35,16.59 12,16V16H12C11.65,16.59 11,17 10.25,17A2,2 0 0,1 8.25,15H8.75A1.5,1.5 0 0,0 10.25,16.5C10.94,16.5 11.5,16.03 11.7,15.39L11,14Z");
            }
        }
    }
}
