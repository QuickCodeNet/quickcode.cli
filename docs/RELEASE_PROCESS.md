# Release Süreci

## ✅ Otomatik Yapılanlar (GitHub Actions)

Git tag push edildiğinde **otomatik olarak** yapılır:

1. ✅ **Build**: Tüm platformlar için binary oluşturma
   - macOS (Apple Silicon - ARM64)
   - macOS (Intel - x64)
   - Windows (x64)
   - Windows (ARM64)

2. ✅ **Packaging**: Binary'leri zip/tar.gz olarak paketleme

3. ✅ **Checksum**: SHA256 checksum dosyaları oluşturma

4. ✅ **GitHub Release**: 
   - Release notları oluşturma
   - Binary'leri GitHub Releases'e yükleme
   - Download linklerini ekleme

## 🔧 Manuel Yapılması Gerekenler

### 1. Version Güncelleme
```bash
# Directory.Build.props dosyasında version'ı güncelle
<Version>1.0.1</Version>
```

### 2. Git Tag Oluşturma
```bash
git add .
git commit -m "Release v1.0.1"
git tag v1.0.1
git push origin main
git push origin v1.0.1
```

**Bu kadar!** Tag push edildiğinde GitHub Actions otomatik olarak:
- Build yapar
- Binary'leri oluşturur
- GitHub Releases'e yükler

### 3. Homebrew Formula Güncelleme

**Otomatik (Önerilen):**
- Eğer `HOMEBREW_TAP_TOKEN` secret'ını GitHub'a eklediysen, GitHub Actions otomatik olarak Homebrew Tap repository'sine push eder
- Hiçbir şey yapmana gerek yok! 🎉

**Manuel:**
- GitHub Actions'dan `homebrew-formula` artifact'ini indir
- `homebrew-quickcode-cli` repository'sine push et
- Detaylar için `HOMEBREW_SETUP.md` dosyasına bak

### 4. Scoop Manifest Güncelleme (Opsiyonel)
Her release'den sonra `scoop-bucket` repository'sinde:
- Manifest dosyasındaki URL'leri güncelle
- Checksum'ları güncelle

---

## 🚀 Hızlı Release Adımları

```bash
# 1. Version güncelle
# Directory.Build.props dosyasını düzenle

# 2. Commit ve tag
git add Directory.Build.props
git commit -m "Bump version to 1.0.1"
git tag v1.0.1
git push origin main
git push origin v1.0.1

# 3. GitHub Actions otomatik olarak release oluşturur!
# GitHub'da Actions sekmesinden ilerlemeyi izleyebilirsin
```

---

## 📋 Release Checklist

- [ ] Version'ı `Directory.Build.props`'ta güncelle
- [ ] Değişiklikleri commit et
- [ ] Git tag oluştur (`v1.0.1` formatında)
- [ ] Tag'ı push et
- [ ] GitHub Actions'ın tamamlanmasını bekle
- [ ] GitHub Releases'de release'i kontrol et
- [ ] (Otomatik) Homebrew formula güncellendi mi kontrol et (eğer `HOMEBREW_TAP_TOKEN` varsa)
- [ ] (Manuel) Homebrew formula'yı push et (eğer otomatik değilse)
- [ ] (Opsiyonel) Scoop manifest güncelle

---

## 🔄 Workflow Dispatch (Manuel Tetikleme)

GitHub Actions'ı manuel olarak da tetikleyebilirsin:

1. GitHub'da **Actions** sekmesine git
2. **Release** workflow'unu seç
3. **Run workflow** butonuna tıkla
4. Version numarasını gir (örn: `1.0.1`)
5. **Run workflow** butonuna tıkla

Bu durumda tag oluşturmadan da release yapabilirsin, ama tag oluşturman önerilir.

