using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.wav {
    public class MediaConvert : CommandProcessor {
        public string InFilePath;
        public string OutFilePath;
        public string Args;

        public MediaConvert() {
            Command = "ffmpeg.exe";
        }
        public MediaConvert(string inFile, string outFile, string args) : this() {
            InFilePath = inFile;
            OutFilePath = outFile;
            Args = args;
        }

        protected override bool Prepare() {
            if( string.IsNullOrWhiteSpace(InFilePath) ||
                string.IsNullOrWhiteSpace(OutFilePath) ||
                string.IsNullOrWhiteSpace(Args)) { 
                LoggerEx.error("in/out file must be specified.");
                return false;
            }
            Arguments = $"-i \"{InFilePath}\" {Args} \"{OutFilePath}\"";
            return true;
        }
    }
}
