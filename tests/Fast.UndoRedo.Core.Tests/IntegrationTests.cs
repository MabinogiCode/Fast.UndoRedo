using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Integration tests that exercise end-to-end attach/change/undo-redo scenarios.
    /// </summary>
    public class IntegrationTests
    {
        /// <summary>
        /// Full end-to-end scenario: attach view model and collection, perform changes and verify undo/redo restores state.
        /// </summary>
        [Fact]
        public void EndToEndAttachChangeUndoRedoVerifyState()
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
    }
}
