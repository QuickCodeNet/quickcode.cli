# GitHub Actions Workflow Nasıl Çalışır?

## 🔍 Workflow Ne Zaman Çalışır?

Mevcut workflow (`release.yml`) sadece şu durumlarda çalışır:

### ✅ Çalışır:
1. **Tag push edildiğinde:**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **Manuel tetiklendiğinde:**
   - GitHub → Actions → Release workflow → Run workflow

### ❌ Çalışmaz:
- Normal commit'lerde (`git commit && git push`)
- Pull request'lerde
- Diğer branch'lere push edildiğinde

---

## 🎯 Bu Normal Mi?

**Evet, bu tamamen normal!** 

Release workflow'u sadece release yaparken çalışmalı. Her commit'te çalışmasına gerek yok.

---

## 🚀 Workflow'u Tetiklemek İçin

**Tag zorunlu değil!** İki yol var:

### Seçenek 1: Manuel Tetikle (Tag Gerekmez!) ⭐

1. GitHub'da: **Actions** sekmesine git
2. **Release** workflow'unu seç
3. Sağ üstte **Run workflow** butonuna tıkla
4. Version numarasını gir (örn: `1.0.1`)
5. **Run workflow** butonuna tıkla

**✅ Tag olmadan da çalışır!**

### Seçenek 2: Tag Oluştur (Otomatik Tetikleme)

```bash
# 1. Version'ı güncelle (Directory.Build.props)
# 2. Commit et
git add .
git commit -m "Bump version to 1.0.1"
git push origin main

# 3. Tag oluştur ve push et (opsiyonel - otomatik tetikleme için)
git tag v1.0.1
git push origin v1.0.1

# ✅ Tag push edilince workflow otomatik çalışır!
```

**Tag'in avantajı:** Otomatik tetiklenir, version GitHub Releases'de görünür.

---

## 📋 Test Etmek İçin

Workflow'un çalışıp çalışmadığını test etmek için:

```bash
# Test tag oluştur
git tag v1.0.0-test
git push origin v1.0.0-test

# GitHub'da Actions sekmesini kontrol et
# Workflow çalışıyor mu bak
```

---

## ⚠️ Önemli Notlar

1. **Normal commit'lerde çalışmaz** - Bu istenen davranış
2. **Sadece tag'lerde çalışır** - Release yaparken
3. **Workflow dosyası değiştiğinde** de çalışmaz (sadece tag'lerde)

---

## 🔍 Workflow'un Çalışıp Çalışmadığını Kontrol Et

1. GitHub'da: **Actions** sekmesine git
2. Son workflow run'larını gör
3. Eğer tag push ettiysen ve görünmüyorsa:
   - Tag formatını kontrol et: `v1.0.0` (v ile başlamalı)
   - Tag'ın push edildiğinden emin ol: `git tag -l`

---

## ✅ Özet

- ❌ Normal commit → Workflow çalışmaz (normal - her commit'te release yapmak istemeyiz)
- ✅ **Manuel tetikleme → Workflow çalışır** (Tag gerekmez!)
- ✅ Tag push → Workflow çalışır (Otomatik tetikleme)

**Tag zorunlu değil!** Manuel tetikleme ile de çalışır. Tag'in avantajı: Otomatik tetiklenir ve GitHub Releases'de görünür. 🎉

