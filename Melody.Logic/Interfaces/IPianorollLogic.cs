// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Interfaces
{
    using Melody.Logic.Models;

    /// <summary>Interface for <see cref="PianorollLogic"/> class.</summary>
    public interface IPianorollLogic
    {
        /// <summary>Gets the list of loaded notes.</summary>
        List<Note> LoadedNotes { get; }

        /// <summary>Gets the dictionary of practice notes.</summary>
        Dictionary<double, List<Models.Note>> PracticeNotes { get; }

        /// <summary>Gets or sets the total number of visible notes.</summary>
        int TotalVisibleNotes { get; set; }

        /// <summary>Gets or sets the minimum octave.</summary>
        int MinOctave { get; set; }

        /// <summary>Gets the maximum octave.</summary>
        int MaxOctave { get; }

        /// <summary>Gets the start time of the piano roll.</summary>
        DateTime StartTime { get; }

        /// <summary>Initializes the piano roll with the specified file path.</summary>
        /// <param name="path">The file path to initialize the piano roll.</param>
        void InitializePianoRoll(string path, bool total = false);

        /// <summary>Stores the notes with the specified window width and practice mode.</summary>
        /// <param name="windowWidth">The width of the window.</param>
        /// <param name="isPractice">Indicates whether it is in practice mode.</param>
        void StoreNotes(double windowWidth, bool isPractice = false);

        /// <summary>Updates the positions of the notes with the specified canvas height.</summary>
        /// <param name="canvasHeight">The height of the canvas.</param>
        void UpdateNotePositions(double canvasHeight);

        /// <summary>Creates a new practice.</summary>
        void CreatePractice();
    }
}
