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

namespace LightsOutCube.ViewModels
{
    public class CubeViewModel : ObservableObject
    {
        private readonly PuzzleModel puzzleModel = new PuzzleModel();

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

            // initialize state
            _pressCount = 0;
            _solutionMask = 0L;
            _solutionPressCount = 0;

            // command to toggle showing solution (View binds to this)
            ToggleShowSolutionCommand = new RelayCommand(ToggleShowSolution);

            SelectedPuzzle = 1;
        }

        // --- New state for solution and press counting ---
        private long _solutionMask;
        public long SolutionMask
        {
            get => _solutionMask;
            private set => SetProperty(ref _solutionMask, value);
        }

        private int _solutionPressCount;
        public int SolutionPressCount
        {
            get => _solutionPressCount;
            private set => SetProperty(ref _solutionPressCount, value);
        }

        private int _pressCount;
        public int PressCount
        {
            get => _pressCount;
            private set => SetProperty(ref _pressCount, value);
        }

        // If true the user used exactly the minimal number of presses
        private bool _perfectSolution;
        public bool PerfectSolution
        {
            get => _perfectSolution;
            private set => SetProperty(ref _perfectSolution, value);
        }

        // Message to display when solved (can be "Perfect solution!" or fallback)
        public string SolvedMessage
        {
            get
            {
                if (!Solved) return string.Empty;
                return PerfectSolution ? "Perfect solution!" : $"Solved Level {SelectedPuzzle}";
            }
        }

        // Banner text (View binds to this)
        public string SolvedBannerText
        {
            get
            {
                if (!Solved) return string.Empty;
                var text = $"Solved Level {SelectedPuzzle}";
                return text;
            }
        }

        // Additional banner text (separate TextBlock)
        public string SolvedBannerAdditional
        {
            get
            {
                if (!Solved) return string.Empty;
                return PerfectSolution ? "Perfect solution" : string.Empty;
            }
        }

        // Original Solved property (puzzle solved when puzzleModel.State == 0)
        public bool Solved => puzzleModel.State == 0;

        // Show-solution state moved into the ViewModel
        private bool _showSolution;
        public bool ShowSolution
        {
            get => _showSolution;
            set
            {
                if (SetProperty(ref _showSolution, value))
                {
                    OnPropertyChanged(nameof(ShowSolutionButtonText));
                }
            }
        }

        public string ShowSolutionButtonText => ShowSolution ? "Hide Solution" : "Show Solution";

        // Command exposed to the View to toggle showing the solution
        public ICommand ToggleShowSolutionCommand { get; }

        private void ToggleShowSolution()
        {
            ShowSolution = !ShowSolution;
        }

        // --- Commands ---
        public ICommand ResetCommand { get; }

        private void ResetPuzzle()
        {
            puzzleModel.Reset();
            PressCount = 0;
            // recompute solution for current puzzle state (after reset)
            ComputeSolutionForCurrentPuzzle();
            SetCube();
            // ensure solution hidden after reset
            ShowSolution = false;
        }

        public void SetPuzzle(int iPuzzle)
        {
            puzzleModel.SetPuzzle(iPuzzle);
            // new puzzle -> reset press count and compute solution
            PressCount = 0;
            // hide any solution when puzzle changes
            ShowSolution = false;
            SetCube();
            ComputeSolutionForCurrentPuzzle();
            OnPropertyChanged(nameof(SolvedBannerText));
            OnPropertyChanged(nameof(SolvedBannerAdditional));
        }

        // Called by the View when model changes or a toggle occurs
        public void SetCube()
        {
            // Ensure cells have been initialised by the View
            if (CellsByIndex == null || CellsByIndex.Count == 0)
                return;

            // Update CellViewModel.IsOn flags from the puzzle model state
            UpdateCellsFromState();

            // Re-evaluate solved/perfect solution state and notify
            OnPropertyChanged(nameof(Solved));
            // Perf/Message depend on PressCount vs SolutionPressCount
            PerfectSolution = Solved && (PressCount == SolutionPressCount);
            OnPropertyChanged(nameof(PerfectSolution));
            OnPropertyChanged(nameof(SolvedMessage));

            // Update banner text bindings
            OnPropertyChanged(nameof(SolvedBannerText));
            OnPropertyChanged(nameof(SolvedBannerAdditional));

            // If puzzle is solved, hide solution highlights (View will react to this property change)
            if (Solved)
            {
                ShowSolution = false;
            }
        }

        // Toggle increments the press counter and updates model
        public void Toggle(int buttonIndex)
        {
            // increment press count (the user pressed a button)
            PressCount++;
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
            // compute solution for the initial set
            ComputeSolutionForCurrentPuzzle();
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

        private int _selectedPuzzle;
        public int SelectedPuzzle
        {
            get => _selectedPuzzle;
            set
            {
                if (SetProperty(ref _selectedPuzzle, value))
                {
                    SetPuzzle(value);
                    // notify that solved/message may change
                    OnPropertyChanged(nameof(SolvedMessage));
                    OnPropertyChanged(nameof(SolvedBannerText));
                    OnPropertyChanged(nameof(SolvedBannerAdditional));
                }
            }
        }

        // Observable puzzle list for binding
        public void PuzzleSolved()
        {
            if (SelectedPuzzle < 54)
                SelectedPuzzle++;
            else
                SelectedPuzzle = 1;
        }
        public ObservableCollection<int> PuzzleList { get; }

        private Color _gradientStart = Colors.DarkBlue;
        public Color GradientStart
        {
            get => _gradientStart;
            set => SetProperty(ref _gradientStart, value);
        }
        private Color _gradientEnd = Colors.Black;
        public Color GradientEnd
        {
            get => _gradientEnd;
            set => SetProperty(ref _gradientEnd, value);
        }
        private readonly double _cubeSize = 3.04;
        public double CubeSize
        {
            get => _cubeSize;
        }

        // --- Solver integration ---
        // Compute solution mask and minimal press count for current puzzle state
        private void ComputeSolutionForCurrentPuzzle()
        {
            try
            {
                var solver = new LightsOutCubeSolver();
                // solver expects the current bitmask of lights; use puzzleModel.State
                solver.SetCurrent(puzzleModel.State);

                // If your solver requires masks (bot/mid/top) call solver setters here.
                if (solver.Solve())
                {
                    SolutionMask = solver.Solution;
                    SolutionPressCount = CountBits(SolutionMask);
                }
                else
                {
                    SolutionMask = 0L;
                    SolutionPressCount = 0;
                }

                // reset perfect flag (will be evaluated in SetCube after any change)
                PerfectSolution = Solved && (PressCount == SolutionPressCount);
                OnPropertyChanged(nameof(SolutionMask));
                OnPropertyChanged(nameof(SolutionPressCount));
                OnPropertyChanged(nameof(PerfectSolution));
                OnPropertyChanged(nameof(SolvedMessage));
                OnPropertyChanged(nameof(SolvedBannerText));
                OnPropertyChanged(nameof(SolvedBannerAdditional));
            }
            catch
            {
                // best-effort: clear solution on error
                SolutionMask = 0L;
                SolutionPressCount = 0;
            }
        }

        // utility: count bits in a long
        private static int CountBits(long value)
        {
            ulong v = (ulong)value;
            int count = 0;
            while (v != 0)
            {
                v &= v - 1;
                count++;
            }
            return count;
        }
    }
}