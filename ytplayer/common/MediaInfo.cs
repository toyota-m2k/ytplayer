using Microsoft.WindowsAPICodePack.Shell;
using System;
using io.github.toyota32k.toolkit.utils;

namespace ytplayer.common {
    public static class MediaInfo {
        public static TimeSpan? GetDuration(string filePath) {
            try {
                using (ShellObject Shell = ShellObject.FromParsingName(filePath)) {
                    var ticks = Shell.Properties.System.Media.Duration.Value;
                    if (ticks.HasValue) {
                        return TimeSpan.FromTicks((long)ticks);
                    }
                    return null;
                }
            }
            catch (Exception e) {
                LoggerEx.error(e);
                return null;
            }
        }
    }
}
