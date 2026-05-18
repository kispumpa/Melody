// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using CommunityToolkit.Mvvm.Messaging;
    using Melody.Logic.Interfaces;

    /// <summary>Handles the logic for toggling between different views.</summary>
    public class ToggleViewLogic : IToggleViewLogic
    {
        private IMessenger messenger;

        /// <summary>Initializes a new instance of the <see cref="ToggleViewLogic"/> class.</summary>
        /// <param name="messenger">The messenger used for sending view change notifications.</param>
        public ToggleViewLogic(IMessenger messenger)
        {
            this.IsPianoRollView = false;
            this.messenger = messenger;
        }

        /// <summary>Gets or sets a value indicating whether the piano roll view is active.</summary>
        public bool IsPianoRollView { get; set; }

        /// <summary>Toggles between the piano roll view and the sheet view.</summary>
        public void ToggleView()
        {
            this.IsPianoRollView = !this.IsPianoRollView;
            this.messenger.Send($"View changed to {(this.IsPianoRollView ? "piano roll" : "sheet music")}", "ViewResult");
        }
    }
}
