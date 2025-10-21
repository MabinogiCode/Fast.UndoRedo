using System.ComponentModel;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for registration tracker behavior when recording property changes on registered objects.
    /// </summary>
    public class RegistrationTrackerTests
    {
        /// <summary>
        /// Verifies that Register records property changes into the undo/redo service.
        /// </summary>
        [Fact]
        public void RegisterRecordsPropertyChange()
        {
            var service = new UndoRedoService();
            var tracker = new RegistrationTracker(service);
            var d = new DummyNotifyFull();

            tracker.Register(d);
            d.Name = "a";

            Assert.True(service.CanUndo);
        }
    }
}
