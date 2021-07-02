// Copyright (C) 2016 by David Jeske, Barend Erasmus and donated to the public domain

using io.github.toyota32k.toolkit.utils;
using log4net;
using SimpleHttpServer;
using SimpleHttpServer.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ytplayer.download;

namespace SimpleHttpServer
{

    public class HttpServer
    {
        #region Fields

        private int Port;
        private TcpListener Listener;
        private HttpProcessor Processor;
        //private bool IsActive = true;

        #endregion

        private static readonly ILog log = LogManager.GetLogger(typeof(HttpServer));
        private bool Alive = true;
        private WeakReference<IReportOutput> mReportOutput;
        private IReportOutput ReportOutput => mReportOutput?.GetValue();

        #region Public Methods
        public HttpServer(int port, List<Route> routes, IReportOutput reportOutput)
        {
            this.Port = port;
            this.Processor = new HttpProcessor();
            this.mReportOutput = new WeakReference<IReportOutput>(reportOutput);

            foreach (var route in routes)
            {
                this.Processor.AddRoute(route);
            }
        }

        //public void Listen()
        //{
        //    this.Listener = new TcpListener(IPAddress.Any, this.Port);
        //    this.Listener.Start();
        //    while (this.IsActive)
        //    {
        //        TcpClient s = this.Listener.AcceptTcpClient();
        //        Thread thread = new Thread(() =>
        //        {
        //            this.Processor.HandleClient(s);
        //        });
        //        thread.Start();
        //        Thread.Sleep(1);
        //    }
        //}

        public bool Start()
        {
            try {
                this.Listener = new TcpListener(IPAddress.Any, this.Port);
                this.Listener.Start();
            } catch(Exception e) {
                log.Error(e);
                Stop();
                return false;
            }
            Task.Run(async () =>
            {
                while (Alive)
                {
                    try
                    {
                        TcpClient s = await this.Listener.AcceptTcpClientAsync();
                        this.Processor.HandleClient(s);
                    }
                    catch(Exception e)
                    {
                        if (Alive) {
                            ReportOutput?.ErrorOutput(e.ToString());
                            log.Error(e);
                        }
                    }
                }
                lock (this) {
                    Listener?.Stop();
                    Listener = null;
                }
            });
            return true;
        }

        public void Stop()
        {
            Alive = false;
            lock (this) {
                Listener?.Stop();
                Listener = null;
            }
        }
        #endregion

    }
}



