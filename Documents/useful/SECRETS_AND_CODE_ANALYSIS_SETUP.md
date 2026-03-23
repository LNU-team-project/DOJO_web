SECRETS AND CODE ANALYSIS — Налаштування для команди

Коротко: цей файл містить покрокову інструкцію українською для всієї команди — як увімкнути TreatWarningsAsErrors у проєкті, що означає <WarningLevel>4</WarningLevel>, як налаштувати SonarLint у Rider, як користуватись dotnet user-secrets та .env, і як уникнути потрапляння секретів у git.

Чекліст (що буде зроблено в інструкції):
- [ ] Додати/пояснити <TreatWarningsAsErrors> і <WarningLevel> у .csproj.
- [ ] Пояснити різницю між WarningLevel 0..4, чому 4.
- [ ] Показати як встановити та зв'язати SonarLint у Rider.
- [ ] Показати як використовувати dotnet user-secrets (UserSecretsId, команди set/list).
- [ ] Надати альтернативу через .env та .gitignore приклади.
- [ ] Дати поради для передачі секретів команді і для CI (GitHub Actions).

1) TreatWarningsAsErrors — як додати (щоб працювало у всіх учасників)

Щоб зробити попередження компілятора помилками для всього проєкту, змініть ваш .csproj (кореневий проєкт або проект Presentation/src): додайте в один із <PropertyGroup> наступне (комітиться у репозиторій):

```xml
<!-- ...existing code... -->
<PropertyGroup>
  <!-- Попередження стають помилками -->
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <!-- Рівень показуваних попереджень: 4 — максимальний штатний рівень -->
  <WarningLevel>4</WarningLevel>
</PropertyGroup>
<!-- ...existing code... -->
```

Якщо хочемо ввімкнути лише для конкретної конфігурації (наприклад, Release):

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

Використання у команді: цей рядок у .csproj вплине на всіх розробників і CI, тому достатньо закомітити зміни.

2) Що означає <WarningLevel>4</WarningLevel>

- WarningLevel встановлює глибину повідомлень компілятора (ціле значення 0..4).
  - 0 — не показувати попередження.
  - 1 — мінімум попереджень.
  - 4 — показуються всі стандартні попередження C# (звичний "максимальний" рівень).
- Рівень 4 — типовий вибір для строгого контролю якості. Він показує найбільшу кількість попереджень, тож у поєднанні з TreatWarningsAsErrors проєкт стане більш чистим, але може потребувати виправлення багатьох дрібних попереджень.

Як тимчасово або локально відключити окремі попередження (якщо вони заважають):

```xml
<PropertyGroup>
  <!-- Вказуємо коди попереджень, які НЕ перетворюємо на помилки -->
  <WarningsNotAsErrors>CS1591;CS0168</WarningsNotAsErrors>
  <!-- або повністю приглушити певні попередження -->
  <NoWarn>CS1591;CS0168</NoWarn>
</PropertyGroup>
```

3) SonarLint у Rider — локальний аналіз + зв'язування з SonarCloud

Кроки (для кожного учасника):
1. Відкрити Rider → Plugins → Marketplace → знайти "SonarLint" → Install → Restart IDE.
2. Відкрити View → Tool Windows → SonarLint (або Settings → Tools → SonarLint).
3. Рекомендується увімкнути Connected Mode (зв'язати з SonarCloud) щоб правила були однакові з сервером:
   - У SonarCloud згенерувати token (My Account → Security → Generate Tokens).
   - У Rider → Settings → Tools → SonarLint → Bind to SonarQube/SonarCloud → ввести URL (https://sonarcloud.io) і token → вибрати проект.
4. Після зв'язування локальний SonarLint використовуватиме ті самі правила, що й SonarCloud.

Поради:
- Якщо хтось не може/не хоче зв'язуватися з SonarCloud, встановлення SonarLint все одно дасть локальний аналіз.
- Якщо у SonarCloud є кастомні правила, їх бачать тільки ті, хто зв'язаний (Connected Mode).

4) dotnet user-secrets — рекомендований шлях для локальних секретів

Чому user-secrets:
- Секрети зберігаються в профілі розробника (поза репозиторієм).
- Добре інтегрується з ASP.NET Core і IConfiguration (автоматично зчитується у Development середовищі).

A) Додати UserSecretsId у .csproj (комітити у репозиторій):

```xml
<!-- ...existing code... -->
<PropertyGroup>
  <UserSecretsId>dojo-web-dev-secrets</UserSecretsId>
</PropertyGroup>
<!-- ...existing code... -->
```

