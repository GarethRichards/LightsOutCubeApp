using LightsOutCube.ViewModels;
using System;
using System.IO;
using System.Xml;

namespace LightsOutCube.Model
{
    internal class PuzzleModel
    {
        readonly XmlDocument puzzles = new XmlDocument();
        public XmlDocument Puzzles => puzzles;

        private long state;
        private long oldState;
        private long initialState;
        public long State
        {
            get => state;
            private set { state = value; }
        }
        public long OldState
        {
            get => oldState;
        }
        public long InitialState
        {
            get => initialState;
        }

        public void LoadPuzzles(Stream stream)
        {
            string puzzlesXml;
            using (var sr = new StreamReader(stream))
            {
                puzzlesXml = sr.ReadToEnd();
            }
            puzzles.LoadXml(puzzlesXml);
        }

        public void SetPuzzle(int iPuzzle)
        {
            oldState = State;
            if (puzzles.FirstChild == null) throw new InvalidOperationException("Puzzles not loaded.");
            XmlNode n = puzzles.FirstChild;
            if (iPuzzle < 1 || iPuzzle > n.ChildNodes.Count) throw new ArgumentOutOfRangeException(nameof(iPuzzle));
            XmlNode p = n.ChildNodes[iPuzzle - 1];
            State = 0;
            for (int i = 0; i < p.ChildNodes.Count; i++)
            {
                int but = Convert.ToInt32(p.ChildNodes[i].InnerText);
                LightsOutCubeModel.Tog1(but, ref state);
            }            // record the initial state for this puzzle so Reset() can restore it
            initialState = state;
        }

        /// <summary>
        /// Toggle the given button index (uses model's Tog5). Returns the new state.
        /// </summary>
        public void Toggle(int buttonIndex)
        {
            oldState = State;
            LightsOutCubeModel.Tog5(buttonIndex, ref state);
        }

        /// <summary>
        /// Reset the puzzle to the initial state that was set by SetPuzzle.
        /// </summary>
        public void Reset()
        {
            oldState = state;
            state = initialState;
        }
    }
}
