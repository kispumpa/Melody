using Melody.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Melody.Logic
{
    public class MxlUnpacker : IMxlUnpacker
    {
        public string MxlPath { get; private set; }

        public string MusicXmlPath { get; private set; }

        public string ExtractMusicXml(string mxlFilePath)
        {
            if (!File.Exists(mxlFilePath))
            {
                throw new FileNotFoundException("Az MXL fájl nem található", mxlFilePath);
            }

            MxlPath = mxlFilePath;

            using (ZipArchive archive = ZipFile.OpenRead(MxlPath))
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
                        var rootfile = doc.Descendants(ns + "rootfile").FirstOrDefault();

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

        public void ExtractToDirectory(string mxlFilePath, string destinationPath)
        {
            if (!File.Exists(mxlFilePath))
            {
                throw new FileNotFoundException("Az MXL fájl nem található", mxlFilePath);
            }

            MxlPath = mxlFilePath;

            Directory.CreateDirectory(destinationPath);

            ZipFile.ExtractToDirectory(MxlPath, destinationPath);

            Console.WriteLine($"MXL fájl kicsomagolva ide: {destinationPath}");
        }

        public void ExtractAndSave(string mxlFilePath, string outputXmlPath)
        {
            string musicXmlContent = ExtractMusicXml(mxlFilePath);
            File.WriteAllText(outputXmlPath, musicXmlContent);

            Console.WriteLine($"MusicXML mentve ide: {outputXmlPath}");
            MusicXmlPath = outputXmlPath;
        }
    }
}
