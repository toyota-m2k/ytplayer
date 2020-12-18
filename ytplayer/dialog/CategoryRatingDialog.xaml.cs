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
        public CategoryRatingDialog() {
            InitializeComponent();
        }

        private void OnCategorySelected(object sender, SelectionChangedEventArgs e) {

        }
    }
}
