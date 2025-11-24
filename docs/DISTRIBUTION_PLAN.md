# QuickCode.Cli Dağıtım Planı

Bu doküman, QuickCode.Cli'nin Mac (Homebrew) ve Windows için dağıtım stratejisini açıklar.

## 📋 Genel Bakış

### Hedef Platformlar
- **macOS**: Homebrew ile dağıtım
- **Windows**: Scoop veya Chocolatey ile dağıtım (veya manuel indirme)
- **Linux**: Opsiyonel (gelecekte eklenebilir)

### Dağıtım Yöntemleri
1. **GitHub Releases**: Binary dosyaların ana dağıtım noktası
2. **Homebrew**: Mac kullanıcıları için `brew install quickcode-cli`
3. **Scoop**: Windows kullanıcıları için `scoop install quickcode-cli`
4. **Chocolatey**: Alternatif Windows paket yöneticisi

---

## 🏗️ Adım 1: Proje Yapılandırması

### 1.1 Single-File Self-Contained Build

`.csproj` dosyasına aşağıdaki özellikler eklenmeli:

```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>osx-arm64</RuntimeIdentifier> <!-- Mac Apple Silicon -->
  <RuntimeIdentifier>osx-x64</RuntimeIdentifier>    <!-- Mac Intel -->
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>    <!-- Windows 64-bit -->
  <RuntimeIdentifier>win-arm64</RuntimeIdentifier>  <!-- Windows ARM -->
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  <IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
  <PublishTrimmed>false</PublishTrimmed> <!-- SignalR için gerekli -->
</PropertyGroup>
```

### 1.2 Versioning

- Semantic Versioning kullanılacak (v1.0.0, v1.0.1, vb.)
- Version bilgisi `Directory.Build.props` veya `.csproj` içinde tutulacak
- GitHub Releases ile tag'ler eşleştirilecek

---

## 🔨 Adım 2: Build Script'leri

### 2.1 Build Script Yapısı

```
scripts/
├── build.sh          # macOS/Linux build script
├── build.ps1         # Windows build script
└── publish-all.sh    # Tüm platformlar için build
```

### 2.2 Build Komutları

**macOS (Apple Silicon):**
```bash
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

**macOS (Intel):**
```bash
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

**Windows (x64):**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

**Windows (ARM64):**
```bash
dotnet publish -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

---

## 🚀 Adım 3: GitHub Actions CI/CD

### 3.1 Workflow Yapısı

`.github/workflows/release.yml` dosyası oluşturulacak:

**Özellikler:**
- Her tag push'unda otomatik build
- Tüm platformlar için binary oluşturma
- GitHub Releases'e otomatik yükleme
- Checksum dosyaları oluşturma
- Homebrew formula güncelleme (opsiyonel)

### 3.2 Release Workflow Adımları

1. **Build**: Tüm platformlar için build
2. **Package**: Binary'leri zip/tar.gz olarak paketle
3. **Checksum**: SHA256 checksum'ları oluştur
4. **Release**: GitHub Releases'e yükle
5. **Homebrew**: Formula'yı güncelle (opsiyonel)

---

## 🍺 Adım 4: Homebrew Formula (Mac)

### 4.1 Formula Yapısı

Homebrew formula iki şekilde olabilir:

**Seçenek A: Homebrew Core (Zor)**
- Homebrew'un resmi repository'sine PR açmak gerekir
- Çok sayıda kullanıcı ve yıldız gerektirir
- Onay süreci uzun olabilir

**Seçenek B: Homebrew Tap (Kolay - Önerilen)**
- Kendi repository'nde `homebrew-quickcode-cli` tap'i oluştur
- Kullanıcılar `brew tap uzeyirapaydin/quickcode-cli` yapabilir
- Daha hızlı ve kontrol sizde

### 4.2 Tap Repository Yapısı

```
homebrew-quickcode-cli/
└── Formula/
    └── quickcode-cli.rb
