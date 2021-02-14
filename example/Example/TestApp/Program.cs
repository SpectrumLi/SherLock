using System;
using System.Threading;

namespace TestApp
{
    class Program
    {
        int x;
        int y;
        public void update()
        {
            this.x = this.x * 2;
        }
        static void Main(string[] args)
        {
            Program p = new Program();
            p.x = 3;
            Thread thread1 = new Thread(p.update);
            thread1.Start();
            thread1.Join();
        }
    }
}
