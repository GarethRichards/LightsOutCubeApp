using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightsOutCube.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Threading;

namespace LightsOutCube.ViewModels
{
    public class CubeViewModel : ObservableObject
    {
        private const int SpeedRunPuzzleId = 0; // sentinel id selected in the drop-down to start a speed run

        private readonly PuzzleModel _puzzleModel = new PuzzleModel();
        private readonly Stopwatch _solveTimer = new Stopwatch();
        private readonly Stopwatch _speedRunStopwatch = new Stopwatch();
        private readonly LightsOutCube.Model.ScoreStore _scoreStore = new LightsOutCube.Model.ScoreStore();

        // UI timer to update elapsed display
        private readonly DispatcherTimer _uiTimer;

        // speed-run state
        private bool _isSpeedRunMode;
        public bool IsSpeedRunMode
        {
            get => _isSpeedRunMode;
            private set => SetProperty(ref _isSpeedRunMode, value);
        }

        private readonly List<ScoreRecord> _speedRunRecords = new List<ScoreRecord>();
        public IReadOnlyList<ScoreRecord> SpeedRunRecords => _speedRunRecords.AsReadOnly();

        private int _speedRunSolvedCount;
        public int SpeedRunSolvedCount
        {
            get => _speedRunSolvedCount;
            private set => SetProperty(ref _speedRunSolvedCount, value);
        }

        public TimeSpan SpeedRunElapsed => _speedRunStopwatch.Elapsed;

        // formatted text for binding (bind this in XAML)
        public string SpeedRunElapsedText => FormatDuration(SpeedRunElapsed);

        // used to detect solved transitions (so we call the solved handling once)
        private bool _wasSolved;

        public CubeViewModel()
        {
            // UI timer setup (updates elapsed display ~10x/sec)
            _uiTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(100),                 // update ~10x/sec (adjustable)
                DispatcherPriority.Normal,
                (s, e) =>
                {
                    if (IsSpeedRunMode)
                        OnPropertyChanged(nameof(SpeedRunElapsedText));
                },
                Dispatcher.CurrentDispatcher);

            // Load puzzles into the ViewModel
            Cells = new ObservableCollection<CellViewModel>();
            CellsByIndex = new Dictionary<int, CellViewModel>();

            using (var stream = OpenPuzzleStream())
            {
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine("Puzzle.xml resource not found in any location.");
                }
                else
                {
                    _puzzleModel.LoadPuzzles(stream);
                }
            }

            PuzzleList = new ObservableCollection<int>();
            var n = _puzzleModel.Puzzles.FirstChild;
            for (int i = 0; i < n.ChildNodes.Count; i++)
                PuzzleList.Add(i + 1);

            // add a special item at the start for Speed Run (UI can render this sentinel as "Speed Run")
            PuzzleList.Insert(0, SpeedRunPuzzleId);

            ResetCommand = new RelayCommand(ResetPuzzle);

            // initialize state
            _pressCount = 0;
            _solutionMask = 0L;
            _solutionPressCount = 0;
            _wasSolved = false;

            // command to toggle showing solution (View binds to this)
            ToggleShowSolutionCommand = new RelayCommand(ToggleShowSolution);

