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
brew tap uzeyirapaydin/quickcode-cli
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
scoop bucket add quickcode-cli https://github.com/uzeyirapaydin/scoop-bucket
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

# 2. Proje bilgilerini kaydet
quickcode demo config --set email=demo@quickcode.net
quickcode demo config --set secret_code=SECRET123

# 2a. Konfigürasyonu doğrula
quickcode config validate
quickcode project validate --name demo

# 3. Proje işlemleri (proje adı önde)
quickcode demo create --email demo@quickcode.net
quickcode demo check
quickcode demo forgot-secret [--email demo@quickcode.net]
quickcode demo verify-secret [--email demo@quickcode.net --secret-code SECRET123]
quickcode demo get-dbmls
quickcode demo update-dbmls
quickcode demo validate
quickcode demo remove

`quickcode demo get-dbmls` komutu aynı klasöre güncel `README.md` dosyasını da indirir; böylece dokümana çevrimdışı erişebilirsin.

# 4. Modül örnekleri
quickcode demo modules
quickcode templates

# 5. Generate + watch
quickcode demo generate --watch
```

### B Seçeneği – Geliştirme modu (kaynak koddan)
> Geliştirme için veya binary kurmamışsan kullan.

```bash
cd /path/to/quickcode.cli

# 1. Yardım
dotnet run --project src/QuickCode.Cli -- --help

# 2. Proje bilgileri
dotnet run --project src/QuickCode.Cli -- demo config --set email=demo@quickcode.net
dotnet run --project src/QuickCode.Cli -- demo config --set secret_code=SECRET123

# 2a. Konfigürasyonu doğrula
dotnet run --project src/QuickCode.Cli -- config validate
dotnet run --project src/QuickCode.Cli -- project validate --name demo

# 3. Proje işlemleri (proje adı önde)
dotnet run --project src/QuickCode.Cli -- demo create --email demo@quickcode.net
dotnet run --project src/QuickCode.Cli -- demo check
dotnet run --project src/QuickCode.Cli -- demo forgot-secret --email demo@quickcode.net
dotnet run --project src/QuickCode.Cli -- demo verify-secret --email demo@quickcode.net --secret-code SECRET123
dotnet run --project src/QuickCode.Cli -- demo get-dbmls
dotnet run --project src/QuickCode.Cli -- demo update-dbmls
dotnet run --project src/QuickCode.Cli -- demo validate

# 4. Modül örnekleri
dotnet run --project src/QuickCode.Cli -- demo modules
dotnet run --project src/QuickCode.Cli -- templates

# 5. Generate + watch
dotnet run --project src/QuickCode.Cli -- demo generate --watch
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

