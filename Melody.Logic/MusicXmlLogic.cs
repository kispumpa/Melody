namespace Melody.Logic
{
    using CommunityToolkit.Mvvm.Messaging;

    public class MusicXmlLogic : IMusicXmlLogic
    {
        private IMessenger messenger;

        public MusicXmlLogic(IMessenger messenger)
        {
            this.messenger = messenger;
        }

        public void LoadMusicXml()
        {
            this.messenger.Send("Music xml file loaded successfully", "MusicXmlLoadResult");
        }
    }
}
