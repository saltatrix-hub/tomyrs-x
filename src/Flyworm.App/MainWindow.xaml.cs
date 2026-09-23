using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    public MainWindow()
    {
        InitializeComponent();
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/app.ico"));
        LogoImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/logo.png"));

        _features = CatalogLoader.LoadFeatures(AppPaths.FeaturesJson);
        _defaultFeatureIds = CatalogLoader.LoadDefaultFeatureIds(AppPaths.DefaultSettingsJson);
        _tweaks = CatalogLoader.LoadTweaks(AppPaths.TweakCatalog);

        SeedOptions();
        ApplyLanguage();
        ShowPage("debloat");
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
    }

    private void ShowPage(string page)
    {
        _page = page;
        var showLog = page == "log";
        LogBox.Visibility = showLog ? Visibility.Visible : Visibility.Collapsed;
        PageScroll.Visibility = showLog ? Visibility.Collapsed : Visibility.Visible;
        PageHint.Text = page switch
        {
            "debloat" => _i18n.T("page.debloat.hint"),
            "tweaks" => _i18n.T("page.tweaks.hint"),
            "gpu" => _i18n.T("page.gpu.hint"),
            _ => _i18n.T("log.empty")
        };

        if (showLog)
        {
            if (string.IsNullOrWhiteSpace(LogBox.Text))
            {
                LogBox.Text = _i18n.T("log.empty");
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
        else
        {
            BuildTweakList(_tweaks.Gpu);
        }
    }

    private void BuildDebloatPage()
    {
        AddSection(_i18n.T("cat.Apps"), AppRemovalIds.Select(id => _options[id]));

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

            AddSection(_i18n.CategoryName(category.Name), items, groups);
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

    private void AddSection(string title, IEnumerable<SelectableOption> options, IEnumerable<SelectableGroup>? groups = null)
    {
        var box = new Border
        {
            Background = (Brush)FindResource("CardBrush"),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        });

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
        foreach (var option in _options.Values.Where(o => o.Kind == "feature"))
        {
            option.IsChecked = _defaultFeatureIds.Contains(option.Id) || option.Id == "RemoveApps";
        }

        foreach (var group in _groups)
        {
            group.SelectedIndex = 0;
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

        var selectedFeatures = _options.Values.Where(o => o.Kind == "feature" && o.IsChecked).Select(o => o.Id).ToList();
        foreach (var group in _groups.Where(g => g.SelectedIndex > 0))
        {
            selectedFeatures.Add(group.Values[group.SelectedIndex - 1].FeatureId);
        }

        var selectedScripts = _options.Values
            .Where(o => o.IsChecked && o.Kind is "tweak" or "gpu")
            .ToList();

        if (selectedFeatures.Count == 0 && selectedScripts.Count == 0)
        {
            MessageBox.Show(_i18n.T("empty"), _i18n.T("app.title"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show(_i18n.T("confirm.body"), _i18n.T("confirm.title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
        LogBox.Clear();

        try
        {
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

                AppendLog(_i18n.IsTurkish ? "Win11Debloat çalışıyor..." : "Running Win11Debloat...");
                await _runner.RunPowerShellAsync(AppPaths.Win11DebloatScript, args, AppPaths.Win11Debloat, AppendLog);
            }

            foreach (var script in selectedScripts)
            {
                var path = Path.Combine(AppPaths.Tweaks, script.ScriptFile!.Replace('/', Path.DirectorySeparatorChar));
                AppendLog((_i18n.IsTurkish ? "Tweak çalışıyor: " : "Running tweak: ") + script.Id);
                await _runner.RunPowerShellAsync(
                    AppPaths.TweakRunner,
                    ["-ScriptPath", path, "-Choice", script.Choice ?? "1"],
                    AppPaths.Tweaks,
                    AppendLog);
            }

            StatusText.Text = _i18n.T("status.done");
            AppendLog(_i18n.T("status.done"));
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            AppendLog(ex.ToString());
            MessageBox.Show(ex.Message, _i18n.T("app.title"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _busy = false;
            ApplyButton.IsEnabled = true;
        }
    }

    private void AppendLog(string line)
    {
        Dispatcher.Invoke(() =>
        {
            if (LogBox.Text == _i18n.T("log.empty"))
            {
                LogBox.Clear();
            }

            LogBox.AppendText(line + Environment.NewLine);
            LogBox.ScrollToEnd();
        });
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
