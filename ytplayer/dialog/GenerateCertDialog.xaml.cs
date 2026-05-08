using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Windows;
using ytplayer.common;

namespace ytplayer.dialog {
    public class GenerateCertViewModel : ViewModelBase<GenerateCertDialog> {
        public ReactivePropertySlim<string> SubjectCN { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> DnsNames { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> IpAddresses { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> PfxPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> Password { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<int> ValidityYears { get; } = new ReactivePropertySlim<int>(10);
        public ReactivePropertySlim<string> ErrorMessage { get; } = new ReactivePropertySlim<string>();

        public ReactiveCommand CommandPfxPath { get; } = new ReactiveCommand();
        public ReactiveCommand CommandRandomPassword { get; } = new ReactiveCommand();
        public ReactiveCommand GenerateCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CancelCommand { get; } = new ReactiveCommand();
        public ReactiveCommand<bool> Completed { get; } = new ReactiveCommand<bool>();

        public GenerateCertViewModel(GenerateCertDialog owner, string defaultPfxPath, string defaultPassword) : base(owner) {
            var hostName = Environment.MachineName;
            SubjectCN.Value = $"BooTube-{hostName}";

            var dns = new[] { hostName, "localhost" }
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            DnsNames.Value = string.Join(", ", dns);

            var ips = CertificateGenerator.GetLocalIPv4Addresses();
            IpAddresses.Value = string.Join(", ", ips.Select(ip => ip.ToString()));

            PfxPath.Value = !string.IsNullOrEmpty(defaultPfxPath)
                ? defaultPfxPath
                : Path.Combine(Settings.AppPath ?? Environment.CurrentDirectory, "bootube.pfx");

            Password.Value = !string.IsNullOrEmpty(defaultPassword)
                ? defaultPassword
                : GenerateRandomPassword(16);

            CommandPfxPath.Subscribe(() => SelectPfxPath());
            CommandRandomPassword.Subscribe(() => Password.Value = GenerateRandomPassword(16));
            GenerateCommand.Subscribe(() => Generate());
            CancelCommand.Subscribe(() => Completed.Execute(false));
        }

        private void SelectPfxPath() {
            var dlg = new Microsoft.Win32.SaveFileDialog {
                Title = "PFX file output",
                Filter = "PFX files (*.pfx)|*.pfx|All files (*.*)|*.*",
                DefaultExt = "pfx",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = string.IsNullOrEmpty(PfxPath.Value) ? "bootube.pfx" : Path.GetFileName(PfxPath.Value),
                InitialDirectory = PathUtil.getDirectoryName(PfxPath.Value) ?? "",
            };
            if (dlg.ShowDialog(Owner) == true) {
                PfxPath.Value = dlg.FileName;
            }
        }

        private static string GenerateRandomPassword(int length) {
            // 紛らわしい文字 (0/O, 1/l/I) を除外
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            using (var rng = RandomNumberGenerator.Create()) {
                var buf = new byte[length];
                rng.GetBytes(buf);
                return new string(buf.Select(b => chars[b % chars.Length]).ToArray());
            }
        }

        private void Generate() {
            ErrorMessage.Value = "";
            try {
                if (string.IsNullOrWhiteSpace(SubjectCN.Value)) {
                    ErrorMessage.Value = "Subject CN is required.";
                    return;
                }
                if (string.IsNullOrWhiteSpace(PfxPath.Value)) {
                    ErrorMessage.Value = "PFX file path is required.";
                    return;
                }
                if (ValidityYears.Value < 1 || ValidityYears.Value > 100) {
                    ErrorMessage.Value = "Validity years must be 1..100.";
                    return;
                }

                var dnsList = SplitList(DnsNames.Value);
                var ipList = new List<IPAddress>();
                foreach (var s in SplitList(IpAddresses.Value)) {
                    if (!IPAddress.TryParse(s, out var ip)) {
                        ErrorMessage.Value = $"Invalid IP address: {s}";
                        return;
                    }
                    ipList.Add(ip);
                }

                if (dnsList.Count == 0 && ipList.Count == 0) {
                    ErrorMessage.Value = "At least one DNS name or IP address is required.";
                    return;
                }

                CertificateGenerator.GenerateAndExport(
                    SubjectCN.Value,
                    dnsList,
                    ipList,
                    ValidityYears.Value,
                    PfxPath.Value,
                    Password.Value);

                var cerPath = Path.ChangeExtension(PfxPath.Value, ".cer");
                MessageBox.Show(Owner,
                    $"Generated:\n  {PfxPath.Value}\n  {cerPath}\n\n" +
                    $"Distribute the .cer file to clients and add it to their trusted root store.",
                    "Self-Signed Certificate", MessageBoxButton.OK, MessageBoxImage.Information);

                Completed.Execute(true);
            }
            catch (Exception ex) {
                LoggerEx.error(ex);
                ErrorMessage.Value = $"Failed: {ex.Message}";
            }
        }

        private static List<string> SplitList(string s) {
            if (string.IsNullOrWhiteSpace(s)) return new List<string>();
            return s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
        }
    }

    public partial class GenerateCertDialog : Window {
        public class DResult {
            public bool Ok { get; }
            public string PfxPath { get; }
            public string Password { get; }
            public DResult(bool ok, string pfxPath, string password) {
                Ok = ok;
                PfxPath = pfxPath;
                Password = password;
            }
        }
        public DResult Result { get; private set; }

        public GenerateCertDialog(string defaultPfxPath, string defaultPassword) {
            viewModel = new GenerateCertViewModel(this, defaultPfxPath, defaultPassword);
            InitializeComponent();
            viewModel.Completed.Subscribe(ok => {
                Result = new DResult(ok, viewModel.PfxPath.Value, viewModel.Password.Value);
                Close();
            });
        }

        private GenerateCertViewModel viewModel {
            get => DataContext as GenerateCertViewModel;
            set => DataContext = value;
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            viewModel?.Dispose();
        }
    }
}
