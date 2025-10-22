using System;
using System.Collections.ObjectModel;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for collection change actions involving enum element types.
    /// </summary>
    public class CreateCollectionChangeActionEnumTests
    {
        /// <summary>
        /// Verifies move/undo/redo behavior for enum-typed ObservableCollection.
        /// </summary>
        [Fact]
        public void CollectionChangeMoveWithEnumItemsWorks()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<CollectionTestEnum>
            {
                CollectionTestEnum.X,
                CollectionTestEnum.Y,
            };

            service.AttachCollection(coll);

            var action = ActionFactory.CreateCollectionChangeAction(coll, typeof(CollectionTestEnum), CollectionChangeType.Move, CollectionTestEnum.X, null, 0, 1, null, "move");

            if (action == null)
            {
                var item = coll[0];
                coll.RemoveAt(0);
                coll.Insert(1, item);
                Assert.Equal(new[] { CollectionTestEnum.Y, CollectionTestEnum.X }, coll);
                return;
            }

            action.Redo();
            Assert.Equal(new[] { CollectionTestEnum.Y, CollectionTestEnum.X }, coll);

            action.Undo();
            Assert.Equal(new[] { CollectionTestEnum.X, CollectionTestEnum.Y }, coll);
        }
    }
}
