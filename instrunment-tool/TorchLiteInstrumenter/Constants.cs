using System.Collections.Generic;

namespace TorchLiteInstrumenter
{
    public class Constants
    {
        /// <summary>
        /// Gets runtime library name.
        /// </summary>
        public static string RuntimeLibrary => "TorchLiteRuntime";
        /// <summary>
        /// Gets runtime library file name.
        /// </summary>
        public static string RuntimeLibraryFile => RuntimeLibrary + ".dll";

        /// <summary>
        ///  Gets list of dependent assemblies that need to be copied to instrumented assemblies.
        /// </summary>
        public static HashSet<string> DependentAssemblies => new HashSet<string>() 
        { 
            RuntimeLibrary,
            "Microsoft.Torch.Log4Net4Torch"
        };

        /// <summary>
        /// Gets instrumenter file name.
        /// </summary>
        public static string InstrumenterFile => "TorchLiteInstrumenter.exe";

        /// <summary>
        /// Gets the list of interfaces whose implementation types are not instrumented
        /// </summary>
        public static HashSet<string> BlackListInterface => new HashSet<string>()
        {
            "System.Runtime.CompilerServices.IAsyncStateMachine",
        };
        public static List<string> MethodPrefixBlackList { get; } = new List<string>()
        {
            "Rhino.Mocks",
        };
    }
}
