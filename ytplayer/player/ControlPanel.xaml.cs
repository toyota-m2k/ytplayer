﻿using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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

        private common.DisposablePool mDisposablePool = new common.DisposablePool();

        IDisposable mChapterEditingObserver = null;
        private void OnLoaded(object sender, RoutedEventArgs e) {
            mChapterEditingObserver = ViewModel.ChapterEditing.Subscribe(OnChapterEditing);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            mChapterEditingObserver?.Dispose();
            mChapterEditingObserver = null;
            OnChapterEditing(false);
        }


        private void OnChapterEditing(bool editing) {
            ViewModel?.KeyCommands?.Pause(editing);
            if(editing) {
                mDisposablePool.Add(ViewModel.SyncChapterCommand.Subscribe(SyncChapter));
                mDisposablePool.Add(ViewModel.SelectChapterEvent.Subscribe(OnChapterChanged));
            } else {
                mDisposablePool.Reset();
            }
        }

        private void OnChapterChanged(int index) {
            if (index >= 0) {
                chapterListView.SelectedIndex = index;
                chapterListView.ScrollIntoView(chapterListView.Items[index]);
            }
        }

        private void SyncChapter() {
            var pos = ViewModel.PlayerPosition;
            if (ViewModel.Chapters.Value.GetNeighborChapterIndex(pos, out var prev, out var next)) {
                prev++;
            } 
            if(prev>=0) {
                chapterListView.SelectedIndex = prev;
                chapterListView.ScrollIntoView(chapterListView.Items[prev]);
            }
        }

        //private void UpdateChapterLength() {
        //    if (!ViewModel.ChapterEditing.Value) return;

        //    var duration = ViewModel?.Duration?.Value;
        //    if (duration == null) return;

        //    UpdateLengthField(duration.Value);
        //}

        //private void UpdateLengthField(ulong duration) {
        //    if (duration == 0) return;
        //    var editingList = ViewModel?.EditingChapterList?.Value;
        //    if (editingList == null) return;

        //    if (editingList.Count == 0) return;
        //    for (var i = 0; i < editingList.Count - 1; i++) {
        //        var c0 = editingList[i];
        //        var c1 = editingList[i + 1];
        //        c0.Length = c1.Position - c0.Position;
        //        c0.Index = i+1;
        //    }
        //    var c = editingList[editingList.Count - 1];
        //    c.Length = duration - c.Position;
        //    c.Index = editingList.Count;
        //}

        //private void OnChapterListChanged(object sender, NotifyCollectionChangedEventArgs e) {
        //    UpdateChapterLength();
        //}

        private void OnSkipChapterButtonClicked(object sender, RoutedEventArgs e) {
            var entry = (ChapterInfo)((FrameworkElement)sender).Tag;
            ViewModel.ChapterEditor.Value?.SetSkip(entry, !entry.Skip);
        }
        private void OnDeleteChapterButtonClicked(object sender, RoutedEventArgs e) {
            var entry = (ChapterInfo)((FrameworkElement)sender).Tag;
            ViewModel.ChapterEditor.Value?.RemoveChapter(entry);
        }

        private void OnChapterSelected(object sender, SelectionChangedEventArgs e) {
            var listView = sender as ListView;
            if(listView!=null) {
                var item = listView.SelectedItem as ChapterInfo;
                if(item!=null) {
                    // 現在のチャプターを取得
                    var current = ViewModel.Chapters.Value.GetChapterAtPosition(ViewModel.Position.Value);
                    if(current?.Position!=item.Position) {
                        // 現在このチャプターを再生中でなければ、そのチャプターの先頭にシーク
                        ViewModel.Position.Value = item.Position;
                    }
                }
            }
        }

        /**
         * Chapter Label 設定用 TextBox で Enterキー, Up/Down キーの押下で、
         * 次（前）のアイテムのLabel編集TextBoxにフォーカスを移動するための（大掛かりな）仕掛け
         */

        /**
         * 指定タイプの先祖Viewを取得
         */
        private DependencyObject FindParent<T>(DependencyObject obj) where T:DependencyObject {
            do {
                obj = VisualTreeHelper.GetParent(obj);
            } while (obj!=null && obj.GetType() != typeof(T));
            return (T)obj;
        }

        /**
         * 指定タイプの子孫Viewを取得
         */
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

        /**
         * 子ビューを列挙
         */
        private IEnumerable<DependencyObject> GetChildren(DependencyObject obj) {
            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++) {
                yield return VisualTreeHelper.GetChild(obj,i);
            }
        }

        /**
         * Label入力TextBoxのキー押下 preview event リスナー
         */
        private void OnLabelEditKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            var textBox = sender as TextBox;
            if(textBox!=null) {
                // 次、前のターゲット(index)を取得
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
                // ターゲットのTextBoxを探してフォーカスをセット
                if(0<=index && index< ViewModel.EditingChapterList.Value.Count) {
                    entry = ViewModel.EditingChapterList.Value[index];
                    // VirtualizingPanelにビューがなければ生成させるため、まず表示する
                    chapterListView.ScrollIntoView(entry);
                    // 現在フォーカスを持っているtextBox の親コンテナ、VirtualizingStackPanel を取得
                    var container = FindParent<VirtualizingStackPanel>(textBox);
                    if(container!=null) {
                        // 親コンテナ内で、tag が entry と一致するものを探す。
                        var tx = GetChildren(container).Select(c => FindChild<TextBox>(c)).Where(c=> c!=null && c.Tag == entry).FirstOrDefault();
                        tx?.Focus();
                    }
                }
            }
        }
   }
}
