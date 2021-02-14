using System;
using System.IO;

namespace TorchLiteInstrumenter
{
    class InstrumentationHelper
    {
        public static string MethodSignatureWithoutReturnType(string fullName)
        {
            string[] tokens = fullName.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 1)
            {
                return tokens[1].Replace("::", ".").Replace("get_Item", "Item.get").Replace("set_Item", "Item.set");
            }
            else
            {
                return fullName;
            }
        }

        public static string GetTypeName(string fullName)
        {
            string[] tokens = fullName.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 1)
            {
                return tokens[1].Replace("::", ".").Replace("get_Item", "Item.get").Replace("set_Item", "Item.set");
            }
            else
            {
                return fullName;
            }
        }

        /// <summary>
        /// Copy dependent assemblies.
        /// </summary>
        /// <param name="applicationDirectory">Application directory.</param>
        public static void CopyDependentAssemblies(string applicationDirectory)
        {
            string instrumenterDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            foreach (string dependentAssemblyName in Constants.DependentAssemblies)
            {
                string dllPath = Path.Combine(instrumenterDirectory, dependentAssemblyName + ".dll");
                string pdbPath = Path.Combine(instrumenterDirectory, dependentAssemblyName + ".pdb");
                if (File.Exists(dllPath))
                {
                    File.Copy(dllPath, Path.Combine(applicationDirectory, dependentAssemblyName + ".dll"), true);
                    if (File.Exists(pdbPath))
                    {
                        File.Copy(pdbPath, Path.Combine(applicationDirectory, dependentAssemblyName + ".pdb"), true);
                    }
                }
            }
        }

    }
}
