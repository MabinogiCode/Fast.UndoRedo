using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Fast.UndoRedo.Core;
using Fast.UndoRedo.ReactiveUI;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Miscellaneous unit tests covering general service behavior and helpers.
    /// </summary>
    public class MoreTests
    {
        /// <summary>
        /// Ensure calling Undo/Redo on an empty service does not throw and push(null) is ignored.
        /// </summary>
        [Fact]
        public void UndoRedoService_NoThrowOnEmptyUndoRedo()
        {
            var svc = new UndoRedoService();
            // should not throw
            svc.Undo();
            svc.Redo();

            // Push null ignored
            svc.Push(null);

            Assert.False(svc.CanUndo);
            Assert.False(svc.CanRedo);
        }

        /// <summary>
        /// Clearing the service removes items from undo/redo stacks.
        /// </summary>
        [Fact]
        public void UndoRedoService_Clear_ClearsStacks()
        {
            var svc = new UndoRedoService();
            var action = new TestAction(() => { }, () => { }, "a");
            svc.Push(action);
            Assert.True(svc.CanUndo);
            svc.Clear();
            Assert.False(svc.CanUndo);
            Assert.False(svc.CanRedo);
        }

        /// <summary>
        /// Subscribing observers receives initial state and subsequent updates.
        /// </summary>
        [Fact]
        public void Subscribe_ObserverReceivesInitialAndUpdates()
        {
            var svc = new UndoRedoService();
            int updateCount = 0;
            var obs = new TestObserver(state => updateCount++);
            using (svc.Subscribe(obs))
            {
                // initial call increments once
                Assert.Equal(1, updateCount);
                svc.Push(new TestAction(() => { }, () => { }, "x"));
                Assert.Equal(2, updateCount);
            }
        }

        /// <summary>
        /// Registering then unregistering stops recording property changes.
        /// </summary>
        [Fact]
        public void RegistrationTracker_UnregisterStopsRecording()
        {
            var svc = new UndoRedoService();
            var tracker = new RegistrationTracker(svc);
            var d = new DummyNotify();
            tracker.Register(d);
            d.Name = "a";
            Assert.True(svc.CanUndo);

            tracker.Unregister(d);
            var before = svc.CanUndo;
            d.Name = "b"; // should not push new actions
            Assert.Equal(before, svc.CanUndo);
        }

        /// <summary>
        /// Nested registration records changes on nested child objects.
        /// </summary>
        [Fact]
        public void RegistrationTracker_NestedRegistration_RecordsNestedChanges()
        {
            var svc = new UndoRedoService();
            var tracker = new RegistrationTracker(svc);
            var parent = new Parent();
            tracker.Register(parent);
            parent.Child.Value = 42;
            Assert.True(svc.CanUndo);
        }

        /// <summary>
        /// Verifies ReactiveAdapter registers/unregisters and records observable changes.
        /// </summary>
        [Fact]
        public void ReactiveAdapter_Unregister_RemovesSubscriptions()
        {
            var svc = new UndoRedoService();
            var adapter = new ReactiveAdapter(svc);
            var d = new DummyReactiveSimple();
            adapter.Register(d);
            d.Name = "v1";
            Assert.True(svc.CanUndo);

            adapter.Unregister(d);
            var before = svc.CanUndo;
            d.Name = "v2";
            Assert.Equal(before, svc.CanUndo);
        }

        /// <summary>
        /// Ensures CoreObserverWrapper invokes the provided action and swallows exceptions.
        /// </summary>
        [Fact]
        public void CoreObserverWrapper_OnNext_InvokesActionAndIgnoresExceptions()
        {
            int called = 0;
            var wrapper = new CoreObserverWrapper<string>(o => { called++; if (o is string s && s == "boom") throw new Exception("err"); });
            wrapper.OnNext("ok");
            Assert.Equal(1, called);
            // exception inside should be swallowed
            wrapper.OnNext("boom");
            Assert.Equal(2, called);
        }

        // helper types
        private class TestObserver : IObserver<UndoRedoState>
        {
            private readonly Action<UndoRedoState> _onNext;
            public TestObserver(Action<UndoRedoState> onNext) { _onNext = onNext; }
            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(UndoRedoState value) => _onNext(value);
        }

        private class TestAction : IUndoableAction
        {
            private readonly Action _undo;
            private readonly Action _redo;
            public string Description { get; }
            public TestAction(Action undo, Action redo, string desc) { _undo = undo; _redo = redo; Description = desc; }
            public void Undo() => _undo();
            public void Redo() => _redo();
        }

        private class DummyNotify : INotifyPropertyChanging, INotifyPropertyChanged
        {
            public event PropertyChangingEventHandler PropertyChanging;
            public event PropertyChangedEventHandler PropertyChanged;
            private string _name;
            public string Name { get => _name; set { PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Name))); _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
        }

        private class Parent
        {
            public Parent() { Child = new Child(); }
            public Child Child { get; }
        }
        private class Child : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private int _value;
            public int Value { get => _value; set { _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); } }
        }

        private class DummyReactiveSimple : INotifyPropertyChanged
        {
            private string _name;
            public event PropertyChangedEventHandler PropertyChanged;
            public IObservable<object> Changing => _changing;
            public IObservable<object> Changed => _changed;
            private readonly System.Reactive.Subjects.Subject<object> _changing = new System.Reactive.Subjects.Subject<object>();
            private readonly System.Reactive.Subjects.Subject<object> _changed = new System.Reactive.Subjects.Subject<object>();
            public string Name { get => _name; set { _changing.OnNext(new { PropertyName = nameof(Name) }); _name = value; _changed.OnNext(new { PropertyName = nameof(Name) }); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
        }
    }
}
