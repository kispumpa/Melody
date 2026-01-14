using Melody.Logic.Models;

namespace Melody.Logic
{
    public interface IPracticeLogic
    {
        void CreatePractice(Dictionary<double, List<Note>> practiceNotes, string fileName);
        void LoadPractice(string path);
    }
}