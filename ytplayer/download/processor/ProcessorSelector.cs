using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.download.processor.impl;

namespace ytplayer.download.processor {
    public static class ProcessorSelector {
        
        private static List<IProcessor> Processors = new List<IProcessor>() {
            new YoutubeProcessor()
        };

        // fallback
        private static CommonProcessor CommonProcessor = new CommonProcessor();

        public static IProcessor Select(Uri uri) {
            foreach(var f in Processors) {
                if(f.IsAcceptableUrl(uri)) {
                    return f;
                }
            }
            return CommonProcessor;
        }
    }
}
