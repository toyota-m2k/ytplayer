using common;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    public class RatingFilter : MicViewModelBase {
        public ReactiveProperty<bool> Normal { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> Good { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> Excellent { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> Bad { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> Dreadful { get; } = new ReactiveProperty<bool>(true);

        private ReactiveProperty<bool>[] Filters;

        public RatingFilter() {
            Filters = new ReactiveProperty<bool>[] { Dreadful, Bad, Normal, Good, Excellent };
            FromArray(Settings.Instance.Ratings);
        }
        private bool this[int i] {
            get => Filters[i].Value;
            set => Filters[i].Value = value;
        }

        public bool this[Rating i] {
            get => Filters[(int)i].Value;
            set => Filters[(int)i].Value = value;
        }

        public bool[] ToArray() {
            return Filters.Select((p) => p.Value).ToArray();
        }
        public void FromArray(bool[] vals) {
            if (vals == null) return;
            for (int i = 0, ci = vals.Length; i < ci; i++) {
                this[i] = vals[i];
            }
        }

        public IEnumerable<DLEntry> Filter(IEnumerable<DLEntry> src) {
            return src.Where((e) => this[e.Rating]);
        }

        //private static RatingFilter sInstance = null;
        //public static RatingFilter Instance {
        //    get {
        //        if (sInstance == null) {
        //            sInstance = new RatingFilter();
        //        }
        //        return sInstance;
        //    }
        //}
    }
}
