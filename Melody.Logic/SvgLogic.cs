namespace Melody.Logic
{
    using CommunityToolkit.Mvvm.Messaging;

    public class SvgLogic
    {
        private IMessenger messenger;

        public SvgLogic(IMessenger messenger)
        {
            this.messenger = messenger;
        }
    }
}
