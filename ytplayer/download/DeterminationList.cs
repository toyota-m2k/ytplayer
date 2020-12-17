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
        public HashSet<string> AcceptSet { get; } = new HashSet<string>() { "www.youtube.com",};
        public HashSet<string> RejectSet { get; } = new HashSet<string>();

        public Determination Query(string host) {
            host = host.ToLower();
            if(AcceptSet.Contains(host)) {
                return Determination.ACCEPT;
            }
            if(RejectSet.Contains(host)) {
                return Determination.REJECT;
            }
            return Determination.UNKNOWN;
        }

        private void AddList(HashSet<string> list, string host) {
            host = host.ToLower();
            if (!list.Contains(host)) {
                list.Add(host);
            }
        }

        public void Reject(string host) {
            AddList(RejectSet, host);
        }
        public void Accept(string host) {
            AddList(AcceptSet, host);
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
