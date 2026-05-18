// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Models
{
    using System.Text.Json.Serialization;

    /// <summary>Represents a musical measure in the Melody application.</summary>
    public class Measure
    {
        /// <summary>Gets or sets the unique identifier of the measure.</summary>
        [JsonPropertyName("id")]
        public string ID { get; set; }

        /// <summary>Gets or sets the list of note numbers in the measure.</summary>
        [JsonPropertyName("noteNumbers")]
        public List<int> NoteNumbers { get; set; }
    }
}
