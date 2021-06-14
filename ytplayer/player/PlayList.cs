using Reactive.Bindings;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ytplayer.data;

namespace ytplayer.player {
    public class PlayList {
        private ReactivePropertySlim<List<DLEntry>> List { get; } = new ReactivePropertySlim<List<DLEntry>>(null);
        public ReactivePropertySlim<int> CurrentIndex { get; } = new ReactivePropertySlim<int>(-1, ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        
        public ReadOnlyReactivePropertySlim<int> CurrentPos { get; }
        public ReadOnlyReactivePropertySlim<int> TotalCount { get; }

        public ReadOnlyReactivePropertySlim<DLEntry> Current { get; }
        public ReadOnlyReactivePropertySlim<bool> HasNext { get; }
        public ReadOnlyReactivePropertySlim<bool> HasPrev { get; }

        public Subject<int> ListItemAdded = new Subject<int>();

        public PlayList() {
            CurrentPos = CurrentIndex.Select((v) => v + 1).ToReadOnlyReactivePropertySlim();
            TotalCount = CurrentIndex.Select((v) => List?.Value?.Count ?? 0).ToReadOnlyReactivePropertySlim();
            Current = List.CombineLatest(CurrentIndex, (list, index) => {
                return (0<=index && index<list.Count) ? list[index] : null;
            }).ToReadOnlyReactivePropertySlim();

            HasNext = List.CombineLatest(CurrentIndex, (list, index) => {
                return index+1 < (list?.Count ?? 0);
            }).ToReadOnlyReactivePropertySlim();
            HasPrev = List.CombineLatest(CurrentIndex, (List, index) => {
                return 0 < index && List!=null;
            }).ToReadOnlyReactivePropertySlim();
        }

        public void SetList(IEnumerable<DLEntry> s, DLEntry initialItem =null) {
            List.Value = new List<DLEntry>(s.Where((e) => e.HasFile));
            if(List.Value.Count==0) {
                CurrentIndex.Value = -1;
            } else if(initialItem!=null && List.Value.Contains(initialItem)) { 
                CurrentIndex.Value = List.Value.IndexOf(initialItem);
            } else {
                CurrentIndex.Value = 0;
            }
        }

        public void Add(DLEntry item) {
            int index = CurrentIndex.Value;
            if (List.Value==null) {
                List.Value = new List<DLEntry>();
                index = 0;
            }
            if (!List.Value.Where((v) => v.Url == item.Url).Any()) {
                List.Value.Add(item);
            }
            CurrentIndex.Value = index;    // has next を更新するため
            ListItemAdded.OnNext(List.Value.Count-1); // Endedのプレーヤーに再生を再開させるため
        }

        public bool Next() {
            if(HasNext.Value) {
                CurrentIndex.Value++;
                return true;
            }
            return false;
        }
        public bool Prev() {
            if(HasPrev.Value) {
                CurrentIndex.Value--;
                return true;
            }
            return false;
        }
    }
}
