using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuickCode.Cli.Configuration;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _configPath;
    private readonly object _lock = new();
    private static readonly byte[] EncryptionKey = GetOrCreateEncryptionKey();

    public ConfigService()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configPath = Path.Combine(home, ".quickcode", "config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
    }

    private static byte[] GetOrCreateEncryptionKey()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var keyPath = Path.Combine(home, ".quickcode", ".key");
        var keyDir = Path.GetDirectoryName(keyPath)!;
        Directory.CreateDirectory(keyDir);

        if (File.Exists(keyPath))
        {
            return Convert.FromBase64String(File.ReadAllText(keyPath));
        }

        // Generate a new 256-bit key
        var key = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }

        // Save key with restricted permissions (Unix only)
        File.WriteAllText(keyPath, Convert.ToBase64String(key));
        try
        {
            File.SetAttributes(keyPath, FileAttributes.Hidden);
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                // Set permissions to 600 (owner read/write only)
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = "600 \"" + keyPath + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
        }
        catch
        {
            // Ignore if we can't set permissions
        }

        return key;
    }

    private static string EncryptSecret(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = EncryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    private static string DecryptSecret(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return cipherText;

        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = EncryptionKey;

            var iv = new byte[aes.BlockSize / 8];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch
        {
            // If decryption fails, assume it's plain text (for backward compatibility)
            return cipherText;
        }
    }

    public QuickCodeConfig Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_configPath))
            {
                return new QuickCodeConfig();
            }

            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<QuickCodeConfig>(json, SerializerOptions) ?? new QuickCodeConfig();
            
            // Migrate plain text secrets to encrypted format
            var needsSave = false;
            foreach (var (projectName, projectConfig) in config.Projects)
            {
                if (!string.IsNullOrWhiteSpace(projectConfig.SecretCode))
                {
                    // Check if it's already encrypted (base64 format, longer than typical secret)
                    // Encrypted secrets are base64 and typically longer
                    var isLikelyEncrypted = projectConfig.SecretCode.Length > 50 && 
                                           projectConfig.SecretCode.All(c => char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=');
                    
                    if (!isLikelyEncrypted)
                    {
                        // Likely plain text, encrypt it
                        projectConfig.SecretCode = EncryptSecret(projectConfig.SecretCode);
                        needsSave = true;
                    }
                    else
                    {
                        // Verify it can be decrypted (if not, it might be corrupted)
                        try
                        {
                            DecryptSecret(projectConfig.SecretCode);
                        }
                        catch
                        {
                            // Can't decrypt, might be corrupted - skip migration for this one
                        }
                    }
                }
            }
            
            if (needsSave)
            {
                Save(config);
            }
            
            return config;
        }
    }

    public void Save(QuickCodeConfig config)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(config, SerializerOptions);
            File.WriteAllText(_configPath, json);
        }
    }

    public (string projectName, string projectEmail, string secret) ResolveProjectCredentials(
        QuickCodeConfig config,
        string? projectName,
        string? email,
        string? secret)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new InvalidOperationException("Project name is required. Pass --project <name>.");
        }

        var projectConfig = config.Projects.TryGetValue(projectName, out var pConfig)
            ? pConfig
            : null;

        var resolvedEmail = email ?? projectConfig?.Email;
        if (string.IsNullOrWhiteSpace(resolvedEmail))
        {
            throw new InvalidOperationException("Project email is required. Pass --email or set it via 'config --project <name> --set email=...'.");
        }

        var encryptedSecret = secret ?? projectConfig?.SecretCode;
        if (string.IsNullOrWhiteSpace(encryptedSecret))
        {
            throw new InvalidOperationException("Project secret code is required. Pass --secret-code or set it via 'config --project <name> --set secret_code=...'.");
        }

        // Decrypt secret code if it's encrypted, otherwise use as-is (backward compatibility)
        var resolvedSecret = DecryptSecret(encryptedSecret);

        return (projectName, resolvedEmail, resolvedSecret);
    }

    public string EncryptSecretValue(string plainText)
    {
        return EncryptSecret(plainText);
    }
}

