using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ytplayer.data;

namespace ytplayer.player {
    /// <summary>
    /// ControlPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class ControlPanel : UserControl {
        PlayerViewModel ViewModel => DataContext as PlayerViewModel;

        public ControlPanel() {
            InitializeComponent();
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e) {
            TimelineSlider.ReachRangeEnd += OnReachRangeEnd;
        }
        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e) {
            TimelineSlider.ReachRangeEnd -= OnReachRangeEnd;
        }

        //private void SetTrim(object obj) {
        //    var pos = Player.SeekPosition;
        //    switch (obj as string) {
        //        case "Start":
        //            TimelineSlider.RangeLimit.Start = Convert.ToUInt64(pos);
        //            PlayList.Current.Value.TrimStart = TimelineSlider.RangeLimit.Start;
        //            ViewModel.TrimStart.Value = TimelineSlider.RangeLimit.Start;
        //            break;
        //        case "End":
        //            TimelineSlider.RangeLimit.End = Convert.ToUInt64(pos);
        //            PlayList.Current.Value.TrimEnd = TimelineSlider.RangeLimit.End;
        //            ViewModel.TrimEnd.Value = TimelineSlider.RangeLimit.End;
        //            break;
        //        default:
        //            return;
        //    }
        //}

        //private void ResetTrim(object obj) {
        //    switch (obj as string) {
        //        case "Start":
        //            TimelineSlider.RangeLimit.Start = 0;
        //            PlayList.Current.Value.TrimStart = 0;
        //            ViewModel.TrimStart.Value = 0;
        //            break;
        //        case "End":
        //            TimelineSlider.RangeLimit.End = 0;
        //            PlayList.Current.Value.TrimEnd = 0;
        //            ViewModel.TrimEnd.Value = 0;
        //            break;
        //        default:
        //            return;
        //    }
        //}

        private void OnReachRangeEnd() {
            if (ViewModel.PlayList.HasNext.Value) {
                ViewModel.GoForwardCommand.Execute();
            } else {
                ViewModel.PauseCommand.Execute();
            }
        }

        private void OnSkipChapterButtonClicked(object sender, System.Windows.RoutedEventArgs e) {
            var entry = (ChapterInfo)((FrameworkElement)sender).Tag;
            entry.Skip = !entry.Skip;
            ViewModel.NotifyChapterUpdated();
        }
        private void OnDeleteChapterButtonClicked(object sender, System.Windows.RoutedEventArgs e) {
            var entry = (ChapterInfo)((FrameworkElement)sender).Tag;
            if (ViewModel.Chapters.Value.RemoveChapter(entry)) {
                ViewModel.NotifyChapterUpdated();
            }
        }
    }
}
