using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ytplayer.data;

namespace ytplayer.player {
    /// <summary>
    /// ChapterView.xaml の相互作用ロジック
    /// </summary>
    public partial class ChapterView : UserControl {
        public PlayerViewModel ViewModel => DataContext as PlayerViewModel;

        public ChapterView() {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ViewModel.Chapters.Subscribe(OnChapterListChanged);
            ViewModel.DisabledRanges.Subscribe(OnDisabledRangesChanged);
        }

        private double Time2Position(ulong time) {
            var dur = ViewModel.Duration.Value;
            if (dur == 0) return 0;
            return this.ActualWidth * (double)time / (double)dur;
        }

        private void OnChapterListChanged(ChapterList list) {
            TickerView.Children.Clear();
            RangeView.Children.Clear();
            var duration = ViewModel.Duration.Value;
            if (list != null && duration > 0) {
                foreach (var c in list.Values) {
                    var rc = new Rectangle() {
                        Width = 2,
                        Fill = new SolidColorBrush(Colors.White),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(Time2Position(c.Position), 0, 0, 0)
                    };
                    TickerView.Children.Add(rc);
                }
            }
        }

        private void OnDisabledRangesChanged(List<PlayRange> list) {
            RangeView.Children.Clear();
            var duration = ViewModel.Duration.Value;
            if (list != null && duration > 0) {
                foreach (var r in list) {
                    var end = r.End == 0 ? duration : r.End;
                    var rc = new Rectangle() {
                        Width = Time2Position(end - r.Start),
                        Fill = new SolidColorBrush(Colors.Gray),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(Time2Position(r.Start), 0, 0, 0)
                    };
                    RangeView.Children.Add(rc);
                }
            }
        }

    }
}
