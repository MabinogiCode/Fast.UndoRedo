using System;
using System.Reflection;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests covering reflection helpers and property-action behaviour used by the core.
    /// </summary>
    public class ActionAndReflectionTests
    {
        /// <summary>
        /// Verifies that a setter delegate can be created and applied to set a property value.
        /// </summary>
        [Fact]
        public void ReflectionHelpersCreateSetterAllowsSettingValue()
        {
            var p = new Person { Name = "old" };
            var prop = typeof(Person).GetProperty(nameof(Person.Name));
            var setterObj = ReflectionHelpers.CreateSetter(typeof(Person), prop);
            Assert.NotNull(setterObj);

            var setter = setterObj as Action<Person, string>;
            Assert.NotNull(setter);

            setter(p, "new");
            Assert.Equal("new", p.Name);
        }

        /// <summary>
        /// Verifies that read-only properties yield a null setter.
        /// </summary>
        [Fact]
        public void ReflectionHelpersCreateSetterNullForReadOnly()
        {
            var prop = typeof(Person).GetProperty(nameof(Person.ReadOnly));
            var setterObj = ReflectionHelpers.CreateSetter(typeof(Person), prop);
            Assert.Null(setterObj);
        }

        /// <summary>
        /// Verifies that private setters can be accessed via reflection helper.
        /// </summary>
        [Fact]
        public void ReflectionHelpersCreateSetterPrivateSetterWorks()
        {
            var p = new Person();
            var prop = typeof(Person).GetProperty(nameof(Person.PrivateSet));
            var setterObj = ReflectionHelpers.CreateSetter(typeof(Person), prop);
            Assert.NotNull(setterObj);

            var setter = setterObj as Action<Person, string>;
            Assert.NotNull(setter);

            setter(p, "changed");

            // private setter should have changed value
            var v = prop.GetValue(p) as string;
            Assert.Equal("changed", v);
        }

        /// <summary>
        /// Ensures PropertyChangeAction applies Undo/Redo values correctly.
        /// </summary>
        [Fact]
        public void PropertyChangeActionUndoRedoAppliesValues()
        {
            var p = new Person { Name = "old" };
            Action<Person, string> setter = (t, v) => t.Name = v;

            var action = new PropertyChangeAction<Person, string>(p, setter, "old", "new", "desc");
            Assert.Equal("desc", action.Description);

            action.Redo();
            Assert.Equal("new", p.Name);

            action.Undo();
            Assert.Equal("old", p.Name);
        }

        /// <summary>
        /// Validates constructor argument checks for PropertyChangeAction.
        /// </summary>
        [Fact]
        public void PropertyChangeActionConstructorValidatesArgs()
        {
            Action<Person, string> setter = (t, v) => t.Name = v;
            Assert.Throws<ArgumentNullException>(() => new PropertyChangeAction<Person, string>(null, setter, "old", "new"));
            Assert.Throws<ArgumentNullException>(() => new PropertyChangeAction<Person, string>(new Person(), null, "old", "new"));
        }

        /// <summary>
        /// Ensures ActionFactory creates a working property-change action from reflection-based setter.
        /// </summary>
        [Fact]
        public void ActionFactoryCreatesPropertyActionAndApplies()
        {
            var p = new Person { Name = "initial" };
            var prop = typeof(Person).GetProperty(nameof(Person.Name));
            var setterObj = ReflectionHelpers.CreateSetter(typeof(Person), prop);
            Assert.NotNull(setterObj);

            var action = ActionFactory.CreatePropertyChangeAction(p, prop, setterObj, (object)"initial", (object)"changed", "desc");
            Assert.NotNull(action);

            action.Redo();
            Assert.Equal("changed", p.Name);

            action.Undo();
            Assert.Equal("initial", p.Name);
        }
    }
}
