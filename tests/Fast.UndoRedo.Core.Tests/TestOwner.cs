namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Simple owner class exposing properties used as backing fields in tests.
    /// </summary>
    internal class TestOwner
    {
        /// <summary>
        /// Enum property for testing.
        /// </summary>
        public TestEnum EnumProp { get; set; }

        /// <summary>
        /// String property for testing.
        /// </summary>
        public string StringProp { get; set; }

        /// <summary>
        /// Integer property for testing.
        /// </summary>
        public int IntProp { get; set; }
    }
}
