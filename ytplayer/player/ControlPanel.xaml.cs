using common;
using Reactive.Bindings;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ytplayer.player {

    public class ControlPanelViewModel : MicViewModelBase {
        public PlayList PlayList { get; } = new PlayList();

        public ReadOnlyReactiveProperty<string> DurationText { get; }
        public ReadOnlyReactiveProperty<string> PositionText { get; }
        public ReadOnlyReactiveProperty<bool> IsPlaying { get; }
        public ReadOnlyReactiveProperty<bool> IsReady { get; }
        private IDisposable EndEventRegester { get; }

        public ReactiveCommand PlayCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PauseCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoBackCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoForwardCommand { get; } = new ReactiveCommand();
        public ReactiveCommand TrashCommand { get; } = new ReactiveCommand();
        public ReactiveCommand FitCommand { get; } = new ReactiveCommand();

        private string FormatDuration(double duration) {
            var t = TimeSpan.FromMilliseconds(duration);
            return string.Format("{0}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
        }

        //dummy for initialize
        public ControlPanelViewModel() {
            var dummyString = new ReactiveProperty<string>();
            var dummyBool = new ReactiveProperty<bool>();
            DurationText = dummyString.ToReadOnlyReactiveProperty();
            PositionText = dummyString.ToReadOnlyReactiveProperty();
            IsPlaying = dummyBool.ToReadOnlyReactiveProperty();
            IsReady = dummyBool.ToReadOnlyReactiveProperty();
        }

        public ControlPanelViewModel(IPlayer player, TimelineSlider slider) {
            DurationText = player.ViewModel.Duration.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            PositionText = slider.Position.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            IsPlaying = player.ViewModel.IsPlaying.ToReadOnlyReactiveProperty();
            IsReady = player.ViewModel.IsReady.ToReadOnlyReactiveProperty();
            EndEventRegester = player.ViewModel.Ended.Subscribe((v) => GoForwardCommand.Execute());
        }
    }

    /// <summary>
    /// ControlPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class ControlPanel : UserControl {
        private ControlPanelViewModel ViewModel {
            get => DataContext as ControlPanelViewModel;
            set {
                if(DataContext is ControlPanelViewModel) {
                    ((ControlPanelViewModel)DataContext).Dispose();
                }
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
            ViewModel = new ControlPanelViewModel(player, TimelineSlider);
            ViewModel.PlayCommand.Subscribe(Play);
            ViewModel.PauseCommand.Subscribe(Pause);
            ViewModel.GoForwardCommand.Subscribe(Next);
            ViewModel.GoBackCommand.Subscribe(Prev);
            ViewModel.TrashCommand.Subscribe(Trash);
            ViewModel.FitCommand.Subscribe(FitView);
            ViewModel.PlayList.Current.Subscribe(OnCurrentChanged);
            ViewModel.PlayList.ListItemAdded.Subscribe((v) => {
                if(!ViewModel.IsPlaying.Value) {
                    ViewModel.PlayList.CurrentIndex.Value = v;
                }
            });

        }
        private void OnCurrentChanged(IPlayable item) {
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
        public void FitView() {
            Player?.Apply((player) => player.Stretch = (player.Stretch == Stretch.UniformToFill) ? Stretch.Uniform : Stretch.UniformToFill);
        }
        private void OnUnloaded(object sender, RoutedEventArgs e) {
            ViewModel.Dispose();
            ViewModel = null;
        }
    }
}
