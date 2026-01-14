using Melody.Logic.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Melody.Logic
{
    public static class FileController
    {
        private static string AppName = "Melody";
        public static string FileName = "sheetCollections.json";

        public static string GetCollectionPath()
        {
            string folderPath = GetAppDataPath();

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var colPath = Path.Combine(folderPath, FileName);

            if (!File.Exists(colPath))
            {
                SheetCollection coll = new SheetCollection
                {
                    Sheets = new Dictionary<string, string>()
                };
                string json = JsonSerializer.Serialize(coll, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                Save(json, FileName);
            }

            return Path.Combine(folderPath, FileName);
        }

        private static string GetAppDataPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(appData, AppName);
            return folderPath;
        }

        public static SheetCollection GetCollection()
        {
            string sheetCollectionJson = File.ReadAllText(GetCollectionPath());

            SheetCollection collection = JsonSerializer.Deserialize<SheetCollection>(sheetCollectionJson);

            return collection;
        }

        public static void Save(string data, string name)
        {
            string appDataPath = GetAppDataPath();
            string goalPath = Path.Combine(appDataPath, name);

            File.WriteAllText(goalPath, data);

            Debug.WriteLine($"File saved here: {goalPath}");
        }

        public static void Load(PracticeLogic logic)
        {
            var practiceNotesPath = Path.Combine(GetAppDataPath(), $"{logic.Key}_notes.json");
            var structurePath = Path.Combine(GetAppDataPath(), $"{logic.Key}_structure.json");
            var measureListPath = Path.Combine(GetAppDataPath(), $"{logic.Key}_measureList.json");
            var progressPath = Path.Combine(GetAppDataPath(), $"{logic.Key}_progress.json");

            string practiceNotes = File.ReadAllText($"{practiceNotesPath}");
            string structure = File.ReadAllText($"{structurePath}");
            string measureList = File.ReadAllText($"{measureListPath}");
            string progress = File.ReadAllText($"{progressPath}");

            logic.PracticeNotes = JsonSerializer.Deserialize<Dictionary<double, List<Note>>>(practiceNotes);
            logic.Structure = JsonSerializer.Deserialize<PracticeStructure>(structure);
            logic.MeasureList = JsonSerializer.Deserialize<MeasureList>(measureList);
            logic.Progress = JsonSerializer.Deserialize<Progress>(progress);
        }
    }
}
