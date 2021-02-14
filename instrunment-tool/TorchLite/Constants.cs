// <copyright file="Constants.cs" company="Microsoft Research">
// Copyright (c) Microsoft Research. All rights reserved.
// </copyright>

namespace TorchLite
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
        /// Gets instrumenter file name.
        /// </summary>
        public static string InstrumenterFile => "TorchLiteInstrumenter.exe";
    }
}
