// Copyright (C) 2016 by Barend Erasmus and donated to the public domain

using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// NOTE: two consequences of this simplified response model are:
//
//      (a) it's not possible to send 8-bit clean responses (like file content)
//      (b) it's 
//       must be loaded into memory in the the Content property. If you want to send large files,
//       this has to be reworked so a handler can write to the output stream instead. 

namespace SimpleHttpServer.Models {
    public enum HttpStatusCode {
        // for a full list of status codes, see..
        // https://en.wikipedia.org/wiki/List_of_HTTP_status_codes

        Continue = 100,

        Ok = 200,
        Created = 201,
        Accepted = 202,
        PartialContent = 206,
        MovedPermanently = 301,
        Found = 302,
        NotModified = 304,
        BadRequest = 400,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        InternalServerError = 500,
        ServiceUnavailable = 503,
    }

    public interface IHttpResponse {
        void WriteResponse(Stream outputStream);
    }

    public abstract class AbstractHttpResponse : IHttpResponse {
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }

        public string ContentType {
            get => Headers.GetValue("Content-Type");
            set => Headers["Content-Type"] = value;
        }
        public long ContentLength {
            get => Convert.ToInt64(Headers.GetValue("Content-Length", "0"));
            set => Headers["Content-Length"] = $"{value}";
        }

        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        protected abstract void Prepare();

        protected virtual void WriteHeaders(Stream output) {
            WriteText(output, $"HTTP/1.0 {StatusCode} {ReasonPhrase}\r\n");
            WriteText(output, string.Join("\r\n", Headers.Select(x => $"{x.Key}: {x.Value}")));
            WriteText(output, "\r\n");
        }
        protected abstract void WriteBody(Stream output);

        public void WriteResponse(Stream output) {
            Prepare();
            WriteHeaders(output);
            WriteText(output, "\r\n");
            WriteBody(output);
        }

        protected static void WriteText(Stream output, string text) {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            output.Write(bytes, 0, bytes.Length);
        }
    }


    public class TextHttpResponse : AbstractHttpResponse {
        public string Content { get; set; }
        private byte[] Buffer = null;

        public TextHttpResponse() {
            StatusCode = 200;
            ReasonPhrase = "OK";
        }

        public TextHttpResponse(string content, string contentType, int code=200, string reason="OK") {
            StatusCode = code;
            ReasonPhrase = reason;
            Content = content;
            ContentType = contentType;
        }

        protected override void Prepare() { 
            Buffer = Content!=null ? Encoding.UTF8.GetBytes(Content) : new byte[] { };
            ContentLength = Buffer.Length;
        }

        protected override void WriteBody(Stream output) {
            output.Write(Buffer, 0, Buffer.Length);
        }

        // informational only tostring...
        public override string ToString() {
            return string.Format($"TextHttpResponse status {this.StatusCode} {this.ReasonPhrase}");
        }
    }

    public class FileHttpResponse : AbstractHttpResponse {
        public string ContentFilePath { get; set; }

        public FileHttpResponse() {
        }

        public FileHttpResponse(string path, string contentType) {
            ContentFilePath = path;
            StatusCode = 200;
            ReasonPhrase = "OK";
            ContentType = contentType;
        }

        protected long FileLength => new System.IO.FileInfo(ContentFilePath).Length;

        protected FileStream OpenFile() {
            return new FileStream(ContentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        protected override void Prepare() {
            ContentLength = FileLength;
        }

        protected override void WriteBody(Stream output) {
            using (var input = OpenFile()) {
                input.CopyTo(output);
                output.Flush();
            }
        }
        // informational only tostring...
        public override string ToString() {
            return string.Format($"FileHttpResponse status {this.StatusCode} {this.ReasonPhrase}");
        }
    }

    public class StreamingHttpResponse : FileHttpResponse {
        public StreamingHttpResponse() { }

        public long Start { get; set; } = 0;
        public long End { get; set; } = 0;

        public StreamingHttpResponse(string path, string contentType, long start, long end) 
            : base(path, contentType) {
            Start = start;
            End = end;
        }

        protected override void Prepare() {
            if (Start == 0 && End == 0) {
                StatusCode = 200;
                Headers["Accept-Range"] = "bytes";
                base.Prepare();
            } else {
                var fileLength = FileLength;
                if (End == 0) {
                    End = fileLength - 1;
                }
                StatusCode = 206;
                Headers["Content-Range"] = $"bytes {Start}-{End}/{fileLength}";
                Headers["Accept-Range"] = "bytes";
                ContentLength = End - Start + 1;
            }
        }

        protected override void WriteBody(Stream output) {
            if (Start == 0 && End == 0) {
                base.WriteBody(output);
            } else {
                long chunkLength = End - Start + 1;
                long remain = chunkLength;
                int read=0;
                using (var input = OpenFile()) {
                    byte[] buffer = new byte[Math.Max(chunkLength, 1*1024*1024)];
                    input.Seek(Start, SeekOrigin.Begin);
                    while (remain > 0) {
                        read = input.Read(buffer, 0, Math.Min(buffer.Length, (int)remain));
                        output.Write(buffer, 0, read);
                        remain -= read;
                    }
                }
            }
        }

    }
}
