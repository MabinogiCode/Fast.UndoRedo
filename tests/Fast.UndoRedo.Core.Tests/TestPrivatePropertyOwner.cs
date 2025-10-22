namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Simple class exposing a non-public property usable via reflection in tests.
    /// </summary>
    internal class TestPrivatePropertyOwner
    {
        /// <summary>
        /// A private property only accessible via reflection in tests.
        /// </summary>
        private string PrivateProp { get; set; } = "initial";

        /// <summary>
        /// Helper method to expose the private property for assertions.
        /// </summary>
        /// <returns>The current value of the private property.</returns>
        public string GetPrivatePropViaMethod()
        {
            return PrivateProp;
        }
    }
}
