using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for collection-related action factory behavior (add/remove/replace/move/clear).
    /// </summary>
    public class ActionFactoryCollectionTests
    {
        /// <summary>
        /// Verifies that CreateCollectionChangeAction produces an Add action that modifies the collection.
        /// </summary>
        [Fact]
        public void Factory_CreatesAddAction()
        {
            var coll = new ObservableCollection<string>();
            var action = ActionFactory.CreateCollectionChangeAction(coll, typeof(string), CollectionChangeType.Add, "a", null, 0, -1, null, "add", null);
            Assert.NotNull(action);

            action.Redo();
            Assert.Single(coll);
            Assert.Equal("a", coll[0]);

            action.Undo();
            Assert.Empty(coll);
        }

        /// <summary>
        /// Verifies Remove action behaviour.
        /// </summary>
        [Fact]
        public void Factory_CreatesRemoveAction()
        {
            var coll = new ObservableCollection<string> { "a", "b" };
            var action = ActionFactory.CreateCollectionChangeAction(coll, typeof(string), CollectionChangeType.Remove, "a", null, 0, -1, null, "remove", null);
            Assert.NotNull(action);

            action.Redo();
            Assert.Single(coll);
            Assert.Equal("b", coll[0]);

            action.Undo();
            Assert.Equal(2, coll.Count);
            Assert.Equal("a", coll[0]);
        }

        /// <summary>
        /// Verifies replace/move/clear composite scenarios.
        /// </summary>
        [Fact]
        public void Factory_CreatesReplace_Move_Clear()
        {
            var coll = new ObservableCollection<string> { "a", "b", "c" };
            var replace = ActionFactory.CreateCollectionChangeAction(coll, typeof(string), CollectionChangeType.Replace, "x", "b", 1, -1, null, "replace", null);
            Assert.NotNull(replace);
            replace.Redo();
            Assert.Equal("x", coll[1]);
            replace.Undo();
            Assert.Equal("b", coll[1]);

            var move = ActionFactory.CreateCollectionChangeAction(coll, typeof(string), CollectionChangeType.Move, "b", null, 1, 2, null, "move", null);
            Assert.NotNull(move);
            move.Redo();
            Assert.Equal(new[] { "a", "c", "b" }, coll.ToArray());
            move.Undo();
            Assert.Equal(new[] { "a", "b", "c" }, coll.ToArray());

            var cleared = new List<string>(coll);
            var clear = ActionFactory.CreateCollectionChangeAction(coll, typeof(string), CollectionChangeType.Clear, null, null, -1, -1, cleared as IEnumerable<object>, "clear", null);
            Assert.NotNull(clear);
            clear.Redo();
            Assert.Empty(coll);
            clear.Undo();
            Assert.Equal(3, coll.Count);
        }
    }
}
