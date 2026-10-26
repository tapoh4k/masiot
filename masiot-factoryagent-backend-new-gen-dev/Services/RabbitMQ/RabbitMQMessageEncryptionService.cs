using Backend.Services;

namespace Backend.Services.RabbitMQ;

/// <summary>
/// Сервис сквозного шифрования сообщений RabbitMQ (E2EE).
/// Использует AES-256-CBC через внутренний IEncryptionService.
/// Формат зашифрованного сообщения: ENC:[base64_payload]
/// </summary>
public interface IRabbitMQMessageEncryptionService
{
    string EncryptPayload(string plainText);
    string DecryptPayload(string cipherText);
    bool IsEncrypted(string payload);
}

public class RabbitMQMessageEncryptionService : IRabbitMQMessageEncryptionService
{
    private readonly IEncryptionService _encryptionService;
    private readonly bool _enabled;

    public RabbitMQMessageEncryptionService(IEncryptionService encryptionService, IConfiguration configuration)
    {
        _encryptionService = encryptionService;
        _enabled = configuration.GetValue<bool>("RabbitMQ:EncryptMessages", true);
    }

    public string EncryptPayload(string plainText)
    {
        if (!_enabled || string.IsNullOrEmpty(plainText))
            return plainText;

        var encrypted = _encryptionService.Encrypt(plainText);
        return $"ENC:[{encrypted}]";
    }

    public string DecryptPayload(string cipherText)
    {
        if (!_enabled || string.IsNullOrEmpty(cipherText))
            return cipherText;

        if (!IsEncrypted(cipherText))
            return cipherText; // Обратная совместимость с незашифрованными сообщениями

        // Извлекаем содержимое между ENC:[ и ]
        var prefix = "ENC:[";
        var suffix = "]";
        if (cipherText.StartsWith(prefix) && cipherText.EndsWith(suffix))
        {
            var base64 = cipherText.Substring(prefix.Length, cipherText.Length - prefix.Length - suffix.Length);
            return _encryptionService.Decrypt(base64);
        }

        return cipherText;
    }

    public bool IsEncrypted(string payload)
    {
        return payload?.StartsWith("ENC:[") == true && payload?.EndsWith("]") == true;
    }
}
