using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ytplayer.dialog {
    public class TextSearchViewModel : ViewModelBase {
        public ReactiveCommand CommandSearchNext { get; } = new ReactiveCommand();
        public ReactiveCommand CommandSearchPrev { get; } = new ReactiveCommand();
        public ObservableCollection<string> SearchHistory => Settings.Instance.SearchHistories.History;
        public ReactivePropertySlim<string> SearchText { get; } = new ReactivePropertySlim<string>();

    }
    /// <summary>
    /// TextSearchDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class TextSearchDialog : Window {
        public event Action<string, bool> SearchText;
        public event Action<TextSearchDialog> WindowClosed;
        public TextSearchViewModel ViewModel {
            get => DataContext as TextSearchViewModel;
            set => DataContext = value;
        }
        public static Point? StartPosition = null;


        public TextSearchDialog() {
            ViewModel = new TextSearchViewModel();
            ViewModel.CommandSearchNext.Subscribe(() => {
                Search(true);
            });
            ViewModel.CommandSearchPrev.Subscribe(() => {
                Search(false);
            });
            InitializeComponent();
        }

        private void Search(bool next) {
            var text = ViewModel.SearchText.Value;
            if (string.IsNullOrEmpty(text)) { return; }
            SearchText?.Invoke(text, next);
            Settings.Instance.SearchHistories.Put(text);
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            StartPosition = new Point(Left, Top);
            WindowClosed?.Invoke(this);
            WindowClosed = null;
            SearchText = null;
            ViewModel?.Dispose();
            ViewModel = null;
        }

        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == Key.Return) {
                ViewModel.CommandSearchNext.Execute();
            }
        }
    }
}
