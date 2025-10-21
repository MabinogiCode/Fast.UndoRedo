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
        private class Dummy : INotifyPropertyChanged, INotifyPropertyChanging
        {
            private string _name;
            public event PropertyChangedEventHandler PropertyChanged;
            public event PropertyChangingEventHandler PropertyChanging;

            public string Name
            {
                get => _name;
                set
                {
                    PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Name)));
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        /// <summary>
        /// Verifies that Register records property changes into the undo/redo service.
        /// </summary>
        [Fact]
        public void Register_RecordsPropertyChange()
        {
            var service = new UndoRedoService();
            var tracker = new RegistrationTracker(service);
            var d = new Dummy();

            tracker.Register(d);
            d.Name = "a";

            Assert.True(service.CanUndo);
        }
    }
}
