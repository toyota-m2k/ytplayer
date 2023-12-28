using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
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
using ytplayer.export;

namespace ytplayer.player {
    public class ExportWindowViewModel : ViewModelBase {
        public ReadOnlyReactiveProperty<bool> IsBusy { get; }
        public ReactiveProperty<bool> Split { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> OnlyAudio { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<string> TargetFolder { get; } = new ReactiveProperty<string>("");
        public ReactiveProperty<string> FileName { get; } = new ReactiveProperty<string>("");
        public ReadOnlyReactiveProperty<bool> CanExecute { get; }
        public ReactiveProperty<ExportProcessor> ExecutingProcessor { get; } = new ReactiveProperty<ExportProcessor>();
        public bool CanSplit { get; }
        public bool HasVideo { get; }
        public Func<string, bool> StdOutProc { get; }
        Func<string, bool> StdErrProc { get; }

        public ReactiveCommand ExecuteCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CancelCommand { get; } = new ReactiveCommand();
        public ReactiveCommand RefPathCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CompletionCommand { get; } = new ReactiveCommand();

        private DLEntry Source { get; }
        private ChapterList ChapterList { get; }

        public ExportWindowViewModel(DLEntry entry, ChapterList list, Func<string, bool> stdOutProc, Func<string, bool> stdErrProc) {
            Source = entry;
            ChapterList = list;
            TargetFolder.Value = Settings.Instance.ExportPath;
            FileName.Value = ExportOption.SafeFileName(entry.Name);
            if (string.IsNullOrWhiteSpace(FileName.Value)) {
                System.IO.Path.GetFileNameWithoutExtension(entry.Path);
            }

            //OnlyAudio.Value = entry.Media.HasFlag(MediaFlag.VIDEO);
            var av = ExportProcessor.GetAvailableOperation(entry, list);
            if (av.HasFlag(ExportProcessor.Operation.SPRIT)) {
                CanSplit = true;
            }
            if (av.HasFlag(ExportProcessor.Operation.EXTRACT_AUDIO)) {
                HasVideo = true;
            }
            StdOutProc = stdOutProc;
            StdErrProc = stdErrProc;

            ExecuteCommand.Subscribe(Execute);
            IsBusy = ExecutingProcessor.Select(p => p != null).ToReadOnlyReactiveProperty();
            CanExecute = TargetFolder.CombineLatest(IsBusy, (f,b) => !b && !string.IsNullOrEmpty(f)).ToReadOnlyReactiveProperty();
            CancelCommand.Subscribe(() => {
                ExecutingProcessor.Value?.Cancel();
            });
        }

        void Execute() {
            if(!PathUtil.isDirectory(TargetFolder.Value)) {
                MessageBox.Show("Target folder is invalid.");
                return;
            }
            Settings.Instance.ExportPath = TargetFolder.Value;
            Settings.Instance.Serialize();

            var proc = new ExportProcessor(Source, ChapterList, TargetFolder.Value, StdOutProc, StdErrProc);
            ExportProcessor.Operation op = ExportProcessor.Operation.TRIMMING;
            if (Split.Value) {
                op |= ExportProcessor.Operation.SPRIT;
            }
            if(OnlyAudio.Value) {
                op |= ExportProcessor.Operation.EXTRACT_AUDIO;
            }
            ExecutingProcessor.Value = proc;
            Task.Run(async () => {
                var result = await proc.Export(op, FileName.Value);
                ExecutingProcessor.Value = null;
                if (result) {
                    CompletionCommand.Execute();
                }
            });
        }

    }

    /// <summary>
    /// ExportWindow.xaml の相互作用ロジック
    /// </summary>
    /// 
    public partial class ExportWindow : Window {
        private ExportWindowViewModel ViewModel {
            get => DataContext as ExportWindowViewModel;
            set {
                ViewModel?.Dispose();
                DataContext = value;
            }
        }

        public ExportWindow(DLEntry entry, ChapterList list) {
            ViewModel = new ExportWindowViewModel(entry, list, HandleStdOut, HandleStdErr);
            ViewModel.CancelCommand.Subscribe(() => Dispatcher.Invoke(Close));
            ViewModel.RefPathCommand.Subscribe(SelectFolder);
            ViewModel.CompletionCommand.Subscribe(OnCompleted);
            InitializeComponent();
        }

        private void AddTextToRichEdit(string text, Brush fg) {
            Dispatcher.Invoke(() => {
                var p = OutputView.Document.Blocks.FirstBlock as Paragraph;
                var run = new Run(text);
                if (fg != null) {
                    run.Foreground = fg;
                }
                p.Inlines.Add(run);
                p.Inlines.Add(new LineBreak());
                OutputView.ScrollToEnd();
            });
        }

        private bool HandleStdOut(string s) {
            AddTextToRichEdit(s, Brushes.Black);
            return true;
        }
        private bool HandleStdErr(string s) {
            var sl = s.ToLower();
            if (sl.Contains("error") || sl.Contains("failed") || sl.Contains("abort") || sl.Contains("invalid") || sl.Contains("cannot") || sl.Contains("no such") || sl.Contains("could not")) {
                AddTextToRichEdit(s, Brushes.Red);
            }
            else {
                AddTextToRichEdit(s, Brushes.Black);
            }
            return true;
        }
        private void SelectFolder() {
            var r = FolderDialogBuilder.Create()
                .title("Export To")
                .initialDirectory(ViewModel.TargetFolder.Value)
                .GetFilePath(Owner);
            if (r != null) {
                ViewModel.TargetFolder.Value = r;
            }
        }
        private void OnCompleted() {
            Dispatcher.Invoke(async () => {
                AddTextToRichEdit("### Completed ###", Brushes.Blue);
                await Task.Delay(1500);
                Close();
            });
        }
        protected override void OnClosing(CancelEventArgs e) {
            if(ViewModel.IsBusy.Value) {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }
        override protected void OnClosed(EventArgs e) {
            base.OnClosed(e);
            ViewModel.Dispose();
            ViewModel = null;
        }
    }
}
