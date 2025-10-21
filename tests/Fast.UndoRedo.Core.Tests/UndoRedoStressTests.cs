using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    public class UndoRedoStressTests
    {
        [Fact(Skip = "Long running stress test, enable manually when needed")]
        public void UndoRedo_MultiThreaded_StressTest()
        {
            var svc = new UndoRedoService();
            var obj = new DummyNotify();
            svc.Attach(obj);

            int threads = 8;
            int iterations = 1000;
            var errors = 0;
            var done = new ManualResetEvent(false);
            int remaining = threads;

            void Worker()
            {
                try
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        obj.Name = "v" + i;
                        svc.Undo();
                        svc.Redo();
                    }
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                }
                if (Interlocked.Decrement(ref remaining) == 0)
                    done.Set();
            }

            for (int t = 0; t < threads; t++)
                ThreadPool.QueueUserWorkItem(_ => Worker());

            Assert.True(done.WaitOne(TimeSpan.FromSeconds(10)), "Threads should complete");
            Assert.Equal(0, errors);
        }

        private class DummyNotify : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private string _name;
            public string Name { get => _name; set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
        }
    }
}
