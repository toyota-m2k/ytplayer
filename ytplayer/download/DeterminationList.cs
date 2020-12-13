using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.download {
    public class DeterminationList {
        public enum Determination {
            UNKNOWN,
            ACCEPT,
            REJECT,
        }
        public List<string> AcceptList { get; } = new List<string>() { "www.youtube.com", "i.ytimg.com",};
        public List<string> RejectList { get; } = new List<string>();

        public Determination Query(string host) {
            host = host.ToLower();
            if(AcceptList.Contains(host)) {
                return Determination.ACCEPT;
            }
            if(RejectList.Contains(host)) {
                return Determination.REJECT;
            }
            return Determination.UNKNOWN;
        }

        private void AddList(List<string> list, string host) {
            host = host.ToLower();
            if (!list.Contains(host)) {
                list.Add(host);
            }
        }

        public void Reject(string host) {
            AddList(RejectList, host);
        }
        public void Accept(string host) {
            AddList(AcceptList, host);
        }
        public void Determine(string host, bool accept) {
            if(accept) {
                Accept(host);
            } else {
                Reject(host);
            }
        }
    }
}
