# Сервіс Catalog

Веб-API контексту Catalog: позиції каталогу (запчастини й послуги), їхні описи, бренди та дерево
категорій. Працює з базою `CatalogDb` через Entity Framework Core: модель описано в коді, схему
створюють міграції, запити пишуться на LINQ to Entities.

## Шари та залежності

```mermaid
flowchart LR
    Api["Catalog.Api<br/>контролери, gRPC-сервіс, обробник помилок, хост"] --> Bll["Catalog.Bll<br/>сервіси, кеш, DTO, специфікації, валідатори"]
    Bll --> Dal["Catalog.Dal<br/>DbContext, міграції, репозиторії, Unit of Work"]
    Bll --> Domain["Catalog.Domain<br/>сутності, винятки"]
    Bll --> Redis[("Redis<br/>L2-кеш")]
    Dal --> Domain
    Dal --> Db[("CatalogDb")]
```

Залежності спрямовані в один бік. Контролер і gRPC-сервіс звертаються лише до сервісу BLL, сервіс —
лише до Unit of Work; `CatalogDbContext` використовують тільки `Catalog.Dal` і стартовий код у
`Program.cs` (реєстрація, застосування міграцій, початкове наповнення).

| Проєкт | Вміст |
|--------|-------|
| `Catalog.Api` | контролери, `Grpc/CatalogGrpcService`, `Protos/catalog.proto`, `GlobalExceptionHandler`, `Program.cs`, `appsettings.json` |
| `Catalog.Bll` | сервіси та їхні інтерфейси, `Caching/` (дворівневий кеш), DTO, профілі AutoMapper, специфікації, валідатори FluentValidation |
| `Catalog.Dal` | `CatalogDbContext`, `Configurations/`, `Migrations/`, репозиторії, `UnitOfWork`, `Seeding/DbInitializer` |
| `Catalog.Domain` | сутності з бізнес-правилами, `DomainException`, `EntityNotFoundException`, `ConflictException`, `ValidationException` |
| `Catalog.Tests` | модульні тести сутностей, сервісів, специфікацій, валідаторів і мапінгу |

## Модель

Сутності не мають відкритих setter-ів: стан задається в конструкторі та змінюється лише методами,
які перевіряють правила й кидають `DomainException`.

| Сутність | Правила, які перевіряє сама сутність |
|----------|--------------------------------------|
| `CatalogItem` | ціна більша за нуль (`ChangePrice`); послуга не має бренду (`AssignBrand`); тривалість виконання задається лише для послуг (`Describe`); основна категорія одна (`AssignCategory`); видалену позицію не можна активувати (`Activate`); видалення м'яке (`MarkDeleted`) |
| `CatalogItemDetail` | гарантія від 0 до 60 місяців; тривалість додатна; характеристики — JSON-об'єкт |
| `Category` | slug без пробілів; порядок показу невід'ємний; категорія не може бути власною батьківською (`MoveTo`) |
| `Brand` | назва обов'язкова й не довша за 100 символів |
| `CatalogItemCategory` | створюється і змінюється лише через `CatalogItem.AssignCategory` |

| Тип | Зв'язок | Налаштування |
|-----|---------|--------------|
| 1:1 | `CatalogItem` ↔ `CatalogItemDetail` | `HasOne().WithOne().HasForeignKey<CatalogItemDetail>()`; ключ опису є водночас зовнішнім; каскад |
| 1:N | `Brand` → `CatalogItem`, `Category` → `Category` | `HasOne().WithMany()`; видалення заборонено, поки є залежні рядки |
| M:N | `CatalogItem` ↔ `Category` | явна проміжна сутність `CatalogItemCategory` зі складеним ключем і власними полями `AddedAt`, `IsPrimary`; каскад з обох боків |

Кожна сутність має окремий клас `IEntityTypeConfiguration<T>` у `Catalog.Dal/Configurations`;
`CatalogDbContext.OnModelCreating` підключає їх усі через `ApplyConfigurationsFromAssembly`.
Видалені позиції приховує глобальний фільтр `HasQueryFilter(i => !i.IsDeleted)`; такий самий фільтр
мають залежні від позиції опис і зв'язки з категоріями.

## Доступ до даних