```

### 4.3 Formula İçeriği

```ruby
class QuickcodeCli < Formula
  desc "QuickCode API CLI tool"
  homepage "https://github.com/uzeyirapaydin/quickcode.cli"
  url "https://github.com/uzeyirapaydin/quickcode.cli/releases/download/v1.0.0/quickcode-cli-osx-arm64.tar.gz"
  sha256 "checksum-here"
  version "1.0.0"
  
  if Hardware::CPU.arm?
    url "https://github.com/uzeyirapaydin/quickcode.cli/releases/download/v1.0.0/quickcode-cli-osx-arm64.tar.gz"
    sha256 "arm64-checksum"
  else
    url "https://github.com/uzeyirapaydin/quickcode.cli/releases/download/v1.0.0/quickcode-cli-osx-x64.tar.gz"
    sha256 "x64-checksum"
  end
  
  def install
    bin.install "quickcode"
  end
  
  test do
    system "#{bin}/quickcode", "--version"
  end
end
```

### 4.4 Kullanım

```bash
# Tap'i ekle
brew tap uzeyirapaydin/quickcode-cli

# Install
brew install quickcode-cli

# Update
brew upgrade quickcode-cli
```

---

## 🪟 Adım 5: Windows Dağıtımı

### 5.1 Seçenek 1: Scoop (Önerilen)

**Avantajlar:**
- Kullanıcı dostu
- Otomatik güncelleme
- Kolay kurulum

**Scoop Bucket Yapısı:**
```
scoop-bucket/
└── quickcode-cli.json
```

**Manifest İçeriği:**
```json
{
  "version": "1.0.0",
  "description": "QuickCode API CLI tool",
  "homepage": "https://github.com/uzeyirapaydin/quickcode.cli",
  "license": "MIT",
  "architecture": {
    "64bit": {
      "url": "https://github.com/uzeyirapaydin/quickcode.cli/releases/download/v1.0.0/quickcode-cli-win-x64.zip",
      "hash": "sha256-checksum-here"
    },
    "arm64": {
      "url": "https://github.com/uzeyirapaydin/quickcode.cli/releases/download/v1.0.0/quickcode-cli-win-arm64.zip",
      "hash": "sha256-checksum-here"
    }
  },
  "bin": "quickcode.exe",
  "checkver": "github",
  "autoupdate": {
    "architecture": {
      "64bit": {
        "url": "https://github.com/uzeyirapaydin/quickcode.cli/releases/download/v$version/quickcode-cli-win-x64.zip"
      },
      "arm64": {
        "url": "https://github.com/uzeyirapaydin/quickcode.cli/releases/download/v$version/quickcode-cli-win-arm64.zip"
      }
    }
  }
}
```

**Kullanım:**
```powershell
# Bucket ekle
scoop bucket add quickcode-cli https://github.com/uzeyirapaydin/scoop-bucket

# Install
scoop install quickcode-cli

# Update
scoop update quickcode-cli
```

### 5.2 Seçenek 2: Chocolatey

**Manifest İçeriği:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd">
  <metadata>
    <id>quickcode-cli</id>
    <version>1.0.0</version>
    <title>QuickCode CLI</title>
    <authors>uzeyirapaydin</authors>
    <description>QuickCode API CLI tool</description>
    <projectUrl>https://github.com/uzeyirapaydin/quickcode.cli</projectUrl>
    <packageSourceUrl>https://github.com/uzeyirapaydin/quickcode.cli</packageSourceUrl>
  </metadata>
  <files>
    <file src="tools\**" target="tools" />
  </files>
</package>
```

### 5.3 Seçenek 3: Manuel İndirme

- GitHub Releases'den zip dosyası indirme
- PATH'e ekleme talimatları README'de

---

## 📦 Adım 6: Binary İsimlendirme

### 6.1 Standart İsim Formatı

```
quickcode-cli-{platform}-{arch}-{version}.{ext}
```

**Örnekler:**
- `quickcode-cli-osx-arm64-v1.0.0.tar.gz`
- `quickcode-cli-osx-x64-v1.0.0.tar.gz`
- `quickcode-cli-win-x64-v1.0.0.zip`
- `quickcode-cli-win-arm64-v1.0.0.zip`

### 6.2 Binary İçindeki Executable İsmi

- **Mac/Linux**: `quickcode` (executable, chmod +x)
- **Windows**: `quickcode.exe`

---

## 🔄 Adım 7: Release Süreci

