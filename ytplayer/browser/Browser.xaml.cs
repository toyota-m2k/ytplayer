using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Microsoft.Web.WebView2.Core;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
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

namespace ytplayer.browser {
    /**
     * ブラウザ画面用ビューモデル
     */
    class BrowserViewModel : ViewModelBase {
        public ReactiveProperty<Bookmarks> Bookmarks { get; } = new ReactiveProperty<Bookmarks>();
        //public ReactiveProperty<bool> ShowBookmark { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<string> Url { get; } = new ReactiveProperty<string>("");
        public ReactiveProperty<string> Title { get; } = new ReactiveProperty<string>("");

        public ReactiveProperty<bool> HasPrev { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> HasNext { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<int> LoadingCount { get; } = new ReactiveProperty<int>(0);
        public ReadOnlyReactiveProperty<bool> Loading { get; }

        public ReadOnlyReactiveProperty<bool> IsBookmarked { get; }
        //public ReactiveProperty<string> StatusLine { get; } = new ReactiveProperty<string>();

        public ReactiveCommand GoBackCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoForwardCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ReloadCommand { get; } = new ReactiveCommand();
        public ReactiveCommand StopCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClearURLCommand { get; } = new ReactiveCommand();
        public ReactiveCommand BookmarkCommand { get; } = new ReactiveCommand();
        //public ReactiveCommand ShowBookmarkCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CopyToClipboardCommand { get; } = new ReactiveCommand();
        public ReactiveCommand<string> NavigateCommand { get; } = new ReactiveCommand<string>();
        public ReactiveCommand<string> RegisterCommand { get; } = new ReactiveCommand<string>();

        public BrowserViewModel() {
            Bookmarks.Value = browser.Bookmarks.CreateInstance();
            ClearURLCommand.Subscribe(() => {
                Url.Value = "";
            });
            IsBookmarked = Url.Select((url) => {
                return IsBookmarkedUrl(url);
            }).ToReadOnlyReactiveProperty();
            Loading = LoadingCount.Select((count) => { return count > 0; }).ToReadOnlyReactiveProperty();

            BookmarkCommand.Subscribe(() => {
                var url = Url.Value;
                if (string.IsNullOrEmpty(url)) return;

                if (!IsBookmarkedUrl(url)) {
                    Bookmarks.Value.AddBookmark(Title.Value, url);
                }
                else {
                    Bookmarks.Value.RemoveBookmark(url);
                    Url.ForceNotify();
                }
            });
        }

        private bool IsBookmarkedUrl(string url) {
            return Bookmarks.Value?.FindBookmark(url) != null;
        }


        public override void Dispose() {
            Bookmarks.Value?.Dispose(); // Disposeでブックマークリストを保存する。
            base.Dispose();
        }
    }
    /// <summary>
    /// Browser.xaml の相互作用ロジック
    /// </summary>
    public partial class Browser : Window {
        #region 公開API・・・ブラウザを開く/閉じる

        private static Browser theBrowser = null;
        public static void ShowBrowser(Action<string> registerUri) {
            if (theBrowser == null) {
                theBrowser = new Browser();
                theBrowser.ViewModel.RegisterCommand.Subscribe(registerUri);
                theBrowser.Show();
            }
        }
        public static void CloseBrowser() {
            if (theBrowser != null) {
                theBrowser.Close();
                theBrowser = null;
            }
        }

        #endregion

        #region プロパティ：ビューモデル

        BrowserViewModel ViewModel {
            get => DataContext as BrowserViewModel;
            set => DataContext = value;
        }

        #endregion

        #region コンストラクション / ライフサイクル

        public Browser() {
            ViewModel = new BrowserViewModel();
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            Settings.Instance.BrowserPlacement.ApplyPlacementTo(this);
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ViewModel.NavigateCommand.Subscribe(Navigate);
            ViewModel.GoBackCommand.Subscribe(webView.GoBack);
            ViewModel.GoForwardCommand.Subscribe(webView.GoForward);
            ViewModel.ReloadCommand.Subscribe(webView.Reload);
            ViewModel.StopCommand.Subscribe(webView.Stop);
            ViewModel.CopyToClipboardCommand.Subscribe(() => RequestDownload(ViewModel.Url.Value));
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            Settings.Instance.BrowserPlacement.GetPlacementFrom(this);
        }

        protected override void OnClosed(EventArgs e) {
            theBrowser = null;
            ViewModel.Dispose();
            base.OnClosed(e);
        }

        #endregion

        #region Navigation Helper

        private static Uri FixUpUrl(string url) {
            if (string.IsNullOrEmpty(url)) {
                return null;
            }
            try {
                return new Uri(url);
            }
            catch (Exception) {
                if (url.StartsWith("//")) {
                    return FixUpUrl("http:" + url);
                }
                else if (!url.StartsWith("http")) {
                    return FixUpUrl("http://" + url);
                }
                else {
                    return null;
                }
            }
        }

        private void Navigate(string url) {
            var uri = FixUpUrl(url);
            if (uri == null) {
                return;
            }
            webView.Source = uri;
        }

        #endregion

        #region WebView2 Event Handlers

        private void WV2CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e) {
            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            //webView.ContentLoading += WebView_ContentLoading;
            //webView.CoreWebView2.DOMContentLoaded += WebView_DOMContentLoaded;
            webView.CoreWebView2.FrameNavigationCompleted += WebView_FrameNavigationCompleted;
            webView.CoreWebView2.FrameNavigationStarting += WebView_FrameNavigationStarting;
            webView.CoreWebView2.NewWindowRequested += WebView_NewWindowRequested;
            webView.CoreWebView2.PermissionRequested += WebView_PermissionRequired;
            webView.CoreWebView2.ProcessFailed += WebView_ProcessFailed;
            webView.CoreWebView2.HistoryChanged += WebView_HistoryChanged;
            webView.SourceChanged += WebView_SourceChanged;
            webView.CoreWebView2.DocumentTitleChanged += WebView_DocumentTitleChanged;

        }

        private void WebView_DocumentTitleChanged(object sender, object e) {
            ViewModel.Title.Value = webView.CoreWebView2.DocumentTitle;
        }

        private string callerName([CallerMemberName] string memberName = "") {
            return memberName;
        }

        private void UpdateHistory() {
            Debug.WriteLine(callerName());
            ViewModel.HasPrev.Value = webView.CanGoBack;
            ViewModel.HasNext.Value = webView.CanGoForward;
        }

        private void RequestDownload(string uri) {
            ViewModel.RegisterCommand.Execute(uri);
        }

        //private class NavigationMap {
        //    Dictionary<ulong, Uri> map = new Dictionary<ulong, Uri>();
        //    public void Register(ulong id, Uri uri) {
        //        map[id] = uri;
        //    }
        //    public void Unregister(ulong id) {
        //        map.Remove(id);
        //    }
        //    public void Clear() {
        //        map.Clear();
        //    }
        //    public Uri GetUri(ulong id) {
        //        return map.GetValue(id);
        //    }
        //    public Uri this[ulong id] => GetUri(id);
        //}
        //private NavigationMap navMap = new NavigationMap();

        private void WebView_HistoryChanged(object sender, object e) {
            UpdateHistory();
        }

        private void WebView_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e) {
            Debug.WriteLine($"{callerName()} {e.Reason}");
        }

