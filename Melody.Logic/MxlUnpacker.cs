// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using System.IO.Compression;
    using System.Xml.Linq;
    using Melody.Logic.Interfaces;

    /// <summary>Class for unzipping mxl file.</summary>
    public class MxlUnpacker : IMxlUnpacker
    {
        /// <summary>Gets the mxl file's path.</summary>
        public string MxlPath { get; private set; }

        /// <summary>Gets the musicxml file's path.</summary>
        public string MusicXmlPath { get; private set; }

        /// <summary>Extract the MusicXML file from mxl.</summary>
        /// <param name="mxlFilePath">The mxl file path.</param>
        /// <returns>The MusicXML content.</returns>
        public string ExtractMusicXml(string mxlFilePath)
        {
            if (!File.Exists(mxlFilePath))
            {
                throw new FileNotFoundException("Az MXL fájl nem található", mxlFilePath);
            }

            this.MxlPath = mxlFilePath;

            using (ZipArchive archive = ZipFile.OpenRead(this.MxlPath))
            {
                ZipArchiveEntry containerEntry = archive.GetEntry("META-INF/container.xml");

                string rootFileName = "score.xml";

                if (containerEntry != null)
                {
                    using (Stream stream = containerEntry.Open())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string containerXml = reader.ReadToEnd();
                        XDocument doc = XDocument.Parse(containerXml);

                        XNamespace ns = "urn:oasis:names:tc:opendocument:xmlns:container";
                        var rootfile = doc.Descendants("rootfile").FirstOrDefault();

                        if (rootfile != null)
                        {
                            rootFileName = rootfile.Attribute("full-path")?.Value ?? rootFileName;
                        }
                    }
                }

                ZipArchiveEntry musicXmlEntry = archive.GetEntry(rootFileName);

                if (musicXmlEntry == null)
                {
                    throw new InvalidDataException($"A {rootFileName} fájl nem található az MXL archívumban");
                }

                using (Stream stream = musicXmlEntry.Open())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>Just extracts the mxl file to the given directory.</summary>
        /// <param name="mxlFilePath">The mxl file path.</param>
        /// <param name="destinationPath">The destination directory path.</param>
        public void ExtractToDirectory(string mxlFilePath, string destinationPath)
        {
            if (!File.Exists(mxlFilePath))
            {
                throw new FileNotFoundException("Az MXL fájl nem található", mxlFilePath);
            }

            this.MxlPath = mxlFilePath;

            Directory.CreateDirectory(destinationPath);

            ZipFile.ExtractToDirectory(this.MxlPath, destinationPath);

            Console.WriteLine($"MXL fájl kicsomagolva ide: {destinationPath}");
        }

        /// <summary>Extracts the mxl file with <see cref="ExtractMusicXml(string)"/> and saves it to the given output path.</summary>
        /// <param name="mxlFilePath">The mxl file path.</param>
        /// <param name="outputXmlPath">The output xml file path.</param>
        public void ExtractAndSave(string mxlFilePath, string outputXmlPath)
        {
            string musicXmlContent = this.ExtractMusicXml(mxlFilePath);
            File.WriteAllText(outputXmlPath, musicXmlContent);

            Console.WriteLine($"MusicXML mentve ide: {outputXmlPath}");
            this.MusicXmlPath = outputXmlPath;
        }
    }
}
