# 🔐 Налаштування User Secrets для локального розвитку

## Вступ

Цей проект використовує **User Secrets Manager** для безпечного зберігання чутливих даних (пароли БД, API ключі тощо) **локально** на вашій машині. Ці дані **не потрапляють в Git репозиторій**.

## 📋 Обов'язкові секрети для развитку

1. **Connection String для PostgreSQL БД**
2. **SendGrid API Key** (для відправки email)

## 🚀 Як налаштувати User Secrets

### Крок 1: Перейти в папку проекту

```bash
cd src/
```

### Крок 2: Встановити секрет для Connection String

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=dojo_web;Username=postgres;Password=YOUR_DB_PASSWORD"
```

**Замініть `YOUR_DB_PASSWORD`** на реальний пароль вашої PostgreSQL БД.

### Крок 3: Встановити SendGrid API Key (опціонально)

```bash
dotnet user-secrets set "SendGrid:SendGridKey" "YOUR_SENDGRID_API_KEY"
```

**Замініть `YOUR_SENDGRID_API_KEY`** на реальний ключ (якщо він у вас є).

### Крок 4: Перевірити встановлені секрети

```bash
dotnet user-secrets list
```

## 📝 Де зберігаються User Secrets

User Secrets зберігаються **за межами проекту** в вашій системі:

- **macOS/Linux**: `~/.microsoft/usersecrets/dojo-web-dev-secrets/secrets.json`
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\dojo-web-dev-secrets\secrets.json`

## ⚠️ Важливо

- ✅ **User Secrets** розроблені **ТІЛЬКИ для development** середовища
- ✅ **Ніколи** не комітьте реальні пароліі та ключі
- ✅ Для **production** використовуйте Azure Key Vault або інші безпечні хранилища
- ✅ Якщо вам потрібна конфігурація без коду, дивіться **appsettings.example.json**

## 🔄 Як оновити секрет

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "нове_значення"
```

## 🗑️ Як видалити секрет

```bash
dotnet user-secrets remove "ConnectionStrings:DefaultConnection"
```

## ❓ Якщо щось не працює

### Проблема: "Configuration key not found"

**Рішення**: Переконайтесь, що ви встановили всі обов'язкові секрети (крок 2-3).

### Проблема: "User secrets are not loaded"

**Рішення**: Перевірте, що ви у **development** середовищі:
```bash
echo $ASPNETCORE_ENVIRONMENT  # має вивести "Development"
```

### Проблема: Видалити всі User Secrets

```bash
dotnet user-secrets clear
```

---

**Якщо виникли питання, звертайтесь до команди розробників!** 🚀

