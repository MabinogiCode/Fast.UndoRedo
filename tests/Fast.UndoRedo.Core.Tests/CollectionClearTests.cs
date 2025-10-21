using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for clear/remove behaviors for collections and enum conversions.
    /// </summary>
    public class CollectionClearTests
    {
        /// <summary>
        /// Verifies that clearing an enum collection produces an action that can undo/redo the cleared items.
        /// </summary>
        [Fact]
        public void ClearUndoRedoRestoresItemsForEnums()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<CollectionTestEnum> { CollectionTestEnum.X, CollectionTestEnum.Y };
            service.AttachCollection(coll);

            // Clear collection
            coll.Clear();

            // Undo should restore two items
            service.Undo();
            Assert.Equal(2, coll.Count);
            Assert.Equal(CollectionTestEnum.X, coll[0]);
            Assert.Equal(CollectionTestEnum.Y, coll[1]);

            // Redo should clear again
            service.Redo();
            Assert.Empty(coll);
        }

        /// <summary>
        /// Verifies CreateCollectionChangeAction handles clearedItems passed as strings for enum collections.
        /// </summary>
        [Fact]
        public void CreateCollectionChangeAction_ClearedItemsAsStrings_ForEnum()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<CollectionTestEnum>();

            var clearedStrings = new List<object> { "X", "Y" };
            var action = ActionFactory.CreateCollectionChangeAction(coll, typeof(CollectionTestEnum), CollectionChangeType.Clear, null, null, -1, -1, clearedStrings, "clear");
            Assert.NotNull(action);

            // Redo should clear (no-op on empty), Undo should restore items
            action.Redo();
            action.Undo();

            // When Undo was applied to empty collection, items should be inserted
            Assert.Equal(2, coll.Count);
            Assert.Equal(CollectionTestEnum.X, coll[0]);
            Assert.Equal(CollectionTestEnum.Y, coll[1]);
        }
    }
}
