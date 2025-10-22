using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Integration tests that exercise attach/change/undo-redo scenarios.
    /// </summary>
    public class IntegrationTests
    {
        /// <summary>
        /// Verifies attaching a view model records property changes and supports undo/redo.
        /// </summary>
        [Fact]
        public void PropertyAttachChangeUndoRedoWorks()
        {
            var svc = new UndoRedoService();
            var vm = new PersonViewModel();
            svc.Attach(vm);

            vm.Name = "Alice";
            vm.Name = "Bob";

            svc.Undo();
            Assert.Equal("Alice", vm.Name);

            svc.Undo();
            Assert.Equal(string.Empty, vm.Name);

            svc.Redo();
            Assert.Equal("Alice", vm.Name);

            svc.Redo();
            Assert.Equal("Bob", vm.Name);
        }

        /// <summary>
        /// Verifies attaching a collection records adds and supports undo/redo.
        /// </summary>
        [Fact]
        public void CollectionAttachChangeUndoRedoWorks()
        {
            var svc = new UndoRedoService();
            var vm = new PersonViewModel();
            svc.Attach(vm);
            svc.AttachCollection(vm.Items);

            vm.Items.Add("Item1");
            vm.Items.Add("Item2");

            svc.Undo();
            Assert.Single(vm.Items);

            svc.Undo();
            Assert.Empty(vm.Items);

            svc.Redo();
            Assert.Single(vm.Items);

            svc.Redo();
            Assert.Equal(2, vm.Items.Count);
        }
    }
}
