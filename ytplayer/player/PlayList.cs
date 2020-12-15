using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.player {
    public interface IPlayable {
        string Path { get; }
        string Name { get; }
        string Url { get; }
        Rating Rating { get; set; }
        bool HasFile { get; }
        void Delete();
    }

    public interface IPlayList {
        void SetList(IEnumerable<IPlayable> s, IPlayable initialItem = null);
        void Add(IPlayable item);
        ReadOnlyReactiveProperty<IPlayable> Current { get; }
    }

    public class PlayList : IPlayList {
        private ReactiveProperty<List<IPlayable>> List { get; } = new ReactiveProperty<List<IPlayable>>();
        public ReactiveProperty<int> CurrentIndex { get; } = new ReactiveProperty<int>(-1,ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        
        public ReadOnlyReactiveProperty<int> CurrentPos { get; }
        public ReadOnlyReactiveProperty<int> TotalCount { get; }

        public ReadOnlyReactiveProperty<IPlayable> Current { get; }
        public ReadOnlyReactiveProperty<bool> HasNext { get; }
        public ReadOnlyReactiveProperty<bool> HasPrev { get; }

        public Subject<int> ListItemAdded = new Subject<int>();

        public PlayList() {
            CurrentPos = CurrentIndex.Select((v) => v + 1).ToReadOnlyReactiveProperty();
            TotalCount = CurrentIndex.Select((v) => List?.Value?.Count ?? 0).ToReadOnlyReactiveProperty();
            Current = List.CombineLatest(CurrentIndex, (list, index) => {
                return (0<=index && index<list.Count) ? list[index] : null;
            }).ToReadOnlyReactiveProperty();

            HasNext = List.CombineLatest(CurrentIndex, (list, index) => {
                return index+1 < (list?.Count ?? 0);
            }).ToReadOnlyReactiveProperty();
            HasPrev = List.CombineLatest(CurrentIndex, (List, index) => {
                return 0 < index && List!=null;
            }).ToReadOnlyReactiveProperty();
        }

        public void SetList(IEnumerable<IPlayable> s, IPlayable initialItem=null) {
            List.Value = new List<IPlayable>(s.Where((e) => e.HasFile));
            if(List.Value.Count==0) {
                CurrentIndex.Value = -1;
            } else if(initialItem!=null && List.Value.Contains(initialItem)) { 
                CurrentIndex.Value = List.Value.IndexOf(initialItem);
            } else {
                CurrentIndex.Value = 0;
            }
        }

        public void Add(IPlayable item) {
            int index = CurrentIndex.Value;
            if (List.Value==null) {
                List.Value = new List<IPlayable>();
                index = 0;
            }
            List.Value.Add(item);
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
