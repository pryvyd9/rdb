namespace ConsoleApp1

open System
open Avalonia;
open Avalonia.Logging;
open Avalonia.Controls.ApplicationLifetimes;
open Avalonia.Markup.Xaml;
open Avalonia.Controls

type App() =
    inherit Application()
    override this.Initialize() = AvaloniaXamlLoader.Load(this)
    override this.OnFrameworkInitializationCompleted() = 
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop -> desktop.MainWindow <- MainWindow()
        base.OnFrameworkInitializationCompleted()
    