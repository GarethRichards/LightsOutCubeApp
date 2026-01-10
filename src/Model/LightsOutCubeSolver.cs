using System;

namespace LightsOutCube.Model
{

    internal class LightsOutCubeSolver
    {
        private long _solution;    // buttons that need to be pressed.
        private long _current;
        private int _nSolutions;

        // bottom button indices (as in original)
        private readonly int[] _bottom = new int[] { 1, 1, 2, 3, 3, 6, 9, 9, 8, 7, 7, 4 };

        // middle, bot, mid, top arrays (initialized in ctor to the same sizes used in original Java)
        private readonly int[][] _middle;   // middle buttons (12 x 3)
        private readonly long[] _bot;      // bottom LightsOut masks for 12 positions
        private readonly long[][] _mid;    // mid masks (12 x 3)
        private readonly long[][] _top;    // top masks (8 x 2)

        public LightsOutCubeSolver()
        {
            // initialize jagged arrays with same dimensions as original Java
            _middle = new int[12][];
            for (int i = 0; i < 12; i++) _middle[i] = new int[3];

            _bot = new long[12];

            _mid = new long[12][];
            for (int i = 0; i < 12; i++) _mid[i] = new long[3];

            _top = new long[8][];
            for (int i = 0; i < 8; i++) _top[i] = new long[2];

            _middle[0][0] = LightsOutCubeModel.Adjacent(1, LightsOutCubeModel.LEFT);
            _middle[1][0] = LightsOutCubeModel.Adjacent(1, LightsOutCubeModel.UP);
            _middle[2][0] = LightsOutCubeModel.Adjacent(2, LightsOutCubeModel.UP);
            _middle[3][0] = LightsOutCubeModel.Adjacent(3, LightsOutCubeModel.UP);
            _middle[4][0] = LightsOutCubeModel.Adjacent(3, LightsOutCubeModel.RIGHT);
            _middle[5][0] = LightsOutCubeModel.Adjacent(6, LightsOutCubeModel.RIGHT);
            _middle[6][0] = LightsOutCubeModel.Adjacent(9, LightsOutCubeModel.RIGHT);
            _middle[7][0] = LightsOutCubeModel.Adjacent(9, LightsOutCubeModel.DOWN);
            _middle[8][0] = LightsOutCubeModel.Adjacent(8, LightsOutCubeModel.DOWN);
            _middle[9][0] = LightsOutCubeModel.Adjacent(7, LightsOutCubeModel.DOWN);
            _middle[10][0] = LightsOutCubeModel.Adjacent(7, LightsOutCubeModel.LEFT);
            _middle[11][0] = LightsOutCubeModel.Adjacent(4, LightsOutCubeModel.LEFT);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    _middle[0][i + 1] = LightsOutCubeModel.Adjacent(_middle[0][i], LightsOutCubeModel.LEFT);
                    _middle[1][i + 1] = LightsOutCubeModel.Adjacent(_middle[1][i], LightsOutCubeModel.LEFT);
                    _middle[2][i + 1] = LightsOutCubeModel.Adjacent(_middle[2][i], LightsOutCubeModel.LEFT);
                    _middle[3][i + 1] = LightsOutCubeModel.Adjacent(_middle[3][i], LightsOutCubeModel.LEFT);
                    _middle[4][i + 1] = LightsOutCubeModel.Adjacent(_middle[4][i], LightsOutCubeModel.DOWN);
                    _middle[5][i + 1] = LightsOutCubeModel.Adjacent(_middle[5][i], LightsOutCubeModel.DOWN);
                    _middle[6][i + 1] = LightsOutCubeModel.Adjacent(_middle[6][i], LightsOutCubeModel.DOWN);
                    _middle[7][i + 1] = LightsOutCubeModel.Adjacent(_middle[7][i], LightsOutCubeModel.DOWN);
                    _middle[8][i + 1] = LightsOutCubeModel.Adjacent(_middle[8][i], LightsOutCubeModel.DOWN);
                    _middle[9][i + 1] = LightsOutCubeModel.Adjacent(_middle[9][i], LightsOutCubeModel.DOWN);
                    _middle[10][i + 1] = LightsOutCubeModel.Adjacent(_middle[10][i], LightsOutCubeModel.LEFT);
                    _middle[11][i + 1] = LightsOutCubeModel.Adjacent(_middle[11][i], LightsOutCubeModel.LEFT);
                }
            }
            _top[0][1] = LightsOutCubeModel.Adjacent(_middle[0][2], LightsOutCubeModel.LEFT);
            _top[1][1] = LightsOutCubeModel.Adjacent(_middle[2][2], LightsOutCubeModel.LEFT);
            _top[2][1] = LightsOutCubeModel.Adjacent(_middle[3][2], LightsOutCubeModel.LEFT);
            _top[3][1] = LightsOutCubeModel.Adjacent(_middle[5][2], LightsOutCubeModel.DOWN);
            _top[4][1] = LightsOutCubeModel.Adjacent(_middle[6][2], LightsOutCubeModel.DOWN);
            _top[5][1] = LightsOutCubeModel.Adjacent(_middle[8][2], LightsOutCubeModel.DOWN);
            _top[6][1] = LightsOutCubeModel.Adjacent(_middle[9][2], LightsOutCubeModel.DOWN);
            _top[7][1] = LightsOutCubeModel.Adjacent(_middle[11][2], LightsOutCubeModel.LEFT);
            for (int j = 0; j < 12; j++)
            {
                LightsOutCubeModel.Tog1(_bottom[j], ref _bot[j]);
                for (int i = 0; i < 3; i++)
                {
                    LightsOutCubeModel.Tog1(_middle[j][i], ref _mid[j][i]);
                }
            }
            _top[0][0] = _mid[0][2];
            _top[1][0] = _mid[2][2];
            _top[2][0] = _mid[3][2];
            _top[3][0] = _mid[5][2];
            _top[4][0] = _mid[6][2];
            _top[5][0] = _mid[8][2];
            _top[6][0] = _mid[9][2];
            _top[7][0] = _mid[11][2];
        }

        /// <summary>
        /// Attempts to solve the puzzle using the original search approach.
        /// Returns true if at least one solution was found and stores a minimal button solution in _solution.
        /// Note: This method uses external static helper methods in LightsOutCubeModel (tog5, tog1).
        /// </summary>
        public bool Solve()
        {
            // Start at the bottom
            _nSolutions = 0;
            _solution = 0L;
            bool solved = false;
            int minButs = int.MaxValue;

            // iterate possible bottom combinations (512 combinations; stepping by 2 as in original)
            for (long botbuts = 0; botbuts < 1024; botbuts += 2L)
            {
                long butMask = 2; // starting power-of-two mask (2^1)
                long c = _current;
                int butIndex = 1;
                long sol = 0;
                int nbuts = 0;

                // apply bottom combination to current state
                do
                {
                    if ((butMask & botbuts) != 0)
                    {
                        LightsOutCubeModel.Tog5(butIndex, ref c);
                        sol |= butMask;
                        nbuts++;
                    }
                    butMask <<= 1;
                    butIndex++;
                } while (butIndex < 10);

                // if middle light cleared, attempt to chase up solution
                if ((c & 32L) == 0L) // test same bit as original (bit 5)
                {
                    long cold = c;
                    long solOld = sol;
                    int nbutsOld = nbuts;

                    // corner lights can be turned off two ways (16 combinations)
                    for (int cornerMask = 0; cornerMask < 16; cornerMask++)
                    {
                        c = cold;
                        sol = solOld;
                        nbuts = nbutsOld;

                        int cpow2 = 1;
                        int cornerCounter = 0;

                        for (int j = 0; j < 12; j++)
                        {
                            if (j < 11 && _bottom[j] == _bottom[j + 1])
                            {
                                // first corner of a pair; may optionally press based on cornerMask
                                if ((cornerMask & cpow2) != 0)
                                {
                                    LightsOutCubeModel.Tog5(_middle[j][0], ref c);
                                    sol ^= _mid[j][0];
                                    nbuts++;
                                }
                                cornerCounter++;
                                cpow2 <<= 1;
                            }
                            else if ((_bot[j] & c) != 0)
                            {
                                // middle or second corner light: must press
                                LightsOutCubeModel.Tog5(_middle[j][0], ref c);
                                sol ^= _mid[j][0];
                                nbuts++;
                            }
                        }

                        // chase lights up through the middle rows (i2 = 1..2)
                        for (int i2 = 1; i2 < 3; i2++)
                        {
                            for (int j = 0; j < 12; j++)
                            {
                                if ((c & _mid[j][i2 - 1]) != 0)
                                {
                                    LightsOutCubeModel.Tog5(_middle[j][i2], ref c);
                                    sol ^= _mid[j][i2];
                                    nbuts++;
                                }
                            }
                        }

                        // Now try to turn the top lights off — there are 8 top positions
                        for (int i3 = 0; i3 < 8; i3++)
                        {
                            if ((c & _top[i3][0]) != 0)
                            {
                                LightsOutCubeModel.Tog5((int)_top[i3][1], ref c);
                                LightsOutCubeModel.Tog1((int)_top[i3][1], ref sol);
                                nbuts++;
                            }
                        }

                        // either we have the solution, pressing top middle gives the solution or there is no solution
                        for (int k = 0; k < 2; k++)
                        {
                            if (c == 0L)
                            {
                                _nSolutions++;
                                solved = true;
                                if (nbuts < minButs)
                                {
                                    minButs = nbuts;
                                    _solution = sol;
                                }
                            }

                            // Toggle the middle button (bit 35 in original)
                            LightsOutCubeModel.Tog5(35, ref c);
                            LightsOutCubeModel.Tog1(35, ref sol);
                            nbuts++;
                        }
                    }
                }
            }

            return solved;
        }

        // Expose solution and utility properties if callers need them:
        public long Solution => _solution;
        public int SolutionCount => _nSolutions;

        // Allow callers to set current state / helpers (kept simple)
        public void SetCurrent(long current) => _current = current;
        public void SetBotMasks(long[] botMasks)
        {
            if (botMasks == null) return;
            int n = Math.Min(botMasks.Length, _bot.Length);
            for (int i = 0; i < n; i++) _bot[i] = botMasks[i];
        }

        public void SetMiddle(int[][] middle)
        {
            if (middle == null) return;
            int n = Math.Min(middle.Length, _middle.Length);
            for (int i = 0; i < n; i++)
            {
                if (middle[i] == null) continue;
                int m = Math.Min(middle[i].Length, _middle[i].Length);
                for (int j = 0; j < m; j++) _middle[i][j] = middle[i][j];
            }
        }

        public void SetMidMasks(long[][] midMasks)
        {
            if (midMasks == null) return;
            int n = Math.Min(midMasks.Length, _mid.Length);
            for (int i = 0; i < n; i++)
            {
                if (midMasks[i] == null) continue;
                int m = Math.Min(midMasks[i].Length, _mid[i].Length);
                for (int j = 0; j < m; j++) _mid[i][j] = midMasks[i][j];
            }
        }

        public void SetTopMasks(long[][] topMasks)
        {
            if (topMasks == null) return;
            int n = Math.Min(topMasks.Length, _top.Length);
            for (int i = 0; i < n; i++)
            {
                if (topMasks[i] == null) continue;
                int m = Math.Min(topMasks[i].Length, _top[i].Length);
                for (int j = 0; j < m; j++) _top[i][j] = topMasks[i][j];
            }
        }
    }
}
