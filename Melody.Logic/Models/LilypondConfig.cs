// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Models
{
    /// <summary>Represents the configuration settings for Lilypond.</summary>
    public class LilypondConfig
    {
        /// <summary>Gets or sets the path to the Python executable.</summary>
        public string PythonPath { get; set; }

        /// <summary>Gets or sets the path to the Lilypond executable.</summary>
        public string LilypondPath { get; set; }

        /// <summary>Gets or sets the path to the Mxml2ly executable.</summary>
        public string Mxml2lyPath { get; set; }
    }
}
