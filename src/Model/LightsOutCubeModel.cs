using System;
using System.Collections.Generic;
using System.Text;

namespace LightsOutCube.Model
{
    static class LightsOutCubeModel
    {
        public const int LEFT = 0;
        public const int RIGHT = 2;
        public const int UP = 1;
        public const int DOWN = 3;

        static readonly int[,] Offsets = {
            { 0, 0, 0, 0 },
            { 52, 48, 1, 3 },
            { 59, 44, 1, 3 },
            { 59, 40, 20, 3 },
            { 52, 57, 1, 3 },
            { 59, 57, 1, 3 },
            { 59, 57, 16, 3 },
            { 52, 57, 1, 4 },
            { 59, 57, 1, 4 },
            { 59, 57, 12, 4 },
            { 0, 0, 0, 0 },
            { 48, 56, 1, 3 },
            { 59, 56, 1, 3 },
            { 59, 56, 8, 3 },
            { 44, 57, 1, 3 },
            { 59, 57, 1, 3 },
            { 59, 57, 8, 3 },
            { 40, 57, 1, 20 },
            { 59, 57, 1, 16 },
            { 59, 57, 8, 12 }
        };
        static readonly List<long> aTog1 = [];
        static readonly List<long> aTog5 = [];
        static bool initialized = false;
        private static void Init()
        {
            if (initialized) return;

            long m = 2;
            aTog1.Add(0);
            aTog5.Add(0);
            for (int i = 1; i < 60; i++)
            {
                aTog1.Add(m);
                aTog5.Add(0);
                m = m * 2;
            }

            for (int i = 1; i < 60; i++)
            {
                if ((i % 10) != 0)
                {
                    TogButton(i, i);
                    TogButton(i, Adjacent(i, LEFT));
                    TogButton(i, Adjacent(i, UP));
                    TogButton(i, Adjacent(i, DOWN));
                    TogButton(i, Adjacent(i, RIGHT));
                }
            }
            initialized = true;
        }
        private static void TogButton(int index, int but)
        {
            aTog5[index] |= aTog1[but];
        }
        public static int Adjacent(int i, int j)
        {
            return (i + Offsets[i % 20, j]) % 60;
        }

        public static void Tog1(int index,ref long loc)
        {
            Init();
            loc ^= aTog1[index];
        }

        public static void Tog5(int index,ref long loc)
        {
            Init();
            loc ^= aTog5[index];
        }



    }
}