`GenericRepository<TEntity, TKey>` працює через `DbSet<TEntity>`: `GetByIdAsync`, `GetAllAsync`,
`AddAsync` і запити за специфікацією (`ListAsync`, `FirstOrDefaultAsync`, `CountAsync`, `AnyAsync`)
асинхронні; `Update` і `Delete` лише змінюють стан сутності в контексті. Жоден репозиторій не
викликає збереження.

| Репозиторій | Запит | Спосіб завантаження |
|-------------|-------|---------------------|
| `CatalogItemRepository` | `GetWithDetailsAsync` — позиція з брендом, описом і категоріями | **eager loading**: `Include` / `ThenInclude`, один SQL-запит із `JOIN` |
| `CategoryRepository` | `GetWithItemsAsync` — категорія з її позиціями | **explicit loading**: спершу категорія, потім `Entry(category).Collection(...).Query().Include(...).LoadAsync()` — два запити |
| `CatalogItemCategoryRepository` | `GetRecentlyAddedAsync` — позиції, додані до категорії після дати | **LINQ to Entities** по проміжній сутності: фільтр за `Category.Slug` і власним полем `AddedAt`, сортування за `AddedAt`, проєкція в `CatalogItem` |

Бренди не мають власних запитів і обслуговуються узагальненим репозиторієм.

### Unit of Work

`UnitOfWork` створюється один на HTTP-запит і надає всі репозиторії поверх спільного
`CatalogDbContext`. Сервіс викликає `SaveChangesAsync` один раз наприкінці операції, тож усі зміни
потрапляють у базу однією транзакцією. Там само помилки бази перетворюються на доменні винятки:
порушення унікального індексу чи обмеження та конфлікт `RowVersion` стають `ConflictException`.

## Специфікації

Умови вибірки винесено із сервісів у класи `Specification<T>` (Ardalis.Specification) у
`Catalog.Bll/Specifications`. Специфікація описує `Where`, `Include`, `OrderBy`, `Skip`, `Take` і не
залежить від EF Core, тому перевіряється модульними тестами на звичайному списку. Репозиторій
застосовує будь-яку специфікацію однаково — `Set.WithSpecification(specification)`, а для
підрахунку бере з неї лише умови фільтрації.

| Специфікація | Що описує |
|--------------|-----------|
| `CatalogItemsFilterSpec` | сторінка списку позицій: пошук, тип, бренд, категорія, діапазон ціни, активність, сортування, `Skip`/`Take`, завантаження бренду |
| `CategoriesFilterSpec`, `BrandsFilterSpec` | сторінки списків категорій і брендів |
| `BrandByNameSpec`, `CategoryDuplicateSpec` | перевірка унікальності назви бренду, назви та slug категорії |
| `CategoryBySlugSpec`, `CategoryChildrenSpec` | категорія за slug; підкатегорії заданої категорії |
| `CatalogItemsByBrandSpec` | позиції бренду, включно з м'яко видаленими (`IgnoreQueryFilters`) |

## Пагінація

Запит списку приймає `page` (з 1) і `pageSize` (типово 20, не більше 100 — більші значення
зменшуються). Сервіс двічі звертається до репозиторію з однією специфікацією: `CountAsync` рахує
всі записи, що відповідають фільтрам, `ListAsync` повертає одну сторінку. Обидва запити виконує
SQL Server: фільтри стають `WHERE`, сортування — `ORDER BY`, сторінка — `OFFSET … FETCH`.
Сортувати можна лише за переліченими полями; невідоме значення відхиляє валідатор.

Відповідь — `PagedResult<T>`: `items`, `totalCount`, `page`, `pageSize`, `totalPages`, `hasNext`,
`hasPrevious`. Ті самі метадані дублюються в заголовку `X-Pagination`.

## Де перевіряється кожне правило

| Правило | Де перевіряється | Відповідь |
|---------|------------------|-----------|
| обов'язковість, довжина рядка, діапазон числа | атрибути DataAnnotations на DTO | 400 |
| формат SKU і slug, точність ціни, послуга без бренду, тривалість лише для послуг, `minPrice ≤ maxPrice`, дозволені поля сортування, характеристики як JSON-об'єкт | валідатори FluentValidation у `Catalog.Bll/Validation` | 400 |
| вказаний бренд або батьківська категорія існують | сервіс (потрібна база) | 400 |
| SKU, назва бренду, назва й slug категорії унікальні | сервіс (потрібна база) | 409 |
| бренд не використовується, категорія не має підкатегорій, немає циклу в дереві | сервіс (потрібна база) | 409 |
| інваріанти сутності (ціна, одна основна категорія тощо) | сутність | 409 |
| запис існує | сервіс | 404 |

