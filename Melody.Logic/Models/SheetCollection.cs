// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Models
{
    using System.Text.Json.Serialization;

    /// <summary>Represents a collection of sheets.</summary>
    public class SheetCollection
    {
        /// <summary>Gets or sets the dictionary of sheets.</summary>
        [JsonPropertyName("sheets")]
        public Dictionary<string, string> Sheets { get; set; } // id, name
    }
}
