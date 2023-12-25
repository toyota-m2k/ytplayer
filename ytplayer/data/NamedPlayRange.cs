using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    public struct NamedPlayRange : IPlayRange {
        public ulong Start { get; }

        public ulong End { get; }

        public string Name { get; }

        public NamedPlayRange(ulong start, ulong end, string name) {
            Start = start;
            End = end;
            Name = name;
        }
        public NamedPlayRange(IPlayRange range, string name) {
            Start = range.Start;
            End = range.End;
            Name = name;
        }
    }
}
