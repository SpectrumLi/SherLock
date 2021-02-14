// <copyright file="Program.cs" company="Microsoft Research">
// Copyright (c) Microsoft Research. All rights reserved.
// </copyright>

namespace TorchLite
{
    using System;
    using TorchLiteInstrumenter;

    /// <summary>
    /// Instrumenter application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>Instrumentation status.</returns>
        public static int Main(string[] args)
        {
            CommandLineArgs arguments = new CommandLineArgs(args);
            if (!arguments.ArgumentsValid)
            {
                arguments.Usage();
                return (int)InstrumentationResult.ERROR_ARGUMENTS;
            }

            Instrumenter instrumenter = new Instrumenter();
            var result = instrumenter.Instrument(arguments.Assemblies);
            Console.WriteLine($"Instrumetation result: " + result);

            return (int)result;
        }
    }
}
