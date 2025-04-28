namespace Melody.Logic
{
    public interface ILilypondLogic
    {
        string SvgPath { get; }
        void LoadLilypond(string mxlFilePath, string outputDirectory = null);
    }
}