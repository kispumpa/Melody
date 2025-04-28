namespace Melody.UI
{
    internal interface IToggleViewLogic
    {
        event EventHandler ViewChanged;

        bool IsPianoRollView { get; }

        void ToggleView();
    }
}