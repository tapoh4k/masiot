# Ключи безопасности для Masiot 

## Ключи шифрования (AES-256)

### Encryption Key (Base64)
```
DgfleYb6u3u3KPWz8nRH+8oAo8tpakpQuo5Vhwjvcy4=
```

### Initialization Vector (Hex)
```
320665ba987d77ab52a6e8981f232c29
```

### Резервный ключ (Base64)
```
Bg5einM2oy++02VnPsHc9Ar3n9jqBl90T4qGVCpoURw=
```

---

## Как использовать

### 1. В appsettings.json (для разработки)
```json
{
  "Encryption": {
    "Key": "DgfleYb6u3u3KPWz8nRH+8oAo8tpakpQuo5Vhwjvcy4=",
    "IV": "320665ba987d77ab52a6e8981f232c29"
  }
}
```

### 2. В переменных окружения (для production)
```bash
ENCRYPTION_KEY=DgfleYb6u3u3KPWz8nRH+8oAo8tpakpQuo5Vhwjvcy4=
ENCRYPTION_IV=320665ba987d77ab52a6e8981f232c29
```

### 3. В Docker
Добавьте в docker-compose.yml:
```yaml
environment:
  - Encryption__Key=DgfleYb6u3u3KPWz8nRH+8oAo8tpakpQuo5Vhwjvcy4=
  - Encryption__IV=320665ba987d77ab52a6e8981f232c29
```

## Генерация новых ключей

Если нужно сгенерировать новые ключи:

```bash
# Encryption Key (Base64)
openssl rand -base64 32

# IV (Hex, 16 bytes)
openssl rand -hex 16
```

---

**Дата генерации:** 2026-03-07

