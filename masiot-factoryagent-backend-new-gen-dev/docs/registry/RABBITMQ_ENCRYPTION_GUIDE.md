# Руководство по шифрованию сообщений RabbitMQ

**Продукт:** MAS-IOT  
**Версия:** 1.0.0  

---

## Назначение

Сквозное шифрование (End-to-End Encryption, E2EE) сообщений, передаваемых через RabbitMQ. Обеспечивает конфиденциальность данных при передаче между backend, агентами и внешними мониторами фабрики.

## Принцип работы

```
Отправитель                    RabbitMQ                     Получатель
┌─────────────┐           ┌─────────────┐           ┌─────────────┐
│  JSON       │ ──AES256──►│  ENC:[...]  │ ──TCP───►│  JSON       │
│  Payload    │  Encrypt   │  Зашифровано│          │  Payload    │
└─────────────┘           └─────────────┘           └─────────────┘
```

## Алгоритм

- **Шифр:** AES-256-CBC
- **Ключ:** 32 байта (256 бит)
- **IV:** 16 байт
- **Формат зашифрованного сообщения:** `ENC:[base64_payload]`

## Конфигурация

В `appsettings.json`:

```json
{
  "Encryption": {
    "Key": "YourSecretKey123456789012345678",
    "IV": "YourInitVector12"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": "5672",
    "Username": "guest",
    "Password": "guest",
    "EncryptMessages": true
  }
}
```

## Требования к интеграции (для монитора фабрики)

Чтобы сторонний монитор мог расшифровывать сообщения, необходимо:

1. Использовать **тот же ключ и IV**, что и в backend
2. Реализовать алгоритм AES-256-CBC с PKCS7 padding
3. Обрабатывать формат `ENC:[base64]`
4. Поддерживать обратную совместимость (незашифрованные сообщения пропускать как есть)

### Пример расшифровки на Python

```python
from Crypto.Cipher import AES
import base64

def decrypt_message(encrypted_text: str, key: str, iv: str) -> str:
    if not encrypted_text.startswith("ENC:[") or not encrypted_text.endswith("]"):
        return encrypted_text  # Незашифрованное сообщение
    
    b64 = encrypted_text[5:-1]  # Убираем ENC:[ и ]
    ciphertext = base64.b64decode(b64)
    
    cipher = AES.new(key.encode(), AES.MODE_CBC, iv.encode())
    plaintext = cipher.decrypt(ciphertext)
    
    # Удаление PKCS7 padding
    pad_len = plaintext[-1]
    return plaintext[:-pad_len].decode('utf-8')

# Использование
key = "YourSecretKey123456789012345678"
iv = "YourInitVector12"
message = "ENC:[SGVsbG8gV29ybGQ=...]"
json_payload = decrypt_message(message, key, iv)
```

### Пример расшифровки на C# (для .NET-монитора)

```csharp
using System.Security.Cryptography;

public string DecryptRabbitMessage(string encrypted)
{
    if (!encrypted.StartsWith("ENC:[") || !encrypted.EndsWith("]"))
        return encrypted;
    
    var b64 = encrypted.Substring(5, encrypted.Length - 6);
    var buffer = Convert.FromBase64String(b64);
    
    using var aes = Aes.Create();
    aes.Key = Encoding.UTF8.GetBytes("YourSecretKey123456789012345678");
    aes.IV = Encoding.UTF8.GetBytes("YourInitVector12");
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;
    
    var decryptor = aes.CreateDecryptor();
    using var ms = new MemoryStream(buffer);
    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
    using var sr = new StreamReader(cs);
    return sr.ReadToEnd();
}
```

## Отключение шифрования

В `appsettings.json` установите:
```json
"RabbitMQ": {
  "EncryptMessages": false
}
```

При этом сообщения будут передаваться в открытом виде (JSON). Не рекомендуется для production.

## Обратная совместимость

Если `EncryptMessages: true`, система:
- **Отправляет** все исходящие сообщения в формате `ENC:[...]`
- **Принимает** как зашифрованные (`ENC:[...]`), так и незашифрованные (plain JSON)

Это позволяет поэтапно внедрять шифрование без остановки системы.
