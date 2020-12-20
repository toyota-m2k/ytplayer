using common;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.dialog {
    public class CategoryRatingDialogViewModel : MicViewModelBase {
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
            get => (CategoryRatingDialogViewModel)DataContext;
            set => DataContext = value;
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

        private void OnClose(object sender, EventArgs e) {
            StartPosition = new Point(Left, Top);
            ViewModel?.Dispose();
            ViewModel = null;
            EditorWindowClosed?.Invoke(this);
        }
    }
}
