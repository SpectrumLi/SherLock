// <copyright file="ObjectId.cs" company="Microsoft Research">
// Copyright (c) Microsoft Research. All rights reserved.
// </copyright>

namespace TorchLiteRuntime
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Class ObjectId.
    /// </summary>
    public static class ObjectId
    {
        /// <summary>
        /// The ids
        /// </summary>
        private static readonly ConditionalWeakTable<object, RefId> _ids = new ConditionalWeakTable<object, RefId>();

        /// <summary>
        /// Gets the reference identifier.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>Guid.</returns>
        public static Guid GetRefId(object obj)
        {
            if (obj is null)
            {
                return default(Guid);
            }

            return _ids.GetOrCreateValue(obj).Id;
        }

        /// <summary>
        /// Class RefId.
        /// </summary>
        private class RefId
        {
            /// <summary>
            /// Gets the identifier.
            /// </summary>
            /// <value>The identifier.</value>
            public Guid Id { get; } = Guid.NewGuid();
        }
    }
}
