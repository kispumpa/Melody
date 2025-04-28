namespace Melody.UI
{
    using System.Text;

    internal class ToggleViewLogic : IToggleViewLogic
    {
        public ToggleViewLogic()
        {
            this.IsPianoRollView = true;
        }

        public event EventHandler ViewChanged;

        public bool IsPianoRollView { get; private set; }

        public void ToggleView()
        {
            this.IsPianoRollView = !this.IsPianoRollView;
            this.ViewChanged?.Invoke(this, null);
        }
    }
}
