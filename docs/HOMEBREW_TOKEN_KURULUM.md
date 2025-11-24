# HOMEBREW_TAP_TOKEN Nasıl Oluşturulur?

## 📋 Adım Adım Rehber

### Adım 1: GitHub Personal Access Token Oluştur

1. **GitHub'a git:**
   - Tarayıcıda: https://github.com
   - Sağ üst köşedeki profil fotoğrafına tıkla
   - **Settings** seçeneğine tıkla

2. **Developer settings'e git:**
   - Sol menüden en altta **Developer settings** seçeneğine tıkla

3. **Personal access tokens bölümüne git:**
   - Sol menüden **Personal access tokens** seçeneğine tıkla
   - **Tokens (classic)** sekmesine tıkla

4. **Yeni token oluştur:**
   - Sağ üstteki **Generate new token** butonuna tıkla
   - **Generate new token (classic)** seçeneğine tıkla

5. **Token ayarlarını yap:**
   - **Note (İsim)**: `Homebrew Tap Auto Update` yaz (ne için olduğunu hatırlamak için)
   - **Expiration (Süre)**: İstediğin süreyi seç (örn: 90 days veya No expiration)
   - **Scopes (Yetkiler)**: Aşağıdaki kutucuğu işaretle:
     - ✅ **`repo`** (tüm repo yetkileri)
       - Bu alt yetkileri de içerir:
         - repo:status
         - repo_deployment
         - public_repo
         - repo:invite
         - security_events
   
6. **Token oluştur:**
   - Sayfanın en altına in
   - **Generate token** (yeşil buton) butonuna tıkla

7. **Token'ı kopyala:**
   - 🔴 **ÖNEMLİ:** Token'ı hemen kopyala! (örn: `ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`)
   - Bu token'ı bir daha göremeyeceksin!
   - Notepad'e veya bir yere kaydet

---

### Adım 2: GitHub Secret Olarak Ekle

1. **Ana repository'ye git:**
   - https://github.com/QuickCodeNet/quickcode.cli sayfasına git

2. **Settings'e git:**
   - Repository sayfasında üst menüden **Settings** sekmesine tıkla

3. **Secrets bölümüne git:**
   - Sol menüden **Secrets and variables** seçeneğine tıkla
   - **Actions** sekmesine tıkla

4. **Yeni secret ekle:**
   - Sağ üstteki **New repository secret** butonuna tıkla

5. **Secret bilgilerini gir:**
   - **Name**: `HOMEBREW_TAP_TOKEN` (tam olarak bu şekilde yaz)
   - **Secret**: Az önce kopyaladığın token'ı yapıştır (örn: `ghp_xxxxxxxxxxxx...`)

6. **Kaydet:**
   - **Add secret** (yeşil buton) butonuna tıkla

---

### Adım 3: Test Et

1. **Yeni bir release yap:**
   ```bash
   # Directory.Build.props'ta version'ı değiştir
   # Tag oluştur
   git tag v1.0.1
   git push origin v1.0.1
   ```

2. **GitHub Actions'ı kontrol et:**
   - GitHub'da **Actions** sekmesine git
   - Release workflow'unu izle
   - "Update Homebrew Tap (Optional - Auto Push)" adımının çalıştığını gör
   - ✅ "Homebrew Tap updated automatically!" mesajını gör

3. **Homebrew Tap repository'sini kontrol et:**
   - https://github.com/QuickCodeNet/homebrew-quickcode-cli sayfasına git
   - Formula dosyasının güncellendiğini gör
   - Commit mesajı: "Update quickcode-cli to v1.0.1"

---

## ✅ Örnek Token Formatı

Token şu şekilde görünür:
```
ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

- `ghp_` ile başlar
- ~40 karakter uzunluğundadır
- Büyük/küçük harf, rakam içerir

---

## 🔒 Güvenlik

- ✅ Token'ı kimseyle paylaşma
- ✅ GitHub'da public olarak paylaşma
- ✅ Kod içinde yazma (sadece secret olarak ekle)
- ✅ Süresi dolduğunda yeniden oluştur

---

## ❓ Sorun Giderme

### Token çalışmıyor
- Token'ın `repo` scope'una sahip olduğundan emin ol
- Token'ın süresi dolmamış olduğundan emin ol
- Secret adının `HOMEBREW_TAP_TOKEN` (tam olarak) olduğundan emin ol

### Otomatik push çalışmıyor
- GitHub Actions loglarını kontrol et
- Token'ın Homebrew Tap repository'sine yazma yetkisi olduğundan emin ol
- Homebrew Tap repository adının `QuickCodeNet/homebrew-quickcode-cli` olduğundan emin ol

---

## 📚 Daha Fazla Bilgi

- [GitHub Personal Access Tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token)
- [GitHub Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)

