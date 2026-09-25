using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TomyrsX.Setup;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/app.ico"));
        LogoImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/logo.png"));
        if (Environment.GetCommandLineArgs().Any(a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase)))
        {
            Loaded += (_, _) => Install_Click(this, new RoutedEventArgs());
        }
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        InstallButton.IsEnabled = false;
        try
        {
            StatusText.Text = "Dosyalar hazırlanıyor...";
            Bar.Value = 10;
            var appDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Tomyrs X");
            if (Directory.Exists(appDir))
            {
                Directory.Delete(appDir, true);
            }

            Directory.CreateDirectory(appDir);
            StatusText.Text = "Tomyrs X kopyalanıyor...";
            Bar.Value = 35;
            await Task.Run(() => ExtractPayload(appDir));

            var exePath = Path.Combine(appDir, "TomyrsX.exe");
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException("Kurulum paketinde TomyrsX.exe yok.");
            }

            StatusText.Text = "Masaüstü kısayolu oluşturuluyor...";
            Bar.Value = 80;
            CreateDesktopShortcut(exePath, appDir);

            Bar.Value = 100;
            StatusText.Text = "Kuruldu. Masaüstündeki Tomyrs X ile aç.";
            MessageBox.Show(
                "Kurulum bitti. Masaüstünde Tomyrs X kısayolu var.",
                "Tomyrs X",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            MessageBox.Show(ex.Message, "Tomyrs X Kurulum", MessageBoxButton.OK, MessageBoxImage.Error);
            InstallButton.IsEnabled = true;
        }
    }

    private static void ExtractPayload(string appDir)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("Payload.zip", StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            throw new InvalidOperationException("Kurulum paketi eksik. TomyrsX-Setup yeniden oluşturulmalı.");
        }

        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Kurulum paketi okunamadı.");
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(appDir, overwriteFiles: true);
    }

    private static void CreateDesktopShortcut(string exePath, string workingDirectory)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var linkPath = Path.Combine(desktop, "Tomyrs X.lnk");
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Kısayol oluşturulamadı.");
        var shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Kısayol oluşturulamadı.");
        var shortcut = shellType.InvokeMember(
            "CreateShortcut",
            BindingFlags.InvokeMethod,
            null,
            shell,
            [linkPath]) ?? throw new InvalidOperationException("Kısayol oluşturulamadı.");
        var shortcutType = shortcut.GetType();
        shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, [exePath]);
        shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, [workingDirectory]);
        shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, [exePath]);
        shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, ["Tomyrs X"]);
        shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
    }
}