Правила не дублюються між DataAnnotations і FluentValidation. Валідатори зареєстровано через
`AddValidatorsFromAssemblyContaining`, а викликає їх контролер явно; автоматичне підключення
FluentValidation до конвеєра MVC не використовується. Унікальні індекси та `CHECK`-обмеження бази —
останній рубіж: якщо два запити одночасно пройдуть перевірку сервісу, другий отримає 409 від бази.

## Обробка помилок

Винятки перехоплює один `GlobalExceptionHandler` (`IExceptionHandler` + `AddProblemDetails`).

| Виняток | Статус | Формат |
|---------|--------|--------|
| `EntityNotFoundException` | 404 | `ProblemDetails` |
| `ValidationException` | 400 | `ValidationProblemDetails` з помилками за полями |
| `ConflictException`, `DomainException` | 409 | `ProblemDetails` |
| будь-який інший | 500 | `ProblemDetails` |

Текст доменного винятку безпечний для клієнта й повертається в `detail`. Для непередбаченої помилки
поза середовищем `Development` повертається лише загальне повідомлення; у `Development` додаються
текст винятку та стек. Обробник записує кожен виняток у журнал один раз: очікувані відмови —
рівнем `Warning`, непередбачені — рівнем `Error` разом зі стеком.

gRPC-сервіс перетворює ті самі винятки на статуси gRPC: `EntityNotFoundException` — `NotFound`,
`ValidationException` — `InvalidArgument`, `ConflictException`/`DomainException` —
`FailedPrecondition`; некоректний вхід (Id ≤ 0, понад 100 Id в одному запиті) відхиляється з
`InvalidArgument` ще до звернення до BLL.

## Кешування

Довідникові дані каталогу читають набагато частіше, ніж змінюють: картку позиції — і REST-клієнти,
і агрегатор при кожній збірці картки ремонту, а бренди й категорії майже не змінюються. Тому
Catalog — сервіс із кешем. Кеш дворівневий і стоїть у BLL, тож працює однаково для REST і gRPC.

```mermaid
flowchart LR
    S["CachedCatalogItemService<br/>CachedCategoryService<br/>CachedBrandService"] -->|1| L1["L1: IMemoryCache<br/>у процесі"]
    S -->|2. промах L1| L2["L2: Redis<br/>IDistributedCache"]
    S -->|3. промах L2| DB[("CatalogDb")]
    DB -. заповнює .-> L2
    L2 -. заповнює .-> L1
```

Читання йде за схемою cache-aside: L1 → L2 → база, а кожен рівень заповнюється на зворотному шляху.
Усе, що стосується кешу, лежить у `Catalog.Bll/Caching/`:

| Клас | Призначення |
|------|-------------|
| `TwoLevelCache` | L1 (`IMemoryCache`) + L2 (`IDistributedCache` поверх Redis), серіалізація DTO через System.Text.Json, пакетне читання, версії списків, робота без Redis |
| `CachedCatalogItemService`, `CachedCategoryService`, `CachedBrandService` | декоратори сервісів BLL: читання через кеш, інвалідація одразу після запису |
| `CatalogCacheInvalidator` | які ключі видаляти після кожної зміни |
| `CatalogCacheKeys` | схема ключів |
| `CatalogCacheOptions` | TTL за типом даних, ліміт L1, перевірка «L1 коротший за L2» під час старту |
| `CatalogCacheMetrics` | `Meter` `Catalog.Cache` |
| `CacheWarmupService` | прогрівання під час старту й кожні 5 хв |
| `CacheStatisticsHealthCheck` | статистика кешу в `/health` |
| `CachingServiceCollectionExtensions` | реєстрація: конкретні сервіси обгортаються декораторами |

У `Program.cs` кеш підключається трьома викликами: `AddMemoryCache` з `SizeLimit` (одиниця розміру —
кілобайт серіалізованого DTO, ліміт 32 МБ, `CompactionPercentage` 0.25, статистика ввімкнена),
`builder.AddRedisDistributedCache("redis")` (Aspire-інтеграція: рядок підключення
`ConnectionStrings:redis`, повторне підключення, трасування команд Redis) і
`AddCatalogServicesWithCaching`.

