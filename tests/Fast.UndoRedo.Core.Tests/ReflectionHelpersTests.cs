using System;
using System.Reflection;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for the reflection helper methods used to create getters and setters.
    /// </summary>
    public class ReflectionHelpersTests
    {
        /// <summary>
        /// Verifies creation and invocation of typed getter and setter for a public property.
        /// </summary>
        [Fact]
        public void CreateSetterAndGetterWorkForPublicProperty()
        {
            var owner = new TestOwner { StringProp = "old" };
            var prop = typeof(TestOwner).GetProperty(nameof(TestOwner.StringProp), BindingFlags.Public | BindingFlags.Instance);
            var setterObj = Fast.UndoRedo.Core.ReflectionHelpers.CreateSetter(typeof(TestOwner), prop, null);
            Assert.NotNull(setterObj);

            var setter = setterObj as Delegate;
            setter.DynamicInvoke(owner, "new");
            Assert.Equal("new", owner.StringProp);

            var getterObj = Fast.UndoRedo.Core.ReflectionHelpers.CreateGetter(typeof(TestOwner), prop, null);
            Assert.NotNull(getterObj);
            var getter = getterObj as Delegate;
            var value = getter.DynamicInvoke(owner);
            Assert.Equal("new", value);
        }

        /// <summary>
        /// Verifies object-based getter and setter work for non-public properties and hit the cache path on repeated calls.
        /// </summary>
        [Fact]
        public void CreateObjectGetterAndSetterWorkForNonPublicProperty()
        {
            var owner = new TestPrivatePropertyOwner();
            var prop = typeof(TestPrivatePropertyOwner).GetProperty("PrivateProp", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(prop);

            var objSetter = Fast.UndoRedo.Core.ReflectionHelpers.CreateObjectSetter(typeof(TestPrivatePropertyOwner), prop, null);
            Assert.NotNull(objSetter);
            objSetter(owner, "secret");

            var objGetter = Fast.UndoRedo.Core.ReflectionHelpers.CreateObjectGetter(typeof(TestPrivatePropertyOwner), prop, null);
            Assert.NotNull(objGetter);
            var val = objGetter(owner);
            Assert.Equal("secret", val);

            // call again to ensure caching path
            var objGetter2 = Fast.UndoRedo.Core.ReflectionHelpers.CreateObjectGetter(typeof(TestPrivatePropertyOwner), prop, null);
            Assert.NotNull(objGetter2);
        }

        /// <summary>
        /// Verifies property lookup and public instance properties caching behavior.
        /// </summary>
        [Fact]
        public void GetPropertyAndGetPublicInstancePropertiesReturnConsistentResults()
        {
            var p1 = Fast.UndoRedo.Core.ReflectionHelpers.GetProperty(typeof(TestOwner), nameof(TestOwner.StringProp));
            Assert.NotNull(p1);
            var p2 = Fast.UndoRedo.Core.ReflectionHelpers.GetProperty(typeof(TestOwner), nameof(TestOwner.StringProp));
            Assert.Same(p1, p2);

            var props1 = Fast.UndoRedo.Core.ReflectionHelpers.GetPublicInstanceProperties(typeof(TestOwner));
            var props2 = Fast.UndoRedo.Core.ReflectionHelpers.GetPublicInstanceProperties(typeof(TestOwner));
            Assert.Equal(props1.Length, props2.Length);
        }
    }
}
