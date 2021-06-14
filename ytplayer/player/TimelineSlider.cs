﻿using io.github.toyota32k.toolkit.utils;
using Reactive.Bindings;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using ytplayer.common;

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
        private DisposablePool mDisposablePool = new DisposablePool();
        private bool mSliderSeekingFromPlayer;

        public ReactiveProperty<double> Position { get; } = new ReactiveProperty<double>();
        public PlayRange RangeLimit { get; set; } = new PlayRange(0,0);

        public event Action ReachRangeEnd;

        public TimelineSlider() {
            mTimer = new DispatcherTimer();
            mTimer.Interval = TimeSpan.FromMilliseconds(50);
            mTimer.Tick += (s, e) => {
                var pos = ViewModel.PlayerPosition;
                mSliderSeekingFromPlayer = true;
                this.Value = pos;
                mSliderSeekingFromPlayer = false;
                if(RangeLimit.End>0 && pos >= RangeLimit.End) {
                    ReachRangeEnd?.Invoke();
                }
            };
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Minimum = 0;
            Loaded -= OnLoaded;
            ValueChanged += OnValueChanged;
            Unloaded += OnUnloaded;

            mDisposablePool.Add(ViewModel.IsPlaying.Subscribe((playing) => {
                if (playing) {
                    mTimer.Start();
                } else {
                    mTimer.Stop();
                }
            }));
            mDisposablePool.Add(ViewModel.Duration.Subscribe((duration) => {
                this.Maximum = duration;
            }));
        }

        private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            Position.Value = this.Value;
            if (!mSliderSeekingFromPlayer) {
                ViewModel.PlayerPosition = (ulong)this.Value;
            }
            //Debug.WriteLine(e.ToString());
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            Unloaded -= OnUnloaded;
            mTimer.Stop();
            ReachRangeEnd = null;
            mDisposablePool.Dispose();
            //mPlayingSubscriber?.Dispose();
            //mDurationSubscriber?.Dispose();
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
