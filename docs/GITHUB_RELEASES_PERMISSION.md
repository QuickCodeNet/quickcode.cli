# GitHub Releases Permission Hatası Çözümü

## ❌ Hata

```
⚠️ GitHub release failed with status: 422
[{"resource":"Release","code":"custom","field":"author_id","message":"author_id does not have push access"}]
```

## 🔍 Sorun

GitHub Actions'ın release oluşturma yetkisi yok. Bu genellikle repository ayarlarından kaynaklanır.

## ✅ Çözüm

### 1. Repository Settings Kontrolü

1. GitHub'da repository'ye git: https://github.com/QuickCodeNet/quickcode.cli
2. **Settings** → **Actions** → **General**
3. **Workflow permissions** bölümünü kontrol et:
   - ✅ **Read and write permissions** seçili olmalı
   - ✅ **Allow GitHub Actions to create and approve pull requests** işaretlenmeli

### 2. Alternatif: Personal Access Token Kullan

Eğer repository ayarlarını değiştiremiyorsan:

1. **GitHub Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**
2. **Generate new token** (classic)
3. Scopes seç:
   - ✅ `repo` (tüm repo yetkileri)
4. Token'ı kopyala

5. **Repository Settings** → **Secrets and variables** → **Actions**
6. **New repository secret**:
   - Name: `RELEASE_TOKEN`
   - Value: Token'ı yapıştır

7. Workflow dosyasında `GITHUB_TOKEN` yerine `RELEASE_TOKEN` kullan (isteğe bağlı)

### 3. En Kolay Çözüm: Repository Settings

**Repository Settings** → **Actions** → **General** → **Workflow permissions** → **Read and write permissions** seç.

## 🔄 Kontrol

1. Repository settings'i güncelle
2. Workflow'u tekrar çalıştır
3. Release oluşturulmalı

---

**Not:** Bu hata genellikle repository'nin varsayılan ayarlarından kaynaklanır. `Read and write permissions` seçilince sorun çözülür.

