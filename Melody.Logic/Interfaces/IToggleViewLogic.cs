// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Interfaces
{
    /// <summary>
    /// Interface for toggling between different views in the application.
    /// </summary>
    public interface IToggleViewLogic
    {
        /// <summary>Gets or sets a value indicating whether the piano roll view is active.</summary>
        bool IsPianoRollView { get; set; }

        /// <summary>Toggles between the piano roll view and the sheet view.</summary>
        void ToggleView();
    }
}
