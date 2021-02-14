using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
{
    class Utils
    {

        public static string FindTestName(string runtime_f)
        {
            string result_f = runtime_f.Replace("Runtime.log", "result.log");
            try
            {
                StreamReader sr = new StreamReader(result_f);

                string line = string.Empty;
                int flag = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    //if (flag > 0) return line.Split(' ')[3];
                    //if (line.StartsWith("A total of")) flag = 1;
                    if (line.Contains(" √ ") || line.Contains(" X "))
                        return line.Split(' ')[3];
                }
            }
            catch (Exception)
            {

            }
            return string.Empty;
        }

        public static void writeThreadBegin(StreamWriter sw, string s)
        {
            string[] ss = s.Split('|');
            string st = (Int64.Parse(ss[0]) - 1).ToString();
            st += "|null|Call|SherLock::Thread.Start|";
            string[] locs = ss[4].Split('_');
            string newloc = locs[0];
            for (int i = 1; i < locs.Length - 1; i++)
                newloc += "_" + locs[i];
            newloc += "_" + (Int64.Parse(locs[locs.Length - 1]) - 1).ToString();
            st = st + newloc;
            sw.WriteLine(st);
            return;
        }

        public static void writeThreadEnd(StreamWriter sw, string s)
        {
            try
            {
                string[] ss = s.Split('|');
                string st = (Int64.Parse(ss[0]) + 1).ToString();
                st += "|null|Call|SherLock::Thread.End|";
                string[] locs = ss[4].Split('_');
                string newloc = locs[0];
                for (int i = 1; i < locs.Length - 1; i++)
                    newloc += "_" + locs[i];
                try
                {
                    newloc += "_" + (Int64.Parse(locs[locs.Length - 1]) + 1).ToString();
                }
                catch (Exception)
                {
                    newloc += locs[locs.Length - 1] + "x";
                }
                st = st + newloc;
                sw.WriteLine(st);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error when processing " + s);
                throw e;
            }
            return;
        }

        public static Int64 GetTimeGap(string st1, string st2)
        {
            //st1 is the previous operation, st2 is the later operation
            string t1 = st1.Split('|')[0];
            string t2 = st2.Split('|')[0];
            return Int64.Parse(t2) - Int64.Parse(t1) - 1;
        }

        public static LogEntry GetPreviousNonsleepEntry(List<LogEntry> list)
        {
            for(int i = list.Count-1; i>=0; i--)
            {
                if (!list[i].isSleep)
                    return list[i];
            }
            return null;
        }

        public static void UpdateDictByOne(Dictionary<string, int> dict, string key)
        {
            if (!dict.ContainsKey(key)) dict[key] = 0;
            dict[key]++;
        }

        public static void WriteLitelog(string dir,Dictionary<string, List<LogEntry>> threadlog)
        {
            foreach (var tid in threadlog)
            {
                if (tid.Value.Count > 0)
                {
                    StreamWriter sw = new StreamWriter(Path.Combine(dir, tid.Key + ".litelog"));
                    
                    for (int i = 0; i < tid.Value.Count - 1; i++)
                    {                   
                        if (tid.Value[i].length != 7) continue; 
                        sw.WriteLine(tid.Value[i].DropTid());
                    }
                    string st = tid.Value[tid.Value.Count - 1].DropTid();
                    if (tid.Value[tid.Value.Count - 1].length != 7)
                        st = st + "|1";
                    sw.WriteLine(st);
                    sw.Close();
                }
            }

        }

        public static Dictionary<string, List<LogEntry>> FindInside(LogEntry l, Dictionary<string, List<LogEntry>>threadlog)
        {
            Dictionary<string, List<LogEntry>> dict = new Dictionary<string, List<LogEntry>>();
            // Console.WriteLine(l + "duration " + l.duration);

            foreach(var p in threadlog)
            {
                var list = FindEntriesByRange(p.Value, l.GetTimestamp(), Convert.ToInt64(l.duration));
                if (list.Count > 0)
                    dict[p.Key] = list;
            }
            return dict;
        }

        public static List<LogEntry> FindEntriesByRange(List<LogEntry> list, Int64 start, Int64 duration)
        {
            List<LogEntry> ans = new List<LogEntry>();
            foreach(var log in list)
            {
                if ((start <= log.GetTimestamp()) && (start + duration >= log.GetTimestamp()))
                    ans.Add(log);
            }
            return ans;
        }

        public static bool CheckCorrectAcq(LogEntry prelog, LogEntry nextlog, out string acq )
        {
            acq = string.Empty;
            var predict = prelog.sleepInsideDic;
            foreach(var p in predict)
            {
                if ((p.Value.Count>0) && (SyncDict.potentialacqs.Contains(p.Value[p.Value.Count-1].variable)))
                {
                    acq = p.Value[p.Value.Count - 1].variable;
                    return true;
                }
            }
            var nextdict = nextlog.sleepInsideDic;
            foreach(var p in nextdict)
            {
                if ((p.Value.Count > 0) && (SyncDict.potentialacqs.Contains(p.Value[0].variable)))
                {
                    acq = p.Value[0].variable;
                    return true;
                }
            }
            return false;
        }

        public static bool CheckExistsAcq(LogEntry prelog, LogEntry nextlog)
        {
            // should be more accurate here [the thread the contains the racing object]
            foreach (var p in prelog.sleepInsideDic)
            {
                foreach (var log in p.Value)
                    if (SyncDict.potentialacqs.Contains(log.variable))
                        return true;
            }
            foreach (var p in nextlog.sleepInsideDic)
            {
                foreach (var log in p.Value)
                    if (SyncDict.potentialacqs.Contains(log.variable))
                        return true;
            }
            return false;
        }

    }
}
