# Flyworm

Windows 11 için mor temalı hızlandırma ve debloat uygulaması. Varsayılan dil Türkçe, İngilizce de var.

Win11Debloat seçeneklerini tek pencerede toplar. Eski sırasıyla tweak scriptleri (ISLC hariç) de aynı uygulamadan seçilip çalıştırılır.

## Çalıştırma

Yönetici olarak:

```powershell
dotnet run --project src/Flyworm.App/Flyworm.App.csproj
```

Tek dosya yayın:

```powershell
dotnet publish src/Flyworm.App/Flyworm.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

`publish\Flyworm.exe` yönetici olarak çalışır.

## Ne var

- Debloat: [Win11Debloat](https://github.com/Raphire/Win11Debloat) özellikleri
- Tweakler: arka plan uygulamaları, MSI, Copilot, widget, Game Bar, güç planı, tema, bloatware ve diğerleri
- Ekran kartı: AMD / Intel / NVIDIA
- ISLC kasıtlı olarak yok

Bazı tweakler Windows Defender veya Edge gibi güvenlik / sistem bileşenlerini değiştirir. Sadece kendi bilgisayarında, riski bilerek kullan.

## Kaynak

- Arayüz: Flyworm (MIT)
- Debloat motoru: Win11Debloat, Raphire (MIT)
- Tweak scriptleri: senin mevcut paketinden
