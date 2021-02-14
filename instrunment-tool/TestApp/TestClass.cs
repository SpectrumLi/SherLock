using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestApp
{
    public class TestClass
    {
        List<int> instanceList;
        static List<int> staticList;

        int instanceInt;
        static int staticInt;

        public bool flag;

        public TestClass()
        {
            instanceList = new List<int>() { 1, 2, 3};
            staticList = new List<int>() { 1, 2, 3};
            flag = true;
        }

        public void UseFields()
        {

            //Log(this, "instanceList");
            staticList.AddRange(instanceList);
            //staticList = null;
            instanceList = staticList ?? new List<int>() { 1, 2, 3 };
            //instanceList = new List<int>() { 1, 2, 3};
            ConcurrentAccessToInstanceList(false);
            instanceInt = 10;
            staticInt = 15;
        }

        public void UseFieldsInAnotherClass(AnotherClass ac)
        {
            ac.InstanceDict = ProduceNewDict(3);
            ac.InstanceDict["hello"] = "world";
            ac.InstanceDict = null;
            AnotherClass.StaticDict = ProduceNewDict(5);
            AnotherClass.StaticDict["Hello"] = "World";
            AnotherClass.StaticDict = null;

            ac.InstanceInt = 10;
            AnotherClass.StaticInt = 15;

            instanceInt = (int)AnotherClass.Square(15);
        }

        private static Dictionary<string, string> ProduceNewDict(int arg)
        {
            return new Dictionary<string, string>();
        }

        private void ConcurrentAccessToInstanceList(bool causeNullRefException)
        {
            var task1 = Task.Run(() => { if (!causeNullRefException) Task.Delay(1000).Wait(); instanceList = null; });
            var task2 = Task.Run(() => { if (causeNullRefException) Task.Delay(1000).Wait(); instanceList.Add(10); });
            var tasks = new Task[] { task1, task2};
            Task.WaitAll(tasks);
        }

        private static void Log(object instance, string fieldName)
        {
            Console.WriteLine($"{instance}\t{fieldName}");
        }
    }

    public class AnotherClass
    {
        public Dictionary<string, string> InstanceDict = new Dictionary<string, string>();
        public static Dictionary<string, string> StaticDict = new Dictionary<string, string>();
        public int InstanceInt;
        public static int StaticInt;

        public static object Square(int x)
        {
            return x * x;
        }
    }
}
