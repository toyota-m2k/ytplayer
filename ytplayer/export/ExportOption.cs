using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.export {
    public class ExportOption {
        public string SrcFile { get; }

        public string DstFile { get; }

        public bool OnlyAudio { get; }

        public bool NoTranscode { get; }
        public bool Overwrite { get; }

        public Func<string, bool> StdOutProc { get; }

        public Func<string, bool> StdErrProc { get; }

        public bool ShowCommandPromptOnConverting { get; }

        public ExportOption(string srcFile, string dstFile, bool onlyAudio, bool noTranscode, bool overwtite, Func<string, bool> stdoutProc, Func<string, bool> stderrProc, bool showCommandPromptOnConverting) {
            this.SrcFile = srcFile;
            this.DstFile = dstFile;
            this.OnlyAudio = onlyAudio;
            this.NoTranscode = noTranscode;
            this.Overwrite = overwtite;
            this.StdOutProc = stdoutProc;
            this.StdErrProc = stderrProc;
            this.ShowCommandPromptOnConverting = showCommandPromptOnConverting;
        }

        /**
         * ExportOptionを新しく作成する
         */
        public static ExportOption Create(string srcFile, string dstFile, bool onlyAudio, bool noTranscode, bool overwrite, Func<string, bool> stdoutProc, Func<string, bool> stderrProc, bool showCommandPromptOnConverting) {
            return new ExportOption(srcFile, dstFile, onlyAudio, noTranscode, overwrite, stdoutProc, stderrProc, showCommandPromptOnConverting);
        }

        /**
         * src から dstFile を変更した ExportOption を作成する
         */
        public static ExportOption DeriveFrom(ExportOption src, string dstFile) {
            return new ExportOption(src.SrcFile, dstFile, src.OnlyAudio, src.NoTranscode, src.Overwrite, src.StdOutProc, src.StdErrProc, src.ShowCommandPromptOnConverting);
        }
        /**
         * src の 出力（DstFile） を次の入力とする ExportOption を作成する
         */
        public static ExportOption ChainFrom(ExportOption src, string dstFile) {
            return new ExportOption(src.DstFile, dstFile, src.OnlyAudio, src.NoTranscode, src.Overwrite, src.StdOutProc, src.StdErrProc, src.ShowCommandPromptOnConverting);
        }

        /**
         * 安全なファイル名を生成する
         * 使えない文字は、replaceに置き換える（デフォルトは空文字。。。つまり削除する）
         */
        public static string SafeFileName(string name, string replace="") {
            if(string.IsNullOrWhiteSpace(name)) {
                return "";
            }
            return Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c.ToString(),replace));
        }
    }
}
