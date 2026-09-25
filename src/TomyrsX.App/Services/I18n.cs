using System.ComponentModel;

namespace TomyrsX.Services;

public enum AppLanguage
{
    Turkish,
    English
}

public sealed class I18n : INotifyPropertyChanged
{
    public static I18n Current { get; } = new();

    private AppLanguage _language = AppLanguage.Turkish;
    public AppLanguage Language
    {
        get => _language;
        set
        {
            if (_language == value)
            {
                return;
            }

            _language = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }

    public bool IsTurkish => Language == AppLanguage.Turkish;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => T(key);

    public string T(string key)
    {
        var table = Language == AppLanguage.Turkish ? Tr : En;
        if (table.TryGetValue(key, out var value))
        {
            return value;
        }

        if (En.TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    public string FeatureTitle(string featureId, string action, string label)
    {
        if (Language == AppLanguage.Turkish && Tr.TryGetValue($"feat.{featureId}", out var tr))
        {
            return tr;
        }

        var prefix = action switch
        {
            "Disable" => Language == AppLanguage.Turkish ? "Kapat: " : "Disable ",
            "Enable" => Language == AppLanguage.Turkish ? "Aç: " : "Enable ",
            "Hide" => Language == AppLanguage.Turkish ? "Gizle: " : "Hide ",
            "Show" => Language == AppLanguage.Turkish ? "Göster: " : "Show ",
            _ => string.IsNullOrWhiteSpace(action) ? "" : action + " "
        };

        return prefix + label;
    }

    public string CategoryName(string category)
    {
        return T($"cat.{category}");
    }

    private static readonly Dictionary<string, string> Tr = new(StringComparer.OrdinalIgnoreCase)
    {
        ["app.title"] = "Tomyrs X",
        ["app.subtitle"] = "Bilgisayar hızlandırma",
        ["nav.debloat"] = "Debloat",
        ["nav.tweaks"] = "Tweakler",
        ["nav.gpu"] = "Ekran kartı",
        ["nav.log"] = "Günlük",
        ["nav.about"] = "Hakkında",
        ["action.apply"] = "Seçilenleri uygula",
        ["action.defaults"] = "Win11Debloat varsayılanları",
        ["action.resetPage"] = "Önerilenlere dön",
        ["action.selectAll"] = "Tümünü seç",
        ["action.clear"] = "Temizle",
        ["action.restorePoint"] = "Sistem geri yükleme noktası oluştur",
        ["lang.tr"] = "TR",
        ["lang.en"] = "EN",
        ["status.ready"] = "Hazır. Seçenekleri işaretleyip uygula.",
        ["status.running"] = "İşleminiz yapılıyor",
        ["status.done"] = "Bitti",
        ["status.cancelled"] = "İşlem iptal edildi.",
        ["confirm.title"] = "Değişiklikleri uygula",
        ["confirm.body"] = "Seçilen ayarlar bu Windows kurulumuna uygulanacak. Devam etmek istiyor musun?",
        ["confirm.danger"] = "Windows Defender kapatılacak. Bu, cihazın güvenlik korumasını azaltır. Emin misin?",
        ["confirm.edge"] = "Microsoft Edge zorla kaldırılacak. Bazı Windows özellikleri etkilenebilir. Emin misin?",
        ["missing.script"] = "Tweak dosyası bulunamadı: {0}",
        ["confirm.yes"] = "Uygula",
        ["confirm.no"] = "Vazgeç",
        ["empty"] = "Seçili bir seçenek yok.",
        ["log.empty"] = "Henüz bir işlem yok.",
        ["page.log.hint"] = "Bu sayfada yalnızca işlem durumu görünür.",
        ["log.working"] = "İşleminiz yapılıyor",
        ["log.done"] = "Bitti",
        ["log.failed"] = "İşlem tamamlanamadı",
        ["group.none"] = "Değiştirme",
        ["admin.ok"] = "Yönetici olarak çalışıyor",
        ["admin.missing"] = "Yönetici izni gerekli",
        ["page.debloat.hint"] = "Win11Debloat seçenekleri. İşaretlediklerin PowerShell ile sessiz uygulanır.",
        ["page.tweaks.hint"] = "Sırasıyla tweakler. ISLC hariç tutuldu. Her satır orijinal scriptin önerilen ayarını uygular.",
        ["page.gpu.hint"] = "Kendi ekran kartın için birini seç. AMD, Intel ve NVIDIA scriptleri ayrıdır.",
        ["page.about.hint"] = "Tomyrs X hakkında ve proje bilgileri.",
        ["about.tagline"] = "Windows hızlandırma ve debloat",
        ["about.desc"] = "Win11Debloat seçeneklerini ve sıralı tweakleri modern bir arayüzde toplar. ISLC dahil değildir.",
        ["about.version"] = "Sürüm 1.0.0",
        ["about.credits"] = "Açık kaynak Win11Debloat motorunu kullanır. Lisans bilgileri uygulama paketindedir.",
        ["about.website"] = "Web sitesi",
        ["about.github"] = "GitHub",
        ["about.linkedin"] = "LinkedIn",
        ["about.open"] = "Aç",
        ["apps.title"] = "Uygulama kaldırma",
        ["feat.RemoveApps"] = "Varsayılan bloatware listesini kaldır",
        ["feat.RemoveCommApps"] = "Mail, Takvim ve Kişiler uygulamalarını kaldır",
        ["feat.RemoveW11Outlook"] = "Yeni Outlook uygulamasını kaldır",
        ["feat.RemoveGamingApps"] = "Xbox uygulaması ve Game Bar'ı kaldır",
        ["feat.RemoveHPApps"] = "HP OEM uygulamalarını kaldır",
        ["feat.ForceRemoveEdge"] = "Microsoft Edge'i zorla kaldır",
        ["feat.DisableTelemetry"] = "Telemetri, izleme ve hedefli reklamları kapat",
        ["feat.DisableSuggestions"] = "İpuçları, öneriler ve önerilen içeriği kapat",
        ["feat.DisableLocationServices"] = "Konum servislerini kapat",
        ["feat.DisableFindMyDevice"] = "Cihazımı Bul takibini kapat",
        ["feat.DisableLockscreenTips"] = "Kilit ekranı ipuçlarını kapat",
        ["feat.DisableDesktopSpotlight"] = "Masaüstü Windows Spotlight'ı kapat",
        ["feat.DisableEdgeAds"] = "Edge reklam, öneri ve haber akışını kapat",
        ["feat.DisableCopilot"] = "Microsoft Copilot'u kapat",
        ["feat.DisableRecall"] = "Windows Recall'ı kapat",
        ["feat.DisableClickToDo"] = "Click to Do yapay zekasını kapat",
        ["feat.DisableAISvcAutoStart"] = "Yapay zeka servisinin otomatik başlamasını kapat",
        ["feat.DisableDVR"] = "Oyun/ekran kaydını kapat",
        ["feat.DisableGameBarIntegration"] = "Xbox Game Bar entegrasyonunu kapat",
        ["feat.DisableStartRecommended"] = "Başlat menüsünde önerilenler bölümünü gizle",
        ["feat.DisableStartAllApps"] = "Başlat menüsünde Tüm uygulamalar bölümünü gizle",
        ["feat.DisableStartPhoneLink"] = "Başlat menüsünde Telefon Bağlantısı'nı kapat",
        ["feat.DisableBing"] = "Aramada Bing ve Copilot entegrasyonunu kapat",
        ["feat.DisableStoreSearchSuggestions"] = "Aramada Store önerilerini kapat",
        ["feat.DisableSearchHighlights"] = "Görev çubuğu arama vurgularını kapat",
        ["feat.DisableSearchHistory"] = "Yerel arama geçmişini kapat",
        ["feat.DisableSettings365Ads"] = "Ayarlar ana sayfasındaki Microsoft 365 reklamlarını gizle",
        ["feat.DisableSettingsHome"] = "Ayarlar ana sayfasını gizle",
        ["feat.DisableEdgeAI"] = "Edge yapay zeka özelliklerini kapat",
        ["feat.DisablePaintAI"] = "Paint yapay zeka özelliklerini kapat",
        ["feat.DisableNotepadAI"] = "Not Defteri yapay zeka özelliklerini kapat",
        ["feat.EnableDarkMode"] = "Koyu temayı aç",
        ["feat.DisableDragTray"] = "Sürükle-bırak paylaşım tepsisini kapat",
        ["feat.RevertContextMenu"] = "Eski Windows 10 sağ tık menüsünü geri getir",
        ["feat.DisableMouseAcceleration"] = "Fare ivmesini kapat",
        ["feat.DisableStickyKeys"] = "Yapışkan tuşlar kısayolunu kapat",
        ["feat.DisableWindowSnapping"] = "Pencere yapıştırmayı kapat",
        ["feat.DisableSnapAssist"] = "Yapıştırma önerilerini kapat",
        ["feat.DisableSnapLayouts"] = "Yapıştırma düzenlerini kapat",
        ["feat.TaskbarAlignLeft"] = "Görev çubuğu simgelerini sola hizala",
        ["feat.HideTaskview"] = "Görev görünümü düğmesini gizle",
        ["feat.DisableWidgets"] = "Görev çubuğu widget'larını kapat",
        ["feat.HideChat"] = "Sohbet / Meet Now simgesini gizle",
        ["feat.DisableStorageSense"] = "Depolama Algılama'yı kapat",
        ["feat.DisableFastStartup"] = "Hızlı başlatmayı kapat",
        ["feat.DisableBitlockerAutoEncryption"] = "BitLocker otomatik şifrelemeyi kapat",
        ["feat.DisableModernStandbyNetworking"] = "Modern Standby ağını kapat",
        ["feat.EnableEndTask"] = "Görev çubuğunda Görevi sonlandır seçeneğini aç",
        ["feat.EnableLastActiveClick"] = "Görev çubuğunda son aktif pencereye tıklamayı aç",
        ["feat.ShowKnownFileExt"] = "Bilinen dosya uzantılarını göster",
        ["feat.ShowHiddenFolders"] = "Gizli dosya ve klasörleri göster",
        ["feat.HideHome"] = "Dosya Gezgini'nde Giriş'i gizle",
        ["feat.HideGallery"] = "Dosya Gezgini'nde Galeri'yi gizle",
        ["feat.HideDupliDrive"] = "Yinelenen çıkarılabilir sürücüleri gizle",
        ["feat.AddFoldersToThisPC"] = "Bu PC'ye ortak klasörleri ekle",
        ["feat.DisableTransparency"] = "Saydamlık efektlerini kapat",
        ["feat.DisableAnimations"] = "Animasyonları kapat",
        ["feat.DisableUpdateASAP"] = "Güncellemeleri hemen indirmeyi engelle",
        ["feat.PreventUpdateAutoReboot"] = "Oturum açıkken otomatik yeniden başlatmayı engelle",
        ["feat.DisableDeliveryOptimization"] = "Teslimat Optimizasyonu paylaşımını kapat",
        ["feat.HideIncludeInLibrary"] = "Kitaplığa dahil et seçeneğini gizle",
        ["feat.HideGiveAccessTo"] = "Erişim ver seçeneğini gizle",
        ["feat.HideShare"] = "Paylaş seçeneğini gizle",
        ["feat.HideOnedrive"] = "OneDrive klasörünü gizle",
        ["feat.Hide3dObjects"] = "3B nesneler klasörünü gizle",
        ["feat.HideMusic"] = "Müzik klasörünü gizle",
        ["feat.DisableBraveBloat"] = "Brave tarayıcı şişirmesini kapat",
        ["feat.EnableWindowsSandbox"] = "Windows Sandbox'ı aç",
        ["feat.EnableWindowsSubsystemForLinux"] = "Windows Subsystem for Linux'u aç",
        ["cat.Privacy & Suggested Content"] = "Gizlilik ve önerilen içerik",
        ["cat.System"] = "Sistem",
        ["cat.Start Menu & Search"] = "Başlat menüsü ve arama",
        ["cat.AI"] = "Yapay zeka",
        ["cat.Windows Update"] = "Windows Update",
        ["cat.Taskbar"] = "Görev çubuğu",
        ["cat.Appearance"] = "Görünüm",
        ["cat.File Explorer"] = "Dosya Gezgini",
        ["cat.Gaming"] = "Oyun",
        ["cat.Multi-tasking"] = "Çoklu görev",
        ["cat.Optional Windows Features"] = "İsteğe bağlı Windows özellikleri",
        ["cat.Other"] = "Diğer",
        ["cat.Apps"] = "Uygulama kaldırma",
        ["group.SearchIcon"] = "Görev çubuğu arama stili",
        ["group.MultiMon"] = "Görev çubuğu uygulamalarını göster",
        ["group.CombineButtons"] = "Ana ekranda görev çubuğu düğmelerini birleştir",
        ["group.CombineMMButtons"] = "İkincil ekranlarda görev çubuğu düğmelerini birleştir",
        ["group.ClearStart"] = "Başlat menüsündeki sabitlenmiş uygulamaları kaldır",
        ["group.ExplorerLocation"] = "Dosya Gezgini şurada açılsın",
        ["group.ShowTabsInAltTab"] = "Alt+Tab ve yapıştırmada sekmeleri göster",
        ["gval.Hide"] = "Gizle",
        ["gval.Show search icon only"] = "Yalnızca arama simgesi",
        ["gval.Show search icon and label"] = "Simge ve etiket",
        ["gval.Show search box"] = "Arama kutusu",
        ["gval.All taskbars"] = "Tüm görev çubukları",
        ["gval.Main taskbar and taskbar where window is open"] = "Ana çubuk ve pencerenin açık olduğu çubuk",
        ["gval.Taskbar where window is open"] = "Pencerenin açık olduğu çubuk",
        ["gval.Always"] = "Her zaman",
        ["gval.When taskbar is full"] = "Görev çubuğu dolunca",
        ["gval.Never"] = "Asla",
        ["gval.Remove for the selected user"] = "Seçili kullanıcı için kaldır",
        ["gval.Remove for all users"] = "Tüm kullanıcılar için kaldır",
        ["gval.Home"] = "Giriş",
        ["gval.This PC"] = "Bu PC",
        ["gval.Downloads"] = "İndirilenler",
        ["gval.OneDrive"] = "OneDrive",
        ["gval.Don't show tabs"] = "Sekmeleri gösterme",
        ["gval.Show 3 most recent tabs"] = "Son 3 sekme",
        ["gval.Show 5 most recent tabs"] = "Son 5 sekme",
        ["gval.Show 20 most recent tabs"] = "Son 20 sekme",
        ["tweak.bg-apps"] = "Arka plan uygulamalarını kapat",
        ["tweak.bg-apps.desc"] = "Uygulamaların arka planda çalışmasını kısıtlar.",
        ["tweak.poll-cap"] = "Arka plan yoklama tavanını kapat",
        ["tweak.poll-cap.desc"] = "USB cihazları için 125 Hz tavanını kaldırır.",
        ["tweak.msi"] = "MSI modunu aç",
        ["tweak.msi.desc"] = "Desteklenen cihazlarda Message Signaled Interrupts'ı açar.",
        ["tweak.directx"] = "DirectX çalışma zamanını kur",
        ["tweak.directx.desc"] = "Eksik DirectX paketini indirir ve kurar. İnternet gerekir.",
        ["tweak.cpp"] = "Visual C++ paketlerini kur",
        ["tweak.cpp.desc"] = "Oyun ve uygulama için gerekli C++ redistributable'ları kurar. İnternet gerekir.",
        ["tweak.start-taskbar"] = "Başlat menüsü ve görev çubuğunu sadeleştir",
        ["tweak.start-taskbar.desc"] = "Gereksiz simgeleri temizler, sade bir menü bırakır.",
        ["tweak.copilot"] = "Copilot'u kapat",
        ["tweak.copilot.desc"] = "Copilot uygulamasını kaldırır ve politikayla kapatır.",
        ["tweak.p0"] = "GPU P0 durumunu zorla",
        ["tweak.p0.desc"] = "NVIDIA kartlarda dinamik P-state'i kapatıp en yüksek performansı zorlar.",
        ["tweak.widgets"] = "Widget'ları kapat",
        ["tweak.widgets.desc"] = "Görev çubuğu ve kilit ekranı widget'larını kapatır.",
        ["tweak.gamemode"] = "Oyun modu ayarını aç",
        ["tweak.gamemode.desc"] = "Windows oyun modu ayar sayfasını açar.",
        ["tweak.gamebar"] = "Game Bar ve Xbox bileşenlerini kapat",
        ["tweak.gamebar.desc"] = "Game Bar'ı kapatır ve ilgili Xbox uygulamalarını kaldırır.",
        ["tweak.pointer"] = "İşaretçi hassasiyeti ayarını aç",
        ["tweak.pointer.desc"] = "Fare özellikleri penceresini açar.",
        ["tweak.power"] = "Yüksek performans güç planını uygula",
        ["tweak.power.desc"] = "Önerilen güç planını kurar ve seçer.",
        ["tweak.lockscreen"] = "Kilit ekranı duvar kağıdını siyah yap",
        ["tweak.lockscreen.desc"] = "Oturum kapatma ve kilit ekranını siyah yapar.",
        ["tweak.theme"] = "Siyah tema uygula",
        ["tweak.theme.desc"] = "Windows ve uygulamalar için koyu temayı açar.",
        ["tweak.edge"] = "Edge ve WebView'ı kaldır",
        ["tweak.edge.desc"] = "Microsoft Edge ve WebView kurulumunu kaldırmayı dener.",
        ["tweak.bloatware"] = "Tüm bloatware'i kaldır",
        ["tweak.bloatware.desc"] = "Güvenli tutulanlar dışında UWP uygulamaları ve özellikleri kaldırır.",
        ["tweak.defender"] = "Windows Defender'ı kapat",
        ["tweak.defender.desc"] = "Uyarı: Windows güvenlik korumasını kapatır. Sadece kendi bilgisayarında, riski bilerek kullan.",
        ["tweak.gpu-amd"] = "AMD ekran kartı ayarları",
        ["tweak.gpu-amd.desc"] = "AMD için önerilen performans kayıt defteri ayarlarını uygular.",
        ["tweak.gpu-intel"] = "Intel ekran kartı ayarları",
        ["tweak.gpu-intel.desc"] = "Intel için önerilen performans kayıt defteri ayarlarını uygular.",
        ["tweak.gpu-nvidia"] = "NVIDIA ekran kartı ayarları",
        ["tweak.gpu-nvidia.desc"] = "NVIDIA için önerilen performans kayıt defteri ayarlarını uygular.",
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.OrdinalIgnoreCase)
    {
        ["app.title"] = "Tomyrs X",
        ["app.subtitle"] = "PC speed-up",
        ["nav.debloat"] = "Debloat",
        ["nav.tweaks"] = "Tweaks",
        ["nav.gpu"] = "Graphics",
        ["nav.log"] = "Log",
        ["nav.about"] = "About",
        ["action.apply"] = "Apply selected",
        ["action.defaults"] = "Win11Debloat defaults",
        ["action.resetPage"] = "Reset to recommended",
        ["action.selectAll"] = "Select all",
        ["action.clear"] = "Clear",
        ["action.restorePoint"] = "Create a system restore point",
        ["lang.tr"] = "TR",
        ["lang.en"] = "EN",
        ["status.ready"] = "Ready. Check options, then apply.",
        ["status.running"] = "Your operation is running",
        ["status.done"] = "Done",
        ["status.cancelled"] = "Cancelled.",
        ["confirm.title"] = "Apply changes",
        ["confirm.body"] = "The selected settings will be applied to this Windows install. Continue?",
        ["confirm.danger"] = "Windows Defender will be turned off. This reduces security protection. Are you sure?",
        ["confirm.edge"] = "Microsoft Edge will be force-removed. Some Windows features may break. Are you sure?",
        ["missing.script"] = "Tweak file not found: {0}",
        ["confirm.yes"] = "Apply",
        ["confirm.no"] = "Cancel",
        ["empty"] = "Nothing is selected.",
        ["log.empty"] = "No activity yet.",
        ["page.log.hint"] = "This page only shows operation progress.",
        ["log.working"] = "Your operation is running",
        ["log.done"] = "Done",
        ["log.failed"] = "The operation could not finish",
        ["group.none"] = "Leave unchanged",
        ["admin.ok"] = "Running as administrator",
        ["admin.missing"] = "Administrator permission required",
        ["page.debloat.hint"] = "Win11Debloat options. Checked items are applied silently through PowerShell.",
        ["page.tweaks.hint"] = "Sequential tweaks. ISLC is excluded. Each row applies the original script's recommended setting.",
        ["page.gpu.hint"] = "Pick the script that matches your GPU. AMD, Intel, and NVIDIA are separate.",
        ["page.about.hint"] = "About Tomyrs X and project information.",
        ["about.tagline"] = "Windows speed-up and debloat",
        ["about.desc"] = "Puts Win11Debloat options and sequential tweaks in one modern interface. ISLC is not included.",
        ["about.version"] = "Version 1.0.0",
        ["about.credits"] = "Powered by the open-source Win11Debloat engine. License details are included with the app.",
        ["about.website"] = "Website",
        ["about.github"] = "GitHub",
        ["about.linkedin"] = "LinkedIn",
        ["about.open"] = "Open",
        ["apps.title"] = "App removal",
        ["feat.RemoveApps"] = "Remove the default bloatware list",
        ["feat.RemoveCommApps"] = "Remove Mail, Calendar, and People",
        ["feat.RemoveW11Outlook"] = "Remove the new Outlook app",
        ["feat.RemoveGamingApps"] = "Remove the Xbox app and Game Bar",
        ["feat.RemoveHPApps"] = "Remove HP OEM apps",
        ["feat.ForceRemoveEdge"] = "Force-remove Microsoft Edge",
        ["cat.Privacy & Suggested Content"] = "Privacy & suggested content",
        ["cat.System"] = "System",
        ["cat.Start Menu & Search"] = "Start menu & search",
        ["cat.AI"] = "AI",
        ["cat.Windows Update"] = "Windows Update",
        ["cat.Taskbar"] = "Taskbar",
        ["cat.Appearance"] = "Appearance",
        ["cat.File Explorer"] = "File Explorer",
        ["cat.Gaming"] = "Gaming",
        ["cat.Multi-tasking"] = "Multi-tasking",
        ["cat.Optional Windows Features"] = "Optional Windows features",
        ["cat.Other"] = "Other",
        ["cat.Apps"] = "App removal",
        ["group.SearchIcon"] = "Taskbar search style",
        ["group.MultiMon"] = "Show taskbar apps on",
        ["group.CombineButtons"] = "Combine taskbar buttons on the main display",
        ["group.CombineMMButtons"] = "Combine taskbar buttons on secondary displays",
        ["group.ClearStart"] = "Remove pinned apps from the start menu",
        ["group.ExplorerLocation"] = "Open File Explorer to",
        ["group.ShowTabsInAltTab"] = "Show tabs when snapping or pressing Alt+Tab",
        ["tweak.bg-apps"] = "Turn off background apps",
        ["tweak.bg-apps.desc"] = "Stops apps from running in the background.",
        ["tweak.poll-cap"] = "Remove the background polling-rate cap",
        ["tweak.poll-cap.desc"] = "Removes the 125 Hz USB polling cap.",
        ["tweak.msi"] = "Enable MSI mode",
        ["tweak.msi.desc"] = "Turns on Message Signaled Interrupts for supported devices.",
        ["tweak.directx"] = "Install the DirectX runtime",
        ["tweak.directx.desc"] = "Downloads and installs missing DirectX packages. Needs internet.",
        ["tweak.cpp"] = "Install Visual C++ packages",
        ["tweak.cpp.desc"] = "Installs C++ redistributables used by games and apps. Needs internet.",
        ["tweak.start-taskbar"] = "Clean the Start menu and taskbar",
        ["tweak.start-taskbar.desc"] = "Removes extra icons and leaves a simpler Start menu.",
        ["tweak.copilot"] = "Turn off Copilot",
        ["tweak.copilot.desc"] = "Uninstalls Copilot and disables it by policy.",
        ["tweak.p0"] = "Force GPU P0 state",
        ["tweak.p0.desc"] = "Disables dynamic P-state on NVIDIA cards to keep max performance.",
        ["tweak.widgets"] = "Turn off widgets",
        ["tweak.widgets.desc"] = "Disables taskbar and lock-screen widgets.",
        ["tweak.gamemode"] = "Open Game Mode settings",
        ["tweak.gamemode.desc"] = "Opens the Windows Game Mode settings page.",
        ["tweak.gamebar"] = "Turn off Game Bar and Xbox extras",
        ["tweak.gamebar.desc"] = "Disables Game Bar and removes related Xbox apps.",
        ["tweak.pointer"] = "Open pointer precision settings",
        ["tweak.pointer.desc"] = "Opens the mouse properties window.",
        ["tweak.power"] = "Apply the high-performance power plan",
        ["tweak.power.desc"] = "Installs and selects the recommended power plan.",
        ["tweak.lockscreen"] = "Set the lock screen wallpaper to black",
        ["tweak.lockscreen.desc"] = "Makes the sign-out and lock screens black.",
        ["tweak.theme"] = "Apply the black theme",
        ["tweak.theme.desc"] = "Turns on dark theme for Windows and apps.",
        ["tweak.edge"] = "Uninstall Edge and WebView",
        ["tweak.edge.desc"] = "Tries to remove Microsoft Edge and WebView.",
        ["tweak.bloatware"] = "Remove all bloatware",
        ["tweak.bloatware.desc"] = "Removes UWP apps and features except a small keep-list.",
        ["tweak.defender"] = "Turn off Windows Defender",
        ["tweak.defender.desc"] = "Warning: turns off Windows security protection. Use only on your own PC, knowing the risk.",
        ["tweak.gpu-amd"] = "AMD graphics settings",
        ["tweak.gpu-amd.desc"] = "Applies the recommended AMD performance registry settings.",
        ["tweak.gpu-intel"] = "Intel graphics settings",
        ["tweak.gpu-intel.desc"] = "Applies the recommended Intel performance registry settings.",
        ["tweak.gpu-nvidia"] = "NVIDIA graphics settings",
        ["tweak.gpu-nvidia.desc"] = "Applies the recommended NVIDIA performance registry settings.",
    };
}
