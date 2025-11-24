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

### Windows (Scoop) - Önerilen
```powershell
scoop bucket add quickcode-cli https://github.com/uzeyirapaydin/scoop-bucket
scoop install quickcode-cli
```

### Manuel Kurulum
1. [GitHub Releases](https://github.com/uzeyirapaydin/quickcode.cli/releases/latest) sayfasından en son sürümü indir
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
git clone https://github.com/uzeyirapaydin/quickcode.cli.git
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
quickcode config --project demo --set email=demo@quickcode.net
quickcode config --project demo --set secret_code=SECRET123

# 3. Proje işlemleri
quickcode project create --name demo --email demo@quickcode.net
quickcode project check --name demo
quickcode project forgot-secret --name demo --email demo@quickcode.net
quickcode project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123

# 4. Modül örnekleri
quickcode module list --project demo
quickcode module available

# 5. Generate + watch
quickcode generate demo --watch
```

### B Seçeneği – Geliştirme modu (kaynak koddan)
> Geliştirme için veya binary kurmamışsan kullan.

```bash
cd /path/to/quickcode.cli

# 1. Yardım
dotnet run --project src/QuickCode.Cli -- --help

# 2. Proje bilgileri
dotnet run --project src/QuickCode.Cli -- config --project demo --set email=demo@quickcode.net
dotnet run --project src/QuickCode.Cli -- config --project demo --set secret_code=SECRET123

# 3. Proje işlemleri
dotnet run --project src/QuickCode.Cli -- project create --name demo --email demo@quickcode.net
dotnet run --project src/QuickCode.Cli -- project check --name demo
dotnet run --project src/QuickCode.Cli -- project forgot-secret --name demo --email demo@quickcode.net
dotnet run --project src/QuickCode.Cli -- project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123

# 4. Modül + generate
dotnet run --project src/QuickCode.Cli -- module list --project demo
dotnet run --project src/QuickCode.Cli -- generate demo --watch
```

---

## Konfigürasyon Kuralları
- API adresi varsayılan olarak `https://api.quickcode.net/`. Farklı bir backend hedefliyorsan `config --set api_url=...` ile güncelle.
- Her proje için `email` ve `secret_code` mutlaka `config --project <isim>` ile kaydedilmeli.
- Tüm komutlar proje adını açıkça ister; default_* yok.
- Konfigürasyon dosyası `~/.quickcode/config.json`.

---

## Komut Referansı

| Komut | Açıklama | Örnek |
|-------|----------|-------|
| `config --set api_url=...` | API adresini değiştir | `quickcode config --set api_url=https://api.quickcode.net/` |
| `config --project demo --set email=... secret_code=...` | Proje email + secret kaydet | `quickcode config --project demo --set email=demo@quickcode.net secret_code=SECRET123` |
| `project create` | Proje oluştur / secret maili | `quickcode project create --name demo --email demo@quickcode.net` |
| `project check` | Proje var mı kontrol et | `quickcode project check --name demo` |
| `project forgot-secret` | Secret kod maili gönder | `quickcode project forgot-secret --name demo --email demo@quickcode.net` |
| `project verify-secret` | Email + secret doğrula | `quickcode project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123` |
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

## Ek Notlar
- Türkçe doküman: `README.tr.md`
- İngilizce doküman: [`README.md`](README.md)
- Daha fazla log görmek için `--verbose` bayrağını kullanabilirsin.

