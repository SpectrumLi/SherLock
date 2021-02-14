// <copyright file="FileLogger.cs" company="Microsoft Research">
// Copyright (c) Microsoft Research. All rights reserved.
// </copyright>

namespace TorchLiteRuntime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// File logger.
    /// </summary>
    public class FileLogger
    {
        /// <summary>
        /// File path for logging.
        /// </summary>
        private readonly string filePath;

        /// <summary>
        /// Log lock.
        /// </summary>
        private readonly object logLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogger"/> class.
        /// </summary>
        /// <param name="filePath">File path to log.</param>
        /// <param name="append">Indicates if the existing logs should be appended or overwritten.</param>
        public FileLogger(string filePath, bool append = false)
        {
            this.filePath = filePath;
            this.logLock = new object();
            lock (this.logLock)
            {
                if (!append && File.Exists(this.filePath))
                {
                    File.Delete(this.filePath);
                }
            }
        }

        public void Log(string line)
        {
            this.Log(new List<string> { line });
        }

        /// <summary>
        /// write formatted strings.
        /// </summary>
        /// <param name="format">string format.</param>
        /// <param name="args">arguments.</param>
        public void Log(string format, params object[] args)
        {
            string line = string.Format(format, args);
            this.Log(new List<string> { line });
        }

        /// <summary>
        /// Write a list of lines.
        /// </summary>
        /// <param name="lines">list of lines to write.</param>
        public void Log(List<string> lines)
        {
            lock (this.logLock)
            {
                try
                {
                    using (var fs = new FileStream(this.filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(fs))
                        {
                            foreach (string line in lines)
                            {
                                streamWriter.WriteLine($"{DateTime.Now.Ticks}\t{Thread.CurrentThread.ManagedThreadId}\t{line}");
                            }
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
