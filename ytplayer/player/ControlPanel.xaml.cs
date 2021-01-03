using common;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using ytplayer.data;

namespace ytplayer.player {

    public class ControlPanelViewModel : MicViewModelBase {
        public PlayList PlayList { get; } = new PlayList();

        public ReadOnlyReactiveProperty<string> DurationText { get; }
        public ReadOnlyReactiveProperty<string> PositionText { get; }
        public ReadOnlyReactiveProperty<bool> IsPlaying { get; }
        public ReadOnlyReactiveProperty<bool> IsReady { get; }
#pragma warning disable IDE0052 // 読み取られていないプライベート メンバーを削除
        private IDisposable EndEventRegester { get; }
        private IDisposable DurationRegester { get; }
#pragma warning restore IDE0052 // 読み取られていないプライベート メンバーを削除
        public ObservableCollection<Category> Categories => new ObservableCollection<Category>(Settings.Instance.Categories.SelectList);
        public ReactiveProperty<bool> FitMode { get; } = new ReactiveProperty<bool>();

        [Disposal(disposable:false)]
        public IReactiveProperty<double> Speed { get; }
        [Disposal(disposable: false)]
        public IReactiveProperty<double> Volume { get; }

        public ReactiveCommand PlayCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PauseCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoBackCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoForwardCommand { get; } = new ReactiveCommand();
        public ReactiveCommand TrashCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetSpeedCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetVolumeCommand { get; } = new ReactiveCommand();

        // Trimming
        public ReactiveProperty<double> TrimStart { get; } = new ReactiveProperty<double>();
        public ReactiveProperty<double> TrimEnd { get; } = new ReactiveProperty<double>();
        public ReadOnlyReactiveProperty<string> TrimStartText { get; }
        public ReadOnlyReactiveProperty<string> TrimEndText { get; }
        public ReactiveCommand SetTrimCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetTrimCommand { get; } = new ReactiveCommand();

        private string FormatDuration(double duration) {
            var t = TimeSpan.FromMilliseconds(duration);
            return string.Format("{0}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
        }

        //dummy for initialize
        public ControlPanelViewModel() {
            var dummyString = new ReactiveProperty<string>();
            var dummyBool = new ReactiveProperty<bool>();
            var dummyDouble = new ReactiveProperty<double>();
            DurationText = dummyString.ToReadOnlyReactiveProperty();
            PositionText = dummyString.ToReadOnlyReactiveProperty();
            IsPlaying = dummyBool.ToReadOnlyReactiveProperty();
            IsReady = dummyBool.ToReadOnlyReactiveProperty();
            Speed = dummyDouble;
            Volume = dummyDouble;
            TrimStart = dummyDouble;
            TrimEnd = dummyDouble;
            TrimStartText = dummyString.ToReadOnlyReactiveProperty();
            TrimEndText = dummyString.ToReadOnlyReactiveProperty();
        }

        public ControlPanelViewModel(IPlayer player, TimelineSlider slider) {
            DurationText = player.ViewModel.Duration.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            PositionText = slider.Position.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            IsPlaying = player.ViewModel.IsPlaying.ToReadOnlyReactiveProperty();
            IsReady = player.ViewModel.IsReady.ToReadOnlyReactiveProperty();
            EndEventRegester = player.ViewModel.Ended.Subscribe((v) => GoForwardCommand.Execute());
            DurationRegester = player.ViewModel.Duration.Subscribe((v)=> {
                if (v>0 && null != PlayList.Current.Value) {
                    PlayList.Current.Value.DurationInSec = (ulong)Math.Round(v/1000);
                }
            });
            Speed = player.ViewModel.Speed;
            Volume = player.ViewModel.Volume;
            Volume.Subscribe((v) => {
                //Logger.debug($"Volume = {v}");
                PlayList.Current.Value?.Apply((o) => o.Volume = v);
            });
            ResetSpeedCommand.Subscribe(() => Speed.Value = 0.5);
            ResetVolumeCommand.Subscribe(() => Volume.Value = 0.5);

            TrimStartText = TrimStart.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            TrimEndText = TrimEnd.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
        }
    }

    /// <summary>
    /// ControlPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class ControlPanel : UserControl {
        private ControlPanelViewModel ViewModel {
            get => DataContext as ControlPanelViewModel;
            set {
                ViewModel?.Dispose();
                DataContext = value;
            }
        }

