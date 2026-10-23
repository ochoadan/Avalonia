using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace AtSpiVerify;

// A ListBox of two rows that gains a third when the file named by the first argument appears, a TextBlock with an
// explicit automation name, and a live TextBlock with an explicit name whose text changes every half second.
public class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var trigger = lifetime.Args![0];
        var rows = new ObservableCollection<string> { "one", "two" };

        var labelled = new TextBlock { Text = "shown text" };
        AutomationProperties.SetName(labelled, "explicit name");

        var ticks = 0;
        var live = new TextBlock { Text = "tick 0" };
        AutomationProperties.SetName(live, "live name");
        AutomationProperties.SetLiveSetting(live, AutomationLiveSetting.Polite);

        var panel = new StackPanel();
        panel.Children.Add(labelled);
        panel.Children.Add(live);
        panel.Children.Add(new ListBox { ItemsSource = rows, Height = 200 });

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) =>
        {
            live.Text = $"tick {++ticks}";
            if (rows.Count == 2 && File.Exists(trigger))
                rows.Add("three");
        };
        timer.Start();

        lifetime.MainWindow = new Window { Title = "AtSpiVerify", Width = 320, Height = 320, Content = panel };
    }
}

public static class Program
{
    public static void Main(string[] args) =>
        AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}
