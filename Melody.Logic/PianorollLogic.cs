// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using CommunityToolkit.Mvvm.Messaging;
    using Melody.Logic.Interfaces;
    using Melody.Logic.Models;
    using MusicXml;
    using MusicXml.Domain;

    /// <summary>Logic for piano roll visualization and note management.</summary>
    public class PianorollLogic : IPianorollLogic
    {
        private const double PixelsPerSecond = 60;
        private const double PlaybackSpeed = 1.0;

        private IMessenger messenger;
        private MusicXml.Domain.Score score;
        private Dictionary<double, Models.Note> notes;
        private int minOctave;
        private int maxOctave;
        private int minIndex;
        private int totalVisibleNotes;
        private DateTime startTime;

        public PianorollLogic(IMessenger messenger)
        {
            this.messenger = messenger;
            this.notes = new Dictionary<double, Models.Note>();
            this.LoadedNotes = new List<Models.Note>();
            this.PracticeNotes = new Dictionary<double, List<Models.Note>>();
        }

        public List<Models.Note> LoadedNotes { get; private set; }

        public Dictionary<double, List<Models.Note>> PracticeNotes { get; private set; }

        public int TotalVisibleNotes
        {
            get => this.totalVisibleNotes;
            set => this.totalVisibleNotes = value;
        }

        public int MinOctave
        {
            get => this.minOctave;
            set => this.minOctave = value;
        }

        public int MaxOctave => this.maxOctave;

        public DateTime StartTime => this.startTime;

        public void InitializePianoRoll(string path)
        {
            try
            {
                this.messenger.Send("Initializing piano roll...", "PianorollLoadResult");

                this.score = MusicXmlParser.GetScore(path);
                this.GetOctaveInterval();
                this.totalVisibleNotes = this.CalculateVisibleNotes();

                this.startTime = DateTime.Now;

                this.messenger.Send("Piano roll initialized successfully", "PianorollLoadResult");
            }
            catch (Exception ex)
            {
                this.messenger.Send($"Error in initializing piano roll: {ex.Message}", "PianorollLoadResult");
            }
        }

        public void StoreNotes(double windowWidth, bool isPractice)
        {
            this.messenger.Send("Storing notes for piano roll...", "PianorollLoadResult");

            this.notes.Clear();
            this.LoadedNotes.Clear();

            double counter = 0;
            double durationSum = 300;
            double divisions = 0;
            double lastDuration = 0; // for chords
            int measureCount = -1;
            bool isRightHand = true;

            foreach (var part in this.score.Parts)
            {
                foreach (var measure in part.Measures)
                {
                    if (measure.Attributes != null && measure.Attributes.Divisions != 0)
                    {
                        divisions = measure.Attributes.Divisions;
                    }

                    if (isPractice)
                    {
                        this.PracticeNotes[++measureCount] = new List<Models.Note>();
                        isRightHand = true;
                        this.messenger.Send($"Processing measure no. {measureCount}...", "PracticeLoadResult");
                    }

                    foreach (var element in measure.MeasureElements)
                    {
                        if (element.Type != MeasureElementType.Note)
                        {
                            if (element.Type == MeasureElementType.Backup)
                            {
                                durationSum -= (1 / (4 * (divisions / ((MusicXml.Domain.Backup)element.Element).Duration))) * 240;

                                isRightHand = false;
                            }
                            else
                            {
                                durationSum += (1 / (4 * (divisions / ((MusicXml.Domain.Forward)element.Element).Duration))) * 240;
                            }

                            continue;
                        }

                        var noteObj = (MusicXml.Domain.Note)element.Element;

                        if (noteObj.IsGrace)
                        {
                            continue;
                        }

                        int duration = noteObj.Duration;

                        if (noteObj.IsRest == true)
                        {
                            durationSum += (1 / (4 * (divisions / duration))) * 240;
                            continue;
                        }

                        string temPitch = $"{((noteObj.Pitch.Alter != 0)
                            ? (MusicNote)(((int)(MusicNote)Enum.Parse(typeof(MusicNote), noteObj.Pitch.Step.ToString()) + noteObj.Pitch.Alter) % 12)
                            : (MusicNote)Enum.Parse(typeof(MusicNote), noteObj.Pitch.Step.ToString()))}{noteObj.Pitch.Octave}";

                        double index = (int)(Step)Enum.Parse(typeof(Step), noteObj.Pitch.Step.ToString()) + (7 * noteObj.Pitch.Octave) - (temPitch.Contains("b") ? 0.5 : 0) - this.minIndex;

                        var note = new Models.Note
                        {
                            X = new Accordinate
                            {
                                Position = (windowWidth / this.TotalVisibleNotes) * index,
                                Length = windowWidth / this.TotalVisibleNotes / (temPitch.Contains("b") ? 2 : 1),
                            },
                            Y = new Accordinate
                            {
                                Position = durationSum - (noteObj.IsChordTone ? lastDuration : 0),
                                Length = (1 / (4 * (divisions / duration))) * 240,
                            },
                            Pitch = temPitch,
                            Velocity = noteObj.Voice, // 7:28
                        };
                        note.IsRightHand = isRightHand;

                        this.notes.Add(counter++, note);
                        this.LoadedNotes.Add(note);

                        if (isPractice)
                        {
                            this.PracticeNotes[measureCount].Add(note);
                        }

                        if (!noteObj.IsChordTone)
                        {
                            lastDuration = (1 / (4 * (divisions / duration))) * 240;
                            durationSum += lastDuration;
                        }
                    }

                    if (isPractice)
                    {
                        this.messenger.Send($"Stored {this.PracticeNotes[measureCount].Count} notes for measure no. {measureCount}.", "PracticeLoadResult");
                    }
                }
            }

            this.messenger.Send("Notes stored successfully for piano roll.", "PianorollLoadResult");

        }

        public void UpdateNotePositions(double canvasHeight)
        {
            double elapsed = (DateTime.Now - this.startTime).TotalSeconds * PlaybackSpeed;

            foreach (var note in this.LoadedNotes)
            {
                double y = note.Y.Position - (elapsed * PixelsPerSecond);

                note.YPosition = y;
                note.IsVisible = y + note.Y.Length > 0 && y < canvasHeight;

                if (!note.Played && y <= 0)
                {
                    note.Played = true;
                    this.messenger.Send(note, "PlayNote");
                }
            }
        }

        private void GetOctaveInterval()
        {
            int maxOctave = 0;
            int minOctave = 10;

            foreach (var part in this.score.Parts)
            {
                foreach (var measure in part.Measures)
                {
                    foreach (var element in measure.MeasureElements)
                    {
                        if (element.Type == MeasureElementType.Note && !((MusicXml.Domain.Note)element.Element).IsRest)
                        {
                            var noteOctave = ((MusicXml.Domain.Note)element.Element).Pitch.Octave;
                            if (noteOctave > maxOctave)
                            {
                                maxOctave = noteOctave;
                            }
                            else if (noteOctave < minOctave)
                            {
                                minOctave = noteOctave;
                            }
                        }
                    }
                }
            }

            this.maxOctave = maxOctave;
            this.minOctave = minOctave;
            this.minIndex = (minOctave == 0) ? (int)Step.A : (int)Step.C + (7 * minOctave);
        }

        private int CalculateVisibleNotes()
        {
            int noteSum = 0;
            if (this.minOctave == 0)
            {
                noteSum += 2;
                this.minOctave++;
            }
            else if (this.maxOctave == 8)
            {
                noteSum++;
                this.maxOctave--;
            }

            noteSum += (this.maxOctave - this.minOctave + 1) * 7;
            return noteSum;
        }

        public void CreatePractice()
        {
            // PracticeNotes -> PracticeStucture.json
        }
    }
}
