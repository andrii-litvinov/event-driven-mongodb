using System;

namespace Common
{
    public class LazyAction : Lazy<object>
    {
        public LazyAction(Action action) : base(() => Invoke(action))
        {
        }

        public void Invoke()
        {
            // ReSharper disable once UnusedVariable
            var value = Value;
        }

        private static object Invoke(Action action)
        {
            action.Invoke();
            return null;
        }
    }
}