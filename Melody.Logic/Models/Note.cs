// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Models
{
    /// <summary>
    /// Represents a pianoroll note.
    /// </summary>
    public class Note
    {
        public Accordinate X { get; set; }

        public Accordinate Y { get; set; }

        public string Pitch { get; set; }

        public bool Played { get; set; } = false;

        public double YPosition { get; set; }

        public bool IsVisible { get; set; }

        public int Velocity { get; set; }

        public bool IsRightHand { get; set; }
    }
}
