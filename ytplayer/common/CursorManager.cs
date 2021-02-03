using common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ytplayer.common {
    public class CursorManager {
        private static long WAIT_TIME = 2000;   //3ms
        private Point mPosition;
        private long mCheck = 0;
        private DispatcherTimer mTimer = null;
        private WeakReference<Window> mWin;
        //private bool mEnabled = false;

        public CursorManager(Window owner) {
            mWin = new WeakReference<Window>(owner);
            mPosition = new Point();
        }
        private System.Windows.Input.Cursor CursorOnWin {
            get => mWin?.GetValue().Cursor;
            set {
                var win = mWin?.GetValue();
                if (null != win) {
                    win.Cursor = value;
                }
            }
        }

        private bool Enabled = false;
        public void Enable(bool enable) {
            if (enable!= Enabled) {
                Enabled = enable;
                if (enable) {
                    //Update();
                } else {
                    Reset();
                }
            }
        }

        public void Reset() {
            if (mTimer != null) {
                mTimer.Stop();
                mTimer = null;
            }
            CursorOnWin = System.Windows.Input.Cursors.Arrow;
        }

        public void Update(Point pos) {
            if (!Enabled) {
                return;
            }

            if (mPosition != pos) {
                mPosition = pos;
                mCheck = System.Environment.TickCount;
                CursorOnWin = System.Windows.Input.Cursors.Arrow;
                if (null == mTimer) {
                    mTimer = new DispatcherTimer();
                    mTimer.Tick += OnTimer;
                    mTimer.Interval = TimeSpan.FromMilliseconds(WAIT_TIME / 3);
                    mTimer.Start();
                }
            }
        }

        private void OnTimer(object sender, EventArgs e) {
            if (null == mTimer) {
                return;
            }
            if (System.Environment.TickCount - mCheck > WAIT_TIME) {
                mTimer.Stop();
                mTimer = null;
                CursorOnWin = System.Windows.Input.Cursors.None;
                var win = mWin?.GetValue();
                if(win?.WindowStyle == WindowStyle.None) {
                    KickOutMouse();
                }
            }
        }
        private void KickOutMouse() {
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(0, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        }
    }
}
