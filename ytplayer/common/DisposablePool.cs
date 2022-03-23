using System;
using System.Collections.Generic;

namespace ytplayer.common {
    public class DisposablePool : List<IDisposable>, IDisposable {
        public void Reset() {
            foreach (var e in this) {
                e.Dispose();
            }
            Clear();
        }

        public void Dispose() {
            Reset();
        }
    }
}
