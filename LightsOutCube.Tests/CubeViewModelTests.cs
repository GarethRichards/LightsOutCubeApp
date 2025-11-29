using Microsoft.VisualStudio.TestTools.UnitTesting;
using LightsOutCube.ViewModels;
using System.Linq;
using System.Collections.Generic;

namespace LightsOutCube.Tests
{
    [TestClass]
    public class CubeViewModelTests
    {
        private static IEnumerable<int> AllIndices()
        {
            for (int i = 1; i < 60; i++)
            {
                if (i % 10 != 0) yield return i;
            }
        }

        [TestMethod]
        public void InitializeCells_PopulatesCellsAndLookup()
        {
            var vm = new CubeViewModel();
            vm.InitializeCells(new[] { 1, 2, 3 });

            Assert.AreEqual(3, vm.Cells.Count);
            Assert.IsTrue(vm.CellsByIndex.ContainsKey(1));
            Assert.IsTrue(vm.CellsByIndex.ContainsKey(2));
            Assert.IsTrue(vm.CellsByIndex.ContainsKey(3));
        }

        [TestMethod]
        public void Toggle_TogglesCellState()
        {
            var vm = new CubeViewModel();
            var index = AllIndices().First();
            vm.InitializeCells(new[] { index });

            bool before = vm.CellsByIndex[index].IsOn;

            vm.Toggle(index);
            bool after = vm.CellsByIndex[index].IsOn;
            Assert.AreNotEqual(before, after, "Toggle should flip the IsOn flag");

            vm.Toggle(index);
            Assert.AreEqual(before, vm.CellsByIndex[index].IsOn, "Toggle twice should restore original state");
        }

        [TestMethod]
        public void SetPuzzle_ChangesCellPatterns()
        {
            var vm = new CubeViewModel();
            var indices = AllIndices().ToArray();
            vm.InitializeCells(indices);

            vm.SetPuzzle(1);
            var pattern1 = indices.Select(i => vm.CellsByIndex[i].IsOn).ToArray();

            vm.SetPuzzle(2);
            var pattern2 = indices.Select(i => vm.CellsByIndex[i].IsOn).ToArray();

            Assert.IsFalse(pattern1.SequenceEqual(pattern2), "Different puzzles should produce different initial patterns");
        }
    }
}