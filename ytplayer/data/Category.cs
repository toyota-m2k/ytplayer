using io.github.toyota32k.toolkit.view;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace ytplayer.data {
    public class Category : PropertyChangeNotifier {
        private string label = null;
        private string svgPath = null;
        private uint color = Color2Int(Colors.Blue);

        public string Label {
            get => label;
            set => setProp(callerName(), ref label, value);
        }
        public string SvgPath {
            get => svgPath;
            set => setProp(callerName(), ref svgPath, value);
        }

        private static byte A(uint v) {
            return (byte)((v & 0xFF000000) >> 24);
        }
        private static byte R(uint v) {
            return (byte)((v & 0x00FF0000) >> 16);
        }
        private static byte G(uint v) {
            return (byte)((v & 0x0000FF00) >> 8);
        }
        private static byte B(uint v) {
            return (byte)(v & 0x000000FF);
        }
        private static uint Color2Int(byte a, byte r, byte g, byte b) {
            return ((((uint)a) << 24) & 0xFF000000) | ((((uint)r) << 16) & 0x00FF0000) | ((((uint)g) << 8) & 0x0000FF00) | (((uint)b) & 0x000000FF);
        }
        private static uint Color2Int(Color c) {
            return Color2Int(c.A, c.R, c.G, c.B);
        }
        //private static string Color2String(Color c) {
        //    return String.Format("#{0:2x}{0:2x}{0:2x}{0:2x}", c.A, c.R, c.G, c.B);
        //}

        public uint IntColor {
            get => color;
            set => setProp(callerName(), ref color, value, "Brush", "Color");
        }

        //public string StringColor {
        //    get => Color2String(Color);
        //}
                
        public int SortIndex { get; set; } = 0;

        [System.Xml.Serialization.XmlIgnore]
        public Color Color {
            get => Color.FromArgb(A(color), R(color), G(color), B(color));
            set => IntColor = Color2Int(value);
        }

        [System.Xml.Serialization.XmlIgnore]
        public SolidColorBrush Brush => new SolidColorBrush(Color);

        public Category() {

        }

        public Category(string label, Color color, int sortIndex, string svgPath) {
            this.label = label;
            this.svgPath = svgPath;
            this.Color = color;
            this.SortIndex = sortIndex;
        }

        public IEnumerable<DLEntry> Filter(IEnumerable<DLEntry> src) {
            if (SortIndex<=0) {
                return src;
            } else {
                return src.Where((e) => e.Category.Label == Label);
            }
        }

        public override bool Equals(object obj) {
            //objがnullか、型が違うときは、等価でない
            if (obj == null || this.GetType() != obj.GetType()) {
                return false;
            }
            if(ReferenceEquals(this,obj)) {
                return true;
            }

            return obj is Category c && c.Label == Label;
        }

        public override int GetHashCode() {
            return Label.GetHashCode();
        }
    }

    public class CategoryList {
        [System.Xml.Serialization.XmlIgnore]
        private Dictionary<string, Category> Dic { get; } = new Dictionary<string, Category>();

        public List<Category> SerializableList {
            get => Dic.Values.ToList();
            set { Dic.Clear(); value.ForEach((c) => Dic[c.Label] = c); }
        }

        private Category get(string label) {
            return Dic.TryGetValue(label.ToLower(), out var c) ? c : null;
        }
        public Category Get(string label) {
            if(string.IsNullOrEmpty(label)||label=="-") {
                label = "unchecked";
            }
            return  get(label) ?? Unknown;
            }
        public Category Create(string label, Color color, int sortIndex, string svgPath, bool resetProp=true) {
            var c = get(label);
            if(c!=null) {
                if (resetProp) {
                    c.Color = color;
                    c.Label = label;
                    c.SortIndex = sortIndex;
                    c.SvgPath = svgPath;
                }
            }
            else {
                c = new Category(label, color, sortIndex, svgPath);
                Dic.Add(label.ToLower(), c);
            }
            return c;
        }

        [System.Xml.Serialization.XmlIgnore]
        public Category All => Get("all");

        [System.Xml.Serialization.XmlIgnore]
        public IEnumerable<Category> SelectList  => Dic.Values.Where((c) => c.SortIndex > 0).OrderBy((c) => c.SortIndex);

        [System.Xml.Serialization.XmlIgnore]
        public IEnumerable<Category> FilterList => Dic.Values.OrderBy((c) => c.SortIndex);

        private static readonly Lazy<Category> lazyCategory = new Lazy<Category>(() => new Category("Unknown", Colors.Gray, -1, "M11,18H13V16H11V18M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20C7.59,20 4,16.41 4,12C4,7.59 7.59,4 12,4C16.41,4 20,7.59 20,12C20,16.41 16.41,20 12,20M12,6A4,4 0 0,0 8,10H10A2,2 0 0,1 12,8A2,2 0 0,1 14,10C14,12 11,11.75 11,15H13C13,12.75 16,12.5 16,10A4,4 0 0,0 12,6Z"));
        public static Category Unknown => lazyCategory.Value;

        public void Initialize() {
            Create("All", Colors.Blue, 0,               "M17.9,17.39C17.64,16.59 16.89,16 16,16H15V13A1,1 0 0,0 14,12H8V10H10A1,1 0 0,0 11,9V7H13A2,2 0 0,0 15,5V4.59C17.93,5.77 20,8.64 20,12C20,14.08 19.2,15.97 17.9,17.39M11,19.93C7.05,19.44 4,16.08 4,12C4,11.38 4.08,10.78 4.21,10.21L9,15V16A2,2 0 0,0 11,18M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z");
            Create("Unchecked", Colors.DarkGray, 1000,  "M19 13H5v-2h14v2z");
            Create("Music", Colors.DarkViolet, 9, "M21,3V15.5A3.5,3.5 0 0,1 17.5,19A3.5,3.5 0 0,1 14,15.5A3.5,3.5 0 0,1 17.5,12C18.04,12 18.55,12.12 19,12.34V6.47L9,8.6V17.5A3.5,3.5 0 0,1 5.5,21A3.5,3.5 0 0,1 2,17.5A3.5,3.5 0 0,1 5.5,14C6.04,14 6.55,14.12 7,14.34V6L21,3Z");
            Create("Rock", Colors.Crimson, 1,   "M19.59,3H22V5H20.41L16.17,9.24C15.8,8.68 15.32,8.2 14.76,7.83L19.59,3M12,8A4,4 0 0,1 16,12C16,13.82 14.77,15.42 13,15.87V16A5,5 0 0,1 8,21A5,5 0 0,1 3,16A5,5 0 0,1 8,11H8.13C8.58,9.24 10.17,8 12,8M12,10.5A1.5,1.5 0 0,0 10.5,12A1.5,1.5 0 0,0 12,13.5A1.5,1.5 0 0,0 13.5,12A1.5,1.5 0 0,0 12,10.5M6.94,14.24L6.23,14.94L9.06,17.77L9.77,17.06L6.94,14.24Z");
            Create("Classic", Colors.ForestGreen, 2, "M13 11V7.5L15.2 5.29C16 4.5 16.15 3.24 15.59 2.26C15.14 1.47 14.32 1 13.45 1C13.24 1 13 1.03 12.81 1.09C11.73 1.38 11 2.38 11 3.5V6.74L7.86 9.91C6.2 11.6 5.7 14.13 6.61 16.34C7.38 18.24 9.06 19.55 11 19.89V20.5C11 20.76 10.77 21 10.5 21H9V23H10.5C11.85 23 13 21.89 13 20.5V20C15.03 20 17.16 18.08 17.16 15.25C17.16 12.95 15.24 11 13 11M13 3.5C13 3.27 13.11 3.09 13.32 3.03C13.54 2.97 13.77 3.06 13.88 3.26C14 3.46 13.96 3.71 13.8 3.87L13 4.73V3.5M11 11.5C10.03 12.14 9.3 13.24 9.04 14.26L11 14.78V17.83C9.87 17.53 8.9 16.71 8.43 15.57C7.84 14.11 8.16 12.45 9.26 11.33L11 9.5V11.5M13 18V12.94C14.17 12.94 15.18 14.04 15.18 15.25C15.18 17 13.91 18 13 18Z");
            Create("Oldies", Colors.Sienna, 3, "M18.5 5A1.5 1.5 0 1 1 17 6.5A1.5 1.5 0 0 1 18.5 5M18.5 11A1.5 1.5 0 1 1 17 12.5A1.5 1.5 0 0 1 18.5 11M10 4A5 5 0 0 0 5 9V10A2 2 0 1 0 7.18 8A3 3 0 0 1 10 6A4 4 0 0 1 14 10C14 13.59 11.77 16.19 7 18.2L7.76 20.04C13.31 17.72 16 14.43 16 10A6 6 0 0 0 10 4Z");
            Create("Song", Colors.DodgerBlue, 4, "M9,3A4,4 0 0,1 13,7H5A4,4 0 0,1 9,3M11.84,9.82L11,18H10V19A2,2 0 0,0 12,21A2,2 0 0,0 14,19V14A4,4 0 0,1 18,10H20L19,11L20,12H18A2,2 0 0,0 16,14V19A4,4 0 0,1 12,23A4,4 0 0,1 8,19V18H7L6.16,9.82C5.67,9.32 5.31,8.7 5.13,8H12.87C12.69,8.7 12.33,9.32 11.84,9.82M9,11A1,1 0 0,0 8,12A1,1 0 0,0 9,13A1,1 0 0,0 10,12A1,1 0 0,0 9,11Z");
            Create("Jazz", Colors.BurlyWood, 6, "M12 3V13.55A4 4 0 1 0 14 17V7H18V3M16.5 20A1.5 1.5 0 1 1 18 18.5A1.5 1.5 0 0 1 16.5 20Z");
            Create("Amateur", Colors.LightSteelBlue, 7, "M11.71 16.81C10.91 17.6 10.88 18.84 11.64 19.58L10.19 21C8.66 19.5 8.72 17.03 10.32 15.46C10.85 14.94 11.5 14.61 12.16 14.42L9 11.34L10.45 9.92L10.82 9.57C11.82 8.59 11.85 7.04 10.9 6.11L9.16 4.42L10.62 3L14.78 7.06C15.54 7.81 15.5 9.05 14.71 9.83L12.53 11.95L16 15.33L15.61 15.72C15.11 16.21 14.38 16.46 13.72 16.28C13.04 16.1 12.26 16.28 11.71 16.81Z");
            Create("Cats & Dogs", Colors.Gold, 10,  "M12,8L10.67,8.09C9.81,7.07 7.4,4.5 5,4.5C5,4.5 3.03,7.46 4.96,11.41C4.41,12.24 4.07,12.67 4,13.66L2.07,13.95L2.28,14.93L4.04,14.67L4.18,15.38L2.61,16.32L3.08,17.21L4.53,16.32C5.68,18.76 8.59,20 12,20C15.41,20 18.32,18.76 19.47,16.32L20.92,17.21L21.39,16.32L19.82,15.38L19.96,14.67L21.72,14.93L21.93,13.95L20,13.66C19.93,12.67 19.59,12.24 19.04,11.41C20.97,7.46 19,4.5 19,4.5C16.6,4.5 14.19,7.07 13.33,8.09L12,8M9,11A1,1 0 0,1 10,12A1,1 0 0,1 9,13A1,1 0 0,1 8,12A1,1 0 0,1 9,11M15,11A1,1 0 0,1 16,12A1,1 0 0,1 15,13A1,1 0 0,1 14,12A1,1 0 0,1 15,11M11,14H13L12.3,15.39C12.5,16.03 13.06,16.5 13.75,16.5A1.5,1.5 0 0,0 15.25,15H15.75A2,2 0 0,1 13.75,17C13,17 12.35,16.59 12,16V16H12C11.65,16.59 11,17 10.25,17A2,2 0 0,1 8.25,15H8.75A1.5,1.5 0 0,0 10.25,16.5C10.94,16.5 11.5,16.03 11.7,15.39L11,14Z");
            Create("Funny", Colors.Cyan, 20,        "M6 11V12.5H7.5V14H9V11M12.5 6H11V9H14V7.5H12.5M9.8 17A5.5 5.5 0 0 0 17 9.8M6.34 6.34A8 8 0 0 1 15.08 4.62A4.11 4.11 0 0 1 15.73 2.72A10 10 0 0 0 2.73 15.72A4.11 4.11 0 0 1 4.63 15.07A8 8 0 0 1 6.34 6.34M17.66 17.66A8 8 0 0 1 8.92 19.38A4.11 4.11 0 0 1 8.27 21.28A10 10 0 0 0 21.27 8.28A4.11 4.11 0 0 1 19.37 8.93A8 8 0 0 1 17.66 17.66M6 11V12.5H7.5V14H9V11M9.8 17A5.5 5.5 0 0 0 17 9.8M12.5 6H11V9H14V7.5H12.5M6 11V12.5H7.5V14H9V11M12.5 6H11V9H14V7.5H12.5M9.8 17A5.5 5.5 0 0 0 17 9.8M4.93 21A2 2 0 0 1 2.93 19A2 2 0 0 1 4.93 17H6.93V19A2 2 0 0 1 4.93 21.07M19.07 2.93A2 2 0 0 1 21.07 4.93A2 2 0 0 1 19.07 6.93H17.07V4.93A2 2 0 0 1 19.07 2.93Z");
            Create("Lovely", Colors.Orange, 30,        "M12.1,18.55L12,18.65L11.89,18.55C7.14,14.24 4,11.39 4,8.5C4,6.5 5.5,5 7.5,5C9.04,5 10.54,6 11.07,7.36H12.93C13.46,6 14.96,5 16.5,5C18.5,5 20,6.5 20,8.5C20,11.39 16.86,14.24 12.1,18.55M16.5,3C14.76,3 13.09,3.81 12,5.08C10.91,3.81 9.24,3 7.5,3C4.42,3 2,5.41 2,8.5C2,12.27 5.4,15.36 10.55,20.03L12,21.35L13.45,20.03C18.6,15.36 22,12.27 22,8.5C22,5.41 19.58,3 16.5,3Z");
            Create("Sexy", Colors.Magenta, 40, "M5,15L4.4,14.5C2.4,12.6 1,11.4 1,9.9C1,8.7 2,7.7 3.2,7.7C3.9,7.7 4.6,8 5,8.5C5.4,8 6.1,7.7 6.8,7.7C8,7.7 9,8.6 9,9.9C9,11.4 7.6,12.6 5.6,14.5L5,15M15,4A4,4 0 0,0 11,8A4,4 0 0,0 15,12A4,4 0 0,0 19,8A4,4 0 0,0 15,4M15,10.1A2.1,2.1 0 0,1 12.9,8A2.1,2.1 0 0,1 15,5.9C16.16,5.9 17.1,6.84 17.1,8C17.1,9.16 16.16,10.1 15,10.1M15,13C12.33,13 7,14.33 7,17V20H23V17C23,14.33 17.67,13 15,13M21.1,18.1H8.9V17C8.9,16.36 12,14.9 15,14.9C17.97,14.9 21.1,16.36 21.1,17V18.1Z");
            Create("Awesome", Colors.MediumSlateBlue, 50, "M19,1L17.74,3.75L15,5L17.74,6.26L19,9L20.25,6.26L23,5L20.25,3.75M9,4L6.5,9.5L1,12L6.5,14.5L9,20L11.5,14.5L17,12L11.5,9.5M19,15L17.74,17.74L15,19L17.74,20.25L19,23L20.25,20.25L23,19L20.25,17.74");
            Create("Instruction", Colors.YellowGreen, 60, "M17.5 14.33C18.29 14.33 19.13 14.41 20 14.57V16.07C19.38 15.91 18.54 15.83 17.5 15.83C15.6 15.83 14.11 16.16 13 16.82V15.13C14.17 14.6 15.67 14.33 17.5 14.33M13 12.46C14.29 11.93 15.79 11.67 17.5 11.67C18.29 11.67 19.13 11.74 20 11.9V13.4C19.38 13.24 18.54 13.16 17.5 13.16C15.6 13.16 14.11 13.5 13 14.15M17.5 10.5C15.6 10.5 14.11 10.82 13 11.5V9.84C14.23 9.28 15.73 9 17.5 9C18.29 9 19.13 9.08 20 9.23V10.78C19.26 10.59 18.41 10.5 17.5 10.5M21 18.5V7C19.96 6.67 18.79 6.5 17.5 6.5C15.45 6.5 13.62 7 12 8V19.5C13.62 18.5 15.45 18 17.5 18C18.69 18 19.86 18.16 21 18.5M17.5 4.5C19.85 4.5 21.69 5 23 6V20.56C23 20.68 22.95 20.8 22.84 20.91C22.73 21 22.61 21.08 22.5 21.08C22.39 21.08 22.31 21.06 22.25 21.03C20.97 20.34 19.38 20 17.5 20C15.45 20 13.62 20.5 12 21.5C10.66 20.5 8.83 20 6.5 20C4.84 20 3.25 20.36 1.75 21.07C1.72 21.08 1.68 21.08 1.63 21.1C1.59 21.11 1.55 21.12 1.5 21.12C1.39 21.12 1.27 21.08 1.16 21C1.05 20.89 1 20.78 1 20.65V6C2.34 5 4.18 4.5 6.5 4.5C8.83 4.5 10.66 5 12 6C13.34 5 15.17 4.5 17.5 4.5Z");
            Create("Other", Colors.Navy, 100,         "M11,15H13V17H11V15M11,7H13V13H11V7M12,2C6.47,2 2,6.5 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20Z");
        }

        public static string CategoryToDbLabel(Category c) {
            switch(c.Label) {
                case "All":
                case "Unchecked":
                case "Unknown":
                    return null;
                default:
                    return c.Label;
            }
        }
    }
}
