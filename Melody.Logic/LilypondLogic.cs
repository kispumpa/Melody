namespace Melody.Logic
{
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using CommunityToolkit.Mvvm.Messaging;

    public class LilypondLogic : ILilypondLogic
    {
        private static readonly string PythonPath = AppDomain.CurrentDomain.BaseDirectory + @"\lilypond-2.24.4-mingw-x86_64\lilypond-2.24.4\bin\python.exe";

        private static readonly string LilypondPath = AppDomain.CurrentDomain.BaseDirectory + @"\lilypond-2.24.4-mingw-x86_64\lilypond-2.24.4\bin\lilypond.exe";

        private static readonly string Mxml2lyPath = AppDomain.CurrentDomain.BaseDirectory + @"\lilypond-2.24.4-mingw-x86_64\lilypond-2.24.4\bin\musicxml2ly.py";

        private IMessenger messenger;

        public string SvgPath { get; private set; }

        public LilypondLogic(IMessenger messenger)
        {
            this.messenger = messenger;
        }

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

                string fileName = Path.GetFileNameWithoutExtension(mxlFilePath);
                string lyFilePath = Path.Combine(outputDirectory, $"{fileName}.ly");
                string svgFilePath = Path.Combine(outputDirectory, $"{fileName}-1.svg");

                this.messenger.Send("Converting MusicXML to LilyPond format...", "MusicXmlLoadResult");

                RunProcess(PythonPath, $"{Mxml2lyPath} --output={lyFilePath} {mxlFilePath}");

                if (!File.Exists(lyFilePath))
                {
                    throw new FileNotFoundException(message: $".ly file not exist here: {lyFilePath}");
                }

                this.messenger.Send("Converting LilyPond to SVG...", "MusicXmlLoadResult");

                RunProcess(LilypondPath, $"--output={outputDirectory} -fsvg {lyFilePath}");

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
