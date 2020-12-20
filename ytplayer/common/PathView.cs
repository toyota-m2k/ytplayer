/**
 * SVG Path を描画するビュークラス
 * UWPには PathIcon というシンプルで使いやすいクラスがあるのにWPFにはない。残念。
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ytplayer.common {

    /**
     * SVG Path を描画するビュークラス
     * 
     * バインド可能なプロパティ
     *  Path            string      SVG Path
     *  Foreground      Brush       パス色   FrameworkElementから借用
     *  Background      Brush       背景色   FrameworkElementから借用
     *  PathWidth       Pathのキャンバスサイズ   デフォルト=24 以外のSVGパスを使う場合に設定
     *  PathHeight      Pathのキャンバスサイズ   デフォルト=24 以外のSVGパスを使う場合に設定
     */
    public class PathView : ContentControl {
        public const string DefPath = "M10,19H13V22H10V19M12,2C17.35,2.22 19.68,7.62 16.5,11.67C15.67,12.67 14.33,13.33 13.67,14.17C13,15 13,16 13,17H10C10,15.33 10,13.92 10.67,12.92C11.33,11.92 12.67,11.33 13.5,10.67C15.92,8.43 15.32,5.26 12,5A3,3 0 0,0 9,8H6A6,6 0 0,1 12,2Z";

        public static readonly DependencyProperty PathProperty
            = DependencyProperty.Register("Path", typeof(string), typeof(PathView),
                new PropertyMetadata(DefPath, (o,c)=> ((PathView)o).UpdatePath()));

        public static readonly DependencyProperty PathWidthProperty
            = DependencyProperty.Register("PathWidth", typeof(int), typeof(PathView),
                new PropertyMetadata(24));

        public static readonly DependencyProperty PathHeightProperty
            = DependencyProperty.Register("PathHeight", typeof(int), typeof(PathView),
                new PropertyMetadata(24));

        public string Path {
            get => (string)GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }
        public int PathWidth {
            get => (int)GetValue(PathWidthProperty);
            set => SetValue(PathWidthProperty, value);
        }
        public int PathHeight {
            get => (int)GetValue(PathHeightProperty);
            set => SetValue(PathHeightProperty, value);
        }

        private Path PathElement { get; set; }

        private void UpdatePath() {
            PathElement.Data = Geometry.Parse(Path);
        }

        public PathView() {
            /*  
             *  こんな構成（イメージ）
             *  <Grid Background="this.Background">
             *      <Viewbox >
             *          <Canvas Width="PathWidth" Height="PathHeight">
             *              <Path Fill="Foreground" Data="Path"/>
             *          </Canvas>
             *      </Viewbox>
             *  </Grid>
             */
            var grid = new Grid();
            var vb = new Viewbox();
            grid.Children.Add(vb);

            var canvas = new Canvas();
            canvas.Width = 24;
            canvas.Height = 24;
            vb.Child = canvas;

            PathElement = new Path();
            UpdatePath();
            canvas.Children.Add(PathElement);
            this.Content = grid;

            // PathViewのBackground をGridのBackgroundに設定
            grid.SetBinding(Grid.BackgroundProperty, new Binding("Background") { Source = this, Mode = BindingMode.OneWay });
            // PathViewのForeground を Path のFillカラーに設定
            PathElement.SetBinding(Shape.FillProperty, new Binding("Foreground") { Source = this, Mode = BindingMode.OneWay });
        }

    }
}
