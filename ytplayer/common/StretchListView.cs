using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ytplayer.common {
    public class StretchListView : ListView {
        public StretchListView() : base() {
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            this.SizeChanged += OnSizeChanged;
        }

        public static readonly DependencyProperty StretchColumnIndexProperty = DependencyProperty.Register("StretchColumnIndex", typeof(int), typeof(StretchListView), new PropertyMetadata(-1));
        public int StretchColumnIndex {
            get => (int)GetValue(StretchColumnIndexProperty);
            set => SetValue(StretchColumnIndexProperty, value);
        }
        public static readonly DependencyProperty StretchColumnMinWidthProperty = DependencyProperty.Register("StretchColumnMinWidth", typeof(int), typeof(StretchListView), new PropertyMetadata(0));
        public int StretchColumnMinWidth {
            get => Math.Max(0,(int)GetValue(StretchColumnMinWidthProperty));
            set => SetValue(StretchColumnMinWidthProperty, value);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e) {
            this.Loaded -= OnLoaded;
            await Task.Delay(500);
            //this.Measure(new Size(ActualWidth, ActualHeight));
            UpdateColumnWidth(ActualWidth);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            this.SizeChanged -= OnSizeChanged;
            this.Unloaded -= OnUnloaded;
        }
        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            if (e.WidthChanged) {
                UpdateColumnWidth(e.NewSize.Width);
            }
        }
        private void UpdateColumnWidth(double width) {
            var gridView = View as GridView;
            if (gridView == null) return;

            int sci = StretchColumnIndex;
            if (sci < 0) {
                sci = gridView.Columns.Count - 1;
            }

            //if (this.ActualWidth == Double.NaN) {
            //    return;
            //    //this.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            //}
            double remainingSpace = width - 30;
            for (int i = 0; i < gridView.Columns.Count; i++) {
                if (i != sci) {
                    remainingSpace -= (this.View as GridView).Columns[i].ActualWidth;
                }
            }
            gridView.Columns[sci].Width = Math.Max(StretchColumnMinWidth, remainingSpace);
        }
    }
}
