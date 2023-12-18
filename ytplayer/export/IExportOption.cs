using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.export {
    internal interface IExportOption {
        string SrcFile { get; }     // -i 
        string DstFile { get; }     // 
        bool OnlyAudio { get; }     // -vn -f mp3
        bool NoTranscode { get; }   // -c copy
        Func<string,bool> StdOutProc { get; }
        Func<string,bool> StdErrProc { get; }
        bool ShowCommandPromptOnConverting { get; }
    }
}
