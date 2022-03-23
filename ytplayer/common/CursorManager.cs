using io.github.toyota32k.toolkit.utils;
using System;
using System.Windows;
using System.Windows.Threading;

namespace ytplayer.common {
    /**
     * 動画再生時にマウスカーソルを非表示にするための管理クラス
     */
    public class CursorManager {
        private static readonly long WAIT_TIME = 2000;   //3ms
        private Point mPosition;
        private long mCheck = 0;
        private DispatcherTimer mTimer = null;
        private readonly WeakReference<Window> mWin;
        private bool mEnabled = false;

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

        /**
         * 当初プロパティにしていたが、プレーヤーを開いたり閉じたりしているとNPEが出たのでメソッドに変えた。
         * セッターメソッドだと ?.オペレータを使えるので、こういうときはプロパティより使いやすいのだ。
         */
        public void Enable(bool enable) {
            if (enable!= mEnabled) {
                mEnabled = enable;
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
            if (!mEnabled) {
                return;
            }

            if (mPosition != pos) {
                mPosition = pos;
                mCheck = Environment.TickCount;
                CursorOnWin = System.Windows.Input.Cursors.Arrow;
                if (null == mTimer) {
                    mTimer = new DispatcherTimer();
                    mTimer.Tick += OnTimer;
                    mTimer.Interval = TimeSpan.FromMilliseconds(WAIT_TIME / 3.0);
                    mTimer.Start();
                }
            }
        }

        private void OnTimer(object sender, EventArgs e) {
            if (null == mTimer) {
                return;
            }
            if (Environment.TickCount - mCheck > WAIT_TIME) {
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
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(0, (int)SystemParameters.PrimaryScreenHeight);
        }
    }
}
