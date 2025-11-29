using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightsOutCube;
using LightsOutCube.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml;

namespace LightsOutCube.ViewModels
{
    public class CubeViewModel : ObservableObject
    {
        PuzzleModel puzzleModel = new PuzzleModel();
        public CubeViewModel()
        {
            // Load puzzles into the ViewModel

            Cells = new ObservableCollection<CellViewModel>();
            CellsByIndex = new Dictionary<int, CellViewModel>();
            puzzleModel.LoadPuzzles(LightsOutCube.Properties.Resources.Puzzle);

            PuzzleList = new ObservableCollection<int>();
            var n = puzzleModel.Puzzles.FirstChild;
            for (int i = 0; i < n.ChildNodes.Count; i++)
                PuzzleList.Add(i + 1);

            ResetCommand = new RelayCommand(ResetPuzzle);
            SelectedPuzzle = 1;
        }
        public bool Solved => puzzleModel.State == 0;

        public ICommand ResetCommand { get; }
        private void ResetPuzzle()
        {
            puzzleModel.Reset();
            SetCube();
        }

        public void SetPuzzle(int iPuzzle)
        {
            puzzleModel.SetPuzzle(iPuzzle);
            SetCube();
        }

        public void SetCube()
        {
            // Ensure cells have been initialised by the View
            if (CellsByIndex == null || CellsByIndex.Count == 0)
                return;

            // Update CellViewModel.IsOn flags from the puzzle model state
            UpdateCellsFromState();

            // Notify consumers that solved state may have changed
            OnPropertyChanged(nameof(Solved));
        }

        public void Toggle(int buttonIndex)
        {
            puzzleModel.Toggle(buttonIndex);
            SetCube();
        }
        // Cells exposed to the View (no WPF visual types here)
        public ObservableCollection<CellViewModel> Cells { get; }
        public Dictionary<int, CellViewModel> CellsByIndex { get; private set; }
        public void InitializeCells(IEnumerable<int> indices)
        {
            Cells.Clear();
            CellsByIndex = new Dictionary<int, CellViewModel>();
            foreach (var i in indices)
            {
                var c = new CellViewModel(i);
                Cells.Add(c);
                CellsByIndex[i] = c;
            }

            UpdateCellsFromState();
        }

        // Update the CellViewModel.IsOn flags from the puzzle model state
        void UpdateCellsFromState()
        {
            if (CellsByIndex == null) return;

            foreach (var kvp in CellsByIndex)
            {
                int i = kvp.Key;
                var cell = kvp.Value;
                // mask: same mapping as original code (bit for index i is 1 << i)
                long mask = 1L << i;
                cell.IsOn = (puzzleModel.State & mask) != 0;
            }
        }

        int _selectedPuzzle;
        public int SelectedPuzzle
        {
            get => _selectedPuzzle;
            set
            {
                if (SetProperty(ref _selectedPuzzle, value))
                {
                    SetPuzzle(value);
                }
            }
        }
        // Observable puzzle list for binding
        public ObservableCollection<int> PuzzleList { get; }
    }
}