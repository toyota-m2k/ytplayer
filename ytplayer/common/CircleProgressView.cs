using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ytplayer.common {
    public class CircleProgressView : UserControl {

        public static readonly DependencyProperty RingStrokeColorProperty
            = DependencyProperty.Register("RingStrokeColor", typeof(Brush), typeof(CircleProgressView),
                new PropertyMetadata(new SolidColorBrush(Colors.LightGray)));
        public Brush RingStrokeColor {     // RingBaseColor
            get => (Brush)GetValue(RingStrokeColorProperty);
            set => SetValue(RingStrokeColorProperty, value);
        }

        public static readonly DependencyProperty RingFillColorProperty
            = DependencyProperty.Register("RingFillColor", typeof(Brush), typeof(CircleProgressView),
                new PropertyMetadata(new SolidColorBrush(Colors.White)));
        public Brush RingFillColor {       // InsideRingColor
            get => (Brush)GetValue(RingFillColorProperty);
            set => SetValue(RingFillColorProperty, value);
        }

        public static readonly DependencyProperty ProgressStrokeColorProperty
            = DependencyProperty.Register("ProgressStrokeColor", typeof(Brush), typeof(CircleProgressView),
                new PropertyMetadata(new SolidColorBrush(Colors.DeepSkyBlue)));
        public Brush ProgressStrokeColor {
            get => (Brush)GetValue(ProgressStrokeColorProperty);
            set => SetValue(ProgressStrokeColorProperty, value);
        }

        public static readonly DependencyProperty RingThicknessRatioProperty
            = DependencyProperty.Register("RingThicknessRatio", typeof(double), typeof(CircleProgressView),
                new PropertyMetadata(0.1, updateProgressCallback));
        public double RingThicknessRatio {
            get => (double)GetValue(RingThicknessRatioProperty);
            set => SetValue(RingThicknessRatioProperty, value);
        }

        static void updateProgressCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as CircleProgressView)?.OnUpdateProgress();
        }

        public static readonly DependencyProperty ShowLabelProperty
            = DependencyProperty.Register("ShowLabel", typeof(bool), typeof(CircleProgressView),
                new PropertyMetadata(true));
        public bool ShowLabel {
            get => (bool)GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        public static readonly DependencyProperty LabelColorProperty
            = DependencyProperty.Register("LabelColor", typeof(Brush), typeof(CircleProgressView),
                new PropertyMetadata(new SolidColorBrush(Colors.Black)));
        public Brush LabelColor {       // TextColor
            get => (Brush)GetValue(LabelColorProperty);
            set => SetValue(LabelColorProperty, value);
        }


        public static readonly DependencyProperty ProgressProperty
            = DependencyProperty.Register("Progress", typeof(int), typeof(CircleProgressView),
                new PropertyMetadata(0, updateProgressCallback));
        public int Progress {
            get => (int)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        private double RingThickness => (double)GetValue(RingThicknessRatioProperty) * 50.0;
        private double Radius => (100 - RingThickness) / 2;
        private Size ArcSize => new Size(Radius, Radius);
        private string ProgressText => $"{Progress}%";

        /**
         * 円弧の始点
         */
        private Point StartPoint => new Point(50, RingThickness / 2);

        /**
         * 円弧の終点
         */
        private Point EndPoint {
            get {
                double progress = Progress / 100.0;
                if (progress >= 1.0) {
                    progress = 0.99999;
                }
                if (progress < 0) {
                    progress = 0;
                }
                double angle = 2 * Math.PI * progress;
                var p = new Point(Math.Sin(angle) * Radius + 50, Math.Cos(angle - Math.PI) * Radius + 50);
                Debug.WriteLine($"progress={Progress}, angle={angle}, x={p.X}, y={p.Y}");
                return p;

            }
        }

        /**
         * 大きい円弧/小さい円弧のどちらで結ぶか
         */
        private bool IsLargeArc => Progress > 50;

        private Ellipse ellipse;
        private Path path;
        private PathFigure pathFigure;
        private ArcSegment arcSegment;
        private TextBlock label;

        private T Create<T>(T view, Action<T> init) {
            init(view);
            return view;
        }

        private void OnUpdateProgress() {
            this.pathFigure.StartPoint = this.StartPoint;
            this.arcSegment.Size = ArcSize;
            this.arcSegment.IsLargeArc = IsLargeArc;
            this.arcSegment.Point = EndPoint;
            this.label.Text = ProgressText;
        }

        public CircleProgressView() {
            this.Content = Create(new Grid(), (rootGrid) => {
                rootGrid.Children.Add(Create(new Viewbox(), (viewbox) => {
                    viewbox.Child = Create(new Grid() { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Transparent) }, (grid) => {
                        grid.Children.Add(Create(new Ellipse() { Width = 100, Height = 100 }, (ellipse) => {
                            this.ellipse = ellipse;
                            this.ellipse.Stroke = RingStrokeColor;
                            this.ellipse.StrokeThickness = RingThickness;
                            this.ellipse.Fill = RingFillColor;
                        }));
                        grid.Children.Add(Create(new Path(), (path) => {
                            this.path = path;
                            this.path.Stroke = ProgressStrokeColor;
                            this.path.StrokeThickness = RingThickness;
                            this.path.Data = Create(new PathGeometry(), (pg) => {
                                pg.Figures.Add(Create(new PathFigure(), (pf) => {
                                this.pathFigure = pf;
                                this.pathFigure.StartPoint = this.StartPoint;
                                    this.pathFigure.Segments.Add(Create(new ArcSegment() { RotationAngle = 0, SweepDirection = SweepDirection.Clockwise }, (arc) => {
                                        this.arcSegment = arc;
                                        this.arcSegment.Size = ArcSize;
                                        this.arcSegment.IsLargeArc = IsLargeArc;
                                        this.arcSegment.Point = EndPoint;
                                    }));
                                }));
                            });
                        }));
                        grid.Children.Add(Create(new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = 30, FontWeight = FontWeight.FromOpenTypeWeight(500) }, (label) => {
                            this.label = label;
                            this.label.Text = ProgressText;
                            this.label.Visibility = ShowLabel ? Visibility.Visible : Visibility.Collapsed;
                        }));
                    });
                }));
            });


            CreateBinding(label, TextBlock.VisibilityProperty, this, "ShowLabel", new BoolVisibilityConverter());
            CreateBinding(label, TextBlock.ForegroundProperty, this, "LabelColor");
            CreateBinding(ellipse, Ellipse.StrokeProperty, this, "RingStrokeColor");
            CreateBinding(ellipse, Ellipse.FillProperty, this, "RingFillColor");
            CreateBinding(ellipse, Ellipse.StrokeThicknessProperty, this, "RingThicknessRatio", new RingThicknessConverter());
            CreateBinding(path, Path.StrokeProperty, this, "ProgressStrokeColor");
            CreateBinding(path, Path.StrokeThicknessProperty, this, "RingThicknessRatio", new RingThicknessConverter());
        }

        class RingThicknessConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                return (double)value * 50;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                return DependencyProperty.UnsetValue;
            }
        }

        class BoolVisibilityConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                if ((bool)value) {
                    return Visibility.Visible;
                } else {
                    return Visibility.Collapsed;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                return DependencyProperty.UnsetValue;
            }
        }

        static void CreateBinding(FrameworkElement target, DependencyProperty targetProperty, 
            object source, string sourceProperty, IValueConverter converter = null) {
            var binding = new Binding();
            binding.Source = source;
            binding.Path = new PropertyPath(sourceProperty);
            binding.Converter = converter;
            target.SetBinding(targetProperty, binding);
        }
    }
}
