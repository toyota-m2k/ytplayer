using common;
using Reactive.Bindings;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace ytplayer.player {
    public class TimelineSlider : Slider {
        private WeakReference<IPlayer> mPlayer;
        private IPlayer Player {
            get => mPlayer?.GetValue();
            set => mPlayer = new WeakReference<IPlayer>(value);
        }
        private DispatcherTimer mTimer;
        private IDisposable mPlayingSubscriber;
        private IDisposable mDurationSubscriber;
        private bool mSliderSeekingFromPlayer;
        public ReactiveProperty<double> Position { get; } = new ReactiveProperty<double>();

        public PlayRange RangeLimit { get; set; } = new PlayRange(0,0);

        public event Action ReachRangeEnd;

        public TimelineSlider() {
            mPlayingSubscriber = null;
            mDurationSubscriber = null;
            mTimer = new DispatcherTimer();
            mTimer.Interval = TimeSpan.FromMilliseconds(50);
            mTimer.Tick += (s, e) => {
                var pos = Player?.SeekPosition ?? 0;
                mSliderSeekingFromPlayer = true;
                this.Value = pos;
                mSliderSeekingFromPlayer = false;
                if(RangeLimit.End>0 && pos >= RangeLimit.End) {
                    ReachRangeEnd?.Invoke();
                }
            };
            Loaded += OnLoaded;
        }

        public void Initialize(IPlayer player) {
            Player = player;
            mPlayingSubscriber = player.ViewModel.IsPlaying.Subscribe((playing) => {
                if (playing) {
                    mTimer.Start();
                } else {
                    mTimer.Stop();
                }
            });
            mDurationSubscriber = player.ViewModel.Duration.Subscribe((duration) => {
                this.Maximum = duration;
                if(RangeLimit.Start>0) {
                    Player.SeekPosition = RangeLimit.Start;
                }
            });
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Minimum = 0;
            Loaded -= OnLoaded;
            ValueChanged += OnValueChanged;
            Unloaded += OnUnloaded;
        }

        private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            Position.Value = this.Value;
            if (!mSliderSeekingFromPlayer) {
                Player.SeekPosition = this.Value;
            }
            //Debug.WriteLine(e.ToString());
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            Unloaded -= OnUnloaded;
            mTimer.Stop();
            ReachRangeEnd = null;
            mPlayingSubscriber?.Dispose();
            mDurationSubscriber?.Dispose();
        }


        private bool mOrgPlaying = false;
        protected override void OnThumbDragStarted(DragStartedEventArgs e) {
            base.OnThumbDragStarted(e);
            mOrgPlaying = Player.ViewModel.IsPlaying.Value;
            Player.Pause();
        }

        protected override void OnThumbDragDelta(DragDeltaEventArgs e) {
            base.OnThumbDragDelta(e);
        }

        protected override void OnThumbDragCompleted(DragCompletedEventArgs e) {
            base.OnThumbDragCompleted(e);
            if (mOrgPlaying) {
                Player.Play();
            }
        }
    }
}
