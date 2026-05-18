// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Models
{
    using System.Text.Json.Serialization;

    /// <summary>Represents a musical combo.</summary>
    public class Combo
    {
        /// <summary>Gets or sets the measure number of the combo.</summary>
        [JsonPropertyName("measureNumber")]
        public object MeasureNumber { get; set; }

        /// <summary>Gets or sets the phase of the combo.</summary>
        [JsonPropertyName("phase")]
        public int Phase { get; set; }
    }
}
