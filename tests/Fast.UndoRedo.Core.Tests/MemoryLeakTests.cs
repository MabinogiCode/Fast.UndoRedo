using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    public class MemoryLeakTests
    {
        [Fact]
        public void Register_Unregister_AllowsGarbageCollection_ForTrackedObject()
        {
            var svc = new UndoRedoService();
            var tracker = new RegistrationTracker(svc);

            WeakReference weakRef = CreateAndRegister(tracker);

            // Force unregister and garbage collect
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // After GC, the object should be collected
            Assert.False(weakRef.IsAlive, "Tracked object should be collectible after unregister and GC");
        }

        private WeakReference CreateAndRegister(RegistrationTracker tracker)
        {
            var obj = new DummyNotify();
            tracker.Register(obj);
            tracker.Unregister(obj);
            var wr = new WeakReference(obj);
            // drop strong reference
            obj = null;
            return wr;
        }

        [Fact]
        public void Register_MultipleThreads_NoRaceOnCollectionSnapshots()
        {
            var svc = new UndoRedoService();
            var tracker = new RegistrationTracker(svc);

            const int threads = 8;
            var done = new ManualResetEvent(false);
            int remaining = threads;

            void Worker()
            {
                var local = new ObservableHolder();
                tracker.Register(local);
                // mutate collection to exercise snapshot logic
                local.Items.Add("x");
                tracker.Unregister(local);

                if (Interlocked.Decrement(ref remaining) == 0)
                {
                    done.Set();
                }
            }

            for (int i = 0; i < threads; i++)
            {
                ThreadPool.QueueUserWorkItem(_ => Worker());
            }

            bool signaled = done.WaitOne(TimeSpan.FromSeconds(5));
            Assert.True(signaled, "Threads should complete within timeout");
            Assert.Equal(0, remaining);
        }

        private class DummyNotify : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private string _name;
            public string Name { get => _name; set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
        }

        private class ObservableHolder : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public System.Collections.ObjectModel.ObservableCollection<string> Items { get; } = new System.Collections.ObjectModel.ObservableCollection<string>();
        }
    }
}
