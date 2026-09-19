# Enix Fit: секреты и локальный Docker

Все команды выполняются из корня репозитория `E:\Net\enx-fit`.

## Разработка без Docker

В проекте уже настроен `UserSecretsId`. В Visual Studio можно выбрать проект → «Управление секретами пользователя» и добавить:

```json
{
  "Database:Password": "пароль вашей локальной PostgreSQL"
}
```

Либо в PowerShell 7 ввести пароль скрыто и передать JSON через stdin, чтобы пароль не попал в историю команд:

```powershell
$dbPassword = Read-Host 'Пароль локальной PostgreSQL' -MaskInput
try {
    @{ 'Database:Password' = $dbPassword } | ConvertTo-Json -Compress | dotnet user-secrets set --project enx-fit/enx-fit.csproj
} finally {
    Remove-Variable dbPassword
}
dotnet run --project enx-fit/enx-fit.csproj --launch-profile https
```

User Secrets хранит данные вне репозитория в профиле пользователя, без шифрования, и используется только в Development. Он не устанавливает и не меняет пароль самой PostgreSQL — укажите уже действующий пароль. Из отслеживаемых appsettings пароль удалён; прежний действующий пароль нужно сменить в самой БД, удаление из файла не очищает историю Git.

Хост, имя БД и пользователь остаются в строке `ConnectionStrings:DefaultConnection`. Локальный `appsettings.Local.json`, если существует, переопределяет обычные appsettings. User Secrets загружается после него; переменные окружения и аргументы запуска — после User Secrets. Можно переопределить и всю строку подключения через User Secrets.

Разрешение пароля: `Database:PasswordFile` → `Database:Password` → пароль в строке подключения. Для окружения имена ключей: `Database__PasswordFile`, `Database__Password`, `ConnectionStrings__DefaultConnection`. Отсутствующий или пустой указанный файл секрета останавливает запуск, без перехода на старый пароль.

## Первый запуск в Docker

1. Установите [Docker Desktop для Windows](https://docs.docker.com/desktop/setup/install/windows-install/), включите WSL 2 и режим Linux containers. Запустите Docker Desktop и откройте новый терминал.
2. Проверьте `docker version` и `docker compose version`.
3. Создайте отдельный случайный пароль для новой контейнерной БД (PowerShell 7):

```powershell
New-Item -ItemType Directory -Force .secrets | Out-Null
if (Test-Path -LiteralPath .secrets/db_password.txt) {
    throw 'Секрет уже существует. Не перезаписывайте пароль существующей БД.'
}
$secretPath = Join-Path (Get-Location).Path '.secrets/db_password.txt'
[IO.File]::WriteAllText($secretPath, [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)))
```

4. Соберите и запустите:

```powershell
docker compose config --quiet
docker compose up -d --build
docker compose ps
docker compose logs --tail 100 web
```

Откройте [http://localhost:8080](http://localhost:8080). Первая сборка скачивает образы и NuGet-пакеты. При старте приложение применяет миграции к новой отдельной БД и создаёт таблицы. Данные из PostgreSQL на Windows автоматически не переносятся. Первый зарегистрированный аккаунт в пустой БД получает роль администратора.

## Что запускается

- `Dockerfile` собирает приложение SDK .NET 10, затем помещает результат в меньший образ ASP.NET Core. Процесс приложения работает от пользователя `app`.
- `compose.yaml` запускает два контейнера: сайт `web` и PostgreSQL 17 `db`. Внутри сети Compose имя `db` является адресом БД.
- Один секрет монтируется обоим контейнерам как `/run/secrets/db_password`. PostgreSQL читает его через `POSTGRES_PASSWORD_FILE`; приложение — через добавленный `Database:PasswordFile`.
- `.secrets/` исключён из Git и контекста сборки Docker. Development/Local appsettings также исключены из образа. Compose secrets здесь — файлы на хосте, а не зашифрованное хранилище: ограничьте доступ к папке средствами ОС.
- База и ключи авторизации хранятся в отдельных именованных volumes и сохраняются при пересоздании контейнеров. Это не заменяет резервные копии.
- Порт БД не публикуется. Сайт доступен только с этого компьютера на `127.0.0.1:8080`.

## Остановка и обновление

```powershell
docker compose stop
docker compose start
# После изменения исходников:
docker compose up -d --build
# Удалить контейнеры и сеть, сохранив volumes:
docker compose down
```

Не добавляйте `-v` к `down`, если данные нужны: это удаляет volumes с БД и ключами.

Изменение файла пароля после первого запуска не меняет пароль существующей PostgreSQL. Для ротации сначала смените пароль роли в БД (например, интерактивной командой `\password enix_fit` в `docker compose exec db psql -U enix_fit -d enix_fit`), затем согласованно обновите файл и пересоздайте контейнеры. Не удаляйте БД ради смены пароля.

## Перед публикацией

Это конфигурация для локальной проверки контейнеров. HTTPS здесь не настроен; приложение может предупреждать, что порт HTTPS для перенаправления не определён. Для интернета нужны домен, TLS на reverse proxy и корректная обработка forwarded headers только от доверенного proxy. Не включайте доверие всем proxy без ограничения сети.

В этой локальной схеме `POSTGRES_USER` создаёт суперпользователя. Для публичной установки выделите приложению отдельную роль без SUPERUSER и отделите применение миграций от обычного запуска. Настройте резервное копирование и восстановление PostgreSQL, доступ к ключам Data Protection, почту и защиту входа. Создайте свой аккаунт администратора до открытия регистрации извне.

Источники: [Microsoft User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-10.0), [Docker Compose secrets](https://docs.docker.com/compose/how-tos/use-secrets/), [официальный образ PostgreSQL](https://hub.docker.com/_/postgres).
