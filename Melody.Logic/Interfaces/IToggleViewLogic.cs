namespace Melody.Logic.Interfaces
{
    public interface IToggleViewLogic
    {
        bool IsPianoRollView { get; set; }

        void ToggleView();
    }
}