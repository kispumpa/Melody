// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using System.Text.Json;
    using CommunityToolkit.Mvvm.Messaging;
    using Melody.Logic.Interfaces;
    using Melody.Logic.Models;

    /// <summary>Implements practice logic operations.</summary>
    public class PracticeLogic : IPracticeLogic
    {
        private IMessenger messenger;
        private Dictionary<double, List<Models.Note>> practiceNotes;
        private PracticeStructure structure;
        private MeasureList measureList;
        private Progress progress;
        private string key;

        /// <summary>Initializes a new instance of the <see cref="PracticeLogic"/> class.</summary>
        /// <param name="messenger">The messenger instance.</param>
        public PracticeLogic(IMessenger messenger)
        {
            this.messenger = messenger;
            this.Structure = new PracticeStructure()
            {
                Combos = new List<Combo>(),
            };
        }

        /// <inheritdoc/>
        public string Key => this.key;

        /// <inheritdoc/>
        public MeasureList MeasureList { get => this.measureList; set => this.measureList = value; }

        /// <inheritdoc/>
        public Dictionary<double, List<Note>> PracticeNotes { get => this.practiceNotes; set => this.practiceNotes = value; }

        /// <inheritdoc/>
        public Progress Progress { get => this.progress; set => this.progress = value; }

        /// <inheritdoc/>
        public PracticeStructure Structure { get => this.structure; set => this.structure = value; }

        /// <inheritdoc/>
        public void CreatePractice(Dictionary<double, List<Models.Note>> practiceNotes, string fileName, int totalVisibleNotes, int minOctave)
        {
            this.PracticeNotes = practiceNotes;

            // ID_measureList.json
            MeasureList measureList = new MeasureList
            {
                Measures = new List<Measure>(),
            };

            List<Measure> rightSeparate = new List<Measure>();
            List<Measure> leftSeparate = new List<Measure>();
            int itemCount = 0;
            foreach (KeyValuePair<double, List<Note>> item in practiceNotes)
            {
                Measure tempComboRight = new Measure
                {
                    ID = $"r{itemCount}",
                    NoteNumbers = new List<int>(),
                };
                Measure tempComboLeft = new Measure
                {
                    ID = $"l{itemCount}",
                    NoteNumbers = new List<int>(),
                };

                int noteCount = 0;
                foreach (Note note in item.Value)
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

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            string measureListData = JsonSerializer.Serialize(measureList, options);

            // ID_structure.json
            this.LoadIntoStructure(0);
            this.LoadIntoStructure(1);
            this.LoadIntoStructure(2);

            string structureData = JsonSerializer.Serialize(this.Structure, options);

            // ID_progress.json
            Progress progress = new Progress
            {
                CurrentCombo = 0,
                TotalCombo = this.Structure.Combos.Count,
                TotalVisibleNotes = totalVisibleNotes,
                MinOctave = minOctave,
            };

            string progressData = JsonSerializer.Serialize(progress, options);

            // ID_notes.json
            string notes = JsonSerializer.Serialize(practiceNotes, options);

            this.StorePractice(fileName, measureListData, structureData, progressData, notes);
        }

        /// <inheritdoc/>
        public void LoadPractice(string key)
        {
            SheetCollection collection = FileController.GetCollection();
            if (collection.Sheets.ContainsKey(key))
            {
                this.messenger.Send($"Practice with id {key} found, loading into Melody...", "PracticeLogicResult");
                this.key = key;
                FileController.Load(this);
            }
            else
            {
                this.messenger.Send($"Practice with id {key} not found!", "PracticeLogicResult");
            }
        }

        /// <inheritdoc/>
        public void LoadPractice()
        {
            SheetCollection collection = FileController.GetCollection();
            if (collection.Sheets.ContainsKey(this.key))
            {
                this.messenger.Send($"Practice with id {this.key} found, loading into Melody...", "PracticeLogicResult");
                FileController.Load(this);
                List<double> keysCache = this.PracticeNotes.Keys.ToList();
            }
            else
            {
                this.messenger.Send($"Practice with id {this.key} not found!", "PracticeLogicResult");
            }
        }

        /// <inheritdoc/>
        public void SaveProgress(int currentCombo)
        {
            this.progress.CurrentCombo = currentCombo;
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            string progressData = JsonSerializer.Serialize(this.progress, options);
            FileController.Save(data: progressData, name: $"{this.key}_progress.json");
        }

        private void LoadIntoStructure(int phase)
        {
            // külön ütemek
            for (int i = 0; i < this.PracticeNotes.Count; i++)
            {
                this.Structure.Combos.Add(new Combo
                {
                    MeasureNumber = i,
                    Phase = phase,
                });
            }

            // összefűzött ütemek
            for (int i = 1; i < this.PracticeNotes.Count; i++)
            {
                this.Structure.Combos.Add(new Combo
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
                this.messenger.Send("Storing practice...", "PracticeLogicResult");

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };

                SheetCollection collection = FileController.GetCollection();

                string id = this.GenerateUniqueKey(collection.Sheets);
                collection.Sheets.Add(id, name);
                this.key = id;

                string updatedSheetCollectionJson = JsonSerializer.Serialize(collection, options);

                FileController.Save(data: updatedSheetCollectionJson, name: FileController.FileName);
                FileController.Save(data: measureList, name: $"{id}_measureList.json");
                FileController.Save(data: structure, name: $"{id}_structure.json");
                FileController.Save(data: progress, name: $"{id}_progress.json");
                FileController.Save(data: notes, name: $"{id}_notes.json");

                this.messenger.Send($"Practice with id {id} is created!", "PracticeLogicResult");
            }
            catch (Exception e)
            {
                this.messenger.Send($"Error in storing practice: {e.Message}", "PracticeLogicResult");
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
    }
}
