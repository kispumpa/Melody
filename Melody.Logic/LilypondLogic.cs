// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using System.Diagnostics;
    using CommunityToolkit.Mvvm.Messaging;
    using Melody.Logic.Interfaces;

    /// <summary>Handles the conversion of MusicXML files to LilyPond format and generates SVG files.</summary>
    public class LilypondLogic : ILilypondLogic
    {
        private IMessenger messenger;

        /// <summary>Initializes a new instance of the <see cref="LilypondLogic"/> class.</summary>
        /// <param name="messenger">The messenger for sending notifications.</param>
        public LilypondLogic(IMessenger messenger)
        {
            this.messenger = messenger;
        }

        /// <summary>Gets the path of the generated svg.</summary>
        public string SvgPath { get; private set; }

        /// <summary>Loads a MusicXML file and converts it to LilyPond format, then generates an SVG from it.</summary>
        /// <param name="mxlFilePath">The path of the MusicXML file to load.</param>
        /// <param name="outputDirectory">The directory where the output SVG will be saved. If null, a default directory is used.</param>
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

                Directory.CreateDirectory(outputDirectory);

                var config = ConfigHandler.ReadConfigFile("C:/Users/matul/OneDrive/Dokumentumok/melody_proj/Melody/Melody.UI/config.yaml");

                string fileName = Path.GetFileNameWithoutExtension(mxlFilePath);
                string lyFilePath = Path.Combine(outputDirectory, $"{fileName}.ly");
                string svgFilePath = Path.Combine(outputDirectory, $"{fileName}.svg");

                this.messenger.Send("Converting MusicXML to LilyPond format...", "MusicXmlLoadResult");

                RunProcess(config.LilypondConfig.PythonPath, $"{config.LilypondConfig.Mxml2lyPath} --output=\"{lyFilePath}\" \"{mxlFilePath}\"");

                if (!File.Exists(lyFilePath))
                {
                    throw new FileNotFoundException(message: $".ly file not exist here: {lyFilePath}");
                }

                string customPaper = @"
\paper { 
    page-breaking = #ly:one-line-breaking 
    ragged-right = ##f 
    check-consistency = ##f
}
";
                //File.AppendAllText(lyFilePath, customPaper);
                string originalContent = File.ReadAllText(lyFilePath);

                //// 3. Az elejére illesztjük az új beállításokat és visszaírjuk
                //File.WriteAllText(lyFilePath, customPaper + Environment.NewLine + originalContent);
                //this.messenger.Send("Converting LilyPond to SVG...", "MusicXmlLoadResult");


                string newRule = "    page-breaking = #ly:one-line-breaking" + Environment.NewLine;

                string updatedContent;

                if (originalContent.Contains("\\paper {"))
                {
                    // Ha már van \paper blokk, beszúrjuk a nyitó zárójel után
                    updatedContent = originalContent.Replace("\\paper {", "\\paper {" + Environment.NewLine + newRule);
                }
                else
                {
                    // Ha véletlenül mégsem lenne (biztonsági játék), az elejére tesszük
                    updatedContent = "\\paper {" + Environment.NewLine + newRule + "}" + Environment.NewLine + originalContent;
                }

                File.WriteAllText(lyFilePath, updatedContent);

                //RunProcess(config.LilypondConfig.LilypondPath, $"-dbackend=svg -dno-pages -dsvg-woff=##f --output={outputDirectory} -fsvg {lyFilePath}");
                RunProcess(config.LilypondConfig.LilypondPath, $"--output={outputDirectory} -fsvg {lyFilePath}");

                if (!File.Exists(svgFilePath))
                {
                    throw new FileNotFoundException(message: $".svg file not exist");
                }

                this.messenger.Send($"SVG created successfully: {outputDirectory}", "MusicXmlLoadResult");
                this.SvgPath = svgFilePath.Replace("\\", "/");
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
