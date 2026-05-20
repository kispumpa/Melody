// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using System.Diagnostics;
    using CommunityToolkit.Mvvm.Messaging;
    using Melody.Logic.Interfaces;
    using Melody.Logic.Models;

    /// <summary>Handles the conversion of MusicXML files to LilyPond format and generates PNG files.</summary>
    public class LilypondLogic : ILilypondLogic
    {
        private IMessenger messenger;

        /// <summary>Initializes a new instance of the <see cref="LilypondLogic"/> class.</summary>
        /// <param name="messenger">The messenger for sending notifications.</param>
        public LilypondLogic(IMessenger messenger)
        {
            this.messenger = messenger;
        }

        /// <inheritdoc/>
        public string PngOutputDirectory { get; private set; }

        /// <inheritdoc/>
        public List<string> GeneratedPngPaths { get; private set; } = new List<string>();

        /// <inheritdoc/>
        public void LoadLilypond(string mxlFilePath, string outputDirectory = null)
        {
            try
            {
                if (!File.Exists(mxlFilePath))
                {
                    throw new ArgumentException(message: $"Input file not found: {mxlFilePath}");
                }

                if (string.IsNullOrEmpty(outputDirectory))
                {
                    outputDirectory = Path.GetDirectoryName(mxlFilePath);
                }

                // Létrehozunk egy almappát a PNG-knek
                string fileName = Path.GetFileNameWithoutExtension(mxlFilePath);
                string pngOutputDir = Path.Combine(outputDirectory, $"{fileName}_pngs");
                Directory.CreateDirectory(pngOutputDir);

                this.PngOutputDirectory = pngOutputDir;
                this.GeneratedPngPaths.Clear();

                ConfigObject config = ConfigHandler.ReadConfigFile("C:/Users/matul/OneDrive/Dokumentumok/melody_proj/Melody/Melody.UI/config.yaml");

                string lyFilePath = Path.Combine(outputDirectory, $"{fileName}.ly");

                this.messenger.Send("Converting MusicXML to LilyPond format...", "MusicXmlLoadResult");

                RunProcess(config.LilypondConfig.PythonPath, $"{config.LilypondConfig.Mxml2lyPath} --output=\"{lyFilePath}\" \"{mxlFilePath}\"");

                if (!File.Exists(lyFilePath))
                {
                    throw new FileNotFoundException(message: $".ly file not exist here: {lyFilePath}");
                }

                string newRule = "    page-breaking = #ly:one-line-breaking" + Environment.NewLine;
                string originalContent = File.ReadAllText(lyFilePath);
                string updatedContent;

                if (originalContent.Contains("\\paper {"))
                {
                    updatedContent = originalContent.Replace("\\paper {", "\\paper {" + Environment.NewLine + newRule);
                }
                else
                {
                    updatedContent = "\\paper {" + Environment.NewLine + newRule + "}" + Environment.NewLine + originalContent;
                }

                if (originalContent.Contains("\\score {"))
                {
                    updatedContent = updatedContent.Replace("\\score {", "\\score {" + Environment.NewLine + "    \\unfoldRepeats {");

                    updatedContent = updatedContent.Replace("\\layout {", "    }" + Environment.NewLine + "    \\layout {");
                }

                string strictProportional = @"
    \context {
      \Score
      proportionalNotationDuration = #(ly:make-moment 1/16)
      
      % Szigorú arányosság kikényszerítése
      \override SpacingSpanner.strict-note-spacing = ##t
      \override SpacingSpanner.strict-grace-spacing = ##t
      \override SpacingSpanner.uniform-stretching = ##t
      
      % Felesleges extra helyek eltávolítása az ütemvonalak körül
      \override SpacingSpanner.base-shortest-duration = #(ly:make-moment 1/16)
    }
";

                updatedContent = updatedContent.Replace("\\context { \\Score", strictProportional);

                File.WriteAllText(lyFilePath, updatedContent);

                this.messenger.Send("Converting LilyPond to PNG...", "MusicXmlLoadResult");

                RunProcess(config.LilypondConfig.LilypondPath, $"--png -dresolution=300 --output={pngOutputDir}/{fileName} {lyFilePath}");

                List<string> pngFiles = Directory.GetFiles(pngOutputDir, "*.png")
                    .OrderBy(f => f)
                    .ToList();

                if (pngFiles.Count == 0)
                {
                    throw new FileNotFoundException(message: $"No PNG files generated in {pngOutputDir}");
                }

                this.GeneratedPngPaths.AddRange(pngFiles);

                this.messenger.Send($"PNG(s) created successfully: {pngOutputDir} ({pngFiles.Count} files)", "MusicXmlLoadResult");
            }
            catch (Exception ex)
            {
                this.messenger.Send($"Error during conversion: {ex.Message}", "MusicXmlLoadResult");
            }
        }

        private static void RunProcess(string executable, string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = executable;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.WriteLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.WriteLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }
    }
}
