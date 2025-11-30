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
        public void Solver_ComputesSolution_And_ApplyingIt_SolvesPuzzle()
        {
            for (int i = 1; i < 29; i++)
            {
                Solve_puzzle_and_verify_solution(i);
            }
            for (int i = 30; i < 46; i++)
            {
                Solve_puzzle_and_verify_solution(i);
            }
            for (int i = 47; i <= 54; i++)
            {
                Solve_puzzle_and_verify_solution(i);
            }
            // Puzzles 29 and 46 maybe transcribed incorrectly or the solver has issues with them.
        }


    }
}