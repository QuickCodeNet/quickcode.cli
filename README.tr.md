# QuickCode.Cli

QuickCode.Cli, QuickCode web arayüzündeki tüm akışı terminale taşıyan .NET 10 tabanlı CLI’dir. Proje oluşturma, modül yönetimi, generate ve SignalR üzerinden izlemeyi Python yerine doğrudan C# ile yapar.

- Varsayılan README dili: **İngilizce** (`README.md`)
- Türkçe sürüm: bu dosya (`README.tr.md`)

---

## Ön Koşullar
- `https://api.quickcode.net/` erişimi
- Proje bazlı email + secret bilgileri (veya `project forgot-secret` ile e-posta yoluyla edinme)

---

## Kurulum

### macOS (Homebrew) - Önerilen
```bash
brew tap QuickCodeNet/quickcode-cli
brew install quickcode-cli
```

> Yeni sürüm çıktığında güncellemek için:
```bash
brew update
brew upgrade quickcode-cli
```

Eğer `brew` çatışma/syntax hatası verirse tap’i temizleyip tekrar deneyin:
```bash
rm -rf /opt/homebrew/Library/Taps/quickcodenet/homebrew-quickcode-cli
brew tap quickcodenet/quickcode-cli
brew upgrade quickcode-cli
```

### Windows (Scoop) - Önerilen
```powershell
scoop bucket add quickcode-cli https://github.com/QuickCodeNet/quickcode-bucket
scoop install quickcode-cli
```

