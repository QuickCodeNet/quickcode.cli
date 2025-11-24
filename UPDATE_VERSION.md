# Version Güncelleme Rehberi

## 📝 Version'ı Güncellemek İçin

### 1. `Directory.Build.props` Dosyasını Düzenle

```xml
<Project>
  <PropertyGroup>
    <Version>1.0.1</Version>  <!-- Yeni version buraya -->
    <AssemblyVersion>1.0.1.0</AssemblyVersion>
    <FileVersion>1.0.1.0</FileVersion>
    <!-- ... -->
  </PropertyGroup>
</Project>
```

### 2. Commit ve Push Et

```bash
git add Directory.Build.props
git commit -m "Bump version to 1.0.1"
git push origin main
```

### 3. Yeni Tag Oluştur

```bash
git tag v1.0.1
git push origin v1.0.1
```

### 4. GitHub Actions Otomatik Olarak Yapacak:

✅ Binary'leri yeni version ile oluşturur  
✅ Formula'yı yeni version ve checksum'larla oluşturur  
✅ Homebrew Tap'e otomatik push eder (token varsa)  
✅ GitHub Releases'e yükler  

## ⚠️ Önemli Notlar

1. **Formula dosyasını manuel değiştirme!**
   - `Formula/quickcode-cli.rb` otomatik oluşturulur
   - Manuel değişiklikler kaybolur

2. **Version formatı:**
   - Semantic Versioning kullan: `1.0.1`, `1.1.0`, `2.0.0`
   - Tag formatı: `v1.0.1` (v ile başlamalı)

3. **AssemblyVersion ve FileVersion:**
   - Genellikle `<Version>` ile aynı olmalı
   - Format: `MAJOR.MINOR.PATCH.BUILD`

## 🔄 Örnek: 1.0.0 → 1.0.1

```bash
# 1. Directory.Build.props dosyasını düzenle
# Version: 1.0.1

# 2. Commit
git add Directory.Build.props
git commit -m "Bump version to 1.0.1"
git push origin main

# 3. Tag
git tag v1.0.1
git push origin v1.0.1

# 4. GitHub Actions otomatik çalışır!
```

