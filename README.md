# Tomyrs X

Tomyrs X, Windows 11 için modern arayüzlü bir **hızlandırma, debloat ve tweak** uygulamasıdır. Türkçe ve İngilizce arayüz sunar.

## İndir

En güncel kurulum dosyası: **[TomyrsX-Setup.exe](https://github.com/saltatrix-hub/tomyrs-x/releases/latest/download/TomyrsX-Setup.exe)**

Kurulum dosyasını çalıştır, **Kur** düğmesine bas ve masaüstündeki Tomyrs X kısayolunu yönetici olarak aç.

<p align="center">
  <img src="assets/logo.png" alt="Tomyrs X" width="160">
</p>

## Ekran görüntüleri

### Debloat

![Tomyrs X Debloat](docs/screenshots/01-debloat.png)

| Tweakler | Ekran kartı |
| --- | --- |
| ![Tomyrs X Tweakler](docs/screenshots/02-tweaks.png) | ![Tomyrs X GPU](docs/screenshots/03-gpu.png) |

| İşlem günlüğü | Hakkında |
| --- | --- |
| ![Tomyrs X Günlük](docs/screenshots/04-log.png) | ![Tomyrs X Hakkında](docs/screenshots/05-about.png) |

## Özellikler

- **Debloat** — telemetri, öneriler, Copilot, widget, görev çubuğu, Dosya Gezgini ve gereksiz UWP uygulamaları
- **Tweakler** — MSI modu, güç planı, Game Bar, arka plan uygulamaları, tema ve çalışma zamanı paketleri
- **Ekran kartı** — AMD, Intel ve NVIDIA için ayrı performans ayarları
- **Güvenlik** — riskli Edge ve Defender işlemlerinde ek onay, isteğe bağlı sistem geri yükleme noktası
- **Çift dil** — Türkçe ve İngilizce

`Seçilenleri uygula` yalnızca açık olan sayfadaki seçimleri çalıştırır.

## Derleme

Gereksinim: Windows ve .NET 8 SDK.

```powershell
dotnet build TomyrsX.sln -c Release
```

Uygulama çıktısı `src/TomyrsX.App/bin/Release/net8.0-windows/` altında oluşur.

Tek dosyalık, .NET çalışma zamanı dahil kurulum paketi üretmek için:

```powershell
.\build-release.ps1
```

Kurulum dosyası `artifacts/TomyrsX-Setup.exe` olarak oluşturulur.

## Kullanım uyarısı

Bazı seçenekler Windows Defender, Microsoft Edge veya sistem bileşenlerini değiştirir. Yalnızca kendi bilgisayarında, etkilerini bilerek kullan ve önce bir geri yükleme noktası oluştur.

## Lisans ve kaynaklar

Bu proje MIT lisanslı [Flyworm](https://github.com/taneryigitxl/flyworm) temel alınarak Tomyrs X markası için yeniden düzenlenmiştir. Dahili Win11Debloat bileşeni Raphire tarafından geliştirilmiştir ve kendi MIT lisansıyla dağıtılır. Ayrıntılar için [LICENSE](LICENSE) ve `vendor/Win11Debloat/LICENSE` dosyalarına bakın.

---

## English

Tomyrs X is a modern Windows 11 speed-up, debloat, and tweak utility with Turkish and English interfaces.

Build with `dotnet build TomyrsX.sln -c Release`. Some options disable Defender or remove Edge; use them only when you understand the impact and create a restore point first.
