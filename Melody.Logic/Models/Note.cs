
namespace Melody.Logic.Models
{

    public class Note
    {
        public Accordinate X { get; set; }

        public Accordinate Y { get; set; }

        public string Pitch { get; set; }

        //public Rectangle SoundCell { get; set; }

        public bool Played { get; set; } = false;

        public double YPosition { get; set; }

        public bool IsVisible { get; set; }

        public int Velocity { get; set; } 
    }
}
