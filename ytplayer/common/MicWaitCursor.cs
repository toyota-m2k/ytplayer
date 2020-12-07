using System;
using System.Windows;
using System.Windows.Input;

namespace common {
    public class MicWaitCursor : IDisposable {
        private WeakReference<FrameworkElement> CursorOwner;

        public Cursor Cursor {
            get => CursorOwner?.GetValue()?.Cursor;
            set {
                var co = CursorOwner?.GetValue();
                if(null!=co) {
                    co.Cursor = value;
                }
            }
        }

        private Cursor OrgCursor;

        public MicWaitCursor(FrameworkElement cursorOwner, Cursor waitCursor=null) {
            CursorOwner = new WeakReference<FrameworkElement>(cursorOwner);
            OrgCursor = cursorOwner.Cursor;
            cursorOwner.Cursor = waitCursor ?? Cursors.Wait;
        }

        public void Dispose() {
            Cursor = OrgCursor;
        }

        public static MicWaitCursor Start(FrameworkElement cursorOwner, Cursor waitCursor=null) {
            return new MicWaitCursor(cursorOwner, waitCursor);
        }
    }
}
