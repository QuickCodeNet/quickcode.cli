# Homebrew Tap Formula Güncelleme

## 🔄 Mevcut Durum

Homebrew Tap repository'sinde farklı bir version var. Güncellemek için iki seçenek:

---

## 🚀 Seçenek 1: Otomatik Güncelleme (Önerilen)

### 1. GitHub Personal Access Token Oluştur

1. GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. **Generate new token (classic)** butonuna tıkla
3. İsim ver: `Homebrew Tap Auto Update`
4. Scope seç: **`repo`** (tüm repo yetkileri)
5. **Generate token** butonuna tıkla
6. Token'ı kopyala (bir daha gösterilmeyecek!)

### 2. GitHub Secret Ekle

1. Ana repository'de: **Settings → Secrets and variables → Actions**
2. **New repository secret** butonuna tıkla
3. **Name**: `HOMEBREW_TAP_TOKEN`
4. **Secret**: Kopyaladığın token'ı yapıştır
5. **Add secret** butonuna tıkla

### 3. Sonraki Release Otomatik Güncellenecek!

Artık her release'de GitHub Actions otomatik olarak:
- ✅ Formula'yı güncel version ile oluşturur
- ✅ Homebrew Tap repository'sine push eder
- ✅ Hiçbir şey yapmana gerek yok!

---

## 📝 Seçenek 2: Manuel Güncelleme

### Adım 1: GitHub Actions'dan Formula İndir

1. GitHub'da: **Actions** sekmesine git
2. Son **Release** workflow'unu bul
3. **homebrew-formula** artifact'ini indir
4. İndirilen zip dosyasını aç
5. `quickcode-cli.rb` dosyasını bul

### Adım 2: Homebrew Tap Repository'sine Push Et

```bash
# Homebrew Tap repository'sini clone et
git clone https://github.com/QuickCodeNet/homebrew-quickcode-cli.git
cd homebrew-quickcode-cli

# Mevcut formula'yı kontrol et
cat Formula/quickcode-cli.rb | grep version

# Yeni formula'yı kopyala (indirdiğin dosyadan)
cp /path/to/downloaded/quickcode-cli.rb Formula/quickcode-cli.rb

# Değişiklikleri kontrol et
git diff Formula/quickcode-cli.rb

# Commit ve push
git add Formula/quickcode-cli.rb
git commit -m "Update quickcode-cli to v1.0.0"
git push origin main
```

### Adım 3: Test Et

```bash
# Homebrew cache'i güncelle
brew update

# Formula'yı kontrol et
brew info quickcode-cli

# Kurulumu test et
brew upgrade quickcode-cli
# veya
brew install quickcode-cli
```

---

## ✅ Hangi Seçeneği Kullanmalıyım?

### Otomatik (Seçenek 1) kullan eğer:
- ✅ Her release'de manuel işlem yapmak istemiyorsan
- ✅ Token'ı güvenli bir şekilde saklayabilirsin
- ✅ Tek seferlik kurulum yapmak istiyorsan

### Manuel (Seçenek 2) kullan eğer:
- ⚠️ Token eklemek istemiyorsan
- ⚠️ Her release'de kontrol etmek istiyorsan
- ⚠️ İlk release için hızlı güncelleme gerekiyorsa

---

## 🔍 Sorun Giderme

### "Formula not found" hatası
- Homebrew Tap repository adını kontrol et: `QuickCodeNet/homebrew-quickcode-cli`
- Formula dosyasının `Formula/` klasöründe olduğundan emin ol

### "Checksum mismatch" hatası
- GitHub Actions'dan indirdiğin formula'yı kullandığından emin ol
- Eski formula'daki checksum'ları kullanma

### Otomatik push çalışmıyor
- `HOMEBREW_TAP_TOKEN` secret'ının doğru olduğundan emin ol
- Token'ın `repo` scope'una sahip olduğunu kontrol et
- GitHub Actions loglarını kontrol et

---

## 📚 İpuçları

1. **İlk release için:** Manuel güncelleme yap (token henüz eklenmemiş olabilir)
2. **Sonraki release'ler için:** Token ekleyip otomatik güncellemeyi kullan
3. **Test için:** Her güncellemeden sonra `brew upgrade quickcode-cli` çalıştır

