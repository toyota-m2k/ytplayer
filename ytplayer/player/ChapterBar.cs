using ytplayer.data;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace ytplayer.player {
    public class ChapterBar : Grid {
        public static readonly DependencyProperty TickColorProperty = DependencyProperty.Register("TickColor", typeof(Brush), typeof(ChapterBar), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.LimeGreen)));
        public Brush TickColor {
            get => GetValue(TickColorProperty) as Brush;
            set => this.SetValue(TickColorProperty, value);
        }
        public static readonly DependencyProperty ActivceRangeColorProperty = DependencyProperty.Register("ActiveRangeColor", typeof(Brush), typeof(ChapterBar), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.LightGray)));
        public Brush ActiveRangeColor {
            get => GetValue(ActivceRangeColorProperty) as Brush;
            set => this.SetValue(ActivceRangeColorProperty, value);
        }
        public static readonly DependencyProperty DraggingRangeColorProperty = DependencyProperty.Register("DraggingRangeColor", typeof(Brush), typeof(ChapterBar), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Orange)));
        public Brush DraggingRangeColor {
            get => GetValue(DraggingRangeColorProperty) as Brush;
            set => this.SetValue(DraggingRangeColorProperty, value);
        }

        PlayerViewModel ViewModel => DataContext as PlayerViewModel;
        const double TICK_WIDTH = 2;
        const int Z_RANGE = 1;
        const int Z_DRAGGING = 2;
        const int Z_TICK = 3;
        private double PrevWidth = 0;

        public ChapterBar() {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            Background = new SolidColorBrush();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ViewModel?.Chapters.Subscribe(OnChapterListChanged);
            ViewModel?.DisabledRanges.Subscribe(OnDisabledRangesChanged);
            ViewModel?.DraggingRange.Subscribe(OnDraggingRangeChanged);
            Loaded -= OnLoaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            Unloaded -= OnUnloaded;
            SizeChanged -= OnSizeChanged;
        }



        private double Time2Position(ulong time) {
            var dur = ViewModel.Duration.Value;
            if (dur == 0) return 0;
            return this.ActualWidth * (double)time / (double)dur;
        }

        private void OnChapterListChanged(ChapterList list) {
            PrevWidth = ActualWidth;
            Children.Clear();
            var duration = ViewModel.Duration.Value;
            if (duration == 0) return;
            if (list != null && duration > 0) {
                foreach (var c in list.Values) {
                    var pos = Time2Position(c.Position)-TICK_WIDTH/2;
                    pos = Math.Min(Math.Max(pos,0), ActualWidth - TICK_WIDTH);
                    var rc = new Rectangle() {
                        Width = TICK_WIDTH,
                        Fill = TickColor,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(pos, 0, 0, 0),
                        Tag = "T",
                    };
                    Children.Add(rc);
                    SetZIndex(rc, Z_TICK);
                }
            }
        }

        void RemoveAllRanges() {
            // Children には linqが使えないので原始的な方法で削除
            int count = Children.Count;
            for(int i = count-1; i>=0; i--) {
                var rc = Children[i] as Rectangle;
                if(rc!=null && rc.Tag as string == "R") {
                    Children.RemoveAt(i);
                }
            }
        }

        private ulong TrueEnd(PlayRange r, ulong duration) {
            return r.End == 0 ? duration : r.End;
        }

        private List<PlayRange> GetActiveRanges(List<PlayRange> disabledRanges) {
            var activeRanges = new List<PlayRange>();
            var duration = ViewModel.Duration.Value;
            var range = new PlayRange(0, duration);
            if (!Utils.IsNullOrEmpty(disabledRanges)) {
                foreach (var d in disabledRanges) {
                    //LoggerEx.debug($"Disabled: {PlayerViewModel.FormatDuration(d.Start)} - {PlayerViewModel.FormatDuration(d.End)}");
                    if (d.Start == range.Start) {
                        range.TrySetStart(TrueEnd(d, duration));
                    }
                    else {
                        range.TrySetEnd(d.Start);
                        activeRanges.Add(range.Clone());
                        range.Set(TrueEnd(d, duration), duration);
                    }
                }
            }
            if (range.Start != duration) {
                activeRanges.Add(range);
            }
            return activeRanges;
        }

        private void OnDisabledRangesChanged(List<PlayRange> list) {
            RemoveAllRanges();
            //if (Utils.IsNullOrEmpty(list)) return;

            var duration = ViewModel.Duration.Value;
            var activeRanges = GetActiveRanges(list);
            foreach (var r in activeRanges) {
                var rc = new Rectangle() {
                    Width = Time2Position(TrueEnd(r,duration) - r.Start),
                    Fill = ActiveRangeColor,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(Time2Position(r.Start), 0, 0, 0),
                    Tag = "R",
                };
                Children.Add(rc);
            }
        }

        private Rectangle mDraggingRectangle = null;
        private void OnDraggingRangeChanged(PlayRange? range) {
            var duration = ViewModel.Duration.Value;
            if (range.HasValue) {
                var r = range.Value;
                if(mDraggingRectangle==null) {
                    mDraggingRectangle = new Rectangle() {
                        Width = Time2Position(TrueEnd(r, duration) - r.Start),
                        Fill = DraggingRangeColor,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(Time2Position(r.Start), 0, 0, 0),
                        Tag = "D",
                    };
                    Children.Add(mDraggingRectangle);
                    SetZIndex(mDraggingRectangle, Z_DRAGGING);
                } else {
                    mDraggingRectangle.Width = Time2Position(TrueEnd(r, duration) - r.Start);
                    mDraggingRectangle.Margin = new Thickness(Time2Position(r.Start), 0, 0, 0);
                }
            } else {
                if(mDraggingRectangle!=null) {
                    Children.Remove(mDraggingRectangle);
                    mDraggingRectangle = null;
                }
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            if (PrevWidth > 0 && PrevWidth != ActualWidth) {
                PrevWidth = ActualWidth;
                OnChapterListChanged(ViewModel.Chapters.Value);
                OnDisabledRangesChanged(ViewModel.DisabledRanges.Value);
            }
        }
    }
}
