// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Interfaces
{
    /// <summary>Interface for <see cref="LilypondLogic"/> class.</summary>
    public interface ILilypondLogic
    {
        /// <summary>Gets the path of the generated svg.</summary>
        string SvgPath { get; }

        /// <summary>Loads a MusicXML file and converts it to LilyPond format, then generates an SVG from it.</summary>
        /// <param name="mxlFilePath">The path of the MusicXML file to load.</param>
        /// <param name="outputDirectory">The directory where the output SVG will be saved. If null, a default directory is used.</param>
        void LoadLilypond(string mxlFilePath, string outputDirectory = null);
    }
}