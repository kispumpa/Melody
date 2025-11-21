// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using CommunityToolkit.Mvvm.Messaging;
    using Melody.Logic.Interfaces;

    public class ToggleViewLogic : IToggleViewLogic
    {
        private IMessenger messenger;

        public ToggleViewLogic(IMessenger messenger)
        {
            this.IsPianoRollView = true;
            this.messenger = messenger;
        }

        public bool IsPianoRollView { get; set; }

        public void ToggleView()
        {
            this.IsPianoRollView = !this.IsPianoRollView;
            this.messenger.Send($"View changed to {(this.IsPianoRollView ? "piano roll" : "sheet music")}", "ViewResult");
        }
    }
}
