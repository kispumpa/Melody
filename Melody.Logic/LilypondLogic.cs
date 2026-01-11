// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using System.Diagnostics;
    using CommunityToolkit.Mvvm.Messaging;
    using Melody.Logic.Interfaces;

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

        /// <summary>Gets the path of the PNG output directory.</summary>
        public string PngOutputDirectory { get; private set; }

        /// <summary>Gets the list of generated PNG file paths.</summary>
        public List<string> GeneratedPngPaths { get; private set; } = new List<string>();

        /// <summary>Loads a MusicXML file and converts it to LilyPond format, then generates PNG files from it.</summary>
        /// <param name="mxlFilePath">The path of the MusicXML file to load.</param>
        /// <param name="outputDirectory">The directory where the output PNGs will be saved. If null, a default directory is used.</param>
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

                var config = ConfigHandler.ReadConfigFile("C:/Users/matul/OneDrive/Dokumentumok/melody_proj/Melody/Melody.UI/config.yaml");

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

                File.WriteAllText(lyFilePath, updatedContent);

                this.messenger.Send("Converting LilyPond to PNG...", "MusicXmlLoadResult");

                // PNG generálás 300 DPI felbontással (jobb minőség)
                RunProcess(config.LilypondConfig.LilypondPath, $"--png -dresolution=300 --output={pngOutputDir}/{fileName} {lyFilePath}");

                // Összegyűjtjük az összes generált PNG-t
                var pngFiles = Directory.GetFiles(pngOutputDir, "*.png")
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