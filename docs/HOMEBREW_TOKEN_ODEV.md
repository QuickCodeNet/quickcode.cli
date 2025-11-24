# 🎯 HOMEBREW_TAP_TOKEN - Hızlı Ödev

## 1️⃣ Token Oluştur

1. GitHub.com → Profil fotoğrafı → **Settings**
2. Sol menü: **Developer settings** → **Personal access tokens** → **Tokens (classic)**
3. **Generate new token (classic)**
4. **Note**: `Homebrew Tap` yaz
5. **Expiration**: İstediğin süre (örn: 90 days)
6. **Scopes**: ✅ **`repo`** işaretle (tüm repo yetkileri)
7. **Generate token** → Token'ı kopyala! (örn: `ghp_xxxxxxxxxxxx...`)

## 2️⃣ Secret Ekle

1. https://github.com/QuickCodeNet/quickcode.cli → **Settings**
2. Sol menü: **Secrets and variables** → **Actions**
3. **New repository secret**
4. **Name**: `HOMEBREW_TAP_TOKEN`
5. **Secret**: Kopyaladığın token'ı yapıştır
6. **Add secret**

## 3️⃣ Test

```bash
# Yeni release yap
git tag v1.0.1
git push origin v1.0.1

# GitHub Actions → Release workflow'unu izle
# Homebrew Tap otomatik güncellenecek!
```

✅ **Tamam!** Artık her release otomatik olarak Homebrew Tap'e push edilecek.

