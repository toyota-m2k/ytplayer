using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using ytplayer.data;
using ytplayer.intelop;

namespace ytplayer.player {
    public class TimelineSlider : Slider {
        PlayerViewModel ViewModel => DataContext as PlayerViewModel;
        private DispatcherTimer mTimer;
        private bool mSliderSeekingFromPlayer;
        private string mCurrentItemId = null;

        public TimelineSlider() {
            mTimer = new DispatcherTimer();
            mTimer.Interval = TimeSpan.FromMilliseconds(50);
            mTimer.Tick += (s, e) => {
                var vm = ViewModel;
                if (null == vm) return;
                var pos = vm.PlayerPosition;
                mSliderSeekingFromPlayer = true;
                this.Value = pos;
                mSliderSeekingFromPlayer = false;
                CheckRangeAndSeek(pos, vm.DisabledRanges.Value);
            };
            Loaded += OnLoaded;
        }

        private void GoNext() {
            if (mCurrentItemId != null) {
                var current = mCurrentItemId;
                mCurrentItemId = null;
                ViewModel.ReachRangeEnd.OnNext(current);
            }
        }

        public void OnMediaEnd() {
            mTimer.Stop();
            GoNext();
        }

        private void CheckRangeAndSeek(ulong pos, List<PlayRange> ranges) {
            if (ranges == null) return;
            var hit = ranges.Where((c) => c.Contains(pos));
            if(hit.Count()>0) {
                var range = hit.First();
                if (range.End == 0) {
                    GoNext();
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

            ViewModel?.IsPlaying?.Subscribe((playing) => {
                if (playing) {
                    mTimer.Start();
                } else {
                    mTimer.Stop();
                }
            });
            ViewModel?.Duration?.Subscribe((duration) => {
                this.Maximum = (double)duration;
            });
            ViewModel?.PlayList?.Current?.Subscribe((item) => {
                mCurrentItemId = item?.KEY;
                Value = 0;
            });
        }

        private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mSliderSeekingFromPlayer) {
                ViewModel?.Apply((vm) => {
                    vm.PlayerPosition = (ulong)this.Value;
                });
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            Unloaded -= OnUnloaded;
            ValueChanged -= OnValueChanged;
            mTimer.Stop();
        }

        private double mDragStartPos = -1;
        private bool mOrgPlaying = false;
        protected override void OnThumbDragStarted(DragStartedEventArgs e) {
            base.OnThumbDragStarted(e);
            mOrgPlaying = ViewModel.IsPlaying.Value;
            ViewModel.PauseCommand.Execute();
            if (ViewModel.ChapterEditing.Value) {
                mDragStartPos = (ulong)Value;
            }
        }

        protected override void OnThumbDragDelta(DragDeltaEventArgs e) {
            base.OnThumbDragDelta(e);

            if (ViewModel.ChapterEditing.Value) {
                var pos = Value;
                if (mDragStartPos >= 0) {
                    if (Math.Abs(pos - mDragStartPos) > 1000) {
                        var range = new PlayRange((ulong)mDragStartPos, (ulong)pos);
                        ViewModel.DraggingRange.Value = range;
                    }
                }
            }
        }

        protected override void OnThumbDragCompleted(DragCompletedEventArgs e) {
            base.OnThumbDragCompleted(e);
            var pos = Value;
            if (ViewModel.ChapterEditing.Value) {
                if (KeyState.IsKeyDown(KeyState.VK_CONTROL)) {
                    if (mDragStartPos >= 0) {
                        if (Math.Abs(pos - mDragStartPos) > 1000) {
                            var range = new PlayRange((ulong)mDragStartPos, (ulong)pos);
                            ViewModel.NotifyRange.Execute(range);
                        }
                        else {
                            ViewModel.NotifyPosition.Execute((ulong)pos);
                        }
                    }
                }
                ViewModel.DraggingRange.Value = null;
            }
            if (mOrgPlaying) {
                ViewModel.PlayCommand.Execute();
            }
        }
    }
}