        public IPlayList PlayList => ViewModel.PlayList;

        private WeakReference<IPlayer> mPlayer;
        private IPlayer Player => mPlayer?.GetValue();

        public ControlPanel() {
            ViewModel = new ControlPanelViewModel();    // エラー回避のためにダミーをセットしておく
            InitializeComponent();
        }

        public void Initialize(IPlayer player) {
            mPlayer = new WeakReference<IPlayer>(player);
            TimelineSlider.Initialize(player);
            TimelineSlider.ReachRangeEnd += OnReachRangeEnd;
            ViewModel = new ControlPanelViewModel(player, TimelineSlider);
            ViewModel.PlayCommand.Subscribe(Play);
            ViewModel.PauseCommand.Subscribe(Pause);
            ViewModel.GoForwardCommand.Subscribe(Next);
            ViewModel.GoBackCommand.Subscribe(Prev);
            ViewModel.TrashCommand.Subscribe(Trash);
            ViewModel.PlayList.Current.Subscribe(OnCurrentChanged);
            ViewModel.PlayList.ListItemAdded.Subscribe((v) => {
                if(!ViewModel.IsPlaying.Value) {
                    ViewModel.PlayList.CurrentIndex.Value = v;
                }
            });
            ViewModel.FitMode.Value = player.Stretch == Stretch.UniformToFill;
            ViewModel.FitMode.Subscribe(FitView);
            ViewModel.SetTrimCommand.Subscribe(SetTrim);
            ViewModel.ResetTrimCommand.Subscribe(ResetTrim);
        }

        private void SetTrim(object obj) {
            var pos = Player.SeekPosition;
            switch (obj as string) {
                case "Start":
                    TimelineSlider.RangeLimit.Start = Convert.ToUInt64(pos);
                    PlayList.Current.Value.TrimStart = TimelineSlider.RangeLimit.Start;
                    ViewModel.TrimStart.Value = TimelineSlider.RangeLimit.Start;
                    break;
                case "End":
                    TimelineSlider.RangeLimit.End = Convert.ToUInt64(pos);
                    PlayList.Current.Value.TrimEnd = TimelineSlider.RangeLimit.End;
                    ViewModel.TrimEnd.Value = TimelineSlider.RangeLimit.End;
                    break;
                default:
                    return;
            }
        }

        private void ResetTrim(object obj) {
            switch (obj as string) {
                case "Start":
                    TimelineSlider.RangeLimit.Start = 0;
                    PlayList.Current.Value.TrimStart = 0;
                    ViewModel.TrimStart.Value = 0;
                    break;
                case "End":
                    TimelineSlider.RangeLimit.End = 0;
                    PlayList.Current.Value.TrimEnd = 0;
                    ViewModel.TrimEnd.Value = 0;
                    break;
                default:
                    return;
            }
        }

        public void Terminate() {
            TimelineSlider.ReachRangeEnd -= OnReachRangeEnd;
            ViewModel = null;
           
        }

        private void OnReachRangeEnd() {
            if (ViewModel.PlayList.HasNext.Value) {
                ViewModel.GoForwardCommand.Execute();
            } else {
                ViewModel.PauseCommand.Execute();
            }
        }

        private void OnCurrentChanged(IPlayable item) {
            if (item != null) {
                ViewModel.Volume.Value = item.Volume;
                ViewModel.TrimStart.Value = item.TrimStart;
                ViewModel.TrimEnd.Value = item.TrimEnd;
                TimelineSlider.RangeLimit = new PlayRange(item.TrimStart, item.TrimEnd);
            }
            Player.SetSource(item?.Path, true);
        }

        public void Play() {
            Player?.Play();
        }
        public void Pause() {
            Player?.Pause();
        }

        public void Next() {
            ViewModel.PlayList.Next();
        }

        public void Prev() {
            ViewModel.PlayList.Prev();
        }
        public void Trash() {
            ViewModel.PlayList.Current.Value.Delete();
            Next();
        }
        public void FitView(bool mode) {
            Player?.Apply((player) => player.Stretch = mode ? Stretch.UniformToFill : Stretch.Uniform);
        }
    }
}
