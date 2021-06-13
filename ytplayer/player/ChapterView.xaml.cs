﻿using io.github.toyota32k.toolkit.utils;
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
        public class ChapterViewModel : ViewModelBase {
            public ReactivePropertySlim<ulong> Duration { get; } = new ReactivePropertySlim<ulong>(0);
            public ReactivePropertySlim<ChapterList> Chapters { get; } = new ReactivePropertySlim<ChapterList>(null);
            public ReadOnlyReactivePropertySlim<bool> HasSkipChapter { get; }

            public ChapterViewModel() {
                HasSkipChapter = Chapters.CombineLatest(Duration, (c, d) => {
                    if (c==null || d == 0) return false;
                    return !Utils.IsNullOrEmpty(c.GetDisabledSpans(d));
                }).ToReadOnlyReactivePropertySlim();
            }
        }

        public ChapterViewModel ViewModel {
            get => (ChapterViewModel)DataContext;
            set {
                ViewModel?.Dispose();
                DataContext = value; 
            }
        }

        public ChapterView() {
            InitializeComponent();
            ViewModel = new ChapterViewModel();
            ViewModel.Chapters.Subscribe(OnChapterListChanged);
        }

        private double Time2Position(ulong time) {
            var dur = ViewModel.Duration.Value;
            if (dur == 0) return 0;
            return this.ActualWidth * (double)time / (double)dur;
        }

        public void SetChapterList(ChapterList list, ulong duration) {
            ViewModel.Chapters.Value = null;
            ViewModel.Duration.Value = duration;
            ViewModel.Chapters.Value = list;
        }

        private void OnChapterListChanged(ChapterList list) {
            TickerView.Children.Clear();
            RangeView.Children.Clear();
            var duration = ViewModel.Duration.Value;
            if (list != null && duration > 0) {
                foreach (var c in list.Keys) {
                    var rc = new Rectangle() {
                        Width = 1,
                        Fill = new SolidColorBrush(Colors.White),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(Time2Position(c), 0, 0, 0)
                    };
                    TickerView.Children.Add(rc);
                }
                foreach (var r in list.GetDisabledSpans(duration)) {
                    var rc = new Rectangle() {
                        Width = Time2Position(r.End - r.Start),
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