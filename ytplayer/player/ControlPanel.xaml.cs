using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        private DependencyObject FindParent<T>(DependencyObject obj) where T:DependencyObject {
            do {
                obj = VisualTreeHelper.GetParent(obj);
            } while (obj!=null && obj.GetType() != typeof(T));
            return (T)obj;
        }


        private T FindChild<T>(DependencyObject obj) where T : DependencyObject {
            DependencyObject child = null;
            int count = VisualTreeHelper.GetChildrenCount(obj);
            for(int i=0; i<count; i++) {
                child = VisualTreeHelper.GetChild(obj, i);
                if(child.GetType() == typeof(T)) {
                    return (T)child; 
                }
                var tc = FindChild<T>(child);
                if(tc!=null) {
                    return tc;
                }
            }
            return null;
        }

        private IEnumerable<DependencyObject> GetChildren(DependencyObject obj) {
            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++) {
                yield return VisualTreeHelper.GetChild(obj,i);
            }
        }

        private void OnLabelEditKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            var textBox = sender as TextBox;
            if(textBox!=null) {
                var entry = (ChapterInfo)textBox.Tag;
                var index = -1;
                switch(e.Key) {
                    case System.Windows.Input.Key.Return:
                    case System.Windows.Input.Key.Down:
                        index = ViewModel.EditingChapterList.Value.IndexOf(entry) + 1;
                        break;
                    case System.Windows.Input.Key.Up:
                        index = ViewModel.EditingChapterList.Value.IndexOf(entry) - 1;
                        break;
                    default:
                        break;
                }
                if(0<=index && index< ViewModel.EditingChapterList.Value.Count) {
                    entry = ViewModel.EditingChapterList.Value[index];
                    chapterListView.ScrollIntoView(entry);
                    var container = FindParent<VirtualizingStackPanel>(textBox);
                    if(container!=null) {
                        foreach(var c in GetChildren(container)) {
                            var tx = FindChild<TextBox>(c);
                            if(tx.Tag == entry) {
                                tx.Focus();
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
