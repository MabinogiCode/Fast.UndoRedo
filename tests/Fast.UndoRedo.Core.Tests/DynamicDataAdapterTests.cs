using System;
using Fast.UndoRedo.Core;
using Fast.UndoRedo.ReactiveUI;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for the DynamicData adapter helper.
    /// </summary>
    public class DynamicDataAdapterTests
    {
        /// <summary>
        /// Verify that registering a null collection does not throw.
        /// </summary>
        [Fact]
        public void RegisterCollection_Null_DoesNotThrow()
        {
            var svc = new UndoRedoService();
            var adapter = new DynamicDataAdapter(svc);
            adapter.RegisterCollection(null);
        }

        /// <summary>
        /// Verify that registering a collection attaches and does not throw when events are raised.
        /// </summary>
        [Fact]
        public void RegisterCollection_AttachesCollection()
        {
            var svc = new UndoRedoService();
            var adapter = new DynamicDataAdapter(svc);
            var c = new DummyCollection();

            adapter.RegisterCollection(c);

            c.RaiseReset();
        }

        /// <summary>
        /// Constructor should throw when service is null.
        /// </summary>
        [Fact]
        public void Constructor_NullService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DynamicDataAdapter(null));
        }
    }
}