UserSecretsId не містить секретів — це лише ідентифікатор набору секретів.

B) Команди (кожен розробник виконує локально):

```bash
# у папці з .csproj (де вказано UserSecretsId)
# перевірити список
dotnet user-secrets list

# додати SendGrid ключ
dotnet user-secrets set "SendGrid:SendGridKey" "<ВАШ_SENDGRID_KEY>"

# додати рядок підключення
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=dojo_web;Username=postgres;Password=<ВАШ_ПАРОЛЬ>"
```

Де зберігаються файли локальних секретів:
- macOS/Linux: ~/.microsoft/usersecrets/<UserSecretsId>/secrets.json
- Windows: %APPDATA%/Microsoft/UserSecrets/<UserSecretsId>/secrets.json

C) Перевірка: після додавання запустіть додаток у середовищі Development — IConfiguration автоматично підхопить ці значення.

5) .env — альтернатива

Можна використовувати .env файли (чисто локально) або змінні середовища. Якщо використовуєте .env — ОБОВ'ЯЗКОВО додайте його в .gitignore.

Приклад .env (не додавати у git):

```
SENDGRID__SENDGRIDKEY=SG.xxxxxxxxx
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=dojo_web;Username=postgres;Password=...
```

У ASP.NET Core можна додати провайдер, який читає .env або використовувати dotnet-dotenv бібліотеку. Але звичайні змінні середовища теж працюють.

6) .gitignore — приклади рядків

Щоб уникнути випадкового відправлення локальних файлів із секретами, додайте у `.gitignore` (файл у корені репозиторію):

```
# локальні секрети
.env
# локальні user-secrets (в профілі користувача) — додатковий захист
.microsoft/usersecrets/
# наш локальний HOWTO (якщо не хочемо його в репозиторій)
SECRETS_SETUP.md
SECRETS_AND_CODE_ANALYSIS_SETUP.md
```

Примітка: комітити `.gitignore` — так, щоб усі учасники мали однакові правила ігнорування.

7) Передача секретів команді і CI

- Не передавайте секрети через git.
- Використовуйте безпечні канали (Signal, менеджери паролів, зашифровані таски у трекері) щоб передати ключі іншим розробникам.
- Кожен розробник додає секрети локально через `dotnet user-secrets set` або через .env.

Для CI (наприклад GitHub Actions): зберігайте секрети у GitHub Repository Secrets або у зовнішньому сховищі (Azure Key Vault) і передавайте в workflow як змінні оточення.

8) Приклади: виключити одну помилку з TreatWarningsAsErrors

Якщо потрібно, щоб більшість попереджень були помилками, але деякі конкретні — ні:

```xml
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsNotAsErrors>CS1591;CS1597</WarningsNotAsErrors>
</PropertyGroup>
```

9) Troubleshooting

- Якщо після додавання TreatWarningsAsErrors збірка валиться локально — запустіть `dotnet build` або збірку у Rider, подивіться список помилок/попереджень і виправте або тимчасово додайте потрібні коди у `WarningsNotAsErrors`.
- Якщо SonarLint не показує правил з SonarCloud — перевірте, чи правильно зробили Binding і чи токен не прострочений.
- Якщо секрети не підхоплюються — впевніться, що запущено у середовищі `Development` і що UserSecretsId у .csproj співпадає з тим, що ви використовуєте.

10) Короткий чекліст для нового учасника

- [ ] Pull останні зміни з репозиторію (має містити зміни .csproj з TreatWarningsAsErrors).
- [ ] Встановити .NET SDK, відкрити рішення в Rider.
- [ ] Встановити плагін SonarLint у Rider (і, за бажанням, зв'язати з SonarCloud).
- [ ] Отримати локальні секрети через безпечний канал.
- [ ] Виконати `dotnet user-secrets set ...` у корені відповідного проєкту.
- [ ] Побудувати проєкт (`dotnet build`) і виправити помилки чи попередження.

---

Якщо потрібно, можу тепер:
- додати цей файл у `.gitignore` і закомітити зміни;
- автоматично додати PropertyGroup у `.csproj` (покажу diff перед комітом);
- зробити приклад GitHub Actions workflow, який використовує repository secrets.

Кращий наступний крок — скажи, чи хочеш, щоб я додав рядки в `.gitignore` і/або вніс <TreatWarningsAsErrors> прямо у проєктні .csproj файли (я можу знайти їх і запропонувати зміни).
