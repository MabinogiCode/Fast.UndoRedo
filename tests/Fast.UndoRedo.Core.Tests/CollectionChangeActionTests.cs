using System.Collections.ObjectModel;
using Fast.UndoRedo.Core;
using Xunit;
using System.Linq;

namespace Fast.UndoRedo.Core.Tests
{
    public class CollectionChangeActionTests
    {
        [Fact]
        public void Add_Remove_UndoRedo_Works()
        {
            var coll = new ObservableCollection<string>();
            var action = new CollectionChangeAction<string>(coll, CollectionChangeType.Add, "a", 0, description: "add a");

            action.Redo();
            Assert.Single(coll);
            Assert.Equal("a", coll[0]);

            action.Undo();
            Assert.Empty(coll);
        }

        [Fact]
        public void Remove_UndoRedo_Works()
        {
            var coll = new ObservableCollection<string> { "a", "b" };
            var action = new CollectionChangeAction<string>(coll, CollectionChangeType.Remove, "a", 0, description: "remove a");

            action.Redo();
            Assert.Single(coll);
            Assert.Equal("b", coll[0]);

            action.Undo();
            Assert.Equal(2, coll.Count);
            Assert.Equal("a", coll[0]);
        }

        [Fact]
        public void Replace_UndoRedo_Works()
        {
            var coll = new ObservableCollection<string> { "a", "b" };
            var action = new CollectionChangeAction<string>(coll, CollectionChangeType.Replace, "x", 1, oldItem: "b", description: "replace");

            action.Redo();
            Assert.Equal("x", coll[1]);

            action.Undo();
            Assert.Equal("b", coll[1]);
        }

        [Fact]
        public void Move_UndoRedo_Works()
        {
            var coll = new ObservableCollection<string> { "a", "b", "c" };
            var action = new CollectionChangeAction<string>(coll, CollectionChangeType.Move, "b", 1, toIndex: 2, description: "move");

            action.Redo();
            Assert.Equal(new[] { "a", "c", "b" }, coll.ToArray());

            action.Undo();
            Assert.Equal(new[] { "a", "b", "c" }, coll.ToArray());
        }

        [Fact]
        public void Clear_UndoRedo_Works()
        {
            var coll = new ObservableCollection<string> { "a", "b" };
            var cleared = coll.ToList();
            var action = new CollectionChangeAction<string>(coll, CollectionChangeType.Clear, default, -1, clearedItems: cleared, description: "clear");

            action.Redo();
            Assert.Empty(coll);

            action.Undo();
            Assert.Equal(2, coll.Count);
        }
    }
}
