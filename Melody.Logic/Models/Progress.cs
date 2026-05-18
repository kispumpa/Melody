// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Models
{
    using System.Text.Json.Serialization;

    /// <summary>Represents the progress of a user in the Melody application.</summary>
    public class Progress
    {
        /// <summary>Gets or sets the current combo.</summary>
        [JsonPropertyName("currentCombo")]
        public int CurrentCombo { get; set; }

        /// <summary>Gets or sets the total combo.</summary>
        [JsonPropertyName("totalCombo")]
        public int TotalCombo { get; set; }

        /// <summary>Gets or sets the total number of visible notes.</summary>
        [JsonPropertyName("totalVisibleNotes")]
        public int TotalVisibleNotes { get; set; }

        /// <summary>Gets or sets the minimum octave.</summary>
        [JsonPropertyName("minOctave")]
        public int MinOctave { get; set; }
    }
}
