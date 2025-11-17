namespace Melody.Logic.Interfaces
{
    public interface ILilypondLogic
    {
        string SvgPath { get; }
        void LoadLilypond(string mxlFilePath, string outputDirectory = null);
    }
}