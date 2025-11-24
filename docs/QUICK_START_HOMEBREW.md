# Homebrew Tap - Hızlı Başlangıç

## 🎯 Best Practice: Otomatik Güncelleme

Homebrew Tap'i otomatik güncellemek için:

### 1. Homebrew Tap Repository Oluştur

```bash
# GitHub'da yeni repository oluştur
# İsim: homebrew-quickcode-cli
# Public repository olmalı
```

### 2. GitHub Secret Ekle

1. Ana repository'de (quickcode.cli): **Settings → Secrets and variables → Actions**
2. **New repository secret** butonuna tıkla
3. Name: `HOMEBREW_TAP_TOKEN`
4. Value: GitHub Personal Access Token (PAT) oluştur:
   - GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
   - `repo` scope'u seç
   - Token oluştur ve kopyala
5. **Add secret** butonuna tıkla

### 3. İlk Formula'yı Manuel Ekle

```bash
# İlk release'den önce, template formula'yı ekle
git clone https://github.com/uzeyirapaydin/homebrew-quickcode-cli.git
cd homebrew-quickcode-cli
mkdir -p Formula

# Formula/quickcode-cli.rb dosyasını buraya kopyala
# (İlk release'den sonra GitHub Actions otomatik güncelleyecek)

git add Formula/quickcode-cli.rb
git commit -m "Add quickcode-cli formula"
git push origin main
```

### 4. Test Et

```bash
brew tap uzeyirapaydin/quickcode-cli
brew install quickcode-cli
quickcode --version
```

## ✅ Artık Her Release Otomatik!

Tag push ettiğinde:
1. ✅ GitHub Actions binary'leri oluşturur
2. ✅ Formula'yı otomatik oluşturur
3. ✅ Homebrew Tap repository'sine otomatik push eder
4. ✅ Kullanıcılar `brew upgrade quickcode-cli` ile güncelleyebilir

**Hiçbir şey yapmana gerek yok!** 🎉

## 📝 Manuel Güncelleme (Opsiyonel)

Eğer otomatik push istemiyorsan:

1. GitHub Actions'dan `homebrew-formula` artifact'ini indir
2. `homebrew-quickcode-cli` repository'sine push et

Detaylar için `HOMEBREW_SETUP.md` dosyasına bak.

