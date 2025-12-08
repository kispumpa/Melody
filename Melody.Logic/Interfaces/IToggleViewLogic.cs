// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic.Interfaces
{
    /// <summary>
    /// Interface for toggling between different views in the application.
    /// </summary>
    public interface IToggleViewLogic
    {
        bool IsPianoRollView { get; set; }

        void ToggleView();
    }
}