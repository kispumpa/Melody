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

        public static void Save(string data, string name)
        {
            string appDataPath = GetAppDataPath();
            string goalPath = Path.Combine(appDataPath, name);

            File.WriteAllText(goalPath, data);

            Debug.WriteLine($"File saved here: {goalPath}");
        }

        
    }
}
