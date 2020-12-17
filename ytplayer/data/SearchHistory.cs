using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    public class SearchHistory {
        //[System.Xml.Serialization.XmlIgnore]
        public ObservableCollection<string> History { get; } = new ObservableCollection<string>();

        public void Put(string value) {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return;
            int i = History.IndexOf(value);
            if (i < 0) {
                History.Insert(0, value);
            } else if(i!=0) {
                History.Move(i, 0);
            }
        }

        //public List<string> List {
        //    get => History.ToList();
        //    set {
        //        History.Clear();
        //        foreach(var s in value) {
        //            History.Add(s);
        //        }
        //    }
        //}
    }
}
