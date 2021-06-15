namespace ytplayer.data {
    public class PlayRange {
        private ulong mStart = 0;
        private ulong mEnd = 0;

        static public PlayRange Empty => new PlayRange(0,0);

        public PlayRange(ulong start, ulong end) {
            mStart = start;
            End = end;
        }

        public ulong Start { 
            get => mStart;
            set {
                mStart = value;
                if(mEnd<mStart) {
                    mEnd = 0;
                }
            }
        }

        public ulong End {
            get => mEnd;
            set {
                if(value>mStart) {
                    mEnd = value;
                }
                else {
                    mEnd = 0;
                }
            }
        }
    }

    //public class PlayRangeWatcher : IDisposable {
    //    public PlayRange Range { get; set; }
    //    private DispatcherTimer Timer;

    //    public PlayRangeWatcher() {
    //        Timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100) };
    //        Timer.Tick += CheckRange;
    //    }

    //    private void CheckRange(object sender, EventArgs e) {
            
    //    }

    //    public void Start() {
    //    }

    //    public void Stop() {

    //    }

    //    public void Dispose() {
    //        Stop();
    //    }
    //}
}