            // default to first real puzzle
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
        public bool Solved => _puzzleModel.State == 0;

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
            EndSpeedRun();
            ShowSolution = !ShowSolution;
        }

        // --- Commands ---
        public ICommand ResetCommand { get; }

        private void ResetPuzzle()
        {
            _puzzleModel.Reset();
            PressCount = 0;
            // recompute solution for current puzzle state (after reset)
            ComputeSolutionForCurrentPuzzle();
            SetCube();
            // ensure solution hidden after reset
            ShowSolution = false;

            // reset timers / solved tracking for a fresh puzzle
            _solveTimer.Reset();
            _wasSolved = false;
        }

        public void SetPuzzle(int iPuzzle)
        {
            _puzzleModel.SetPuzzle(iPuzzle);
            // new puzzle -> reset press count and compute solution
            PressCount = 0;
            // hide any solution when puzzle changes
            ShowSolution = false;
            SetCube();
            // start per-puzzle timer
            OnPuzzleStarted();
            ComputeSolutionForCurrentPuzzle();
            OnPropertyChanged(nameof(SolvedBannerText));
            OnPropertyChanged(nameof(SolvedBannerAdditional));
        }

        private void CubePuzzleSolved()
        {
            _wasSolved = true;
            var rec = OnPuzzleSolved(); // creates and persists per-puzzle record
                                        // if we are in speed-run mode collect the per-puzzle record and continue/finish run
            if (IsSpeedRunMode)
            {
                if (rec != null)
                    _speedRunRecords.Add(rec);

                SpeedRunSolvedCount++;

                // advance to next puzzle if available, otherwise end speed run
                int maxPuzzle = PuzzleList.Where(x => x != SpeedRunPuzzleId).DefaultIfEmpty(1).Max();
                if (_selectedPuzzle < maxPuzzle)
                {
                    // move to next puzzle and continue the run
                    // set backing field directly to avoid re-triggering StartSpeedRun
                    _selectedPuzzle = _selectedPuzzle + 1;
                    OnPropertyChanged(nameof(SelectedPuzzle));
                    SetPuzzle(_selectedPuzzle);
                }
                else
                {
                    EndSpeedRun();
                }
            }

        }

        // Called by the View when model changes or a toggle occurs
        public void SetCube()
        {
            // Ensure cells have been initialised by the View
            if (CellsByIndex == null || CellsByIndex.Count == 0)
                return;

            // capture previous solved state so we only run solved handling once on transition
            var previouslySolved = _wasSolved;

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

                // call solved handling only once when the puzzle becomes solved
                if (!previouslySolved)
                {
                    CubePuzzleSolved();
                }
            }
            else
            {
                // ensure solved tracking cleared while puzzle is not solved
                _wasSolved = false;
            }
        }

        // Toggle increments the press counter and updates model
        public void Toggle(int buttonIndex)
        {
            // increment press count (the user pressed a button)
            PressCount++;

            _puzzleModel.Toggle(buttonIndex);
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
                cell.IsOn = (_puzzleModel.State & mask) != 0;
            }
        }

        private int _selectedPuzzle;
        public int SelectedPuzzle
        {
            get => _selectedPuzzle;
            set
            {
                // if user selects the Speed Run sentinel, start the mode
                if (value == SpeedRunPuzzleId && !_isSpeedRunMode)
                {
                    StartSpeedRun();
                    return;
                }

                // if a user manually changes the selected puzzle while a speed run is active,
                // exit speed run mode so manual selection takes precedence.
                if (_isSpeedRunMode && value != SpeedRunPuzzleId)
                {
                    EndSpeedRun();
                }

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

        // start a speed run: reset counters, start total stopwatch and select first puzzle
        private void StartSpeedRun()
        {
            IsSpeedRunMode = true;
            _speedRunRecords.Clear();
            SpeedRunSolvedCount = 0;
            _speedRunStopwatch.Restart();
            _uiTimer.Start();
            // immediate update so UI shows 0.000s right away
            OnPropertyChanged(nameof(SpeedRunElapsedText));

            // select first real puzzle (first non-sentinel entry)
            var firstPuzzle = PuzzleList.FirstOrDefault(x => x != SpeedRunPuzzleId);
            if (firstPuzzle == 0) firstPuzzle = 1;
            // set backing field and notify so UI shows correct selection
            _selectedPuzzle = firstPuzzle;
            OnPropertyChanged(nameof(SelectedPuzzle));

            // set puzzle to begin the run
            SetPuzzle(firstPuzzle);
        }

        // finish a speed run
        private void EndSpeedRun()
        {
            _speedRunStopwatch.Stop();
            _uiTimer.Stop();
            IsSpeedRunMode = false;
            // final update so UI shows final time
            OnPropertyChanged(nameof(SpeedRunElapsedText));
            // Optionally: persist a summary, surface UI notification or expose SpeedRunRecords to other components.
            // For now, callers can read SpeedRunSolvedCount, SpeedRunElapsed and SpeedRunRecords.
            // Persist last speed run summary (last puzzle solved + per-puzzle times)
            try
            {
                if (_speedRunRecords.Count > 0)
                {
                    var summary = new SpeedRunSummary
                    {
                        Timestamp = DateTimeOffset.UtcNow,
                        LastPuzzleSolved = _speedRunRecords[_speedRunRecords.Count-1].PuzzleId,
                        TimesMs = _speedRunRecords.Select(r => (long)r.Duration.TotalMilliseconds).ToList(),
                        PressCounts = _speedRunRecords.Select(r => r.PressCount).ToList(),
                        IsPerfect = _speedRunRecords.Select(r => r.IsPerfect).ToList(),
                        TotalElapsedMs = (long)_speedRunStopwatch.Elapsed.TotalMilliseconds,
                        SolvedCount = _speedRunRecords.Count
                    };

                    _scoreStore.SaveLastSpeedRun(summary);
                }
            }
            catch
            {
                // best-effort persistence; swallow errors
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
                solver.SetCurrent(_puzzleModel.State);

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

        private static Stream OpenPuzzleStream()
        {
            // 1) Try WPF pack URI (when running as the app)
            try
            {
                var ri = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Puzzle.xml", UriKind.Absolute));
                if (ri?.Stream != null)
                    return ri.Stream;
            }
            catch { /* ignore */ }

            // 2) Try manifest resource (if Puzzle.xml is Embedded Resource)
            try
            {
                var asm = typeof(PuzzleModel).Assembly;
                var names = asm.GetManifestResourceNames();
                var candidate = names.FirstOrDefault(n => n.EndsWith("Resources.Puzzle.xml", StringComparison.OrdinalIgnoreCase)
                                                       || n.EndsWith(".Puzzle.xml", StringComparison.OrdinalIgnoreCase));
                if (candidate != null)
                {
                    var s = asm.GetManifestResourceStream(candidate);
                    if (s != null) return s;
                }
            }
            catch { /* ignore */ }

            // 3) Try file on disk (Content, copied to output folder)
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Puzzle.xml");
                if (File.Exists(path))
                    return File.OpenRead(path);

                // also try a simple filename fallback
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Puzzle.xml");
                if (File.Exists(path))
                    return File.OpenRead(path);
            }
            catch { /* ignore */ }

            return null;
        }

        // call when puzzle starts (example method)
        private void OnPuzzleStarted()
        {
            _solveTimer.Restart();
        }

        // call when puzzle solved
        // returns the created ScoreRecord (or null on error) so callers (eg speed run) can collect it
        private ScoreRecord OnPuzzleSolved()
        {
            try
            {
                _solveTimer.Stop();
                var elapsed = _solveTimer.Elapsed;
                int presses = this.PressCount; // already tracked in your VM
                // Determine perfect: prefer comparing to minimal moves if solver exposes it:
                bool isPerfect = false;
                if (this.SolutionPressCount > 0) // SolutionPressCount = minimal presses from solver
                    isPerfect = (presses == this.SolutionPressCount);

                var record = new LightsOutCube.Model.ScoreRecord
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Duration = elapsed,
                    PressCount = presses,
                    IsPerfect = isPerfect,
                    PuzzleId = this.SelectedPuzzle.ToString(),
                    AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                };

                // Persist only if this is the first record for the puzzle or faster than the stored best
                _scoreStore.AddIfBest(record);

                return record;
            }
            catch
            {
                return null;
            }
            finally
            {
                // reset per-puzzle timer so next puzzle starts fresh; if in speed run the run stopwatch keeps running
                _solveTimer.Reset();
            }
        }

        private static string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalMinutes >= 1)
                return ts.ToString(@"mm\:ss\.fff");
            return ts.ToString(@"s\.fff") + "s";
        }
    }
}