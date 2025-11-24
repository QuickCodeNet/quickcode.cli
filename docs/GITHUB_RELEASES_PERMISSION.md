# GitHub Releases Permission Hatası Çözümü

## ❌ Hata

```
⚠️ GitHub release failed with status: 422
[{"resource":"Release","code":"custom","field":"author_id","message":"author_id does not have push access"}]
```

## 🔍 Sorun

GitHub Actions'ın release oluşturma yetkisi yok. Bu genellikle repository ayarlarından kaynaklanır.

## ⚠️ ÖNEMLİ: GITHUB_TOKEN Hakkında

**`GITHUB_TOKEN` otomatik olarak GitHub tarafından sağlanır!** Manuel oluşturmanıza gerek yok. Ancak repository ayarlarında workflow'ların yeterli yetkiye sahip olması gerekir.

## ✅ Çözüm 1: Repository Settings'i Güncelle (EN KOLAY!) ⭐

### Adım Adım:

1. **GitHub'da repository'ye git:**
   - https://github.com/QuickCodeNet/quickcode.cli

2. **Settings** sekmesine tıkla (repository sayfasında üstte, yanında Insights var)

3. Sol menüden **Actions** → **General** seç

4. Aşağı kaydır, **Workflow permissions** bölümünü bul

5. **✅ Read and write permissions** seç
   - Şu anda muhtemelen "Read repository contents and packages permissions" seçili
   - **Read and write permissions** seçmelisin

6. **Save** butonuna tıkla (sayfanın altında)

**✅ Bu kadar! Artık release oluşturabilir.**

---

## ✅ Çözüm 2: Personal Access Token (PAT) Kullan

Eğer repository ayarlarını değiştiremiyorsan veya hala çalışmıyorsa:

### Adım Adım:

1. **GitHub** → Sağ üstte profil resmi → **Settings**

2. Sol menüden **Developer settings**

3. **Personal access tokens** → **Tokens (classic)**

4. **Generate new token (classic)** butonuna tıkla

5. **Note:** `quickcode-cli-release` yaz

6. **Expiration:** İstediğin süreyi seç (örn: 90 days veya No expiration)

7. **Select scopes:**
   - ✅ **`repo`** (tüm repo yetkileri - release oluşturmak için gerekli)
   - Bu otomatik olarak şunları da seçer: `repo:status`, `repo_deployment`, `public_repo`, `repo:invite`

8. Sayfanın en altında **Generate token** butonuna tıkla

9. **⚠️ ÖNEMLİ: Token'ı kopyala! Bir daha göremezsin!**
   - Token başladığında `ghp_` ile başlar

10. **Repository'ye dön:**
    - https://github.com/QuickCodeNet/quickcode.cli

11. **Settings** → **Secrets and variables** → **Actions**

12. **New repository secret** butonuna tıkla

13. **Name:** `RELEASE_TOKEN` yaz

14. **Secret:** Kopyaladığın token'ı yapıştır

15. **Add secret** butonuna tıkla

16. **✅ Artık `RELEASE_TOKEN` kullanılacak (workflow otomatik algılar)**

---

## 🔄 Test Et

1. Repository settings'i güncelle (Çözüm 1)
2. GitHub Actions → **Release** workflow'unu çalıştır:
   - Actions sekmesi → Release → Run workflow → Version: `1.0.3` → Run workflow
3. Release başarıyla oluşturulmalı!

---

## 📝 Özet

- **`GITHUB_TOKEN` otomatik sağlanır** - Manuel oluşturmana gerek yok ❌
- **En kolay çözüm:** Repository Settings → Actions → General → Workflow permissions → **Read and write permissions** ✅
- **Eğer hala çalışmıyorsa:** Personal Access Token (PAT) kullan (Çözüm 2) ✅

---

## 🆘 Hala Çalışmıyor Mu?

1. Repository'nin **Settings** → **Actions** → **General** → **Workflow permissions** kısmını kontrol et
2. **Read and write permissions** seçili mi kontrol et
3. Workflow'u tekrar çalıştır
4. Hala hata alıyorsan, **Çözüm 2**'yi uygula (PAT kullan)
