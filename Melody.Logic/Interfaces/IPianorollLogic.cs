// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Interfaces
{
    using Melody.Logic.Models;

    /// <summary>
    /// Interface for pianoroll logic operations.
    /// </summary>
    public interface IPianorollLogic
    {
        List<Note> LoadedNotes { get; }

        int TotalVisibleNotes { get; }

        int MinOctave { get; }

        int MaxOctave { get; }

        DateTime StartTime { get; }

        void LoadPianoroll(string path);

        void StoreNotes(double windowWidth);

        void UpdateNotePositions(double canvasHeight);
    }
}