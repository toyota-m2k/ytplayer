using io.github.toyota32k.toolkit.utils;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.player {
    public class TimelineSlider : Slider {
        PlayerViewModel ViewModel => DataContext as PlayerViewModel;

        //private WeakReference<IPlayer> mPlayer;
        //private IPlayer Player {
        //    get => mPlayer?.GetValue();
        //    set => mPlayer = new WeakReference<IPlayer>(value);
        //}
        private DispatcherTimer mTimer;
        //private IDisposable mPlayingSubscriber;
        //private IDisposable mDurationSubscriber;
        //private DisposablePool mDisposablePool = new DisposablePool();
        private bool mSliderSeekingFromPlayer;

        //public ReactiveProperty<double> Position { get; } = new ReactiveProperty<double>();
        //public PlayRange RangeLimit { get; set; } = new PlayRange(0,0);

        public event Action ReachRangeEnd;

        public TimelineSlider() {
            mTimer = new DispatcherTimer();
            mTimer.Interval = TimeSpan.FromMilliseconds(50);
            mTimer.Tick += (s, e) => {
                var pos = ViewModel.PlayerPosition;
                mSliderSeekingFromPlayer = true;
                this.Value = pos;
                mSliderSeekingFromPlayer = false;
                CheckRangeAndSeek(pos, ViewModel.DisabledRanges.Value);
            };
            Loaded += OnLoaded;
        }

        private void CheckRangeAndSeek(ulong pos, List<PlayRange> ranges) {
            if (ranges == null) return;
            var hit = ranges.Where((c) => c.Contains(pos));
            if(hit.Count()>0) {
                var range = hit.First();
                if (range.End == 0) {
                    ReachRangeEnd?.Invoke();
                } else {
                    ViewModel.PlayerPosition = range.End;
                    mSliderSeekingFromPlayer = true;
                    this.Value = pos;
                    mSliderSeekingFromPlayer = false;
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Minimum = 0;
            Loaded -= OnLoaded;
            ValueChanged += OnValueChanged;
            Unloaded += OnUnloaded;

            ViewModel.IsPlaying.Subscribe((playing) => {
                if (playing) {
                    mTimer.Start();
                } else {
                    mTimer.Stop();
                }
            });
            ViewModel.Duration.Subscribe((duration) => {
                this.Maximum = (double)duration;
            });
            //ViewModel.DisabledRanges.Subscribe((ranges) => {
            //    CheckRangeAndSeek(ViewModel.PlayerPosition, ranges);
            //});
        }

        private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            ViewModel.Position.Value = (ulong)this.Value;
            if (!mSliderSeekingFromPlayer) {
                ViewModel.PlayerPosition = (ulong)this.Value;
            }
            //Debug.WriteLine(e.ToString());
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            Unloaded -= OnUnloaded;
            mTimer.Stop();
            ReachRangeEnd = null;
        }


        private bool mOrgPlaying = false;
        protected override void OnThumbDragStarted(DragStartedEventArgs e) {
            base.OnThumbDragStarted(e);
            mOrgPlaying = ViewModel.IsPlaying.Value;
            ViewModel.PauseCommand.Execute();
        }

        protected override void OnThumbDragDelta(DragDeltaEventArgs e) {
            base.OnThumbDragDelta(e);
        }

        protected override void OnThumbDragCompleted(DragCompletedEventArgs e) {
            base.OnThumbDragCompleted(e);
            if (mOrgPlaying) {
                ViewModel.PlayCommand.Execute();
            }
        }
    }
}
