using System;
using System.Diagnostics;

namespace ytplayer.common {
    public enum LogLevel {
        DEBUG,
        INFO,
        WARN,
        ERROR,
    }

    public interface ILogTracer {
        void trace(LogLevel level, string message);
    }

    internal class ConsoleLogger : ILogTracer {
        public void trace(LogLevel level, string message) {
            Debug.WriteLine($"[{level}]: {message}");
        }
    }

    public static class Logger {
        public static ILogTracer Tracer { get; set; } = new ConsoleLogger();
        
        public static void error(string fmt, params object[] args) {
            Tracer?.trace(LogLevel.ERROR, string.Format(fmt, args));
        }
        public static void error(Exception e, string fmt, params object[] args) {
            var msg = string.Format(fmt, args);
            if(!string.IsNullOrEmpty(msg)) {
                msg = msg + "\n" + e.ToString();
            } else {
                msg = e.ToString();
            }
            Tracer?.trace(LogLevel.ERROR, msg);
        }
        public static void error(Exception e) {
            error(e, "");
        }
        public static void warn(string fmt, params object[] args) {
            Tracer?.trace(LogLevel.WARN, string.Format(fmt, args));
        }
        public static void info(string fmt, params object[] args) {
            Tracer?.trace(LogLevel.INFO, string.Format(fmt, args));
        }
        [Conditional("DEBUG")]
        public static void debug(string fmt, params object[] args) {
            Tracer?.trace(LogLevel.DEBUG, string.Format(fmt, args));
        }
    }
}
