namespace Melody.UI
{
    using CommunityToolkit.Mvvm.Messaging;

    internal class ToggleViewLogic : IToggleViewLogic
    {
        private IMessenger messenger;

        public ToggleViewLogic(IMessenger messenger)
        {
            this.IsPianoRollView = true;
            this.messenger = messenger;
        }

        public bool IsPianoRollView { get; set; }

        public void ToggleView()
        {
            this.IsPianoRollView = !this.IsPianoRollView;
            this.messenger.Send("View changed", "ViewResult");
        }
    }
}
