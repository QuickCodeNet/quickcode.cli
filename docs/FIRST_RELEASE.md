# İlk Release Oluşturma Rehberi

## 🚀 Hızlı Başlangıç

### 1. Projeyi GitHub'a Push Et

```bash
cd /Users/uzeyirapaydin/Documents/Projects/quickcode.cli

# Tüm dosyaları ekle
git add .

# Commit et
git commit -m "Initial release setup"

# GitHub'a push et
git push origin main
```

### 2. İlk Release'i Oluştur

```bash
# Version zaten 1.0.0 (Directory.Build.props'ta)

# Tag oluştur
git tag v1.0.0

# Tag'ı push et
git push origin v1.0.0
```

**Bu kadar!** GitHub Actions otomatik olarak:
- ✅ Tüm platformlar için binary oluşturur
- ✅ GitHub Releases'e yükler
- ✅ Homebrew formula oluşturur

### 3. Homebrew Tap Kurulumu

#### Seçenek A: Otomatik (Önerilen)

1. **Homebrew Tap repository oluştur:**
   - GitHub'da `homebrew-quickcode-cli` adında **public** repository oluştur
   - Boş bırak (GitHub Actions doldurur)

2. **GitHub Secret ekle:**
   - Ana repository'de: Settings → Secrets and variables → Actions
   - New repository secret: `HOMEBREW_TAP_TOKEN`
   - Value: GitHub PAT token (repo scope)

3. **İlk formula'yı ekle:**
   ```bash
   git clone https://github.com/QuickCodeNet/homebrew-quickcode-cli.git
   cd homebrew-quickcode-cli
   mkdir -p Formula
   
   # GitHub Actions'dan formula artifact'ini indir ve buraya kopyala
   # veya template'i kullan:
   cp /path/to/quickcode.cli/Formula/quickcode-cli.rb Formula/quickcode-cli.rb
   
   git add Formula/quickcode-cli.rb
   git commit -m "Add quickcode-cli formula"
   git push origin main
   ```

4. **Sonraki release'ler otomatik güncellenecek!**

#### Seçenek B: Manuel

1. GitHub Actions workflow'unu bekle
2. `homebrew-formula` artifact'ini indir
3. `homebrew-quickcode-cli` repository'sine push et

### 4. Test Et

```bash
# Homebrew Tap ekle
brew tap QuickCodeNet/quickcode-cli

# Kur
brew install quickcode-cli

# Test et
quickcode --help
```

## ⚠️ Önemli Notlar

1. **Repository adı:** `QuickCodeNet/quickcode.cli` (otomatik algılanır)
2. **Homebrew Tap adı:** `QuickCodeNet/homebrew-quickcode-cli` olmalı
3. **İlk release'den sonra** Homebrew Tap'e formula eklemen gerekir
4. **Sonraki release'ler** otomatik güncellenecek (eğer token eklersen)

## 🔍 Sorun Giderme

### Release oluşturulmadı
- GitHub Actions loglarını kontrol et
- Tag'ın push edildiğinden emin ol: `git tag -l`

### Homebrew çalışmıyor
- Release'in oluşturulduğundan emin ol
- Formula'daki URL'lerin doğru olduğundan emin ol
- Checksum'ların doğru olduğundan emin ol

### Binary bulunamıyor
- GitHub Releases sayfasını kontrol et
- Binary'lerin yüklendiğinden emin ol
- URL'lerin doğru olduğundan emin ol

