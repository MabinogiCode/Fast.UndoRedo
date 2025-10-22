using System;
using System.Collections.Generic;
using Fast.UndoRedo.Core.Logging;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for ActionFactory.ConvertTo and CreateTypedList fallback behaviors.
    /// </summary>
    public class ActionFactoryConvertTests
    {
        /// <summary>
        /// Verifies ConvertTo parses enums from strings and numeric values.
        /// </summary>
        [Fact]
        public void ConvertToParsesEnumsFromStringAndNumber()
        {
            var t = typeof(TestEnum);
            var obj1 = Fast.UndoRedo.Core.ActionFactory.ConvertToForTests("A", t);
            Assert.IsType<TestEnum>(obj1);
            Assert.Equal(TestEnum.A, (TestEnum)obj1);

            var obj2 = Fast.UndoRedo.Core.ActionFactory.ConvertToForTests(1, t);
            Assert.IsType<TestEnum>(obj2);
            Assert.Equal(TestEnum.B, (TestEnum)obj2);
        }

        /// <summary>
        /// Verifies CreateTypedList handles mixed inputs for enum types.
        /// </summary>
        [Fact]
        public void CreateTypedListHandlesMixedInputsForEnum()
        {
            var input = new object[] { "A", 1, "invalid", null };
            var result = Fast.UndoRedo.Core.ActionFactory.CreateTypedListForTests<TestEnum>(input);
            Assert.NotNull(result);
            var arr = new List<TestEnum>(result);
            Assert.Equal(4, arr.Count);

            // first should be A, second B, third fallback default (A), fourth default
            Assert.Equal(TestEnum.A, arr[0]);
            Assert.Equal(TestEnum.B, arr[1]);
        }
    }
}
