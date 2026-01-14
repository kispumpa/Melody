// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Interfaces
{
    using Melody.Logic.Models;

    /// <summary>Interface for <see cref="PianorollLogic"/> class.</summary>
    public interface IPianorollLogic
    {

        List<Note> LoadedNotes { get; }

        Dictionary<double, List<Models.Note>> PracticeNotes { get; }

        int TotalVisibleNotes { get; }

        int MinOctave { get; }

        int MaxOctave { get; }

        DateTime StartTime { get; }

        void InitializePianoRoll(string path);

        void StoreNotes(double windowWidth, bool isPractice = false);

        void UpdateNotePositions(double canvasHeight);

        void CreatePractice();
    }
}