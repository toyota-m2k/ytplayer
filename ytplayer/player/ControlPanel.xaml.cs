using System.Windows;
using System.Windows.Controls;
using ytplayer.data;

namespace ytplayer.player {
    /// <summary>
    /// ControlPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class ControlPanel : UserControl {
        PlayerViewModel ViewModel => DataContext as PlayerViewModel;

        public ControlPanel() {
            InitializeComponent();
        }

        private void OnSkipChapterButtonClicked(object sender, RoutedEventArgs e) {
            var entry = (ChapterInfo)((FrameworkElement)sender).Tag;
            entry.Skip = !entry.Skip;
            ViewModel.NotifyChapterUpdated();
        }
        private void OnDeleteChapterButtonClicked(object sender, RoutedEventArgs e) {
            var entry = (ChapterInfo)((FrameworkElement)sender).Tag;
            if (ViewModel.Chapters.Value.RemoveChapter(entry)) {
                ViewModel.NotifyChapterUpdated();
            }
        }

        private void OnChapterSelected(object sender, SelectionChangedEventArgs e) {
            var listView = sender as ListView;
            if(listView!=null) {
                var item = listView.SelectedItem as ChapterInfo;
                if(item!=null) {
                    ViewModel.Position.Value = item.Position;
                }
            }
        }

    }
}
