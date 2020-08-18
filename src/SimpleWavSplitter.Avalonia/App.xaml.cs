// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace SimpleWavSplitter.Avalonia
{
    /// <summary>
    /// Main application.
    /// </summary>
    public class App : Application
    {
        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="args">The program arguments.</param>
        static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        /// <summary>
        /// Build Avalonia app.
        /// </summary>
        /// <returns>The Avalonia app builder.</returns>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .LogToDebug();

        /// <inheritdoc/>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <inheritdoc/>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.MainWindow = new MainWindow();
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewLifetime)
            {
                //singleViewLifetime.MainView = new MainView();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