### Manuel Kurulum
1. [GitHub Releases](https://github.com/QuickCodeNet/quickcode.cli/releases/latest) sayfasından en son sürümü indir
2. Platformunuza uygun arşivi çıkar:
   - **macOS (Apple Silicon)**: `quickcode-cli-osx-arm64-v*.tar.gz`
   - **macOS (Intel)**: `quickcode-cli-osx-x64-v*.tar.gz`
   - **Windows (x64)**: `quickcode-cli-win-x64-v*.zip`
   - **Windows (ARM64)**: `quickcode-cli-win-arm64-v*.zip`
3. Çıkar ve executable'ı PATH'e ekle
4. Kurulumu doğrula: `quickcode --version`

### Geliştirme Derlemesi
Kaynak koddan derlemek istersen .NET SDK 10.0.100+ gerekir:
```bash
git clone https://github.com/QuickCodeNet/quickcode.cli.git
cd quickcode.cli
dotnet build
dotnet run --project src/QuickCode.Cli -- --help
```

---

## Hızlı Başlangıç
CLI’yi repository kökünden (önerilir) veya proje klasörü içinden çalıştırabilirsin. Aşağıdaki komutlar tüm akışı kapsar.

### A Seçeneği – Kurulu binary kullan (önerilen)
```bash
# 1. Yardım
quickcode --help

# 2. Projenin var olup olmadığını kontrol et
quickcode demo check

# 3. Proje oluştur (yoksa)
quickcode demo create --email demo@quickcode.net
# Not: Proje oluşturulduğunda email otomatik olarak local konfige kaydedilir.

# 4. Secret code'u kaydet (email'inizde gelen secret code'u kullanın)
quickcode demo config --set secret_code=SECRET123

# 5. Konfigürasyonu doğrula
quickcode config validate
quickcode demo validate

# 6. Projedeki modülleri listele
quickcode demo modules

# 7. Yeni modül ekle (--module-name dışındaki tüm parametreler opsiyonel, varsayılan değerlere sahip)
quickcode demo modules add --module-name UserManager
# Veya tüm parametrelerle açıkça:
quickcode demo modules add --module-name ProductModule --template-key UserManager --db-type mssql --pattern Service
# Modül adı PascalCase formatında olmalı: harf ile başlamalı, rakamdan sonra büyük harf kullanılmalı (örn: SmsModule2Test)
# Varsayılan değerler: --template-key=Empty, --db-type=mssql, --pattern=Service
# Geçerli db-type değerleri: mssql, mysql, postgresql
# Geçerli pattern değerleri: Service, CqrsAndMediator
# Not: Modül eklerken template DBML'i otomatik olarak local'e indirilir ve kaydedilir

# 8. Modül sil
quickcode demo modules remove --module-name UserManager
# Modül adı PascalCase formatında olmalı: harf ile başlamalı, rakamdan sonra büyük harf kullanılmalı (örn: SmsModule2Test)

# 9. Proje & template DBML dosyalarını indir
quickcode demo get-dbmls
# `quickcode demo get-dbmls` komutu aynı klasöre güncel `README.md` dosyasını da indirir; böylece dokümana çevrimdışı erişebilirsin.

# 10. DBML dosyalarını API'ye yükle
quickcode demo update-dbmls

# 11. Generate + watch
# Proje için kod üretimini başlatır. Varsayılan olarak ilerlemeyi gerçek zamanlı izler.
quickcode demo generate
# Veya izlemeyi devre dışı bırak:
quickcode demo generate --watch false

# 12. Diğer yararlı komutlar
quickcode templates                    # Mevcut modül şablonlarını listele
quickcode demo forgot-secret           # Secret code hatırlatma iste
quickcode demo verify-secret           # Email + secret kombinasyonunu doğrula
quickcode demo remove                  # Kayıtlı bilgileri ve local DBML klasörünü sil

# 13. Projeyi GitHub'dan indir veya güncelle
quickcode demo pull

# 14. Projedeki değişiklikleri GitHub'a push et
quickcode demo push
```

### B Seçeneği – Geliştirme modu (kaynak koddan)
> Geliştirme için veya binary kurmamışsan kullan.

```bash
cd /path/to/quickcode.cli

# 1. Yardım
dotnet run --project src/QuickCode.Cli -- --help

# 2. Projenin var olup olmadığını kontrol et
dotnet run --project src/QuickCode.Cli -- demo check

# 3. Proje oluştur (yoksa)
dotnet run --project src/QuickCode.Cli -- demo create --email demo@quickcode.net
# Not: Proje oluşturulduğunda email otomatik olarak local konfige kaydedilir.

# 4. Secret code'u kaydet (email'inizde gelen secret code'u kullanın)
dotnet run --project src/QuickCode.Cli -- demo config --set secret_code=SECRET123

# 5. Konfigürasyonu doğrula
dotnet run --project src/QuickCode.Cli -- config validate
dotnet run --project src/QuickCode.Cli -- demo validate

# 6. Projedeki modülleri listele
dotnet run --project src/QuickCode.Cli -- demo modules

# 7. Yeni modül ekle (--module-name dışındaki tüm parametreler opsiyonel, varsayılan değerlere sahip)
dotnet run --project src/QuickCode.Cli -- demo modules add --module-name UserManager
# Veya tüm parametrelerle açıkça:
dotnet run --project src/QuickCode.Cli -- demo modules add --module-name ProductModule --template-key UserManager --db-type mssql --pattern Service
# Modül adı PascalCase formatında olmalı: harf ile başlamalı, rakamdan sonra büyük harf kullanılmalı (örn: SmsModule2Test)
# Varsayılan değerler: --template-key=Empty, --db-type=mssql, --pattern=Service
# Geçerli db-type değerleri: mssql, mysql, postgresql
# Geçerli pattern değerleri: Service, CqrsAndMediator
# Not: Modül eklerken template DBML'i otomatik olarak local'e indirilir ve kaydedilir

# 8. Modül sil
dotnet run --project src/QuickCode.Cli -- demo modules remove --module-name UserManager
# Modül adı PascalCase formatında olmalı: harf ile başlamalı, rakamdan sonra büyük harf kullanılmalı (örn: SmsModule2Test)

# 9. Proje & template DBML dosyalarını indir
dotnet run --project src/QuickCode.Cli -- demo get-dbmls

# 10. DBML dosyalarını API'ye yükle
dotnet run --project src/QuickCode.Cli -- demo update-dbmls

# 11. Generate + watch
# Proje için kod üretimini başlatır. Varsayılan olarak ilerlemeyi gerçek zamanlı izler.
dotnet run --project src/QuickCode.Cli -- demo generate
# Veya izlemeyi devre dışı bırak:
dotnet run --project src/QuickCode.Cli -- demo generate --watch false

# 6. Demo projesini GitHub'dan indir veya güncelle
dotnet run --project src/QuickCode.Cli -- demo pull

# 7. Demo projesindeki değişiklikleri GitHub'a push et
dotnet run --project src/QuickCode.Cli -- demo push
```

---

## Konfigürasyon Kuralları
- API adresi varsayılan olarak `https://api.quickcode.net/`. Farklı bir backend hedefliyorsan `config --set api_url=...` ile güncelle.
- Her proje için `email` ve `secret_code` mutlaka `config --project <isim>` ile kaydedilmeli.
- Tüm komutlar proje adını açıkça ister; default_* yok.
- Konfigürasyon dosyası `~/.quickcode/config.json`.
- **Güvenlik**: Secret code'lar otomatik olarak AES-256 şifreleme ile şifrelenir. Şifreleme anahtarı `~/.quickcode/.key` dosyasında kısıtlı izinlerle (600) saklanır.
- Tüm projeleri kontrol etmek için `config validate`, belirli bir projeyi kontrol etmek için `project validate --name <proje>` kullan.

---

## Komut Referansı

| Komut | Açıklama | Örnek |
|-------|----------|-------|
| `config --set api_url=...` | API adresini değiştir | `quickcode config --set api_url=https://api.quickcode.net/` |
| `config --project demo --set email=... secret_code=...` | Proje email + secret kaydet (secret_code şifrelenir) | `quickcode config --project demo --set email=demo@quickcode.net secret_code=SECRET123` |
| `config validate` | Tüm proje konfigürasyonlarını doğrula | `quickcode config validate` |
| `project create` | Proje oluştur / secret maili | `quickcode project create --name demo --email demo@quickcode.net` |
| `project check` | Proje var mı kontrol et | `quickcode project check --name demo` |
| `project forgot-secret` | Secret kod maili gönder | `quickcode project forgot-secret --name demo --email demo@quickcode.net` |
| `project verify-secret` | Email + secret doğrula | `quickcode project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123` |
| `project validate --name <proje>` | Belirli bir proje konfigürasyonunu doğrula | `quickcode project validate --name demo` |
| `project get-dbmls --name <proje>` | Proje modüllerini, README.md dosyasını ve template modüllerini ilgili klasörlere indir | `quickcode project get-dbmls --name demo` |
| `project update-dbmls --name <proje>` | Proje klasöründeki tüm DBML dosyalarını API'ye yükle | `quickcode project update-dbmls --name demo` |
| `module available/list/...` | Modül yönetimi | `quickcode module list --project demo` |
| `generate [--watch]` | Generate başlat ve istersen izle | `quickcode generate demo --watch` |
| `status --session-id` | Session ID ile durumu sorgula | `quickcode status --session-id <id>` |
| `<proje> pull` | Projeyi GitHub'dan indir veya güncelle | `quickcode demo pull` |
| `<proje> push` | Projedeki değişiklikleri GitHub'a push et | `quickcode demo push` |

---

## Watcher Davranışı
- SignalR hub: `/quickcodeHub?sessionId=...`
- 5 saniye SignalR güncellemesi gelmezse CLI otomatik HTTP status check yapar.
- Tüm adımlar bitince “✅ Generation completed” mesajı basıp watcher’dan çıkar.
- Süre etiketleri web arayüzüyle aynı (örn. `12.3s`, `5m 8s`, `3h 2m 1s`, `...`).

---

## Güvenlik Özellikleri
- **Şifrelenmiş Secret'lar**: Secret code'lar konfigürasyon dosyasına kaydedilmeden önce AES-256 şifreleme ile otomatik olarak şifrelenir.
- **Şifreleme Anahtarı**: Şifreleme anahtarı `~/.quickcode/.key` dosyasında kısıtlı dosya izinleriyle (Unix/macOS'ta 600) saklanır.
- **Otomatik Migrasyon**: Mevcut düz metin secret'lar ilk yüklemede otomatik olarak şifrelenir.
- **Validasyon**: Eksik kimlik bilgilerini kontrol etmek için `config validate` veya `project validate --name <proje>` kullan.

## Ek Notlar
- Türkçe doküman: `README.tr.md`
- İngilizce doküman: [`README.md`](README.md)
- Daha fazla log görmek için `--verbose` bayrağını kullanabilirsin.
- Secret code'lar asla düz metin olarak gösterilmez; config görüntülenirken `********` olarak görünür.

