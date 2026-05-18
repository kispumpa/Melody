// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Models
{
    using System.Text.Json.Serialization;

    /// <summary>Represents the structure of a practice session.</summary>
    public class PracticeStructure
    {
        /// <summary>Gets or sets the list of combos.</summary>
        [JsonPropertyName("combos")]
        public List<Combo> Combos { get; set; }
    }
}
