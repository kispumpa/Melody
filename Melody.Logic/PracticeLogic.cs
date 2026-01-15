using CommunityToolkit.Mvvm.Messaging;
using Melody.Logic.Interfaces;
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
        private Dictionary<double, List<Models.Note>> practiceNotes;
        private PracticeStructure structure;
        private MeasureList measureList;
        private Progress progress;
        private string key;

        public string Key => key;

        public Dictionary<double, List<Note>> PracticeNotes { get => practiceNotes; set => practiceNotes = value; }
        public PracticeStructure Structure { get => structure; set => structure = value; }
        public MeasureList MeasureList { get => measureList; set => measureList = value; }
        public Progress Progress { get => progress; set => progress = value; }

        public PracticeLogic(IMessenger messenger)
        {
            this.messenger = messenger;
            Structure = new PracticeStructure()
            {
                Combos = new List<Combo>()
            };
        }

        public void CreatePractice(Dictionary<double, List<Models.Note>> practiceNotes, string fileName, int totalVisibleNotes, int minOctave)
        {
            this.PracticeNotes = practiceNotes;

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

            string structureData = JsonSerializer.Serialize(Structure, options);

            //ID_progress.json
            var progress = new Progress
            {
                CurrentCombo = 0,
                TotalCombo = Structure.Combos.Count,
                TotalVisibleNotes = totalVisibleNotes,
                MinOctave = minOctave,
            };

            string progressData = JsonSerializer.Serialize(progress, options);

            //ID_notes.json
            string notes = JsonSerializer.Serialize(practiceNotes, options);

            StorePractice(fileName, measureListData, structureData, progressData, notes);

        }

        private void LoadIntoStructure(int phase)
        {
            // külön ütemek
            for (int i = 0; i < PracticeNotes.Count; i++)
            {
                Structure.Combos.Add(new Combo
                {
                    MeasureNumber = i,
                    Phase = phase,
                });
            }

            // összefűzött ütemek
            for (int i = 1; i < PracticeNotes.Count; i++)
            {
                Structure.Combos.Add(new Combo
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

                SheetCollection collection = FileController.GetCollection();

                string id = GenerateUniqueKey(collection.Sheets);
                collection.Sheets.Add(id, name);
                this.key = id;

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

        private string GenerateUniqueKey(Dictionary<string, string> existingDict)
        {
            string id;
            do
            {
                id = System.Security.Cryptography.RandomNumberGenerator.GetString("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#&@!", 6);
            }
            while (existingDict.ContainsKey(id));

            return id;
        }

        public void LoadPractice(string key)
        {
            SheetCollection collection = FileController.GetCollection();
            if (collection.Sheets.ContainsKey(key))
            {
                messenger.Send($"Practice with id {key} found, loading into Melody...", "PracticeLogicResult");
                this.key = key;
                FileController.Load(this);
            }
            else
            {
                messenger.Send($"Practice with id {key} not found!", "PracticeLogicResult");
            }
        }

        public void LoadPractice()
        {
            SheetCollection collection = FileController.GetCollection();
            if (collection.Sheets.ContainsKey(key))
            {
                messenger.Send($"Practice with id {key} found, loading into Melody...", "PracticeLogicResult");
                FileController.Load(this);
                var keysCache = PracticeNotes.Keys.ToList();
            }
            else
            {
                messenger.Send($"Practice with id {key} not found!", "PracticeLogicResult");
            }
        }

        public void SaveProgress(int currentCombo)
        {
            progress.CurrentCombo = currentCombo;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            string progressData = JsonSerializer.Serialize(progress, options);
            FileController.Save(data: progressData, name: $"{key}_progress.json");
        }
    }
}
