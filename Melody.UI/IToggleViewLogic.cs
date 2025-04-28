namespace Melody.UI
{
    public interface IToggleViewLogic
    {
        bool IsPianoRollView { get; set; }

        void ToggleView();
    }
}