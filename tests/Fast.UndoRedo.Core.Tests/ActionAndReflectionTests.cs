using System;
using System.Reflection;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    public class ActionAndReflectionTests
    {
        private class Person
        {
            public string Name { get; set; }
            public string ReadOnly { get; } = "ro";
            public string PrivateSet { get; private set; } = "priv";
        }

        [Fact]
        public void ReflectionHelpers_CreateSetter_AllowsSettingValue()
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

        [Fact]
        public void ReflectionHelpers_CreateSetter_NullForReadOnly()
        {
            var prop = typeof(Person).GetProperty(nameof(Person.ReadOnly));
            var setterObj = ReflectionHelpers.CreateSetter(typeof(Person), prop);
            Assert.Null(setterObj);
        }

        [Fact]
        public void ReflectionHelpers_CreateSetter_PrivateSetter_Works()
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

        [Fact]
        public void PropertyChangeAction_UndoRedo_AppliesValues()
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

        [Fact]
        public void PropertyChangeAction_Constructor_ValidatesArgs()
        {
            Action<Person, string> setter = (t, v) => t.Name = v;
            Assert.Throws<ArgumentNullException>(() => new PropertyChangeAction<Person, string>(null, setter, "old", "new"));
            Assert.Throws<ArgumentNullException>(() => new PropertyChangeAction<Person, string>(new Person(), null, "old", "new"));
        }

        [Fact]
        public void ActionFactory_CreatesPropertyAction_AndApplies()
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
