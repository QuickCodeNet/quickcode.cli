#!/bin/bash
set -e

echo "🚀 İlk Release Oluşturuluyor..."
echo ""

# 1. Tüm değişiklikleri ekle
echo "📦 Değişiklikler ekleniyor..."
git add .

# 2. Commit et
echo "💾 Commit ediliyor..."
git commit -m "Setup release infrastructure and prepare v1.0.0" || echo "No changes to commit"

# 3. Main branch'e push et
echo "📤 Main branch'e push ediliyor..."
git push origin main

# 4. Tag oluştur
echo "🏷️  Tag oluşturuluyor..."
git tag v1.0.0

# 5. Tag'ı push et
echo "📤 Tag push ediliyor..."
git push origin v1.0.0

echo ""
echo "✅ Tamamlandı!"
echo ""
echo "📋 Sonraki adımlar:"
echo "1. GitHub Actions workflow'unun tamamlanmasını bekle (5-10 dakika)"
echo "2. GitHub Releases sayfasını kontrol et: https://github.com/QuickCodeNet/quickcode.cli/releases"
echo "3. Homebrew Tap repository oluştur: homebrew-quickcode-cli"
echo "4. GitHub Actions'dan 'homebrew-formula' artifact'ini indir"
echo "5. Formula'yı Homebrew Tap repository'sine push et"
echo ""
echo "Detaylar için FIRST_RELEASE.md dosyasına bak."

