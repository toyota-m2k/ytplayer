namespace ytplayer.data {
    public interface IPlayRange {
        ulong Start { get; }
        ulong End { get; }
    }
    public struct PlayRange : IPlayRange {
        //private ulong mStart = 0;
        //private ulong mEnd = 0;
        public ulong Start { get; private set; }
        public ulong End { get; private set; }

        public static PlayRange Empty => new PlayRange(0,0);

        public PlayRange(ulong start, ulong end=0) {
            if (end == 0) {
                Start = start;
                End = 0;
            } else {
                if (start > end) {
                    Start = end;
                    End = start;
                } else {
                    Start = start;
                    End = end;
                }
            }
        }

        public PlayRange Clone() {
            return new PlayRange(Start, End);
        }

        public void Set(ulong start, ulong end) {
            this = new PlayRange(start, end);
        }

        public bool TrySetStart(ulong start) {
            if(start!=Start && (End==0 || start<End)) {
                Start = start;
                return true;
            }
            return false;
        }

        public bool TrySetEnd(ulong end) {
            if(end!=End && (end==0 || Start<end)) {
                End = end;
                return true;
            }
            return false;
        }

        public ulong TrueEnd(ulong duration) {
            return End == 0 ? duration : End;
        }
        public ulong TrueSpan(ulong duration) {
            var end = TrueEnd(duration);
            return (end > Start) ? end - Start : 0;
        }

        public void AdjustTrueEnd(ulong duration) {
            if(End==0) {
                End = duration;
            }
        }

        public bool Contains(ulong value) {
            return Start <= value && (End == 0 || value < End);
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
