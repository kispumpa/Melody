// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Interfaces
{
    /// <summary>Interface for <see cref="MxlUnpacker"/> class.</summary>
    public interface IMxlUnpacker
    {
        /// <summary>Gets the musicxml file's path.</summary>
        string MusicXmlPath { get; }

        /// <summary>Gets the mxl file's path.</summary>
        string MxlPath { get; }

        /// <summary>Extracts the mxl file with <see cref="ExtractMusicXml(string)"/> and saves it to the given output path.</summary>
        /// <param name="mxlFilePath">The mxl file path.</param>
        /// <param name="outputXmlPath">The output xml file path.</param>
        void ExtractAndSave(string mxlFilePath, string outputXmlPath);

        /// <summary>Extract the MusicXML file from mxl.</summary>
        /// <param name="mxlFilePath">The mxl file path.</param>
        /// <returns>The MusicXML content.</returns>
        string ExtractMusicXml(string mxlFilePath);

        /// <summary>Just extracts the mxl file to the given directory.</summary>
        /// <param name="mxlFilePath">The mxl file path.</param>
        /// <param name="destinationPath">The destination directory path.</param>
        void ExtractToDirectory(string mxlFilePath, string destinationPath);
    }
}