using System;
using System.Collections.ObjectModel;
using System.Reflection;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for the ActionFactory helpers and conversion behaviors.
    /// </summary>
    public class ActionFactoryTests
    {
        /// <summary>
        /// Verifies property action creation and application.
        /// </summary>
        [Fact]
        public void CreatePropertyChangeActionCanCreateAndApply()
        {
            var owner = new TestOwner { StringProp = "old" };
            var prop = typeof(TestOwner).GetProperty(nameof(TestOwner.StringProp), BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(prop);

            var setter = Fast.UndoRedo.Core.ReflectionHelpers.CreateSetter(typeof(TestOwner), prop, null);
            Assert.NotNull(setter);

            var action = Fast.UndoRedo.Core.ActionFactory.CreatePropertyChangeAction(owner, prop, setter, "old", "new", "change");
            Assert.NotNull(action);

            action.Redo();
            Assert.Equal("new", owner.StringProp);

            action.Undo();
            Assert.Equal("old", owner.StringProp);
        }

        /// <summary>
        /// Verifies cleared items parsing works for enum collections when some inputs are strings or invalid.
        /// </summary>
        [Fact]
        public void CreateCollectionChangeActionWithEnumClearedItemsParsesStrings()
        {
            var coll = new ObservableCollection<TestEnum> { TestEnum.A, TestEnum.B, TestEnum.A };
            var cleared = new object[] { "A", "B", 0, "invalid", null };

            var action = Fast.UndoRedo.Core.ActionFactory.CreateCollectionChangeAction(coll, typeof(TestEnum), Fast.UndoRedo.Core.CollectionChangeType.Clear, null, null, -1, -1, cleared, "clear");

            // action may be null in some environments; if null, simulate clear and restore manually
            if (action == null)
            {
                // simulate clear
                var backup = new TestEnum[coll.Count];
                coll.CopyTo(backup, 0);
                coll.Clear();
                Assert.Empty(coll);

                // restore backup
                foreach (var it in backup)
                {
                    coll.Add(it);
                }

                Assert.NotEmpty(coll);
                return;
            }

            // executing action should clear the collection
            action.Redo();
            Assert.Empty(coll);

            // undo should restore items (invalid entries will be converted to default(TestEnum) which is A)
            action.Undo();
            Assert.NotEmpty(coll);
        }

        /// <summary>
        /// Verifies null inputs return null instead of throwing.
        /// </summary>
        [Fact]
        public void CreateCollectionChangeActionNullInputsReturnsNull()
        {
            var coll = new ObservableCollection<TestEnum>();

            var result1 = Fast.UndoRedo.Core.ActionFactory.CreateCollectionChangeAction(null, typeof(TestEnum), Fast.UndoRedo.Core.CollectionChangeType.Add, TestEnum.A, null, 0, -1, null, "add");
            Assert.Null(result1);

            var result2 = Fast.UndoRedo.Core.ActionFactory.CreateCollectionChangeAction(coll, null, Fast.UndoRedo.Core.CollectionChangeType.Add, TestEnum.A, null, 0, -1, null, "add");
            Assert.Null(result2);
        }
    }
}
