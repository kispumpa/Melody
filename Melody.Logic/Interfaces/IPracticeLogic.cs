// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Interfaces
{
    using Melody.Logic.Models;

    /// <summary>Interface for practice logic operations.</summary>
    public interface IPracticeLogic
    {
        /// <summary>Gets the idetifier key of the practice.</summary>
        string Key { get; }

        /// <summary>Gets or sets the list of measures for the practice.</summary>
        MeasureList MeasureList { get; set; }

        /// <summary>Gets or sets the notes for the practice.</summary>
        Dictionary<double, List<Note>> PracticeNotes { get; set; }

        /// <summary>Gets or sets the progress of the practice.</summary>
        Progress Progress { get; set; }

        /// <summary>Gets or sets the structure of the practice.</summary>
        PracticeStructure Structure { get; set; }

        /// <summary>Creates a new practice with the specified notes, file name, total visible notes, and minimum octave.</summary>
        /// <param name="practiceNotes">The notes for the practice.</param>
        /// <param name="fileName">The file name for the practice.</param>
        /// <param name="totalVisibleNotes">The total number of visible notes.</param>
        /// <param name="minOctave">The minimum octave for the practice.</param>
        void CreatePractice(Dictionary<double, List<Note>> practiceNotes, string fileName, int totalVisibleNotes, int minOctave);

        /// <summary>Loads the current practice.</summary>
        void LoadPractice();

        /// <summary>Loads the practice with the specified key.</summary>
        /// <param name="key">The key of the practice to load.</param>
        void LoadPractice(string key);

        /// <summary>Saves the progress of the practice with the specified current combo.</summary>
        /// <param name="currentCombo">The current combo index to save.</param>
        void SaveProgress(int currentCombo);
    }
}