        private void WebView_PermissionRequired(object sender, CoreWebView2PermissionRequestedEventArgs e) {
            Debug.WriteLine(callerName());
            e.State = CoreWebView2PermissionState.Deny;
        }

        private void WebView_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e) {
            RequestDownload(e.Uri);
            e.Handled = true;
        }

        private void WebView_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e) {
            Debug.WriteLine($"{callerName()} {webView.Source}");
            ViewModel.Url.Value = webView.Source.ToString();
        }



        private void WebView_FrameNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e) {
            Debug.WriteLine($"{callerName()}: {e.Uri} ({e.NavigationId})");
            //var uri = new Uri(e.Uri);
            //navMap.Register(e.NavigationId, uri);
            //if (e.Uri == "about:blank" || e.Uri.StartsWith("javascript:")) {
            //    return;
            //}
            //UpdateHistory();
            ViewModel.LoadingCount.Value++;
        }

        private void WebView_FrameNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e) {
            Debug.WriteLine(callerName());
            //var uri = navMap[e.NavigationId];
            //if (uri != null) {
            //    navMap.Unregister(e.NavigationId);
            //    Debug.WriteLine($"{callerName()}:{uri}");
            //    UpdateHistory();
            //}
            ViewModel.LoadingCount.Value--;
        }

        //private void WebView_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e) {
        //    Debug.WriteLine(callerName());
        //    UpdateHistory();
        //}

        //private void WebView_ContentLoading(object sender, CoreWebView2ContentLoadingEventArgs e) {
        //    Debug.WriteLine(callerName());
        //}


        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e) {
            Debug.WriteLine($"{callerName()}: {e.Uri} ({e.NavigationId})");
            //var uri = new Uri(e.Uri);
            //navMap.Register(e.NavigationId, uri);
            //UpdateHistory();
            ViewModel.LoadingCount.Value++;
        }
        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e) {
            Debug.WriteLine(callerName());
            //UpdateHistory();
            ViewModel.LoadingCount.Value--;
        }

        #endregion WebView2 Event Handlers
    }
}
