using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Analyzer
{
    class LogEntry
    {
        public List<string> entries;
        public string tid;
        public int length;
        public string variable;

        public string location;

        public string content;
        public double duration;

        public LogEntry previous;
        public LogEntry next;

        public bool isSleep;
        public bool isFinish;
        public Dictionary<string, List<LogEntry>> sleepInsideDic;
        public LogEntry(string s)
        {
            this.content = s;
            //time | tid |objid | optype | oprand | location.
            this.entries = new List<string>(s.Split('|'));
            //if (this.entries[3].Contains("Call") && !this.entries[4].Contains("-Begin") && !this.entries[4].Contains("-End"))
            //    this.entries[4] += "-Begin";
            this.length = this.entries.Count;
            this.location = this.entries[5];
            this.previous = null;
            this.next = null;
            this.duration = -1;
            this.CheckSleep();
            this.CheckFinish();
            try
            {
                if (!this.isOverallBeginOrEnd())
                {
                    this.tid = this.entries[1];
                    this.sleepInsideDic = new Dictionary<string, List<LogEntry>>();
                    if (!this.isSleep)
                    {
                        this.variable = this.entries[3] + "|" + SimplfyName(this.entries[4]);
                        /*
                        if ( this.entries[4].Contains("b__0-Begin"))
                        {
                            Console.WriteLine(this.entries[4]);
                            Console.WriteLine("    " + this.variable);
                        }
                        */
                    }
                }
            }catch(Exception e)
            {
                Console.WriteLine("Error when parsing " + s);
                throw e;
            }
        }

        public bool CheckSleep()
        {
            this.isSleep = this.content.Contains("|Sleep|");
            return this.isSleep;
        }
        public bool CheckFinish()
        {
            this.isFinish = this.content.Contains("|Finish|");
            return this.isFinish;
        }
        public bool isOverallBeginOrEnd()
        {
            return this.content.StartsWith("#INFO");
        }

        public string SimplfyName(string t)
        {
            string s = Regex.Replace(t, @"<.*?>", "");
            s = s.Replace(@"`1", "");
            s = s.Replace(@"`2", "");
            return s;
        }

        public string DropTid()
        {
            string s = entries[0];
            for (int i = 2; i < this.length; i++)
            {
                s = s + "|" + entries[i];
            }
            return s;
        }

        public void AppendTimeGap(string t_gap)
        {
            this.entries.Add(t_gap);
            this.duration = double.Parse(t_gap);
            this.length = this.entries.Count;
            // Console.WriteLine("Append time gap: " + this.DropTid()  );
        }

        public Int64 GetTimestamp()
        {
            return Int64.Parse(this.entries[0]);
        }
    }
}
