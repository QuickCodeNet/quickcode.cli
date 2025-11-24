# QuickCode.Cli

QuickCode.Cli, QuickCode web arayüzündeki tüm akışı terminale taşıyan .NET 10 tabanlı CLI’dir. Proje oluşturma, modül yönetimi, generate ve SignalR üzerinden izlemeyi Python yerine doğrudan C# ile yapar.

- Varsayılan README dili: **İngilizce** (`README.md`)
- Türkçe sürüm: bu dosya (`README.tr.md`)

---

## Ön Koşullar
- .NET SDK 10.0.100+ (`dotnet --version`)
- `https://api.quickcode.net/` erişimi
- Proje bazlı email + secret bilgileri (veya `project forgot-secret` ile e-posta yoluyla edinme)

---

## Hızlı Başlangıç
CLI’yi repository kökünden (önerilir) veya proje klasörü içinden çalıştırabilirsin. Aşağıdaki komutlar tüm akışı kapsar.

### A Seçeneği – Repository kökünden çalıştır
```bash
cd /Users/uzeyirapaydin/Documents/Projects/quickcode-generator

# 1. Yardım
dotnet run --project QuickCode.Cli -- --help

# 2. Proje bilgilerini kaydet
dotnet run --project QuickCode.Cli -- config --project demo --set email=demo@quickcode.net
dotnet run --project QuickCode.Cli -- config --project demo --set secret_code=SECRET123

# 3. Proje işlemleri
dotnet run --project QuickCode.Cli -- project create --name demo --email demo@quickcode.net
dotnet run --project QuickCode.Cli -- project check --name demo
dotnet run --project QuickCode.Cli -- project forgot-secret --name demo --email demo@quickcode.net
dotnet run --project QuickCode.Cli -- project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123

# 4. Modül örnekleri
dotnet run --project QuickCode.Cli -- module list --project demo
dotnet run --project QuickCode.Cli -- module available

# 5. Generate + watch
dotnet run --project QuickCode.Cli -- generate demo --watch
```

### B Seçeneği – Proje klasöründen çalıştır
> `QuickCode.Cli` klasörüne girersen `dotnet run -- ...` komutları daha kısa olur.

```bash
cd /Users/uzeyirapaydin/Documents/Projects/quickcode-generator/QuickCode.Cli

# 1. Yardım
dotnet run -- --help

# 2. Proje bilgileri
dotnet run -- config --project demo --set email=demo@quickcode.net
dotnet run -- config --project demo --set secret_code=SECRET123

# 3. Proje işlemleri
dotnet run -- project create --name demo --email demo@quickcode.net
dotnet run -- project check --name demo
dotnet run -- project forgot-secret --name demo --email demo@quickcode.net
dotnet run -- project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123

# 4. Modül + generate
dotnet run -- module list --project demo
dotnet run -- generate demo --watch
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
| `config --set api_url=...` | API adresini değiştir | `dotnet run --project QuickCode.Cli -- config --set api_url=https://api.quickcode.net/` |
| `config --project demo --set email=... secret_code=...` | Proje email + secret kaydet | `dotnet run --project QuickCode.Cli -- config --project demo --set email=demo@quickcode.net secret_code=SECRET123` |
| `project create` | Proje oluştur / secret maili | `dotnet run --project QuickCode.Cli -- project create --name demo --email demo@quickcode.net` |
| `project check` | Proje var mı kontrol et | `dotnet run --project QuickCode.Cli -- project check --name demo` |
| `project forgot-secret` | Secret kod maili gönder | `dotnet run --project QuickCode.Cli -- project forgot-secret --name demo --email demo@quickcode.net` |
| `project verify-secret` | Email + secret doğrula | `dotnet run --project QuickCode.Cli -- project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123` |
| `module available/list/...` | Modül yönetimi | `dotnet run --project QuickCode.Cli -- module list --project demo` |
| `generate [--watch]` | Generate başlat ve istersen izle | `dotnet run --project QuickCode.Cli -- generate demo --watch` |
| `status --session-id` | Session ID ile durumu sorgula | `dotnet run --project QuickCode.Cli -- status --session-id <id>` |

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

