using CommunityToolkit.Mvvm.Messaging;
using Melody.Logic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Melody.Logic
{
    public class PracticeLogic : IPracticeLogic
    {
        private IMessenger messenger;
        private PracticeStructure structure;
        private Dictionary<double, List<Models.Note>> practiceNotes;

        public PracticeLogic(IMessenger messenger)
        {
            this.messenger = messenger;
            structure = new PracticeStructure()
            {
                Combos = new List<Combo>()
            };
        }

        public void CreatePractice(Dictionary<double, List<Models.Note>> practiceNotes, string fileName)
        {
            this.practiceNotes = practiceNotes;

            // ID_measureList.json
            var measureList = new MeasureList
            {
                Measures = new List<Measure>(),
            };

            var rightSeparate = new List<Measure>();
            var leftSeparate = new List<Measure>();
            int itemCount = 0;
            foreach (var item in practiceNotes)
            {
                var tempComboRight = new Measure
                {
                    ID = $"r{itemCount}",
                    NoteNumbers = new List<int>(),
                };
                var tempComboLeft = new Measure
                {
                    ID = $"l{itemCount}",
                    NoteNumbers = new List<int>(),
                };

                var noteCount = 0;
                foreach (var note in item.Value)
                {
                    if (note.IsRightHand)
                    {
                        tempComboRight.NoteNumbers.Add(noteCount++);
                    }
                    else
                    {
                        tempComboLeft.NoteNumbers.Add(noteCount++);
                    }
                }

                itemCount++;
                rightSeparate.Add(tempComboRight);
                leftSeparate.Add(tempComboLeft);
            }

            measureList.Measures.AddRange(rightSeparate);
            measureList.Measures.AddRange(leftSeparate);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            string measureListData = JsonSerializer.Serialize(measureList, options);

            //ID_structure.json
            LoadIntoStructure(0);
            LoadIntoStructure(1);
            LoadIntoStructure(2);

            string structureData = JsonSerializer.Serialize(structure, options);

            //ID_progress.json
            var progress = new Progress
            {
                CurrentCombo = 0,
                TotalCombo = structure.Combos.Count,
            };

            string progressData = JsonSerializer.Serialize(progress, options);

            string notes = JsonSerializer.Serialize(practiceNotes, options);

            StorePractice(fileName, measureListData, structureData, progressData, notes);

        }

        private void LoadIntoStructure(int phase)
        {
            // külön ütemek
            for (int i = 0; i < practiceNotes.Count; i++)
            {
                structure.Combos.Add(new Combo
                {
                    MeasureNumber = i,
                    Phase = phase,
                });
            }

            // összefűzött ütemek
            for (int i = 1; i < practiceNotes.Count; i++)
            {
                structure.Combos.Add(new Combo
                {
                    MeasureNumber = $"-{i}",
                    Phase = phase,
                });
            }
        }

        private void StorePractice(string name, string measureList, string structure, string progress, string notes)
        {
            try
            {
                messenger.Send("Storing practice...", "PracticeLogicResult");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };

                string sheetCollectionJson = File.ReadAllText(FileController.GetCollectionPath());

                SheetCollection collection = JsonSerializer.Deserialize<SheetCollection>(sheetCollectionJson);

                string id = GenerateUniqueKey(collection.Sheets);
                collection.Sheets.Add(id, name);

                string updatedSheetCollectionJson = JsonSerializer.Serialize(collection, options);

                FileController.Save(data: updatedSheetCollectionJson, name: FileController.FileName);
                FileController.Save(data: measureList, name: $"{id}_measureList.json");
                FileController.Save(data: structure, name: $"{id}_structure.json");
                FileController.Save(data: progress, name: $"{id}_progress.json");
                FileController.Save(data: notes, name: $"{id}_notes.json");

                messenger.Send($"Practice with id {id} is created!", "PracticeLogicResult");
            }
            catch (Exception e)
            {
                messenger.Send($"Error in storing practice: {e.Message}", "PracticeLogicResult");
            }

        }

        string GenerateUniqueKey(Dictionary<string, string> existingDict)
        {
            string id;
            do
            {
                id = System.Security.Cryptography.RandomNumberGenerator.GetString("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#&@!", 6);
            }
            while (existingDict.ContainsKey(id));

            return id;
        }

        public void LoadPractice(string path)
        {
            // Implementation for loading practice structure from a file goes here.
        }
    }
}