### Ключі й TTL

| Дані | Ключ | L1: sliding / absolute | L2 | Пріоритет L1 |
|------|------|------------------------|----|--------------|
| Картка позиції | `catalog:item:{id}` | 30 с / 2 хв | 10 хв | Normal |
| Списки позицій | `catalog:items:list:{версія}:{відбиток запиту}` | 20 с / 1 хв | 5 хв | Low |
| Категорія, бренд | `catalog:category:{id}`, `catalog:brand:{id}` | 2 хв / 10 хв | 1 год | High |
| Списки категорій і брендів | `catalog:categories:list:{версія}:…`, `catalog:brands:list:{версія}:…` | 2 хв / 10 хв | 1 год | High |
| Версії списків | `catalog:items:list-version` тощо | 20 с / 1 хв | 1 доба | High |

- Префікс `catalog:` відокремлює ключі сервісу в спільному Redis; ключ агрегатора починається з
  `aggregator:`.
- L1 завжди живе коротше за L2, інакше застаріла копія в пам'яті одного екземпляра пережила б
  оновлення в Redis. Співвідношення перевіряється під час старту: застосунок із хибними налаштуваннями
  не запуститься.
- Кожен запис L1 має `Size`, sliding і absolute expiration та пріоритет; за перевищення ліміту
  першими витісняються списки (`Low`).
- Відбиток запиту — перші 8 байтів SHA-256 від JSON параметрів фільтра, тож різні сторінки й фільтри
  мають різні ключі.
- `GetCategoryWithItems` і `GetRecentItems` не кешуються: вони залежать одразу від категорій і
  позицій, а інвалідація таких комбінацій складніша за виграш.

### Інвалідація

Декоратор спершу виконує запис у базу, і лише якщо він успішний, синхронно видаляє пов'язані ключі
з обох рівнів (спершу L2, потім L1) і пише в журнал `Cache invalidated {CacheKey}, reason {Reason}`.

| Операція | Що інвалідується |
|----------|------------------|
| створення позиції | версія списків позицій |
| зміна, зміна ціни, видалення позиції, прив'язка/відв'язка категорії | `catalog:item:{id}` і версія списків позицій |
| створення категорії/бренду | версія списків категорій/брендів |
| зміна, видалення категорії | `catalog:category:{id}`, версія списків категорій, картки всіх позицій цієї категорії і версія списків позицій |
| зміна, видалення бренду | `catalog:brand:{id}`, версія списків брендів, картки позицій цього бренду і версія списків позицій |

Списки не перебираються поштучно: зміна версії робить усі старі ключі списків недосяжними, а Redis
сам видалить їх за TTL. Назва бренду й категорій є в картці позиції, тож їхня зміна видаляє й картки
позицій, які їх показують.

### Пакетне читання

`GetByIdsAsync` (для gRPC-методу `GetCatalogItemsByIds`) спершу збирає позиції з L1, потім паралельно
читає з L2 ті, яких немає в пам'яті, і лише решту завантажує з бази **одним** запитом
(`CatalogItemsByIdsSpec`). Кожна завантажена позиція кешується під власним ключем, тож наступне
читання картки через REST теж знайде її в кеші.

### Робота без Redis

Якщо операція з Redis кидає виняток, `TwoLevelCache` пише `Warning`, збільшує `cache.errors` і на
30 с (`DistributedRetryAfter`) перестає звертатися до L2: сервіс відповідає з L1 і бази, без
очікування таймаутів Redis на кожному запиті. Ключі, які не вдалося видалити з Redis під час збою,
запам'ятовуються й видаляються, щойно Redis знову доступний, — після відновлення Redis не віддасть
застарілих даних. `/health` при цьому показує `Unhealthy` для перевірки `redis` (її додає
Aspire-інтеграція) і `Degraded` для `cache`, але API працює.

### Прогрівання

`CacheWarmupService` під час старту й далі кожні 5 хв (менше за TTL L2 карток) завантажує через
кешовані сервіси список категорій, список брендів, першу сторінку позицій і картки цих позицій.
Помилка прогрівання лише пишеться в журнал: дані завантажаться за першим запитом.

