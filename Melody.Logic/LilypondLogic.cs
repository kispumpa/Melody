// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using System.Diagnostics;
    using CommunityToolkit.Mvvm.Messaging;
    using Melody.Logic.Interfaces;

    public class LilypondLogic : ILilypondLogic
    {
        private IMessenger messenger;

        public LilypondLogic(IMessenger messenger)
        {
            this.messenger = messenger;
        }

        public string SvgPath { get; private set; }

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

                this.messenger.Send("Converting LilyPond to SVG...", "MusicXmlLoadResult");

                RunProcess(config.LilypondConfig.LilypondPath, $"--output={outputDirectory} -fsvg {lyFilePath}");

                if (!File.Exists(svgFilePath))
                {
                    throw new FileNotFoundException(message: $".svg file not exist");
                }

                this.messenger.Send($"SVG created successfully: {outputDirectory}", "MusicXmlLoadResult");
                this.SvgPath = svgFilePath;
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
