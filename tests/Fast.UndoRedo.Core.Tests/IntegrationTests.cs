using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public void EndToEnd_Attach_Change_UndoRedo_VerifyState()
        {
            var svc = new UndoRedoService();
            var vm = new PersonViewModel();
            svc.Attach(vm);
            svc.AttachCollection(vm.Items);

            vm.Name = "Alice";
            vm.Items.Add("Item1");
            vm.Items.Add("Item2");
            vm.Name = "Bob";

            svc.Undo(); // Undo name change to "Bob" -> "Alice"
            Assert.Equal("Alice", vm.Name);
            svc.Undo(); // Undo Item2 add
            Assert.Single(vm.Items);
            svc.Undo(); // Undo Item1 add
            Assert.Empty(vm.Items);
            svc.Undo(); // Undo name change to "Alice" -> ""
            Assert.Equal(string.Empty, vm.Name);

            svc.Redo(); // Redo name to "Alice"
            Assert.Equal("Alice", vm.Name);
            svc.Redo(); // Redo Item1 add
            Assert.Single(vm.Items);
            svc.Redo(); // Redo Item2 add
            Assert.Equal(2, vm.Items.Count);
            svc.Redo(); // Redo name to "Bob"
            Assert.Equal("Bob", vm.Name);
        }

        private class PersonViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private string _name = string.Empty;
            public string Name
            {
                get => _name;
                set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
            }
            public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();
        }
    }
}
