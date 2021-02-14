using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        public static volatile int x = 10;
        static void Main(string[] args)
        {
            if (x == 10)
                x = 3;


            Task t = Task.Run(() => {
                TestClass tc2 = new TestClass();
                tc2.UseFields();
                Thread.Sleep(100);
                tc2.UseFieldsInAnotherClass(new AnotherClass());
            });

            TestClass tc = new TestClass();
            tc.UseFields();
            tc.UseFieldsInAnotherClass(new AnotherClass());
            t.Wait();

            // create object via newobj
            List<int> list = new List<int>(10);
            list.Add(10);

            //List<int> list2 = new List<int>();
            //list2.Add(10);

            //DateTime now = DateTime.Now;
            //Guid guid = Guid.NewGuid();
        }
    }
}
