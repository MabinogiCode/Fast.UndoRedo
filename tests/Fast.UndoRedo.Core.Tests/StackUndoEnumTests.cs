using System;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for StackUndo behavior with enum-backed properties.
    /// </summary>
    public class StackUndoEnumTests
    {
        /// <summary>
        /// Verifies that StackUndo records an action and assigns when given a different enum value.
        /// </summary>
        [Fact]
        public void StackUndoSetsAndCreatesActionForEnumFromEnumValue()
        {
            var service = new UndoRedoService();
            var owner = new TestOwner { EnumProp = TestEnum.A };
            var backing = owner.EnumProp;

            var result = service.StackUndo(owner, TestEnum.B, ref backing, nameof(owner.EnumProp));

            Assert.Equal(TestEnum.B, result);
            Assert.True(service.CanUndo);
            service.Undo();
            Assert.Equal(TestEnum.A, owner.EnumProp);
        }

        /// <summary>
        /// Verifies that StackUndo handles enum values supplied as underlying integers.
        /// </summary>
        [Fact]
        public void StackUndoSetsAndCreatesActionForEnumFromInt()
        {
            var service = new UndoRedoService();
            var owner = new TestOwner { EnumProp = TestEnum.A };
            var backing = owner.EnumProp;

            var result = service.StackUndo(owner, (TestEnum)1, ref backing, nameof(owner.EnumProp));

            Assert.Equal(TestEnum.B, result);
            Assert.True(service.CanUndo);
            service.Undo();
            Assert.Equal(TestEnum.A, owner.EnumProp);
        }

        /// <summary>
        /// Verifies that StackUndo handles enum values parsed from strings.
        /// </summary>
        [Fact]
        public void StackUndoSetsAndCreatesActionForEnumFromString()
        {
            var service = new UndoRedoService();
            var owner = new TestOwner { EnumProp = TestEnum.A };
            var backing = owner.EnumProp;

            var newEnum = (TestEnum)Enum.Parse(typeof(TestEnum), "B", true);
            var result = service.StackUndo(owner, newEnum, ref backing, nameof(owner.EnumProp));

            Assert.Equal(TestEnum.B, result);
            Assert.True(service.CanUndo);
            service.Undo();
            Assert.Equal(TestEnum.A, owner.EnumProp);
        }
    }
}
