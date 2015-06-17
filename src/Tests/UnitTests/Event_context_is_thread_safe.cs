using System.Collections.Generic;
using System.Threading.Tasks;
using Kekiri;
using Spiffy.Monitoring;

namespace UnitTests
{
    public class Event_context_is_thread_safe : ScenarioTest
    {
        readonly EventContext _eventContext = new EventContext();
        readonly List<Task> _tasks = new List<Task>();

        [Given]
        public void Multiple_concurrent_operations()
        {
            for (int i = 0; i < 500; i++)
            {
                var key = i.ToString();
                _tasks.Add(Task.Factory.StartNew(() => _eventContext[key] = "value"));
                _tasks.Add(Task.Factory.StartNew(() => _eventContext.Time(key)));
                _tasks.Add(Task.Factory.StartNew(() => _eventContext.Dispose()));
            }
        }

        [When]
        public void When_executing_in_parallel()
        {
            Task.WaitAll(_tasks.ToArray());
        }

        [Then]
        public void It_should_not_throw_an_error()
        {
        }
    }
}