using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Spiffy
{
    public class Event
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal Event()
        {
            Component = Operation = "[Unknown]";

            var stackFrame = new StackFrame(2, false);
            var method = stackFrame.GetMethod();
            if (method != null)
            {
                var declaringType = method.DeclaringType;
                if (declaringType != null)
                {
                    Component = declaringType.Name;
                }
                Operation = method.Name;
            }
        }

        internal Event(string component, string operation)
        {
            Component = component;
            Operation = operation;
        }

        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public object this[string key]
        {
            get { return _values[key]; }
            set { _values[key] = value; }
        }

        public string Component { get; private set; }
        public string Operation { get; private set; }
    }
}