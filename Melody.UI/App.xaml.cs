// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI
{
    using System.Windows;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using CommunityToolkit.Mvvm.Messaging;
    using Melody.Logic;
    using Melody.Logic.Interfaces;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Initializes a new instance of the <see cref="App"/> class.</summary>
        public App()
        {
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                .AddSingleton<IToggleViewLogic, ToggleViewLogic>()
                .AddSingleton<ILilypondLogic, LilypondLogic>()
                .AddSingleton<IPianorollLogic, PianorollLogic>()
                .AddSingleton<IMxlUnpacker, MxlUnpacker>()
                .AddSingleton<IPracticeLogic, PracticeLogic>()
                .AddSingleton<IMessenger>(WeakReferenceMessenger.Default)
                .BuildServiceProvider());
        }
    }
}
