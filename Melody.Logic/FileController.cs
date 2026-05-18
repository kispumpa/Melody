// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using System.Diagnostics;
    using System.Text.Json;
    using Melody.Logic.Models;

    /// <summary>Provides methods for managing files related to the Melody application.</summary>
    public static class FileController
    {
        private static string appName = "Melody";
        public static string fileName = "sheetCollections.json";

        /// <summary>Gets the path to the collection file.</summary>
        /// <returns>The path to the collection file.</returns>
        public static string GetCollectionPath()
        {
            string folderPath = GetAppDataPath();

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string colPath = Path.Combine(folderPath, fileName);

            if (!File.Exists(colPath))
            {
                SheetCollection coll = new SheetCollection
                {
                    Sheets = new Dictionary<string, string>(),
                };
                string json = JsonSerializer.Serialize(coll, new JsonSerializerOptions
                {
                    WriteIndented = true,
                });
                Save(json, fileName);
            }

            return Path.Combine(folderPath, fileName);
        }

        /// <summary>Gets the sheet collection.</summary>
        /// <returns>The sheet collection.</returns>
        public static SheetCollection GetCollection()
        {
            string sheetCollectionJson = File.ReadAllText(GetCollectionPath());

            SheetCollection collection = JsonSerializer.Deserialize<SheetCollection>(sheetCollectionJson);

            return collection;
        }

        /// <summary>Saves the specified data to a file.</summary>
        /// <param name="data">The data to save.</param>
        /// <param name="name">The name of the file.</param>
        public static void Save(string data, string name)
        {
            string appDataPath = GetAppDataPath();
            string goalPath = Path.Combine(appDataPath, name);

            File.WriteAllText(goalPath, data);

            Debug.WriteLine($"File saved here: {goalPath}");
        }

        /// <summary>Loads the specified practice logic from files.</summary>
        /// <param name="logic">The practice logic to load.</param>
        public static void Load(PracticeLogic logic)
        {
            string practiceNotesPath = Path.Combine(GetAppDataPath(), $"{logic.Key}_notes.json");
            string structurePath = Path.Combine(GetAppDataPath(), $"{logic.Key}_structure.json");
            string measureListPath = Path.Combine(GetAppDataPath(), $"{logic.Key}_measureList.json");
            string progressPath = Path.Combine(GetAppDataPath(), $"{logic.Key}_progress.json");

            string practiceNotes = File.ReadAllText($"{practiceNotesPath}");
            string structure = File.ReadAllText($"{structurePath}");
            string measureList = File.ReadAllText($"{measureListPath}");
            string progress = File.ReadAllText($"{progressPath}");

            logic.PracticeNotes = JsonSerializer.Deserialize<Dictionary<double, List<Note>>>(practiceNotes);
            logic.Structure = JsonSerializer.Deserialize<PracticeStructure>(structure);
            logic.MeasureList = JsonSerializer.Deserialize<MeasureList>(measureList);
            logic.Progress = JsonSerializer.Deserialize<Progress>(progress);
        }

        private static string GetAppDataPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(appData, appName);
            return folderPath;
        }
    }
}
