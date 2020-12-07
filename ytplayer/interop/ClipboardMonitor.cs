using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace ytplayer.interop {
    public class ClipboardMonitor : IDisposable {
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        private readonly IntPtr windowHandle;

        /// <summary>
        /// Event for clipboard update notification.
        /// </summary>
        public event EventHandler ClipboardUpdate;

        private bool isMonitoring = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="window">Main window of the application.</param>
        /// <param name="start">Enable clipboard notification on startup or not.</param>
        public ClipboardMonitor(Window window, bool start = true) {
            windowHandle = new WindowInteropHelper(window).EnsureHandle();
            HwndSource.FromHwnd(windowHandle)?.AddHook(HwndHandler);
            if (start) Start();
        }

        public void Dispose() {
            Stop();
            ClipboardUpdate = null;
        }

        /// <summary>
        /// Enable clipboard notification.
        /// </summary>
        public void Start() {
            if (!isMonitoring) {
                NativeMethods.AddClipboardFormatListener(windowHandle);
                isMonitoring = true;
            }
        }

        /// <summary>
        /// Disable clipboard notification.
        /// </summary>
        public void Stop() {
            if (isMonitoring) {
                NativeMethods.RemoveClipboardFormatListener(windowHandle);
                isMonitoring = false;
            }
        }

        private IntPtr HwndHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled) {
            if (msg == WM_CLIPBOARDUPDATE) {
                this.ClipboardUpdate?.Invoke(this, new EventArgs());
            }
            handled = false;
            return IntPtr.Zero;
        }

        private static class NativeMethods {
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AddClipboardFormatListener(IntPtr hwnd);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        }
    }
}
