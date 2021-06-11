using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ytplayer.data;

namespace ytplayer.dialog {
    public class CategoryRatingDialogViewModel : ViewModelBase {
        public ObservableCollection<Category> Categories => new ObservableCollection<Category>(Settings.Instance.Categories.SelectList);

        public ReactiveCommand CommandDreadful  { get; } = new ReactiveCommand();
        public ReactiveCommand CommandBad       { get; } = new ReactiveCommand();
        public ReactiveCommand CommandNormal    { get; } = new ReactiveCommand();
        public ReactiveCommand CommandGood      { get; } = new ReactiveCommand();
        public ReactiveCommand CommandExcellent { get; } = new ReactiveCommand();
    }

    /// <summary>
    /// CategoryRatingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class CategoryRatingDialog : Window {
        private CategoryRatingDialogViewModel ViewModel {
            get => DataContext as CategoryRatingDialogViewModel;
            set {
                ViewModel?.Dispose();
                DataContext = value;
            }
        }

        public CategoryRatingDialog() {
            ViewModel = new CategoryRatingDialogViewModel();
            ViewModel.CommandExcellent.Subscribe(OnRatingChanged);
            ViewModel.CommandGood.Subscribe(OnRatingChanged);
            ViewModel.CommandNormal.Subscribe(OnRatingChanged);
            ViewModel.CommandBad.Subscribe(OnRatingChanged);
            ViewModel.CommandDreadful.Subscribe(OnRatingChanged);
            InitializeComponent();
        }

        private void OnRatingChanged(object obj) {

            if (Enum.TryParse((string)obj, out Rating rating)) {
                CategoryRatingSelected?.Invoke(rating, null);
            }
        }

        private void OnCategorySelected(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count == 1) {
                CategoryRatingSelected?.Invoke(null, e.AddedItems[0] as Category);
            }
            CategoryListBox.SelectedItem = null;
        }

        public event Action<CategoryRatingDialog> EditorWindowClosed;
        public event Action<Rating?, Category> CategoryRatingSelected;
        public static Point? StartPosition = null;

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            StartPosition = new Point(Left, Top);
            ViewModel = null;
            EditorWindowClosed?.Invoke(this);
        }
    }
}
