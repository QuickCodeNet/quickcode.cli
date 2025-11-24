# Homebrew Tap Kurulum Rehberi

Bu rehber, QuickCode CLI için Homebrew Tap oluşturma ve otomatik güncelleme sürecini açıklar.

## 📋 Ön Koşullar

1. GitHub hesabınızda `homebrew-quickcode-cli` adında **public** bir repository oluşturun
2. Repository'yi boş bırakın (GitHub Actions otomatik olarak doldurur)

## 🚀 Kurulum

### Seçenek 1: Manuel Kurulum (Önerilen - İlk Kurulum)

1. **Repository oluştur:**
   ```bash
   # GitHub'da yeni repository oluştur: homebrew-quickcode-cli
   ```

2. **İlk formula'yı ekle:**
   ```bash
   # GitHub Actions'dan formula artifact'ini indir
   # veya Formula/quickcode-cli.rb dosyasını kopyala
   
   git clone https://github.com/uzeyirapaydin/homebrew-quickcode-cli.git
   cd homebrew-quickcode-cli
   mkdir -p Formula
   
   # Formula dosyasını buraya kopyala
   # Formula/quickcode-cli.rb
   
   git add Formula/quickcode-cli.rb
   git commit -m "Add quickcode-cli formula"
   git push origin main
   ```

3. **Test et:**
   ```bash
   brew tap uzeyirapaydin/quickcode-cli
   brew install quickcode-cli
   quickcode --version
   ```

### Seçenek 2: Otomatik Güncelleme (GitHub Actions ile)

GitHub Actions'ın otomatik olarak Homebrew Tap repository'nize push yapması için:

1. **Personal Access Token (PAT) oluştur:**
   - GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
   - `repo` scope'u ile token oluştur
   - Token'ı kopyala

2. **GitHub Secret ekle:**
   - Ana repository'de (quickcode.cli): Settings → Secrets and variables → Actions
   - Yeni secret: `HOMEBREW_TAP_TOKEN`
   - Value: Oluşturduğun PAT token'ı

3. **Homebrew Tap repository'ye erişim:**
   - PAT token'ın `homebrew-quickcode-cli` repository'sine yazma yetkisi olduğundan emin ol

4. **GitHub Actions workflow'unu güncelle:**
   - `.github/workflows/release.yml` dosyasında otomatik push adımı zaten var
   - Sadece `HOMEBREW_TAP_TOKEN` secret'ını eklemen yeterli

## 🔄 Güncelleme Süreci

### Manuel Güncelleme (Her Release'den Sonra)

1. **GitHub Actions'dan formula artifact'ini indir:**
   - GitHub'da release workflow'unu aç
   - `homebrew-formula` artifact'ini indir
   - `quickcode-cli.rb` dosyasını çıkar

2. **Homebrew Tap repository'sine push et:**
   ```bash
   cd homebrew-quickcode-cli
   git pull origin main
   
   # Yeni formula dosyasını kopyala
   cp /path/to/downloaded/quickcode-cli.rb Formula/quickcode-cli.rb
   
   git add Formula/quickcode-cli.rb
   git commit -m "Update quickcode-cli to v1.0.1"
   git push origin main
   ```

3. **Kullanıcılar güncelleyebilir:**
   ```bash
   brew update
   brew upgrade quickcode-cli
   ```

### Otomatik Güncelleme (GitHub Actions ile)

Eğer `HOMEBREW_TAP_TOKEN` secret'ını eklediysen, GitHub Actions otomatik olarak:
- Formula'yı oluşturur
- Homebrew Tap repository'sine push eder
- Commit mesajı: "Update quickcode-cli to v{version}"

**Hiçbir şey yapmana gerek yok!** 🎉

## 📁 Repository Yapısı

```
homebrew-quickcode-cli/
└── Formula/
    └── quickcode-cli.rb
```

## ✅ Test Etme

Formula'yı test etmek için:

```bash
# Local test
brew install --build-from-source Formula/quickcode-cli.rb

# Tap test
brew tap uzeyirapaydin/quickcode-cli
brew install quickcode-cli
quickcode --version
```

## 🔍 Troubleshooting

### Formula çalışmıyor
- Checksum'ları kontrol et
- URL'lerin doğru olduğundan emin ol
- `brew audit Formula/quickcode-cli.rb` çalıştır

### Otomatik push çalışmıyor
- `HOMEBREW_TAP_TOKEN` secret'ının doğru olduğundan emin ol
- Token'ın `repo` scope'una sahip olduğunu kontrol et
- GitHub Actions loglarını kontrol et

### Kullanıcılar güncelleme görmüyor
- Homebrew cache'i temizle: `brew update`
- Formula'nın push edildiğini kontrol et
- Repository'nin public olduğundan emin ol

## 📚 Kaynaklar

- [Homebrew Formula Cookbook](https://docs.brew.sh/Formula-Cookbook)
- [Homebrew Tap Documentation](https://docs.brew.sh/Taps)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)

