using System;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests ensuring get-only properties are not captured as separate undo actions.
    /// </summary>
    public class PersonAgeTests
    {
        /// <summary>
        /// Changing DateOfBirth should create a single undo action for DateOfBirth and not for the get-only Age property.
        /// </summary>
        [Fact]
        public void ChangingDateOfBirthShouldCreateUndoActionButNotForAgeGetOnly()
        {
            // arrange
            var service = new UndoRedoService(new TestCoreLogger());
            var vm = new PersonViewModel();

            // Attach vm to service so registrar subscribes
            service.Attach(vm);

            // act - change date of birth
            vm.DateOfBirth = new DateTime(1990, 1, 1);

            // assert - there should be an undo available (for DateOfBirth)
            Assert.True(service.CanUndo);

            // perform undo and verify DOB reverted
            service.Undo();
            Assert.Null(vm.DateOfBirth);

            // after undo stack should be empty
            Assert.False(service.CanUndo);
        }
    }
}
