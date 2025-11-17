using Melody.Logic.Models;

namespace Melody.Logic.Interfaces
{
    public interface IPianorollLogic
    {
        List<Note> LoadedNotes { get; }

        int TotalVisibleNotes { get; }

        int MinOctave { get; }

        int MaxOctave { get; }

        DateTime StartTime { get; }

        void LoadPianoroll(string path);

        void StoreNotes(double windowWidth);

        void UpdateNotePositions(double canvasHeight);
    }
}