using Microsoft.VisualStudio.TestTools.UnitTesting;
using LightsOutCube.ViewModels;
using System.Linq;

namespace LightsOutCube.Tests
{
    [TestClass]
    public class SolverTests
    {
        static void Solve_puzzle_and_verify_solution(int puzzle_index)
        {
            // Arrange
            var vm = new CubeViewModel();

            // Initialize cell indices used by the solver (1..59)
            vm.InitializeCells(Enumerable.Range(1, 59));

            // Ensure puzzle 1 is loaded and solution computed
            vm.SetPuzzle(puzzle_index);

            var solutionMask = vm.SolutionMask;
            Assert.IsTrue(solutionMask != 0L, $"Solver returned no solution for puzzle {puzzle_index}.");

            // Act: apply each press in the solution mask
            for (int i = 1; i <= 59; i++)
            {
                if ((solutionMask & (1L << i)) != 0)
                {
                    vm.Toggle(i);
                }
            }
            // Assert: ViewModel should report the puzzle solved
            Assert.IsTrue(vm.Solved, "Puzzle was not solved after applying solver presses.");
        }
        [TestMethod]
        public void Solve_EveryPuzzle()
        {
            for (int i = 1; i < 54; i++)
            {
                Solve_puzzle_and_verify_solution(i);
            }
        }
        [TestMethod]
        public void Toggle_IncrementsPressCount()
        {
            var vm = new CubeViewModel();
            vm.InitializeCells(Enumerable.Range(1, 59));
            vm.SetPuzzle(1);

            int initialCount = vm.PressCount;
            vm.Toggle(5);

            Assert.AreEqual(initialCount + 1, vm.PressCount);
        }

        [TestMethod]
        public void Reset_ClearsPressCount()
        {
            var vm = new CubeViewModel();
            vm.InitializeCells(Enumerable.Range(1, 59));
            vm.SetPuzzle(1);
            vm.Toggle(1);

            vm.ResetCommand.Execute(null);

            Assert.AreEqual(0, vm.PressCount);
        }

    }
}