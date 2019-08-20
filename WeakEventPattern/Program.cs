namespace WeakEventPattern
{
    using System;
    using System.Windows;

    class TestGC
    {
        public TestGC()
        {
            Console.WriteLine("TestGC class initialized.");
        }

        ~TestGC()
        {
            Console.WriteLine("TestGC class destruct.");
        }
    }

    class OriginEventSource
    {
        public event EventHandler OriginEvent;

        public OriginEventSource()
        {
        }

        public void Raise()
        {
            OriginEvent?.Invoke(this, new EventArgs());
        }

        ~OriginEventSource()
        {
            Console.WriteLine("OriginEventSource destruct.");
        }
    }

    class OriginEventListener
    {
        public void Subscribe(OriginEventSource source)
        {
            source.OriginEvent += this.Source_OriginEvent;
        }

        private void Source_OriginEvent(object sender, EventArgs e)
        {
            Console.WriteLine("OriginEvent listened by OriginEventListener");
        }

        ~OriginEventListener()
        {
            Console.WriteLine("OriginEventListener destruct.");
        }
    }

    class WeakEventListener
    {
        public WeakEventListener()
        {
        }

        public void Subscribe(OriginEventSource source)
        {
            WeakEventManager<OriginEventSource, EventArgs>.AddHandler(
                source, "OriginEvent", OnOriginEvent);
        }

        private void OnOriginEvent(object sender, EventArgs e)
        {
            Console.WriteLine("OriginEvent listened by WeakEventListener");

        }

        ~WeakEventListener()
        {
            Console.WriteLine("WeakEventListener destruct.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Program program = new Program();
            //program.TestTriggerGC(); 
            //program.TestOriginEventPattern();

            program.TestWeakEventPattern();
        }

        private void TestOriginEventPattern()
        {
            OriginEventSource source = new OriginEventSource();
            OriginEventListener listener = new OriginEventListener();
            listener.Subscribe(source);

            listener = null;

            this.TriggerGC();
            Console.WriteLine("setted listener to null and invoke TriggerGC()");
            source = null;
            this.TriggerGC();
            Console.WriteLine("setted source to null and invoke TriggerGC()");

            WaitForAnyKey();
        }

        private void TestWeakEventPattern()
        {
            WeakEventListener listener = new WeakEventListener();
            OriginEventSource source = new OriginEventSource();
            listener.Subscribe(source);
            source.Raise();
            listener = null;
            this.TriggerGC();
            source = null;
            this.TriggerGC();
            this.WaitForAnyKey();
        }

        private void TestTriggerGC()
        {
            TestGC tgc = new TestGC(); // 实例化类
            tgc = null; // 类型不在需要

            this.TriggerGC(); // 调用GC, 回收不再被引用的类型

            WaitForAnyKey();
        }

        private void WaitForAnyKey()
        {
            ConsoleColor originColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Press any key to continue...");
            Console.ForegroundColor = originColor;
            Console.ReadLine();
            Console.Clear();
        }

        private void TriggerGC()
        {
            GC.Collect(); //触发.net的CLR垃圾收集器，对于负责清理不再使用的对象，和那些类中没有终结器（即c#中的析构函数）的对象
            GC.WaitForPendingFinalizers(); // 等待其他对象的终结器执行；我们需要这样做，因为，这样就能使用终结器方法去追踪我们的对象在什么时候被收集的
            GC.Collect(); // 确保新生成的对象也被清理了
        }
    }
}
