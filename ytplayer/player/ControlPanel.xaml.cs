﻿using common;
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

        [Disposal(disposable:false)]
        public IReactiveProperty<double> Speed { get; }
        [Disposal(disposable: false)]
        public IReactiveProperty<double> Volume { get; }

        public ReactiveProperty<bool> FitMode { get; } = new ReactiveProperty<bool>();
        //public ReactiveProperty<bool> MaximumWindow { get; } = new ReactiveProperty<bool>();

        public ReactiveCommand PlayCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PauseCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoBackCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoForwardCommand { get; } = new ReactiveCommand();
        public ReactiveCommand TrashCommand { get; } = new ReactiveCommand();
        //public ReactiveCommand FitCommand { get; } = new ReactiveCommand();

        public ReactiveCommand ResetSpeedCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetVolumeCommand { get; } = new ReactiveCommand();



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
                PlayList.Current.Value?.Apply((o) => o.Volume = v);
            });
            ResetSpeedCommand.Subscribe(() => Speed.Value = 0.5);
            ResetVolumeCommand.Subscribe(() => Volume.Value = 0.5);
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
            //ViewModel.FitCommand.Subscribe(FitView);
            ViewModel.PlayList.Current.Subscribe(OnCurrentChanged);
            ViewModel.PlayList.ListItemAdded.Subscribe((v) => {
                if(!ViewModel.IsPlaying.Value) {
                    ViewModel.PlayList.CurrentIndex.Value = v;
                }
            });
            ViewModel.FitMode.Value = player.Stretch == Stretch.UniformToFill;
            ViewModel.FitMode.Subscribe(FitView);
            //ViewModel.MaximumWindow.Value = Window.GetWindow(this).WindowStyle == WindowStyle.None;
            //ViewModel.MaximumWindow.Subscribe(MaximizeWindow);
        }

        public void Terminate() {
            ViewModel.Dispose();
            ViewModel = null;
        }

        private void OnCurrentChanged(IPlayable item) {
            if (item != null) {
                ViewModel.Volume.Value = item.Volume;
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
        //public void MaximizeWindow(bool max) {
        //    var win = Window.GetWindow(this);
        //    if (max) {
        //        win.WindowStyle = WindowStyle.None;         // タイトルバーと境界線を表示しない
        //        win.WindowState = WindowState.Maximized;    // 最大化表示
        //    } else {
        //        win.WindowStyle = WindowStyle.SingleBorderWindow;
        //        win.WindowState = WindowState.Normal;
        //    }
        //}
        private void OnUnloaded(object sender, RoutedEventArgs e) {
        }
    }
}
