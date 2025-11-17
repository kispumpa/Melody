using System.Drawing;

namespace Melody.Logic.Models
{
    public class Note
    {
        private double yPosition;
        private bool visibility;

        public Accordinate X;
        public Accordinate Y;
        public string Pitch;
        public Rectangle soundCell;
        public bool Played = false;

        public double YPosition 
        {
            get => yPosition;

            set => yPosition = value;
        }

        public bool Visibility 
        {
            get => visibility;

            set => visibility = value;
        }
    }
}
