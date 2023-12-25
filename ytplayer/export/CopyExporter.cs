using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.export {
    public class CopyExporter : IExporter {
        protected static readonly LoggerEx logger = new LoggerEx("EXPORTER");
        protected ExportOption Option { get; }
        public virtual string DstFile => Option.DstFile;
        public CopyExporter(ExportOption option) {
            Option = option;
        }

        public virtual void DeleteResult() {
            PathUtil.safeDeleteFile(DstFile);
        }

        public virtual Task<bool> Export() {
            return Task.Run(() => {
                try {
                    File.Copy(Option.SrcFile, Option.DstFile);
                    return true;
                } catch (Exception e) {
                    Option.StdErrProc?.Invoke(e.Message);
                    logger.error(e, "cannot copy");
                    return false;
                }
            });
        }

        public virtual void Cancel() {
            // copyはキャンセルできない   
        }
    }
}
