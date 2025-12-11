using System;
using System.Collections.Generic;
using System.Text;

namespace LightsOutCube.Model
{
    class LightsOutCubeModel
    {
        public const int LEFT = 0;
        public const int RIGHT = 2;
        public const int UP = 1;
        public const int DOWN = 3;

        static List<List<int>> Offsets = new List<List<int>>();
        static List<long> aTog1 = new List<long>();
        static List<long> aTog5 = new List<long>();

        public LightsOutCubeModel()
        {
            int[] i0 = { 0, 0, 0, 0 };
            Offsets.Add(new List<int>(i0));
            int[] i1 = { 52, 48, 1, 3 };
            Offsets.Add(new List<int>(i1));
            int[] i2 = { 59, 44, 1, 3 };
            Offsets.Add(new List<int>(i2));
            int[] i3 = { 59, 40, 20, 3 };
            Offsets.Add(new List<int>(i3));
            int[] i4 = { 52, 57, 1, 3 };
            Offsets.Add(new List<int>(i4));
            int[] i5 = { 59, 57, 1, 3 };
            Offsets.Add(new List<int>(i5));
            int[] i6 = { 59, 57, 16, 3 };
            Offsets.Add(new List<int>(i6));
            int[] i7 = { 52, 57, 1, 4 };
            Offsets.Add(new List<int>(i7));
            int[] i8 = { 59, 57, 1, 4 };
            Offsets.Add(new List<int>(i8));
            int[] i9 = { 59, 57, 12, 4 };
            Offsets.Add(new List<int>(i9));
            int[] i10 = { 0, 0, 0, 0 };
            Offsets.Add(new List<int>(i10));
            int[] i11 = { 48, 56, 1, 3 };
            Offsets.Add(new List<int>(i11));
            int[] i12 = { 59, 56, 1, 3 };
            Offsets.Add(new List<int>(i12));
            int[] i13 = { 59, 56, 8, 3 };
            Offsets.Add(new List<int>(i13));
            int[] i14 = { 44, 57, 1, 3 };
            Offsets.Add(new List<int>(i14));
            int[] i15 = { 59, 57, 1, 3 };
            Offsets.Add(new List<int>(i15));
            int[] i16 = { 59, 57, 8, 3 };
            Offsets.Add(new List<int>(i16));
            int[] i17 = { 40, 57, 1, 20 };
            Offsets.Add(new List<int>(i17));
            int[] i18 = { 59, 57, 1, 16 };
            Offsets.Add(new List<int>(i18));
            int[] i19 = { 59, 57, 8, 12 };
            Offsets.Add(new List<int>(i19));

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
        }

        public static int Adjacent(int i, int j)
        {
            return (i + Offsets[i % 20][j]) % 60;
        }

        public static void Tog1(int index,ref long loc)
        {
            loc ^= aTog1[index];
        }

        public static void Tog5(int index,ref long loc)
        {
            loc ^= aTog5[index];
        }

        public static void TogButton(int index, int but)
        {
            aTog5[index] |= aTog1[but];
        }

    }
}
