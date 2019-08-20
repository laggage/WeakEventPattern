[TOC]

## C#弱事件模式

普通的C#事件模式, 存在内存泄漏问题, 下面通过实验代码来展现出问题.

为了展现出问题, 需要能够确定性析构, 下面的这段代码, 调用GC, 执行析构, 使我们能够在C#中手动的确定析构.

```
private void TriggerGC()
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
}
```

做一个简单的实验, 看一下上面的代码是否能够起作用
```
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

class Program
{
    static void Main(string[] args)
    {
        Program program = new Program();
        TestGC tgc = new TestGC();
        tgc = null;

        program.TriggerGC();
        Console.ReadLine();
    }

    private void TriggerGC()
    {
        GC.Collect(); //触发.net的CLR垃圾收集器，对于负责清理不再使用的对象，和那些类中没有终结器（即c#中的析构函数）的对象
        GC.WaitForPendingFinalizers(); // 等待其他对象的终结器执行；我们需要这样做，因为，这样就能使用终结器方法去追踪我们的对象在什么时候被收集的
        GC.Collect(); // 确保新生成的对象也被清理了
    }
}
```

运行程序, 结果如图:
![](https://img2018.cnblogs.com/blog/1596066/201908/1596066-20190818124444275-450303157.png)

然后把上面的代码中, `program.TriggerGC();` 这句去除, 程序输出如图:
![](https://img2018.cnblogs.com/blog/1596066/201908/1596066-20190818124559537-388316283.png)

说明TriggerGC这个方法生效了, 成功调用了GC, 回收了被我们解除引用的类 `TestGC` , 调用它的析构函数.

## 原生事件模式测试
建立两个类用来测试
```
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
```

测试代码:
```
Program program = new Program();

OriginEventSource source = new OriginEventSource();
OriginEventListener listener = new OriginEventListener();
listener.Subscribe(source);
listener = null;

program.TriggerGC();
Console.WriteLine("setted listener to null and invoke TriggerGC()");
source = null;
program.TriggerGC();
Console.WriteLine("setted source to null and invoke TriggerGC()");
Console.ReadLine();
```
运行结果:
![](https://img2018.cnblogs.com/blog/1596066/201908/1596066-20190818130437089-172130343.png)

### 发现问题
第一次调用 `program.TriggerGC();` 时, `listener` 已经被设置为null, 然而, `listener` 的析构函数没有被调用.
 然后第二次调用 `program.TriggerGC();`, 同时将 `source` 设置为null, 此时, `listener` `source` , 的析构函数都被调用了, listener这才被GC成功回收了. 
问题就在于第一次调用 `program.TriggerGC();` 时 `listener` 已经被设置成null了, GC应该回收`listener` 这个对象才对, 但是由于 `listener` 订阅了 `source` 中的一个事件, 使得 `source` 保持了对 `listener` 的引用, 所以这时, 尽管 `listener` 已经被我们设置为 null, 而GC 却依然无法回收 `listener` , 直到我们将 `source` 也设置为 null, 此时没有任何指向 `listener` 的引用了, 这时GC才成功回收了 `listener` `source` 对象.

## 解决问题

微软给出了这个问题的解决方案, 那就是 **弱事件模式**.
看代码:
```
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

static void Main(string[] args)
{
    Program program = new Program();

    WeakEventListener listener = new WeakEventListener();
    OriginEventSource source = new OriginEventSource();
    listener.Subscribe(source);
    source.Raise();

    listener = null;
    program.TriggerGC();

    program.WaitForAnyKey();
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
```

使用了 `WeakEventManager<TEventSource,TEventArgs>` 这个泛型类的 `AddHandler` 方法来订阅事件. 这个类位于WindowsBase程序集下的 `System.Windows;` 名称空间中.

程序执行的结果:
![](https://img2018.cnblogs.com/blog/1596066/201908/1596066-20190818211022701-802309729.png)

此时, 尽管 `listener` 订阅了 `source` 中的事件, 但当 `listener` 被置为null时, `listener` 指向的对象依然可以成功的被GC回收, 这就说明弱事件起作用了!



## 参考
- [博客园-C#中的 .NET 弱事件模式](https://www.cnblogs.com/rinack/p/3668041.html)
- [知乎文章-C#之弱事件（Weak Event）的实现](https://zhuanlan.zhihu.com/p/33870370)
