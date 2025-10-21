using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Stress tests related to memory and repeated register/unregister operations.
    /// </summary>
    public class MemoryStressTests
    {
        /// <summary>
        /// Long running stress test that verifies many register/unregister cycles do not leak memory. Skipped by default.
        /// </summary>
        [Fact(Skip = "Long running stress test, enable manually when needed")]
        public void RegisterUnregisterLargeLoopDoesNotLeak()
        {
            var svc = new UndoRedoService();
            var tracker = new RegistrationTracker(svc);

            const int iterations = 50_000;
            var weakRefs = new List<WeakReference>();

            for (int i = 0; i < iterations; i++)
            {
                var obj = new DummyNotify();
                tracker.Register(obj);
                tracker.Unregister(obj);

                weakRefs.Add(new WeakReference(obj));
            }

            // drop any strong refs
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // assert most objects are collectible
            int alive = 0;
            foreach (var wr in weakRefs)
            {
                if (wr.IsAlive)
                {
                    alive++;
                }
            }

            // we allow a small number to be alive due to finalizer timing; expect vast majority collected
            var ratio = (double)alive / iterations;
            Assert.True(ratio < 0.01, $"Too many objects still alive after GC: {alive}/{iterations}");
        }
    }
}
