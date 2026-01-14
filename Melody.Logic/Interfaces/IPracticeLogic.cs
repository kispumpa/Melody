using Melody.Logic.Models;

namespace Melody.Logic.Interfaces
{
    public interface IPracticeLogic
    {
        string Key { get; }
        MeasureList MeasureList { get; set; }
        Dictionary<double, List<Note>> PracticeNotes { get; set; }
        Progress Progress { get; set; }
        PracticeStructure Structure { get; set; }

        void CreatePractice(Dictionary<double, List<Note>> practiceNotes, string fileName, int totalVisibleNotes, int minOctave);
        void LoadPractice();
        void LoadPractice(string key);
    }
}