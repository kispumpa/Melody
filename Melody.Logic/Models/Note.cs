// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Models
{
    /// <summary>
    /// Represents a pianoroll note.
    /// </summary>
    public class Note
    {
        /// <summary>Gets or sets the X coordinate of the note.</summary>
        public Accordinate X { get; set; }

        /// <summary>Gets or sets the Y coordinate of the note.</summary>
        public Accordinate Y { get; set; }

        /// <summary>Gets or sets the pitch of the note.</summary>
        public string Pitch { get; set; }

        /// <summary>Gets or sets a value indicating whether the note has been played.</summary>
        public bool Played { get; set; } = false;

        /// <summary>Gets or sets the Y position of the note.</summary>
        public double YPosition { get; set; }

        /// <summary>Gets or sets a value indicating whether the note is visible.</summary>
        public bool IsVisible { get; set; }

        /// <summary>Gets or sets the velocity of the note.</summary>
        public int Velocity { get; set; }

        /// <summary>Gets or sets a value indicating whether the note is played by the right hand.</summary>
        public bool IsRightHand { get; set; }
    }
}