### Метрики й журнал

`Meter` `Catalog.Cache` (`CatalogCacheMetrics`), підключений у ServiceDefaults як
`<префікс сервісу>.Cache`:

| Інструмент | Тип | Теги |
|------------|-----|------|
| `cache.hits`, `cache.misses` | Counter | `cache.level` (`L1`, `L2`), `cache.entry` (`item`, `categories:list`, …) |
| `cache.invalidations` | Counter | `cache.entry`, `cache.reason` |
| `cache.evictions` | Counter | `cache.level`, `cache.reason` (`Expired`, `Capacity`) |
| `cache.errors` | Counter | `cache.level`, `cache.operation` |
| `cache.operation.duration` | Histogram, мс | `cache.level`, `cache.operation` (`get`, `set`, `remove`) |
| `cache.memory.entries`, `cache.memory.size` | ObservableGauge | — |

Частка попадань — `cache.hits / (cache.hits + cache.misses)` для кожного рівня; поточне значення для
L1 є і в `/health` (`l1.hitRatio`). Попадання й промахи пишуться рівнем `Debug` з ключем, рівнем,
розміром даних і часом до закінчення життя запису; запис у кеш — з обома TTL. Виклики через gRPC
мають у журналі властивості `Transport = grpc` і `RpcMethod`, тож їх видно окремо від REST.

![Dashboard, метрика cache.hits сервісу catalog-api з фільтрами cache.level і cache.entry](img/catalog-cache-hits-metric.png)

Повторне читання тієї самої категорії: перший запит іде в Redis (промах) і в `CatalogDb`, другий
обслуговує L1 — у trace лише шлюз і catalog-api, без SQL- і Redis-spans:

![Два запити GET /api/categories/4: перший зі spans redis і CatalogDb, повторний лише gateway і catalog-api](img/catalog-cache-traces.png)

![Trace повторного запиту: gateway → catalog-api, 3 spans, без SQL](img/catalog-cache-hit-trace.png)

## Журнал

Журнал, трасування й кореляцію запитів дає `Platform.ServiceDefaults` (див.
[`platform.md`](platform.md)): `builder.AddServiceDefaults()` підключає Serilog і OpenTelemetry, а
спільний `CorrelationIdMiddleware` бере ідентифікатор із заголовка `X-Correlation-Id` (або створює
новий), додає його до контексту журналу й повертає в заголовку відповіді; `Program.cs` додає його ж
у тіло `ProblemDetails`. Розділ `Serilog` в `appsettings.json` містить лише налаштування Catalog:
файловий приймач і рівень `Information` для категорії `Microsoft.EntityFrameworkCore.Database.Command`,
тому в журналі видно кожну SQL-команду. Бізнес-операції з позиціями (створення, зміна, зміна ціни)
мають власні spans `ActivitySource` `Catalog.Bll`.

## Початкові дані

| Спосіб | Дані | Коли виконується |
|--------|------|------------------|
| `HasData` у конфігураціях | 5 брендів, 11 категорій із фіксованими Id | у складі міграції `InitialCreate` |
| `DbInitializer.SeedAsync` | 10 позицій з описами та 15 зв'язків із категоріями | після `MigrateAsync()`, якщо таблиця позицій порожня; не виконується в `Production` |

Позиції створюються конструкторами й методами сутностей, тобто проходять ті самі перевірки, що й
дані з API. Їхні Id 1..10 задаються явно, бо на них посилаються `RepairsDb` і MongoDB.

## Тести

`dotnet test Platform.slnx` запускає модульні тести всієї платформи (у Catalog — 78). Тести Catalog перевіряють правила
сутностей, сервіси на підставному Unit of Work (конфлікти, відсутні записи, одне збереження на
операцію, обмеження розміру сторінки), специфікації на списку в пам'яті, валідатори та
конфігурацію AutoMapper. Тести кешу працюють на справжньому `MemoryCache` і `MemoryDistributedCache`:
повторне читання не йде в базу, L2 заповнює L1, збій Redis не ламає читання й не губить
інвалідацію, пакетне читання завантажує з бази лише відсутні Id, невдалий запис не чіпає кеш, а
зміна ціни робить наступне читання свіжим. Тести gRPC-сервісу перевіряють `InvalidArgument`,
`NotFound` і перетворення ціни на `DecimalValue`.
