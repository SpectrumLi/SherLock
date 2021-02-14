namespace TorchLiteRuntime
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using Microsoft.Torch.Log4Net4Torch;
    using Microsoft.Torch.Log4Net4Torch.Appender;
    using Microsoft.Torch.Log4Net4Torch.Config;
    using Microsoft.Torch.Log4Net4Torch.Core;
    using Microsoft.Torch.Log4Net4Torch.Layout;
    using Microsoft.Torch.Log4Net4Torch.Repository.Hierarchy;

    public class Log4NetLogger
    {
        public readonly ILog Logger;
        private readonly string LogFile;
        private readonly FileAppender fileAppender;
        private readonly object lockObj = new object();

        public List<string> Mybuffer = new List<string>();

        private int BUFFER_SIZE = 5000;
        private StreamWriter sw;

        private Semaphore semaphore = new Semaphore(0, 1);

        ~ Log4NetLogger()
        {
            Thread.Sleep(4000);
        }

        public Log4NetLogger(string logFile)
        {
            this.LogFile = logFile;
            if (File.Exists(this.LogFile))
            {
                try
                {
                    File.Delete(this.LogFile);
                }
                catch
                {
                }
            }

            /*
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Root.RemoveAllAppenders(); 

            var layout = new PatternLayout("%message%newline"); // ("%date %-5level %logger - %message%newline");
            layout.ActivateOptions(); // According to the docs this must be called as soon as any properties have been changed.

            FileAppender fileAppender = new FileAppender()
            {
                File = logFile,
                AppendToFile = true,
                Encoding = Encoding.UTF8,
                Threshold = Level.Debug,
                LockingModel = new FileAppender.MinimalLock(),
                Layout = layout,
                ImmediateFlush = false,
            };

            fileAppender.ActivateOptions();

            ConsoleAppender consoleAppender = new ConsoleAppender();
            consoleAppender.Layout = layout;
            consoleAppender.ActivateOptions();

            IAppender[] appenders = { fileAppender }; // { fileAppender, consoleAppender };
            // var buffered =  as BufferingAppenderSkeleton;
            BasicConfigurator.Configure(appenders);
            this.Logger = LogManager.GetLogger(Path.GetFileNameWithoutExtension(logFile));
            ((Hierarchy)LogManager.GetRepository()).Root.Level = Level.Debug;
            // HookShutdown(this);
            */

            HookShutdown_buffer(this);
            this.sw = new StreamWriter(this.LogFile);
            this.sw.WriteLine("#INFO: Starting Torch run-time pre rel size " + CallbacksV2.potentialrels.Count);

        }
        
        private void HookShutdown_buffer(Log4NetLogger logger)
        {
            AppDomain.CurrentDomain.DomainUnload += (s, e) => flush_buffer(logger,1);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => flush_buffer(logger,1);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => flush_buffer(logger,1);
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => { flush_buffer(logger,1); return null; };
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (s, e) => { flush_buffer(logger,1); return null; };
            AppDomain.CurrentDomain.ResourceResolve += (s, e) => { flush_buffer(logger,1); return null; };
        }

        public void flush_buffer(Log4NetLogger logger, int final_flush)
        {
            try
            {
                foreach (var s in logger.Mybuffer)
                {
                    logger.sw.WriteLine(s);
                }

                // logger.sw.WriteLine("size " + logger.Mybuffer.Count);
                logger.sw.Flush();
                if (final_flush > 0)
                {
                    logger.sw.WriteLine("#INFO: Shutting down Torch run-time");
                    logger.sw.Close();
                }
            }
            catch (Exception)
            {

            }
            logger.Mybuffer.Clear();
        }

        private void HookShutdown(Log4NetLogger logger)
        {
            AppDomain.CurrentDomain.DomainUnload += (s, e) => Shutdown(logger);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown(logger);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Shutdown(logger);
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => { Shutdown(logger); return null; };
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (s, e) => { Shutdown(logger); return null; };
            AppDomain.CurrentDomain.ResourceResolve += (s, e) => { Shutdown(logger); return null; };
        }

        private volatile bool shutdown;
        private readonly object Sync = new object();

        private void Shutdown(Log4NetLogger logger)
        {
            lock (Sync)
            {
                if (!shutdown)
                {
                    try
                    {
                        if (logger != null)
                        {
                            logger.Log(logger.Mybuffer);
                            logger.Log("#INFO: Shutting down Torch run-time");
                            ShutDown(logger);
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.Error.WriteLine($"#ERROR: {exception.GetType()} thrown shutting down Torch run-time. {exception.Message}");
                        Console.Error.WriteLine(exception.StackTrace);
                    }
                    finally
                    {
                        shutdown = true;
                    }
                }
            }
        }

        public void ShutDown(Log4NetLogger logger)
        {
            var rep = LogManager.GetRepository();
            foreach (IAppender appender in rep.GetAppenders())
            {
                var buffered = appender as BufferingAppenderSkeleton;
                if (buffered != null)
                {
                    buffered.Flush();
                }
            }

            logger.Logger.Logger.Repository.Shutdown();
        }

        public void PushBuffer(string line)
        {
            lock (this.Mybuffer)
            {
                this.Mybuffer.Add(line);
                if (this.Mybuffer.Count > this.BUFFER_SIZE)
                {
                    this.flush_buffer(this,0);
                }
            }
        }

        public void Log(string line)
        {
            this.Logger.Debug($"{line}");
        }

        public void Log(List<string> lines)
        {
            var s = string.Empty;
            foreach (var st in lines)
            {
                string.Join(s, st + "\n");
            }
            lock (this.lockObj)
            {
                this.Log(s);
            }
        }

    }
}
