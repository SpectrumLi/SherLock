using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TorchLiteInstrumenter
{
    /// <summary>
    /// ILRewriter class.
    /// </summary>
    public class ILRewriter
    {
        private readonly Dictionary<string, MethodDefinition> SRTRuntimeMethodDef = new Dictionary<string, MethodDefinition>();
        private readonly ModuleDefinition srtRuntimeModule;

        /// <summary>
        /// Initializes a new instance of the <see cref="ILRewriter"/> class.
        /// </summary>
        /// <param name="configuration">Instrumentation Configuration.</param>
        public ILRewriter()
        {
            string cwd = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string runtimeAssembly = Path.Combine(cwd, Constants.RuntimeLibraryFile);
            this.srtRuntimeModule = ModuleDefinition.ReadModule(runtimeAssembly);
        }

        /// <summary>
        /// Rewrites an assembly to (1) add new concurrent tests and (2) remove existing tests.
        /// </summary>
        /// <param name="assemblyPath">Assembly file path.</param>
        /// <returns>Instrumentation result.</returns>
        public InstrumentationResult InstrumentAssembly(string assemblyPath)
        {
            AssemblyDefinition assembly = null;
            InstrumentationResult preInstrumentationResult = this.CheckAndLoadAssembly(assemblyPath, out assembly);
            if (preInstrumentationResult != InstrumentationResult.OK)
            {
                return preInstrumentationResult;
            }

            bool instrumented = false;
            List<IInstrumenter> instrumenters = new List<IInstrumenter>();

            foreach (var module in assembly.Modules)
            {
                bool isMixedMode = (module.Attributes & ModuleAttributes.ILOnly) == 0;
                if (isMixedMode)
                {
                    return InstrumentationResult.SKIPPED_MixedModeAssembly;
                }

                //instrumenters.Add(new FieldUseInstrumenter(module, this.srtRuntimeModule));
                var async_instrumenter = new AsyncInstrumenter();
                instrumenters.Add(new InstrumenterV2(module, this.srtRuntimeModule));

                foreach (var type in module.GetAllTypes())
                {
                    var allMethods = type.Methods.Where(x => x.HasBody);

                    bool implementsBlacklistedInterface = type.Interfaces.Any(x => Constants.BlackListInterface.Contains(x.InterfaceType.FullName));
                    if (implementsBlacklistedInterface)
                    {
                        // async_instrumenter.Instrument(allMethods);
                        continue;
                    }

                    foreach (IInstrumenter instrumenter in instrumenters)
                    {
                        instrumented |= instrumenter.Instrument(allMethods);
                    }
                }
            }

            try
            {
                assembly.Write(new WriterParameters() { WriteSymbols = this.PdbExists(assemblyPath) });
            }
            catch (Exception e)
            {
                return InstrumentationResult.ERROR_Other;
            }

            assembly.Dispose();
            return instrumented ? InstrumentationResult.OK : InstrumentationResult.SKIPPED_NothingToInstrument;
        }

        private string GetPdbPath(string assemblyPath)
        {
            string pdbPath = Path.Combine(Path.GetDirectoryName(assemblyPath), Path.GetFileNameWithoutExtension(assemblyPath) + ".pdb");
            return pdbPath;
        }

        private bool PdbExists(string assemblyPath)
        {
            return false;
            //return File.Exists(this.GetPdbPath(assemblyPath));
        }

        private InstrumentationResult CheckAndLoadAssembly(string assemblyPath, out AssemblyDefinition assembly)
        {
            assembly = null;
            string assemblyName = Path.GetFileName(assemblyPath);

            string assemblyNameWithoutExtension = Path.GetFileNameWithoutExtension(assemblyPath);

            if (Constants.DependentAssemblies.Contains(assemblyNameWithoutExtension))
            {
                return InstrumentationResult.SKIPPED_DependentAseembly;
            }
            if (!File.Exists(assemblyPath))
            {
                return InstrumentationResult.ERROR_FileNotFound;
            }


            try
            {
                assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadWrite = true, ReadSymbols = this.PdbExists(assemblyPath) });
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot load assembly " + assemblyName);
                //Console.Error.WriteLine(e);
                assembly = null;
            }

            if (assembly == null)
            {
                return InstrumentationResult.ERROR_CannotLoadAssembly;
            }

            /*
            if ((assembly.MainModule.Attributes & ModuleAttributes.StrongNameSigned) != 0)
            {
                assembly.Dispose();
                return InstrumentationResult.SKIPPED_SignedAssembly;
            }
            */

            bool alreadyInstrumented = assembly.MainModule.AssemblyReferences.Any(x => x.Name == Constants.RuntimeLibrary);
            if (alreadyInstrumented)
            {
                assembly.Dispose();
                return InstrumentationResult.SKIPPED_AlreadyInstrumented;
            }

            return InstrumentationResult.OK;
        }

    }
}
