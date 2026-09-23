using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Flyworm.Models;
using Flyworm.Services;

namespace Flyworm;

public partial class MainWindow : Window
{
    private static readonly string[] AppRemovalIds =
    [
        "RemoveApps", "RemoveCommApps", "RemoveW11Outlook",
        "RemoveGamingApps", "RemoveHPApps", "ForceRemoveEdge"
    ];

    private readonly I18n _i18n = I18n.Current;
    private readonly ScriptRunner _runner = new();
    private readonly Dictionary<string, SelectableOption> _options = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SelectableGroup> _groups = [];
    private readonly Dictionary<string, CheckBox> _checkBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ComboBox> _comboBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _defaultFeatureIds;
    private readonly FeatureFile _features;
    private readonly TweakCatalog _tweaks;
    private string _page = "debloat";
    private bool _busy;
    private double _displayPercent;
    private double _targetPercent;
    private DispatcherTimer? _progressTimer;

    public MainWindow()
    {
        InitializeComponent();
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/app.ico"));
        LogoImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/logo.png"));

        if (!File.Exists(AppPaths.FeaturesJson) || !File.Exists(AppPaths.TweakCatalog))
        {
            MessageBox.Show(
                "Gerekli dosyalar bulunamadı. Uygulamayı publish klasöründen veya proje çıktısından çalıştır.",
                "Flyworm",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            _features = new FeatureFile();
            _defaultFeatureIds = [];
            _tweaks = new TweakCatalog();
        }
        else
        {
            _features = CatalogLoader.LoadFeatures(AppPaths.FeaturesJson);
            _defaultFeatureIds = CatalogLoader.LoadDefaultFeatureIds(AppPaths.DefaultSettingsJson);
            _tweaks = CatalogLoader.LoadTweaks(AppPaths.TweakCatalog);
        }

        SeedOptions();
        ApplyLanguage();
        ShowPage("debloat");
        ResetLogUi();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        StartNeonText(TitleText, 22);
        StartNeonText(SubtitleText, 13);

        var args = Environment.GetCommandLineArgs();
        var shot = Array.FindIndex(args, a => a.Equals("--export-screenshots", StringComparison.OrdinalIgnoreCase));
        if (shot >= 0)
        {
            var folder = shot + 1 < args.Length
                ? args[shot + 1]
                : System.IO.Path.Combine(Path.GetTempPath(), "flyworm-shots");
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "_started.txt"), string.Join(Environment.NewLine, args));
                await ExportScreenshotsAsync(folder);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "flyworm-shot-error.txt"), ex.ToString());
                Close();
            }
        }
    }

    private void SeedOptions()
    {
        var groupedIds = _features.UiGroups
            .SelectMany(g => g.Values.SelectMany(v => v.FeatureIds))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var id in AppRemovalIds)
        {
            _options[id] = new SelectableOption
            {
                Id = id,
                Kind = "feature",
                IsDangerous = id == "ForceRemoveEdge",
                IsChecked = id == "RemoveApps"
            };
        }

        foreach (var feature in _features.Features)
        {
            if (string.IsNullOrWhiteSpace(feature.Category) || groupedIds.Contains(feature.FeatureId))
            {
                continue;
            }

            _options[feature.FeatureId] = new SelectableOption
            {
                Id = feature.FeatureId,
                Kind = "feature",
                IsChecked = _defaultFeatureIds.Contains(feature.FeatureId)
            };
        }

        foreach (var group in _features.UiGroups)
        {
            _groups.Add(new SelectableGroup
            {
                GroupId = group.GroupId,
                Category = group.Category,
                SelectedIndex = 0,
                Values = group.Values.Select(v => new SelectableGroupValue
                {
                    LabelKey = $"gval.{v.Label}",
                    Fallback = v.Label,
                    FeatureId = v.FeatureIds.FirstOrDefault() ?? ""
                }).ToList()
            });
        }

        foreach (var tweak in _tweaks.Tweaks.OrderBy(t => t.Order))
        {
            _options[tweak.Id] = new SelectableOption
            {
                Id = tweak.Id,
                Kind = "tweak",
                IsDangerous = tweak.Dangerous,
                OpensSettings = tweak.OpensSettings,
                ScriptFile = tweak.File,
                Choice = tweak.Choice,
                IsChecked = !tweak.Dangerous && !tweak.OpensSettings && tweak.Id is not ("directx" or "cpp" or "edge" or "bloatware")
            };
        }

        foreach (var gpu in _tweaks.Gpu)
        {
            _options[gpu.Id] = new SelectableOption
            {
                Id = gpu.Id,
                Kind = "gpu",
                ScriptFile = gpu.File,
                Choice = gpu.Choice
            };
        }
    }

    private void ApplyLanguage()
    {
        Title = _i18n.T("app.title");
        TitleText.Text = _i18n.T("app.title");
        SubtitleText.Text = _i18n.T("app.subtitle");
        NavDebloat.Content = _i18n.T("nav.debloat");
        NavTweaks.Content = _i18n.T("nav.tweaks");
        NavGpu.Content = _i18n.T("nav.gpu");
        NavLog.Content = _i18n.T("nav.log");
        NavAbout.Content = _i18n.T("nav.about");
        ApplyButton.Content = _i18n.T("action.apply");
        DefaultsButton.Content = _i18n.T("action.defaults");
        SelectAllButton.Content = _i18n.T("action.selectAll");
        ClearButton.Content = _i18n.T("action.clear");
        RestorePointBox.Content = _i18n.T("action.restorePoint");
        StatusText.Text = _busy ? _i18n.T("status.running") : _i18n.T("status.ready");
        AdminText.Text = IsAdministrator() ? _i18n.T("admin.ok") : _i18n.T("admin.missing");
        AdminText.Foreground = IsAdministrator()
            ? (Brush)FindResource("AccentSoftBrush")
            : (Brush)FindResource("DangerBrush");
        LangTr.Opacity = _i18n.IsTurkish ? 1 : 0.55;
        LangEn.Opacity = _i18n.IsTurkish ? 0.55 : 1;
        ShowPage(_page);
    }

    private void LangTr_Click(object sender, RoutedEventArgs e)
    {
        _i18n.Language = AppLanguage.Turkish;
        ApplyLanguage();
    }

    private void LangEn_Click(object sender, RoutedEventArgs e)
    {
        _i18n.Language = AppLanguage.English;
        ApplyLanguage();
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (ReferenceEquals(sender, NavDebloat)) ShowPage("debloat");
        else if (ReferenceEquals(sender, NavTweaks)) ShowPage("tweaks");
        else if (ReferenceEquals(sender, NavGpu)) ShowPage("gpu");
        else if (ReferenceEquals(sender, NavLog)) ShowPage("log");
        else if (ReferenceEquals(sender, NavAbout)) ShowPage("about");
    }

    private void ShowPage(string page)
    {
        _page = page;
        var showLog = page == "log";
        LogPanel.Visibility = showLog ? Visibility.Visible : Visibility.Collapsed;
        PageScroll.Visibility = showLog ? Visibility.Collapsed : Visibility.Visible;
        PageHint.Text = page switch
        {
            "debloat" => _i18n.T("page.debloat.hint"),
            "tweaks" => _i18n.T("page.tweaks.hint"),
            "gpu" => _i18n.T("page.gpu.hint"),
            "about" => _i18n.T("page.about.hint"),
            _ => _i18n.T("page.log.hint")
        };

        var showActions = page is "debloat" or "tweaks" or "gpu";
        ActionButtons.Visibility = showActions ? Visibility.Visible : Visibility.Collapsed;
        RestorePointBox.Visibility = page == "debloat" ? Visibility.Visible : Visibility.Collapsed;
        DefaultsButton.Content = page == "debloat" ? _i18n.T("action.defaults") : _i18n.T("action.resetPage");

        if (showLog)
        {
            if (!_busy && LogStatus.Text == "")
            {
                ResetLogUi();
            }

            return;
        }

        PageHost.Children.Clear();
        _checkBoxes.Clear();
        _comboBoxes.Clear();

        if (page == "debloat")
        {
            BuildDebloatPage();
        }
        else if (page == "tweaks")
        {
            BuildTweakList(_tweaks.Tweaks.OrderBy(t => t.Order));
        }
        else if (page == "gpu")
        {
            BuildTweakList(_tweaks.Gpu);
        }
        else if (page == "about")
        {
            BuildAboutPage();
        }
    }

    private void BuildDebloatPage()
    {
        AddSection(_i18n.T("cat.Apps"), AppRemovalIds.Select(id => _options[id]), icon: "\uE71D");

        foreach (var category in _features.Categories)
        {
            var items = _features.Features
                .Where(f => string.Equals(f.Category, category.Name, StringComparison.OrdinalIgnoreCase))
                .Where(f => _options.ContainsKey(f.FeatureId))
                .Select(f => _options[f.FeatureId])
                .ToList();
            var groups = _groups.Where(g => string.Equals(g.Category, category.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (items.Count == 0 && groups.Count == 0)
            {
                continue;
            }

            AddSection(_i18n.CategoryName(category.Name), items, groups, CatalogLoader.DecodeIcon(category.Icon));
        }
    }

    private void BuildTweakList(IEnumerable<TweakEntry> entries)
    {
        var panel = new StackPanel();
        foreach (var entry in entries)
        {
            if (!_options.TryGetValue(entry.Id, out var option))
            {
                continue;
            }

            panel.Children.Add(CreateOptionCard(option, _i18n.T($"tweak.{entry.Id}"), _i18n.T($"tweak.{entry.Id}.desc")));
        }

        PageHost.Children.Add(panel);
    }

    private void AddSection(string title, IEnumerable<SelectableOption> options, IEnumerable<SelectableGroup>? groups = null, string? icon = null)
    {
        var box = new Border
        {
            Background = (Brush)FindResource("CardBrush"),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var stack = new StackPanel();
        stack.Children.Add(CreateSectionHeader(title, icon));

        foreach (var option in options)
        {
            var feature = _features.Features.FirstOrDefault(f => f.FeatureId == option.Id);
            var label = feature is null
                ? option.Id
                : _i18n.FeatureTitle(option.Id, feature.Action ?? "", feature.Label);
            stack.Children.Add(CreateOptionRow(option, label, feature?.ToolTip));
        }

        if (groups != null)
        {
            foreach (var group in groups)
            {
                stack.Children.Add(CreateGroupRow(group));
            }
        }

        box.Child = stack;
        PageHost.Children.Add(box);
    }

    private UIElement CreateOptionCard(SelectableOption option, string title, string description)
    {
        var box = new Border
        {
            Background = (Brush)FindResource("CardBrush"),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var row = new DockPanel();
        var check = CreateCheckBox(option);
        DockPanel.SetDock(check, Dock.Left);
        check.VerticalAlignment = VerticalAlignment.Top;
        check.Margin = new Thickness(0, 2, 12, 0);
        var texts = new StackPanel();
        texts.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            Foreground = option.IsDangerous ? (Brush)FindResource("DangerBrush") : (Brush)FindResource("TextBrush"),
            TextWrapping = TextWrapping.Wrap
        });
        texts.Children.Add(new TextBlock
        {
            Text = description,
            Foreground = (Brush)FindResource("MutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });
        row.Children.Add(check);
        row.Children.Add(texts);
        box.Child = row;
        return box;
    }

    private UIElement CreateOptionRow(SelectableOption option, string title, string? tip)
    {
        var check = CreateCheckBox(option);
        check.Content = title;
        if (!string.IsNullOrWhiteSpace(tip))
        {
            check.ToolTip = tip;
        }

        if (option.IsDangerous)
        {
            check.Foreground = (Brush)FindResource("DangerBrush");
        }

        check.Margin = new Thickness(0, 0, 0, 8);
        return check;
    }

    private UIElement CreateGroupRow(SelectableGroup group)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 6, 0, 10) };
        stack.Children.Add(new TextBlock
        {
            Text = _i18n.T($"group.{group.GroupId}"),
            Foreground = (Brush)FindResource("MutedBrush"),
            Margin = new Thickness(0, 0, 0, 6)
        });

        var combo = new ComboBox { SelectedIndex = group.SelectedIndex };
        combo.Items.Add(_i18n.T("group.none"));
        foreach (var value in group.Values)
        {
            var text = _i18n.T(value.LabelKey);
            combo.Items.Add(text == value.LabelKey ? value.Fallback : text);
        }

        combo.SelectionChanged += (_, _) => group.SelectedIndex = combo.SelectedIndex;
        _comboBoxes[group.GroupId] = combo;
        stack.Children.Add(combo);
        return stack;
    }

    private CheckBox CreateCheckBox(SelectableOption option)
    {
        var check = new CheckBox { IsChecked = option.IsChecked, Tag = option.Id };
        check.Checked += (_, _) => option.IsChecked = true;
        check.Unchecked += (_, _) => option.IsChecked = false;
        _checkBoxes[option.Id] = check;
        return check;
    }

    private void Defaults_Click(object sender, RoutedEventArgs e)
    {
        if (_page == "debloat")
        {
            foreach (var option in _options.Values.Where(o => o.Kind == "feature"))
            {
                option.IsChecked = _defaultFeatureIds.Contains(option.Id) || option.Id == "RemoveApps";
            }

            foreach (var group in _groups)
            {
                group.SelectedIndex = 0;
            }
        }
        else if (_page == "tweaks")
        {
            foreach (var tweak in _tweaks.Tweaks)
            {
                if (_options.TryGetValue(tweak.Id, out var option))
                {
                    option.IsChecked = !tweak.Dangerous && !tweak.OpensSettings && tweak.Id is not ("directx" or "cpp" or "edge" or "bloatware");
                }
            }
        }
        else if (_page == "gpu")
        {
            foreach (var option in _options.Values.Where(o => o.Kind == "gpu"))
            {
                option.IsChecked = false;
            }
        }

        ShowPage(_page);
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        var kind = CurrentKind();
        foreach (var option in _options.Values.Where(o => o.Kind == kind && !o.IsDangerous))
        {
            option.IsChecked = true;
        }

        ShowPage(_page);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        var kind = CurrentKind();
        foreach (var option in _options.Values.Where(o => o.Kind == kind))
        {
            option.IsChecked = false;
        }

        if (kind == "feature")
        {
            foreach (var group in _groups)
            {
                group.SelectedIndex = 0;
            }
        }

        ShowPage(_page);
    }

    private string CurrentKind() => _page switch
    {
        "tweaks" => "tweak",
        "gpu" => "gpu",
        _ => "feature"
    };

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        var selectedFeatures = new List<string>();
        var selectedScripts = new List<SelectableOption>();
        if (_page == "debloat")
        {
            selectedFeatures = _options.Values.Where(o => o.Kind == "feature" && o.IsChecked).Select(o => o.Id).ToList();
            foreach (var group in _groups.Where(g => g.SelectedIndex > 0))
            {
                selectedFeatures.Add(group.Values[group.SelectedIndex - 1].FeatureId);
            }
        }
        else if (_page == "tweaks")
        {
            selectedScripts = _options.Values.Where(o => o.IsChecked && o.Kind == "tweak").ToList();
        }
        else if (_page == "gpu")
        {
            selectedScripts = _options.Values.Where(o => o.IsChecked && o.Kind == "gpu").ToList();
        }

        if (selectedFeatures.Count == 0 && selectedScripts.Count == 0)
        {
            MessageBox.Show(_i18n.T("empty"), _i18n.T("app.title"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show(_i18n.T("confirm.body"), _i18n.T("confirm.title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        if ((selectedFeatures.Contains("ForceRemoveEdge", StringComparer.OrdinalIgnoreCase) ||
             selectedScripts.Any(s => s.Id == "edge")) &&
            MessageBox.Show(_i18n.T("confirm.edge"), _i18n.T("confirm.title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        if (selectedScripts.Any(s => s.IsDangerous) &&
            MessageBox.Show(_i18n.T("confirm.danger"), _i18n.T("confirm.title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        _busy = true;
        ApplyButton.IsEnabled = false;
        StatusText.Text = _i18n.T("status.running");
        NavLog.IsChecked = true;
        ShowPage("log");
        BeginLogProgress();

        try
        {
            var jobs = new List<Func<Task>>();
            if (selectedFeatures.Count > 0)
            {
                var args = new List<string> { "-Silent" };
                if (RestorePointBox.IsChecked == true)
                {
                    args.Add("-CreateRestorePoint");
                }

                foreach (var id in selectedFeatures.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    args.Add("-" + id);
                }

                jobs.Add(() => _runner.RunPowerShellAsync(AppPaths.Win11DebloatScript, args, AppPaths.Win11Debloat));
            }

            foreach (var script in selectedScripts)
            {
                var path = System.IO.Path.Combine(AppPaths.Tweaks, script.ScriptFile!.Replace('/', System.IO.Path.DirectorySeparatorChar));
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(string.Format(_i18n.T("missing.script"), script.ScriptFile));
                }

                jobs.Add(() => _runner.RunPowerShellAsync(
                    AppPaths.TweakRunner,
                    ["-ScriptPath", path, "-Choice", script.Choice ?? "1"],
                    AppPaths.Tweaks));
            }

            for (var i = 0; i < jobs.Count; i++)
            {
                AimProgress(i * 100.0 / jobs.Count, ((i + 1) * 100.0 / jobs.Count) - 1);
                await jobs[i]();
                SetProgress((i + 1) * 100.0 / jobs.Count);
            }

            StopProgressTimer();
            SetProgress(100);
            LogStatus.Text = _i18n.T("log.done");
            StatusText.Text = _i18n.T("status.done");
        }
        catch (Exception ex)
        {
            StopProgressTimer();
            LogStatus.Text = _i18n.T("log.failed");
            StatusText.Text = _i18n.T("log.failed");
            MessageBox.Show(ex.Message, _i18n.T("app.title"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _busy = false;
            ApplyButton.IsEnabled = true;
        }
    }

    private void StartNeonText(TextBlock target, double size)
    {
        target.FontSize = size;
        target.Foreground = Brushes.White;

        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(-1, 0.5),
            EndPoint = new Point(0, 0.5),
            MappingMode = BrushMappingMode.RelativeToBoundingBox
        };
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xF5, 0xF3, 0xFF), 0.00));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xC0, 0x84, 0xFC), 0.25));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x22, 0xF0, 0xFF), 0.50));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xA7, 0x8B, 0xFA), 0.75));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xF5, 0xF3, 0xFF), 1.00));
        target.Foreground = brush;
        target.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Color.FromRgb(0xA7, 0x8B, 0xFA),
            BlurRadius = 16,
            ShadowDepth = 0,
            Opacity = 0.85
        };

        var start = new PointAnimation
        {
            From = new Point(-1, 0.5),
            To = new Point(1, 0.5),
            Duration = TimeSpan.FromSeconds(2.4),
            RepeatBehavior = RepeatBehavior.Forever
        };
        var end = new PointAnimation
        {
            From = new Point(0, 0.5),
            To = new Point(2, 0.5),
            Duration = TimeSpan.FromSeconds(2.4),
            RepeatBehavior = RepeatBehavior.Forever
        };
        brush.BeginAnimation(LinearGradientBrush.StartPointProperty, start);
        brush.BeginAnimation(LinearGradientBrush.EndPointProperty, end);
    }

    private void ResetLogUi()
    {
        StopProgressTimer();
        _displayPercent = 0;
        _targetPercent = 0;
        LogStatus.Text = _i18n.T("log.empty");
        LogPercent.Text = "";
        LogProgress.Value = 0;
    }

    private void BeginLogProgress()
    {
        _displayPercent = 0;
        _targetPercent = 4;
        LogStatus.Text = _i18n.T("log.working");
        UpdateLogProgress();
        _progressTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) };
        _progressTimer.Tick -= ProgressTick;
        _progressTimer.Tick += ProgressTick;
        _progressTimer.Start();
    }

    private void AimProgress(double from, double to)
    {
        _displayPercent = Math.Max(_displayPercent, from);
        _targetPercent = Math.Max(to, _displayPercent);
        UpdateLogProgress();
    }

    private void SetProgress(double value)
    {
        _displayPercent = value;
        _targetPercent = value;
        UpdateLogProgress();
    }

    private void ProgressTick(object? sender, EventArgs e)
    {
        if (_displayPercent >= _targetPercent)
        {
            return;
        }

        _displayPercent = Math.Min(_targetPercent, _displayPercent + 0.8);
        UpdateLogProgress();
    }

    private void UpdateLogProgress()
    {
        var value = Math.Clamp(_displayPercent, 0, 100);
        LogPercent.Text = $"{value:0}%";
        LogProgress.Value = value;
        if (_busy)
        {
            LogStatus.Text = _i18n.T("log.working");
        }
    }

    private void StopProgressTimer()
    {
        _progressTimer?.Stop();
    }

    private async Task ExportScreenshotsAsync(string folder)
    {
        Directory.CreateDirectory(folder);
        await Task.Delay(400);
        await CapturePageAsync("debloat", System.IO.Path.Combine(folder, "01-debloat.png"));
        await CapturePageAsync("tweaks", System.IO.Path.Combine(folder, "02-tweaks.png"));
        await CapturePageAsync("gpu", System.IO.Path.Combine(folder, "03-gpu.png"));

        ShowPage("log");
        NavLog.IsChecked = true;
        LogStatus.Text = _i18n.T("log.working");
        LogPercent.Text = "64%";
        LogProgress.Value = 64;
        await Task.Delay(250);
        SaveWindowPng(System.IO.Path.Combine(folder, "04-log.png"));

        await CapturePageAsync("about", System.IO.Path.Combine(folder, "05-about.png"));
        Close();
    }

    private async Task CapturePageAsync(string page, string path)
    {
        ShowPage(page);
        if (page == "debloat") NavDebloat.IsChecked = true;
        else if (page == "tweaks") NavTweaks.IsChecked = true;
        else if (page == "gpu") NavGpu.IsChecked = true;
        else if (page == "about") NavAbout.IsChecked = true;
        UpdateLayout();
        await Task.Delay(250);
        SaveWindowPng(path);
    }

    private void SaveWindowPng(string path)
    {
        UpdateLayout();
        var width = (int)Math.Round(double.IsNaN(ActualWidth) || ActualWidth < 100 ? Width : ActualWidth);
        var height = (int)Math.Round(double.IsNaN(ActualHeight) || ActualHeight < 100 ? Height : ActualHeight);
        width = Math.Max(width, 980);
        height = Math.Max(height, 640);
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(this);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private void BuildAboutPage()
    {
        var card = new Border
        {
            Background = (Brush)FindResource("CardBrush"),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(22),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var stack = new StackPanel();
        var logo = new Image
        {
            Source = LogoImage.Source,
            Width = 72,
            Height = 72,
            HorizontalAlignment = HorizontalAlignment.Left,
            Stretch = Stretch.Uniform
        };
        RenderOptions.SetBitmapScalingMode(logo, BitmapScalingMode.HighQuality);
        stack.Children.Add(logo);
        stack.Children.Add(new TextBlock
        {
            Text = _i18n.T("app.title"),
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("TextBrush"),
            Margin = new Thickness(0, 14, 0, 0)
        });
        stack.Children.Add(new TextBlock
        {
            Text = _i18n.T("about.tagline"),
            Foreground = (Brush)FindResource("AccentSoftBrush"),
            Margin = new Thickness(0, 4, 0, 0)
        });
        stack.Children.Add(new TextBlock
        {
            Text = _i18n.T("about.version"),
            Foreground = (Brush)FindResource("MutedBrush"),
            Margin = new Thickness(0, 4, 0, 4)
        });
        card.Child = stack;
        PageHost.Children.Add(card);

        PageHost.Children.Add(CreateLinkCard(
            _i18n.T("about.website"),
            "yigittaner.com",
            "https://yigittaner.com",
            CreateSegoeIcon("\uE774")));
        PageHost.Children.Add(CreateLinkCard(
            _i18n.T("about.github"),
            "github.com/taneryigitxl",
            "https://github.com/taneryigitxl",
            CreatePathIcon("M12 0C5.37 0 0 5.37 0 12c0 5.3 3.44 9.8 8.21 11.39.6.11.82-.26.82-.58 0-.28-.01-1.04-.02-2.04-3.34.73-4.04-1.61-4.04-1.61-.55-1.39-1.33-1.76-1.33-1.76-1.09-.74.08-.73.08-.73 1.2.08 1.84 1.23 1.84 1.23 1.07 1.84 2.81 1.3 3.5 1 .11-.78.42-1.31.76-1.61-2.67-.3-5.47-1.33-5.47-5.93 0-1.31.47-2.38 1.24-3.22-.12-.3-.54-1.52.12-3.18 0 0 1.01-.32 3.3 1.23a11.5 11.5 0 0 1 6 0c2.29-1.55 3.3-1.23 3.3-1.23.66 1.66.24 2.88.12 3.18.77.84 1.24 1.91 1.24 3.22 0 4.61-2.81 5.63-5.49 5.93.43.37.81 1.1.81 2.22 0 1.61-.01 2.91-.01 3.31 0 .32.22.69.83.57C20.56 21.8 24 17.3 24 12 24 5.37 18.63 0 12 0z")));
        PageHost.Children.Add(CreateLinkCard(
            _i18n.T("about.linkedin"),
            "linkedin.com/in/taneryigit",
            "https://www.linkedin.com/in/taneryigit/",
            CreatePathIcon("M20.45 20.45h-3.56v-5.57c0-1.33-.03-3.04-1.85-3.04-1.85 0-2.14 1.45-2.14 2.94v5.67H9.35V9h3.41v1.56h.05c.48-.9 1.64-1.85 3.37-1.85 3.6 0 4.27 2.37 4.27 5.46v6.28zM5.34 7.43a2.06 2.06 0 1 1 0-4.12 2.06 2.06 0 0 1 0 4.12zM7.12 20.45H3.56V9h3.56v11.45zM22.23 0H1.77C.79 0 0 .77 0 1.73v20.54C0 23.23.79 24 1.77 24h20.46c.98 0 1.77-.77 1.77-1.73V1.73C24 .77 23.21 0 22.23 0z")));
    }

    private UIElement CreateLinkCard(string title, string subtitle, string url, UIElement icon)
    {
        var box = new Border
        {
            Background = (Brush)FindResource("CardBrush"),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 10),
            Cursor = Cursors.Hand
        };
        var row = new DockPanel();
        var badge = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(10),
            Background = (Brush)FindResource("InputBrush"),
            Margin = new Thickness(0, 0, 12, 0),
            Child = icon
        };
        DockPanel.SetDock(badge, Dock.Left);
        var open = new TextBlock
        {
            Text = _i18n.T("about.open"),
            Foreground = (Brush)FindResource("AccentSoftBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };
        DockPanel.SetDock(open, Dock.Right);
        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold });
        texts.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = (Brush)FindResource("MutedBrush"),
            Margin = new Thickness(0, 2, 0, 0)
        });
        row.Children.Add(badge);
        row.Children.Add(open);
        row.Children.Add(texts);
        box.Child = row;
        box.MouseLeftButtonUp += (_, _) => OpenUrl(url);
        return box;
    }

    private UIElement CreateSectionHeader(string title, string? icon)
    {
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };
        if (!string.IsNullOrWhiteSpace(icon))
        {
            header.Children.Add(new TextBlock
            {
                Text = icon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                Foreground = (Brush)FindResource("GlyphBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });
        }

        header.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextBrush"),
            VerticalAlignment = VerticalAlignment.Center
        });
        return header;
    }

    private UIElement CreateSegoeIcon(string glyph)
    {
        return new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 18,
            Foreground = (Brush)FindResource("GlyphBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private UIElement CreatePathIcon(string data)
    {
        return new Viewbox
        {
            Width = 18,
            Height = 18,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(data),
                Fill = (Brush)FindResource("GlyphBrush")
            }
        };
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
