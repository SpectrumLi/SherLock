using System;
using System.Collections.Generic;
using System.IO;

namespace TorchLiteInstrumenter
{
    /// <summary>
    /// SRT instrumenter class.
    /// </summary>
    public class Instrumenter
    {
        private readonly ILRewriter ilRewriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Instrumenter"/> class.
        /// </summary>
        public Instrumenter()
        {
            this.ilRewriter = new ILRewriter();

            // a dummy call to force copying SRTRuntime.dll to the build directory
            TorchLiteRuntime.Dummy.Call();
        }

        /// <summary>
        /// Instruments a list of assemblies.
        /// </summary>
        /// <param name="assemblyPaths">A list of assemblies to instrument.</param>
        /// <returns>0, if at least one input assembly is instrumented correctly.</returns>
        public InstrumentationResult Instrument(List<string> assemblyPaths)
        {
            if (assemblyPaths == null)
            {
                return InstrumentationResult.ERROR_ARGUMENTS;
            }

            bool instrumented = false;
            string directoryPath = null;

            Console.WriteLine("Instrumenting assemblies.");
            foreach (string path in assemblyPaths)
            {
                Console.WriteLine($"Instrumenting {Path.GetFileName(path)}");
                // Console.Write($"{Path.GetFileName(path)} ...");

                InstrumentationResult insResult = this.ilRewriter.InstrumentAssembly(path);

                Console.WriteLine(insResult);
                if (insResult == InstrumentationResult.OK)
                {
                    directoryPath = Path.GetDirectoryName(path);
                    instrumented = true;
                }
            }

            Console.WriteLine("Instrumentation complete. Copying SRT files.");

            if (instrumented)
            {
                InstrumentationHelper.CopyDependentAssemblies(directoryPath);
            }

            Console.WriteLine($"Done.");
            return instrumented ? InstrumentationResult.OK : InstrumentationResult.ERROR_INSTRUMENTATION;
        }
    }
}
