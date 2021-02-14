// <copyright file="FieldInterceptor.cs" company="Microsoft Research">
// Copyright (c) Microsoft Research. All rights reserved.
// </copyright>

namespace TorchLiteRuntime
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Implements the field usage callbacks.
    /// </summary>
    public class CallbacksV1
    {
        /// <summary>
        /// A file logger to write all field usage information.
        /// </summary>
        private static readonly FileLogger Logger = new FileLogger("FieldUsage.log");

        /// <summary>
        /// A dictionary to map field value to field name.
        /// </summary>
        private static readonly Dictionary<Guid, string> FieldNameDict = new Dictionary<Guid, string>();

        /// <summary>
        /// Callback for instance field write event.
        /// IL instrumentation uses the specific signature, so don't change it.
        /// </summary>
        /// <param name="parentObject">Object containing the field.</param>
        /// <param name="currentValue">Field value before the write.</param>
        /// <param name="newValue">Field value after the write.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="caller">Method writing the field.</param>
        /// <param name="ilOffset">ILOffset of the write operation.</param>
        /// <returns>Field value after the write. (This simplifies IL instrumentation.)</returns>
        public static object BeforeFieldWrite(object parentObject, object currentValue, object newValue, string fieldName, string caller, int ilOffset)
        {
            string uniqueFieldName = GetUniqueFieldId(parentObject, fieldName);
            Guid currValueId = ObjectId.GetRefId(currentValue);
            Guid newValueId = ObjectId.GetRefId(newValue);
            FieldNameDict[currValueId] = uniqueFieldName;

            Logger.Log($"FieldWrite\t{uniqueFieldName}\t{currValueId}\t{newValueId}\t{caller}\t{ilOffset}");

            return newValue;
        }

        /// <summary>
        /// Callback for static field write event.
        /// IL instrumentation uses the specific signature, so don't change it.
        /// </summary>
        /// <param name="newValue">Field value after the write.</param>
        /// <param name="currentValue">Field value before the write.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="caller">Method writing the field.</param>
        /// <param name="ilOffset">ILOffset of the write operation.</param>
        /// <returns>Field value after the write. (This simplifies IL instrumentation.)</returns>
        public static object BeforeStaticFieldWrite(object newValue, object currentValue, string fieldName, string caller, int ilOffset)
        {
            return BeforeFieldWrite(null, currentValue, newValue, fieldName, caller, ilOffset);
        }

        public static void AfterFieldWrite(object parentObject, object fieldValue, string fieldName, string caller, int ilOffset)
        {
            string uniqueFieldName = GetUniqueFieldId(parentObject, fieldName);
            Guid currValueId = ObjectId.GetRefId(fieldValue);
            FieldNameDict[currValueId] = uniqueFieldName;

            Logger.Log($"FieldWrite\t{uniqueFieldName}\t{currValueId}\t{caller}\t{ilOffset}");
        }


        /// <summary>
        /// Callback for field read event.
        /// IL instrumentation uses the specific signature, so don't change it.
        /// </summary>
        /// <param name="parentObject">Object containing the field.</param>
        /// <param name="fieldValue">Field value.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="caller">Method writing the field.</param>
        /// <param name="ilOffset">ILOffset of the write operation.</param>
        /// <returns>Parent object. (This simplifies IL instrumentation.)</returns>
        public static object BeforeFieldRead(object parentObject, object fieldValue, string fieldName, string caller, int ilOffset)
        {
            string uniqueFieldName = GetUniqueFieldId(parentObject, fieldName);
            Guid objId = ObjectId.GetRefId(fieldValue);
            Logger.Log($"FieldRead\t{uniqueFieldName}\t{objId}\t{caller}\t{ilOffset}");
            return parentObject;
        }

        public static void BeforeMethodCall(object instance, string caller, int ilOffset, string callee)
        {
            var objId = ObjectId.GetRefId(instance);
            Logger.Log($"BeforeMethodCall\t{objId}\t{callee}\t{caller}\t{ilOffset}");
        }

        private static string GetUniqueFieldId(object parentObject, string fieldName)
        {
            Guid parentObjId = ObjectId.GetRefId(parentObject);
            return $"{parentObjId}_{fieldName}";
        }
    }
}
