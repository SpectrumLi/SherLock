using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
{
    class Program
    {

        public static int sum = 0;
        public static int sum_mt = 0;

        public static List<string> locations = new List<string>();
        public static List<string> apis = new List<string>();

        public static Dictionary<string, int> VerifiedResults = new Dictionary<string, int>();
        public static Dictionary<string, int> FalseResults    = new Dictionary<string, int>();
        public static Dictionary<string, int> UnsureResults = new Dictionary<string, int>();
        public static Dictionary<string, int> CheckedResults  = new Dictionary<string, int>();
        public static Dictionary<string, int> EncounterResults = new Dictionary<string, int>();

        public static Dictionary<string, LogEntry> LocationToMethodname = new Dictionary<string, LogEntry>();

        public static List<string> MTtests = new List<string>();
        public static List<string> checkedrels = new List<string>();

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Plese specify the input directory");
                return;
            }
            List<string> recordfs = getfiles(args[0], "Runtime.log");
            foreach (var f in recordfs)
            {
                processRecordFile(f);
            }
            Console.WriteLine("Total log entries : " + sum);
            Console.WriteLine("API number : " + apis.Count);
            Console.WriteLine("Location number : " + locations.Count);
            //PrintResults();
            //PrintMTUnitTests(args[0]);
            //PrintCheckedRels();
        }


        public static void processRecordFile(string f)
        {
            Console.WriteLine("Spliting " + f);
            string dir = Path.GetDirectoryName(f);
            Dictionary<string, List<LogEntry>> threadlog = new Dictionary<string, List<LogEntry>>();
            Dictionary<string, bool> flags = new Dictionary<string, bool>();
            StreamReader sr = new StreamReader(f);

            Dictionary<string, Stack<string>> threadfuncinvoke = new Dictionary<string, Stack<string>>();
            LogEntry last_entry = null;
            int ignoreEndSum = 0;
            int ignoreBeginSum = 0;
            Dictionary<string, bool> verify_flags = new Dictionary<string, bool>();
            string line = "";
            string preline = "";
            while ((line = sr.ReadLine()) != null)
            {
                preline = line;
                sum++;
                if (line.Length < 1) continue;
                try
                {
                    last_entry = new LogEntry(line);
                }
                catch (Exception)
                {
                    continue;
                }
                if (last_entry.isOverallBeginOrEnd())
                    continue;

                // thread ID
                string tid = last_entry.tid;
                if (!threadlog.ContainsKey(tid))
                {
                    threadlog[tid] = new List<LogEntry>();
                    threadfuncinvoke[tid] = new Stack<string>();
                    verify_flags[tid] = false;
                }

                if (last_entry.length < 6)
                {
                    continue;
                }

                /*
                if (SyncDict.potentialacqs.Contains(last_entry.variable) || SyncDict.potentialrels.Contains(last_entry.variable))
                {
                    if (!EncounterResults.ContainsKey(last_entry.variable)) EncounterResults[last_entry.variable] = 0;
                    EncounterResults[last_entry.variable]++;
                }
                */

                List<string> ss = last_entry.entries;              

                // compute the time gap here
                if (threadlog[tid].Count > 0)
                {
                    var entry = threadlog[tid][threadlog[tid].Count - 1];
                   
                    if (entry.length < 7)
                    {                        
                        Int64 t_gap = Utils.GetTimeGap(entry.content, last_entry.content);
                        threadlog[tid][threadlog[tid].Count - 1].AppendTimeGap(t_gap.ToString());
                    }
                }

                if(!last_entry.isFinish && !last_entry.isSleep && (!last_entry.variable.Contains("-Begin") || !last_entry.variable.Contains("-End")) )
                {
                    LocationToMethodname[last_entry.location] = last_entry;
                }

                if (last_entry.isFinish)
                {
                    //*
                    if (!LocationToMethodname.ContainsKey(last_entry.location))
                        continue;

                    var its = LocationToMethodname[last_entry.location].variable.Split('|');
                    if (!its[0].Contains("Call")) continue;
                    last_entry.entries[3] = "Call";
                    last_entry.entries[4] = its[1].Replace("-Begin","-End");
                    Int64 t_gap = Utils.GetTimeGap(LocationToMethodname[last_entry.location].content, last_entry.content);
                    last_entry.AppendTimeGap(t_gap.ToString());
                    //Console.WriteLine("Finishing " + LocationToMethodname[last_entry.location]);
                    //*//
                }



                // Remove the redundent log entries.
                //
                if (!last_entry.isSleep)
                {
                    if (!locations.Contains(ss[5]))
                        locations.Add(ss[5]);
                    var api = ss[3] + "_" + ss[4];
                    if (!apis.Contains(api))
                        apis.Add(api);

                    if (ss[4].Contains("-Begin"))
                    {
                        string funcname = ss[4].Split('-')[0];
                        var previous_nonsleep = Utils.GetPreviousNonsleepEntry(threadlog[tid]);
                        //if ((threadlog[tid].Count > 0) && threadlog[tid][threadlog[tid].Count - 1].content.Contains("Call|" + funcname + "|"))
                        if ((previous_nonsleep!= null) && previous_nonsleep.content.Contains("Call|" + funcname + "|"))
                        {
                            threadfuncinvoke[tid].Push(funcname);
                            ignoreBeginSum++;
                            continue;
                        }
                    }
                    if (ss[4].Contains("-End"))
                    {
                        string funcname = ss[4].Split('-')[0];
                        if ((threadfuncinvoke[tid].Count > 0) && threadfuncinvoke[tid].Peek().Equals(funcname))
                        {
                            threadfuncinvoke[tid].Pop();
                            ignoreEndSum++;
                            continue;
                        }
                    }
                }

                threadlog[tid].Add(last_entry);

            }

            if (threadlog.Keys.Count > 1)
            {
                ValidateResult(threadlog);
                sum_mt++;
                Utils.WriteLitelog(dir,threadlog);
                string ut = Utils.FindTestName(f);
                if ((ut.Length > 0) && !MTtests.Contains(ut))
                {
                    MTtests.Add(ut);
                    Console.WriteLine(ut + " is mttest");
                }
                else
                {
                    Console.Write(ut + " is drop because it is ");
                    if (MTtests.Contains(ut))
                        Console.WriteLine("repeated");
                }
            }

            if (!preline.StartsWith("#INFO") && (preline.Count()>0))
            {
                Console.WriteLine("last entry " + last_entry.DropTid());
                Console.WriteLine("Unfinished unit test");
            }

        }
     
        public static void ValidateResult(Dictionary<string, List<LogEntry>> threadlog)
        {
            // Console.WriteLine("Validating results");
            foreach(var p in threadlog)
            {
                var l = p.Value;
                Parallel.ForEach(Enumerable.Range(0, l.Count).Select(x => x),
                    (i) =>
                {
                    if (i > 0)
                        l[i].previous = l[i - 1];
                    if (i < l.Count - 1)
                        l[i].next = l[i + 1];
                    if (
                        (l[i].previous  !=  null) &&    l[i].previous.isSleep 
                        //&& (l[i].next      !=  null) &&    l[i].next.isSleep
                        )
                    {
                        // Console.WriteLine("Found Sleep " + l[i].DropTid());
                        //l[i].sleepInsideDic = Utils.FindInside(l[i], threadlog);
                        // Console.WriteLine("Found sleep concurrent with " + l[i].sleepInsideDic.Keys.Count + " threads");
                        lock (checkedrels)
                        {
                            if (!checkedrels.Contains(l[i].variable))
                                checkedrels.Add(l[i].variable);
                        }
                    }
                });
            }
            /*
            foreach(var p in threadlog)
            {
                foreach(var entry in p.Value)
                {
                    if ((entry.previous != null) && (entry.next != null) && entry.previous.isSleep && entry.next.isSleep)
                    {
                        
                        // List<string> pre = Utils.CheckAcq(entry.previous.sleepInsideDic);
                        // List<string> next = Utils.CheckAcq(entry.next.sleepInsideDic);
                        
                        Utils.UpdateDictByOne(CheckedResults, entry.variable);
                        string acq = string.Empty;

                        if (Utils.CheckCorrectAcq(entry.previous, entry.next, out acq))
                        {
                            Utils.UpdateDictByOne(VerifiedResults, entry.variable);
                            Utils.UpdateDictByOne(VerifiedResults, acq);
                        }
                        else if (Utils.CheckExistsAcq(entry.previous, entry.next))
                        {
                            Utils.UpdateDictByOne(FalseResults, entry.variable);
                        }
                        else
                        {
                            Utils.UpdateDictByOne(UnsureResults, entry.variable);
                        }
                    
                    }
                }
            }
            */
        }

        public static void PrintResults()
        {
            /*
            Console.WriteLine("Encounter results:");
            foreach (var p in EncounterResults)
            {
                Console.WriteLine("    " + p.Key + " " + p.Value);
            }
            */
            Console.WriteLine("Verifed results:");
            foreach (var p in VerifiedResults)
            {
                Console.WriteLine("    " + p.Key + " " + p.Value);
            }
            Console.WriteLine("False results:");
            foreach (var p in FalseResults)
            {
                Console.WriteLine("    " + p.Key + " " + p.Value);
            }
            Console.WriteLine("Untouched results:");
            foreach (var v in SyncDict.potentialacqs)
            {
                if (!VerifiedResults.ContainsKey(v) && !FalseResults.ContainsKey(v))
                    Console.WriteLine("    " + v);
            }
            foreach (var v in SyncDict.potentialrels)
            {
                if (!VerifiedResults.ContainsKey(v) && !FalseResults.ContainsKey(v))
                    Console.WriteLine("    " + v);
            }
            /*
            foreach (var p in CheckedResults)
            {
                Console.WriteLine("    " + p.Key + " " + p.Value);
            }
            */
            /*
            Console.WriteLine("Unsured results:");
            foreach (var p in UnsureResults)
            {
                Console.WriteLine("    " + p.Key + " " + p.Value);
            }
            */

        }

        public static void PrintCheckedRels()
        {
            Console.WriteLine("Checked Rels:");
            foreach (var p in checkedrels)
            {
                Console.WriteLine("    " + p);
            }
        }

        public static List<string> getfiles(string dir, string key)
        {
            List<string> ans = new List<string>();
            string[] fs = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            foreach (var f in fs)
            {
                if (f.Contains(key))
                    ans.Add(f);
            }
            return ans;
        }

        public static void PrintMTUnitTests(string dir)
        {
            string f = dir + @"\mttest.txt";
            StreamWriter sw = new StreamWriter(f);
            // Console.WriteLine("  MT Tests : ");
            foreach(var s in MTtests)
            {
                sw.WriteLine(s);
            }
            sw.Flush();
            sw.Close();
            Console.WriteLine("MTtests size [" + MTtests.Count + "] Expect[" + sum_mt + "]");
        }
    }
}

