namespace Melody.Logic.Interfaces
{
    public interface IMxlUnpacker
    {
        string MusicXmlPath { get; }
        string MxlPath { get; }

        void ExtractAndSave(string mxlFilePath, string outputXmlPath);
        string ExtractMusicXml(string mxlFilePath);
        void ExtractToDirectory(string mxlFilePath, string destinationPath);
    }
}