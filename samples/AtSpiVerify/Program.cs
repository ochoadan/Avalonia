using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace AtSpiVerify;

// A ListBox of two rows that gains a third when the file named by the first argument appears, then, when "<file>.2"
// appears, has all its rows removed and put back in one turn of the dispatcher, a burst of changes like a scroll;
// and an open ComboBox that gains an item when "<file>.3" appears. Beside them, a TextBlock with an explicit automation
// name and a live TextBlock whose text changes every half second.
public class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            return;

        var trigger = lifetime.Args![0];
        var rows = new ObservableCollection<string> { "one", "two" };
        var choices = new ObservableCollection<string> { "red", "green" };
        var combo = new ComboBox { ItemsSource = choices };
        AutomationProperties.SetName(combo, "colour");
        var burst = false;

        var labelled = new TextBlock { Text = "shown text" };
        AutomationProperties.SetName(labelled, "explicit name");

        var ticks = 0;
        var live = new TextBlock { Text = "tick 0" };
        AutomationProperties.SetName(live, "live name");
        AutomationProperties.SetLiveSetting(live, AutomationLiveSetting.Polite);

        var panel = new StackPanel();
        panel.Children.Add(labelled);
        panel.Children.Add(live);
        panel.Children.Add(new ListBox { ItemsSource = rows, Height = 120 });
        panel.Children.Add(combo);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) =>
        {
            live.Text = $"tick {++ticks}";
            if (rows.Count == 2 && File.Exists(trigger))
            {
                rows.Add("three");
                combo.IsDropDownOpen = true;
            }

            if (!burst && File.Exists(trigger + ".2"))
            {
                burst = true;
                var kept = new List<string>(rows);
                rows.Clear();
                foreach (var row in kept)
                    rows.Add(row);
            }

            if (choices.Count == 2 && File.Exists(trigger + ".3"))
                choices.Add("blue");
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
