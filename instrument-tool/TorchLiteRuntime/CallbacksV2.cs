namespace TorchLiteRuntime
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;

    public class CallbacksV2
    {
        private static readonly Log4NetLogger Logger;
        private static readonly Dictionary<Guid, string> FieldNameDict = new Dictionary<Guid, string>();

        public static List<string> potentialrels = new List<string>();
        private static ConcurrentDictionary<int, string> delaycredit = new ConcurrentDictionary<int, string>();

        static CallbacksV2()
        {
            // load the result file automatically.

            string f = @"E:\TSVD\Benchmarks\config\rel_vars.lp";
            LoadRelFile(f);
            Logger = new Log4NetLogger("Runtime.log");
            /*
            potentialrels.Add("Call|System.Threading.Tasks.Dataflow.DataflowBlock::Post");
            potentialrels.Add("Call|System.Threading.Tasks.Dataflow.ActionBlock::Post");
            potentialrels.Add("Call|ExtensionMethods::ToEpoch");
            potentialrels.Add("Call|statsd.net_Tests.Infrastructure.MockTimeWindowService::GetTimeWindow-End");
            potentialrels.Add("Call|statsd.net.shared.Messages.StatsdMessageFactory::ParseMessage-End");
            potentialrels.Add("Call|System.Threading.Monitor::Exit");
            potentialrels.Add("Call|statsd.net.Framework.TimedLatencyPercentileAggregatorBlockFactory/c__DisplayClass0_1::b__3-End");
            */
        }

        public static void BeforeFieldWrite(object parentObject, string fieldName, object currentValue, object newValue, string caller, int ilOffset)
        {
            string uniqueFieldName = GetUniqueFieldId(parentObject, fieldName);
            // Guid currValueId = ObjectId.GetRefId(currentValue);
            // FieldNameDict[currValueId] = uniqueFieldName;
            ProcessInstrumentationPoint(DateTime.Now, uniqueFieldName, "Write", fieldName, caller, ilOffset);
        }

        public static void BeforeFieldRead(object parentObject, string fieldName, object fieldValue, string caller, int ilOffset)
        {
            string uniqueFieldName = GetUniqueFieldId(parentObject, fieldName);
            ProcessInstrumentationPoint(DateTime.Now, uniqueFieldName, "Read", fieldName, caller, ilOffset);
        }

        public static void BeforeMethodCall(object instance, string caller, int ilOffset, string callee)
        {
            var objId = ObjectId.GetRefId(instance);
            ProcessInstrumentationPoint(DateTime.Now, objId.ToString(), "Call", callee, caller, ilOffset);
        }

        public static void AfterInstructionCall(object instance, string caller, int ilOffset, string callee)
        {
            
            int tid = Thread.CurrentThread.ManagedThreadId;
            DateTime hdt = HighResolutionDateTime.UtcNow;
            string s = hdt.Ticks + "|" + tid + "|null|Finish|null|" + callee + "_" + ilOffset;
            Logger.PushBuffer(s);
            
        }

        private static void ProcessInstrumentationPoint(DateTime dateTime, string objid, string optype, string oprand, string md, int il)
        {
            int tid = Thread.CurrentThread.ManagedThreadId;

            oprand = oprand.Replace("|", string.Empty);
            md     = md.Replace("|", string.Empty);

            var simplename = SimpleName(oprand);
            string variable = optype + "|" + simplename;

            if (potentialrels.Contains(variable))
            {
                ExecuteDelay(tid, "Before" , variable);
                delaycredit[tid] = variable;
            }
            else
            {
                if (delaycredit.ContainsKey(tid) && (delaycredit[tid].Length > 0))
                {
                    ExecuteDelay(tid, "After", variable);
                    delaycredit[tid] = string.Empty;
                }
            }

            DateTime hdt = HighResolutionDateTime.UtcNow;
            // write the log entry to buffer
            string s = hdt.Ticks + "|" + tid + "|" + objid + "|"
                + optype + "|"
                + oprand + "|"
                + md + "_" + il.ToString();
            Logger.PushBuffer(s);
            //Logger.PushBuffer("Load Rels size " + potentialrels.Count);
            return;
        }

        private static void ExecuteDelay(int tid, string t, string s)
        {
            int delayduration = 100;
            try
            {
                DateTime hdt = HighResolutionDateTime.UtcNow;
                Thread.Sleep(delayduration);
                s = hdt.Ticks + "|" + tid + "|null|Sleep|null|null_0";
                Logger.PushBuffer(s);
            }
            catch (Exception)
            {

            }
        }

        private static string SimpleName(string t)
        {
            if (t.Contains("__"))
            {
                return t;
            }

            string s = Regex.Replace(t, "<.*?>", string.Empty);
            s = s.Replace(@"`1", string.Empty);
            s = s.Replace(@"`2", string.Empty);
            return s;
        }

        private static string GetUniqueFieldId(object parentObject, string fieldName)
        {
            Guid parentObjId = ObjectId.GetRefId(parentObject);
            return $"{parentObjId}_{fieldName}";
        }

        private static void LoadRelFile(string f)
        {
            if (!File.Exists(f))
            {
                return;
            }

            StreamReader sr = new StreamReader(f);

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] ss = line.Split(' ');
                if (ss[2].Equals("False"))
                {
                    potentialrels.Add(ss[1]);
                }
            }
            Console.WriteLine("Load Rels size " + potentialrels.Count);
        }

        /*
        public static void AfterFieldWrite(object parentObject, string fieldName, object fieldValue, string caller, int ilOffset)
        {
            string uniqueFieldName = GetUniqueFieldId(parentObject, fieldName);
            Guid currValueId = ObjectId.GetRefId(fieldValue);
            FieldNameDict[currValueId] = uniqueFieldName;

        }
        */
    }
}
