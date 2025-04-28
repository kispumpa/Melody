namespace Melody.Logic
{
    public interface IToggleViewLogic
    {
        bool IsPianoRollView { get; set; }

        void ToggleView();
    }
}