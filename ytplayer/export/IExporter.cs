using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.export {
    internal interface IExporter {
        Task<bool> Export(IExportOption option);
    }
    interface IExtractor : IExporter {
        string SrcFile { get; }
        string DstFile { get; }
        bool IsExtracted { get; }
        void ExtractTo(string dstFile, int start, int end);
    }
}
