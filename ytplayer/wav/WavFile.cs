using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ytplayer.wav {
    // ffmpeg -i "c.mp4" -map 0:1 -vn -ac 1 -ar 8000  -acodec pcm_s16le -f wav "c.wav"
    public class WavFile : IDisposable {
        public bool DeleteWaveFileOnDispose { get; set; } = false;

        private string Path;
        private FileStream WaveStream;
        private BinaryReader Reader;
        private bool EOS = false;

        const int HeaderLength = 44;

        public long Length { get; private set; } = 0;              // HeaderLength(44)+DataLength
        public short Channels { get; private set; } = 0;           // 1:mono / 2:stereo
        public int SamplingRate { get; private set; } = 0;         // sample/seconds
        public int BitsPerSample { get; private set; } = 0;        // bit/sample
        public int DataLength { get; private set; } = 0;           // bytes
        public double Duration { get; private set; } = 0;          // seconds
        public int BytesPerSample => BitsPerSample / 8;
        public int SampleCount => DataLength / BytesPerSample;

        public WavFile() {

        }

        public void Dispose() {
            Reader?.Close();
            Reader?.Dispose();
            Reader = null;
            WaveStream?.Close();
            WaveStream?.Dispose();
            WaveStream = null;
            if(DeleteWaveFileOnDispose) {
                PathUtil.safeDeleteFile(Path);
            }
        }

        public void Open(string path, bool deleteOnDispose=false) {
            if (WaveStream != null) {
                throw new InvalidOperationException("wave file already opened.");
            }
            Path = path;
            DeleteWaveFileOnDispose = deleteOnDispose;
            WaveStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            Length = (int)WaveStream.Length - 8;

            Reader = new BinaryReader(WaveStream);
            WaveStream.Position = 22;
            Channels = Reader.ReadInt16(); //1
            WaveStream.Position = 24;
            SamplingRate = Reader.ReadInt32(); //8000
            WaveStream.Position = 34;
            BitsPerSample = Reader.ReadInt16(); //16
            DataLength = (int)WaveStream.Length - 44;
            Duration = (double)DataLength / (double)BytesPerSample / (double)SamplingRate / (double)Channels;
            Logger.info($"Channels      = {Channels}");
            Logger.info($"SamplingRate  = {SamplingRate} sample/second");
            Logger.info($"BitsPerSample = {BitsPerSample} bits/sample");
            Logger.info($"DataLength    = {DataLength} bytes");
            Logger.info($"Duration      = {Duration} seconds.");

            WaveStream.Position = HeaderLength;
            EOS = false;
        }

        public void Rewind() {
            WaveStream.Position = HeaderLength;
            EOS = false;
        }

        /// <summary>
        /// In stereo wave format, samples are stored in 2's complement. For Mono, it's necessary to 
        /// convert those samples to their equivalent signed value. This method is used 
        /// by other public methods to equilibrate wave formats of different files.
        /// </summary>
        /// <param name="bytArr">Sample data in array</param>
        /// <param name="intPos">Array offset</param>
        /// <returns>Mono value as signed short</returns>
        public bool ReadDataSequential(out short data) // 2's complement to normal signed value
        {
            try {
                var bytes = Reader.ReadBytes(2);
                data = BitConverter.ToInt16(bytes, 0);
                if (data != 0) {
                    data = Convert.ToInt16((~data | 1));
                }
                return true;
            } catch(Exception e) {
                LoggerEx.error(e);
                EOS = true;
                data = 0;
                return false;
            }
        }

        public long Time2Position(double timeInSec) {
            return HeaderLength + (int)((double)DataLength * timeInSec / Duration);
        }

        public double Position2Time(long pos) {
            return Duration * ((double)(pos - HeaderLength) / (double)DataLength);
        }

        public bool SeekTo(double timeInSec) {
            var pos = Time2Position(timeInSec);
            if(pos<Length) {
                WaveStream.Position = pos;
                return true;
            }
            return false;
        }

        public IEnumerable<short> ReadReducedData(int reduce) {
            Rewind();
            short data;
            while (!EOS) {
                var pos = WaveStream.Position + (reduce*BytesPerSample);
                if (pos + BytesPerSample >= Length) {
                    EOS = true;
                } else {
                    WaveStream.Position = pos;
                    ReadDataSequential(out data);
                    yield return data;
                }
            }
        }

        public (short min, short max) GetRange() {
            Rewind();
            short data, min=short.MaxValue, max=short.MinValue;

            while(ReadDataSequential(out data)) {
                min = Math.Min(min, data);
                max = Math.Max(max, data);
            }
            return (min, max);
        }

        public IEnumerable<(double,double)> ScanChapter(short threshold=500, double minSeconds=1.5) {
            Rewind();
            bool fetching = false;
            long start = 0, end =0;
            long minSamples = (long)((double)SamplingRate * minSeconds)* BytesPerSample;
            short data;
            while (ReadDataSequential(out data)) {
                if(Math.Abs(data)<threshold) {
                    if(!fetching) {
                        fetching = true;
                        start = WaveStream.Position;
                    } else {
                        
                    }
                } else {
                    if(fetching) {
                        fetching = false;
                        end = WaveStream.Position;
                        if(end-start>minSamples) {
                            // found
                            yield return (Position2Time(start), Position2Time(end));
                        }
                    }
                }
            }
        }

        public static async Task<WavFile> CreateFromMP4(string inFile, string outWorkFile, Action<string> stdOutput, Action<string> stdError) {
            var conv = new MediaConvert(inFile, outWorkFile, "-map 0:1 -vn -ac 1 -ar 8000  -acodec pcm_s16le -f wav");
            //conv.ShowCommandPrompt = true;
            conv.StandardOutput = stdOutput;
            conv.StandardError = stdError;
            if (!await conv.Execute()) {
                return null;
            }
            if(!PathUtil.isFile(outWorkFile)) {
                return null;
            }
            var wf = new WavFile();
            try {
                wf.Open(outWorkFile, deleteOnDispose: true);
                return wf;
            } catch(Exception e) {
                LoggerEx.error(e);
                return null;
            }
        }
    }
}
