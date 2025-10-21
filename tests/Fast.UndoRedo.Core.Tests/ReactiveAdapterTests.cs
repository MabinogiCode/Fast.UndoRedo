using Fast.UndoRedo.Core;
using Fast.UndoRedo.ReactiveUI;
using System;
using System.ComponentModel;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests verifying ReactiveAdapter behavior and concurrency characteristics.
    /// </summary>
    public class ReactiveAdapterTests
    {
        /// <summary>
        /// Verifies that ReactiveAdapter records changes when observables are used.
        /// </summary>
        [Fact]
        public void ReactiveAdapterRecordsPropertyChangesFromObservables()
        {
            var service = new UndoRedoService();
            var adapter = new ReactiveAdapter(service);

            var d = new DummyReactive();
            adapter.Register(d);

            d.Name = "a";
            Assert.True(service.CanUndo);
        }

        /// <summary>
        /// Verifies that ReactiveAdapter falls back to INotifyPropertyChanged path when observables are not subscribed.
        /// </summary>
        [Fact]
        public void ReactiveAdapterFallbackINotifyPropertyChanged()
        {
            var service = new UndoRedoService();
            var adapter = new ReactiveAdapter(service);

            var d = new DummyReactive();

            // trigger only the INotifyPropertyChanged path
            d.Name = "x";
            adapter.Register(d);
            d.Name = "y";

            Assert.True(service.CanUndo);
        }

        /// <summary>
        /// Ensures Unregister allows garbage collection of reactive objects.
        /// </summary>
        [Fact]
        public void UnregisterAllowsGarbageCollectionForReactiveObject()
        {
            var svc = new UndoRedoService();
            var adapter = new ReactiveAdapter(svc);

            WeakReference wr = CreateAndRegister(adapter);

            // Force GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(wr.IsAlive, "Reactive object should be collectible after Unregister and GC");
        }

        private WeakReference CreateAndRegister(ReactiveAdapter adapter)
        {
            var obj = new DummyReactive();
            adapter.Register(obj);
            adapter.Unregister(obj);
            var wr = new WeakReference(obj);
            obj = null;
            return wr;
        }

        /// <summary>
        /// Validates concurrent register/unregister operations do not throw or corrupt state.
        /// </summary>
        [Fact]
        public void ConcurrentRegisterUnregisterDoesNotThrowOrCorruptState()
        {
            var svc = new UndoRedoService();
            var adapter = new ReactiveAdapter(svc);

            const int tasks = 16;
            const int iterations = 100;
            var exceptions = 0;

            Parallel.For(0, tasks, i =>
            {
                try
                {
                    for (int j = 0; j < iterations; j++)
                    {
                        var obj = new DummyReactive();
                        adapter.Register(obj);

                        // optionally mutate
                        obj.Name = "v" + j;
                        adapter.Unregister(obj);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref exceptions);
                }
            });

            Assert.Equal(0, exceptions);
        }
    }
}
