using System;
using System.Reactive.Subjects;
using System.ComponentModel;
using Fast.UndoRedo.Core;
using Fast.UndoRedo.ReactiveUI;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    public class ReactiveAdapterTests
    {
        private class DummyReactive : INotifyPropertyChanged
        {
            private string _name;
            public event PropertyChangedEventHandler PropertyChanged;

            public IObservable<object> Changing => _changing;
            public IObservable<object> Changed => _changed;

            private readonly Subject<object> _changing = new Subject<object>();
            private readonly Subject<object> _changed = new Subject<object>();

            public string Name
            {
                get => _name;
                set
                {
                    _changing.OnNext(new { PropertyName = nameof(Name) });
                    _name = value;
                    _changed.OnNext(new { PropertyName = nameof(Name) });
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        [Fact]
        public void ReactiveAdapter_RecordsPropertyChanges_FromObservables()
        {
            var service = new UndoRedoService();
            var adapter = new ReactiveAdapter(service);

            var d = new DummyReactive();
            adapter.Register(d);

            d.Name = "a";
            Assert.True(service.CanUndo);
        }

        [Fact]
        public void ReactiveAdapter_Fallback_INotifyPropertyChanged()
        {
            var service = new UndoRedoService();
            var adapter = new ReactiveAdapter(service);

            var d = new DummyReactive();
            // trigger only the INotifyPropertyChanged path
            d.Name = "x";
            adapter.Register(d);
            d.Name = "y";

            Assert.True(service.CanUndo);
        }
    }
}
