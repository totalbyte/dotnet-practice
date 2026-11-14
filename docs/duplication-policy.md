# Політика дублювання даних і узгодженості між базами

Кожен сервіс має власну базу, тому дані іншого контексту, потрібні під час читання, зберігаються
локально як **копія**. Це прибирає синхронний міжсервісний виклик при кожному читанні; натомість копія
може короткий час відставати від джерела (*eventual consistency*).

Розрізняються два види дублікатів:

- **Знімок** — значення фіксується в момент події й надалі **свідомо не оновлюється**
  (ціна в кошторисі не має змінитися через те, що в Каталозі її підняли).
- **Копія для читання** — оновлюється **подіями від сервісу-власника** (механізм доставки — брокер
  повідомлень). Сервіс, що зберігає копію, сам її не редагує.

У кожного дубльованого поля рівно один власник. Двох копій, які незалежно редагуються в різних
сервісах (*dual write*), немає.

## Таблиця політики дублювання

| № | Поле (дублікат) | Де зберігається | Джерело (власник даних) | Вид | Політика узгодженості |
|---|-----------------|-----------------|-------------------------|-----|----------------------|
| 1 | `RepairOrderItems.ItemName`, `RepairOrderItems.UnitPrice` | `RepairsDb` (Repairs) | `CatalogItems.Name`, `CatalogItems.Price` — **Catalog** | знімок | Копіюється процедурою `usp_AddRepairOrderItem` у момент додавання позиції до кошторису. **Не оновлюється**: клієнт платить за погодженою ціною. |
| 2 | `CatalogItems.Name`, `ItemType`, `Price`, `IsActive` (локальна копія довідника) | `RepairsDb` (Repairs) | `CatalogItems` — **Catalog** | копія для читання | **Оновлюється подіями з Каталогу** (створення, зміна, деактивація позиції). `SyncedAt` — час останньої синхронізації. Repairs ці поля не редагує. |
| 3 | `reviews.itemName`, `discussions.itemName`, `itemRatingSummaries.itemName` | `reviews` (Reviews, MongoDB) | `CatalogItems.Name` — **Catalog** | копія для читання | **Оновлюється подіями з Каталогу**: при перейменуванні позиції сервіс Reviews виконує `updateMany` за `catalogItemId`. |
| 4 | `reviews.author.displayName`, `discussions.startedBy.displayName`, `discussionMessages.author.displayName` | `reviews` (Reviews, MongoDB) | `Customers.FullName` — **Repairs** | копія для читання | **Оновлюється подіями з Repairs** при зміні імені клієнта (`updateMany` за `customerId`). |
| 5 | `reviews.device` (`type`, `brand`, `model`) | `reviews` (Reviews, MongoDB) | `RepairOrderDetails.DeviceType`, `DeviceBrand`, `DeviceModel` — **Repairs** | знімок | Фіксується під час створення відгуку про ремонт. **Не оновлюється**: відгук описує пристрій таким, яким він був у ремонті. |
| 6 | `reviews.usedItems[].itemName` | `reviews` (Reviews, MongoDB) | `RepairOrderItems.ItemName` — **Repairs** | знімок | Копіюється зі складу замовлення під час створення відгуку. **Не оновлюється** (джерело саме є знімком). |

Кожна пара контекстів, що взаємодіють, має щонайменше один рядок:
Catalog → Repairs (рядки 1–2), Catalog → Reviews (рядок 3), Repairs → Reviews (рядки 4–6).

## Логічні посилання між базами (без FOREIGN KEY)

Поруч із кожним дубльованим полем зберігається ідентифікатор сутності-власника. Це **звичайний
стовпець або поле**: обмеження `FOREIGN KEY` на іншу базу не створюється, цілісність посилання за
потреби перевіряє прикладний код.

| Поле-посилання | Де зберігається | На що посилається | Тип |
|----------------|-----------------|-------------------|-----|
| `CatalogItems.Id` | `RepairsDb` | `CatalogDb.dbo.CatalogItems.Id` | `BIGINT`, первинний ключ локальної копії без `IDENTITY` |
| `reviews.catalogItemId`, `reviews.usedItems[].catalogItemId`, `discussions.catalogItemId`, `itemRatingSummaries._id` | `reviews` (MongoDB) | `CatalogDb.dbo.CatalogItems.Id` | `long` |
| `reviews.repairOrderId` | `reviews` (MongoDB) | `RepairsDb.dbo.RepairOrders.Id` | `long` |
| `reviews.author.customerId`, `discussions.startedBy.customerId`, `discussionMessages.author.customerId` | `reviews` (MongoDB) | `RepairsDb.dbo.Customers.Id` | `long` |

Зовнішній ключ `RepairOrderItems.CatalogItemId → CatalogItems.Id` існує, але обидві таблиці лежать у
`RepairsDb`: він посилається на **локальну копію**, а не на базу Каталогу.

## Що не є дублюванням

- **Залишок на складі `RepairsDb.dbo.CatalogItems.Stock`.** Його єдиний власник — Repairs (запчастини
  списуються під час ремонту). У `CatalogDb` цього поля немає взагалі, тож двох незалежно
  редагованих копій не виникає.
- **`discussions.lastMessage`, `reviews.commentsCount`, `itemRatingSummaries`.** Це денормалізація
  всередині одного контексту Reviews: дані оновлює той самий сервіс у тій самій операції, що й
  першоджерело (`itemRatingSummaries` перераховується агрегацією з `$merge`).

## Як перевірити

```sql
-- у RepairsDb і в CatalogDb: усі зовнішні ключі посилаються лише на таблиці цієї ж бази
SELECT name, OBJECT_NAME(parent_object_id) AS FromTable, OBJECT_NAME(referenced_object_id) AS ToTable
FROM sys.foreign_keys;

-- знімок не оновився після зміни ціни в довіднику (RepairsDb)
SELECT i.ItemName, i.UnitPrice AS SnapshotPrice, ci.Price AS CurrentPrice
FROM dbo.RepairOrderItems i JOIN dbo.CatalogItems ci ON ci.Id = i.CatalogItemId
WHERE i.UnitPrice <> ci.Price;
```