### 7.1 Manuel Release

1. **Version Güncelleme**
   ```bash
   # .csproj veya Directory.Build.props'ta version güncelle
   <Version>1.0.0</Version>
   ```

2. **Tag Oluştur**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. **GitHub Actions Otomatik Build**
   - Tag push'u workflow'u tetikler
   - Binary'ler otomatik oluşturulur
   - GitHub Releases'e yüklenir

4. **Homebrew Formula Güncelle**
   ```bash
   # homebrew-quickcode-cli repository'sinde
   # Formula/quickcode-cli.rb dosyasını güncelle
   # URL ve checksum'ları değiştir
   ```

5. **Scoop Manifest Güncelle**
   ```bash
   # scoop-bucket repository'sinde
   # quickcode-cli.json dosyasını güncelle
   ```

### 7.2 Otomatik Release (GitHub Actions)

- Tag push'u ile otomatik release
- Homebrew ve Scoop güncellemeleri manuel (veya ayrı workflow)

---

## 📝 Adım 8: Dokümantasyon

### 8.1 README Güncellemeleri

**Installation Bölümü Eklenmeli:**

```markdown
## Installation

### macOS (Homebrew)
```bash
brew tap uzeyirapaydin/quickcode-cli
brew install quickcode-cli
```

### Windows (Scoop)
```powershell
scoop bucket add quickcode-cli https://github.com/uzeyirapaydin/scoop-bucket
scoop install quickcode-cli
```

### Manual Installation
1. [Latest Release](https://github.com/uzeyirapaydin/quickcode.cli/releases/latest) sayfasından binary indir
2. Extract et
3. PATH'e ekle
```

### 8.2 Release Notes

Her release için:
- Changelog
- Breaking changes
- Migration guide (varsa)

---

## ✅ Uygulama Sırası

### Faz 1: Temel Yapılandırma (1-2 gün)
- [ ] `.csproj` dosyasını single-file için yapılandır
- [ ] Build script'lerini oluştur
- [ ] Manuel olarak tüm platformlar için build test et

### Faz 2: GitHub Actions (1 gün)
- [ ] `.github/workflows/release.yml` oluştur
- [ ] Test release yap
- [ ] Binary'lerin doğru oluşturulduğunu kontrol et

### Faz 3: Homebrew Tap (1 gün)
- [ ] `homebrew-quickcode-cli` repository oluştur
- [ ] Formula dosyasını hazırla
- [ ] Test install yap

### Faz 4: Scoop Bucket (1 gün)
- [ ] `scoop-bucket` repository oluştur
- [ ] Manifest dosyasını hazırla
- [ ] Test install yap

### Faz 5: Dokümantasyon (1 gün)
- [ ] README'yi güncelle
- [ ] Installation talimatları ekle
- [ ] Release notes template hazırla

### Faz 6: İlk Release (1 gün)
- [ ] v1.0.0 tag oluştur
- [ ] GitHub Release oluştur
- [ ] Homebrew ve Scoop'u güncelle
- [ ] Test et

---

## 🎯 Öncelikler

1. **Yüksek Öncelik:**
   - Single-file self-contained build
   - GitHub Actions CI/CD
   - GitHub Releases
   - Homebrew Tap

2. **Orta Öncelik:**
   - Scoop bucket
   - Dokümantasyon

3. **Düşük Öncelik:**
   - Chocolatey
   - Homebrew Core submission
   - Linux support

---

## 📚 Kaynaklar

- [.NET Single-File Publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)
- [Homebrew Formula Cookbook](https://docs.brew.sh/Formula-Cookbook)
- [Scoop App Manifests](https://github.com/ScoopInstaller/Scoop/wiki/App-Manifests)
- [GitHub Actions](https://docs.github.com/en/actions)

---

## ❓ Sorular ve Notlar

- **Runtime Identifier Seçimi**: net10.0 için hangi RID'ler destekleniyor kontrol et
- **Binary Boyutu**: Self-contained binary'ler büyük olabilir, trim edilebilir mi?
- **Code Signing**: Mac için code signing gerekli mi? (Notarization için)
- **Windows Code Signing**: Windows için sertifika gerekli mi?

