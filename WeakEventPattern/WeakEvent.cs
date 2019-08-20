namespace WeakEventPattern
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class WeakEvent<TEventArgs> where TEventArgs:EventArgs
    {
        public delegate void Func(TEventArgs e);
        private static object[] ARGS = new object[1];

        private class Unit
        {
            private readonly WeakReference reference;
            private readonly MethodInfo method;
            private bool isStatic;

            public bool IsDead
            {
                get
                {
                    return !this.isStatic && !this.reference.IsAlive;
                }
            }

            public Unit(Func callback)
            {
                this.isStatic = callback.Target == null;
                this.reference = new WeakReference(callback.Target);
                this.method = callback.Method;
            }

            public bool Equals(Func callback)
            {
                return this.reference.Target == callback.Target && this.method == callback.Method;
            }

            public void Invoke(object[] args)
            {
                this.method.Invoke(this.reference.Target, args);
            }
        }

        private List<Unit> list = new List<Unit>();

        public int Count
        {
            get
            {
                return this.list.Count;
            }
        }

        public void Add(Func callback)
        {
            this.list.Add(new Unit(callback));
        }

        public void Remove(Func callback)
        {
            for (int i = this.list.Count - 1; i > -1; i--)
            {
                if (this.list[i].Equals(callback))
                {
                    this.list.RemoveAt(i);
                }
            }
        }

        public void Invoke(TEventArgs args = null)
        {
            ARGS[0] = args;

            for (int i = this.list.Count - 1; i > -1; i--)
            {
                if (this.list[i].IsDead)
                {
                    this.list.RemoveAt(i);
                }
                else
                {
                    this.list[i].Invoke(ARGS);
                }
            }
        }

        public void Clear()
        {
            this.list.Clear();
        }
    }
}
