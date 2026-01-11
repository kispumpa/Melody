// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Interfaces
{
    /// <summary>Interface for handling LilyPond conversions.</summary>
    public interface ILilypondLogic
    {
        /// <summary>Gets the path of the PNG output directory.</summary>
        string PngOutputDirectory { get; }

        /// <summary>Gets the list of generated PNG file paths.</summary>
        List<string> GeneratedPngPaths { get; }

        /// <summary>Loads a MusicXML file and converts it to LilyPond format, then generates PNG files from it.</summary>
        /// <param name="mxlFilePath">The path of the MusicXML file to load.</param>
        /// <param name="outputDirectory">The directory where the output PNGs will be saved. If null, a default directory is used.</param>
        void LoadLilypond(string mxlFilePath, string outputDirectory = null);
    }
}