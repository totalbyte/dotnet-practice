/* Проєкт №3 - MongoDB reviews. Тестові документи; запускати після collections.js.
   Ідемпотентно: фіксовані _id та upsert із $setOnInsert - наявні документи не дублюються. */

db = db.getSiblingDB("reviews");

function upsertAll(collectionName, documents) {
  const operations = documents.map(function (doc) {
    const { _id, ...fields } = doc;
    return { updateOne: { filter: { _id: _id }, update: { $setOnInsert: fields }, upsert: true } };
  });
  const result = db.getCollection(collectionName).bulkWrite(operations, { ordered: true });
  print(`${collectionName}: inserted ${result.upsertedCount}, already existed ${result.matchedCount}`);
}

const ids = {
  reviewRepair1: ObjectId("671000000000000000000001"),
  reviewItemScreenA: ObjectId("671000000000000000000002"),
  reviewItemSsd: ObjectId("671000000000000000000003"),
  reviewItemScreenB: ObjectId("671000000000000000000004"),
  reviewRepair5: ObjectId("671000000000000000000005"),
  reviewItemBattery: ObjectId("671000000000000000000006"),
  reviewItemPaste: ObjectId("671000000000000000000007"),
  discussionScreen: ObjectId("672000000000000000000001"),
  discussionDefect: ObjectId("672000000000000000000002"),
  msg1: ObjectId("673000000000000000000001"),
  msg2: ObjectId("673000000000000000000002"),
  msg3: ObjectId("673000000000000000000003"),
  msg4: ObjectId("673000000000000000000004"),
  msg5: ObjectId("673000000000000000000005"),
  msg6: ObjectId("673000000000000000000006")
};

// reviews: документи з різними наборами полів в одній колекції
upsertAll("reviews", [
  {
    _id: ids.reviewRepair1,
    reviewType: "repair",
    rating: NumberInt(5),
    title: "Екран замінили за два дні",
    text: "Розбила екран, думала, що телефону кінець. Замінили швидко, сенсор працює як новий.",
    author: { customerId: NumberLong("1"), displayName: "Олена Коваленко" },
    repairOrderId: NumberLong("1"),
    device: { type: "Smartphone", brand: "Apple", model: "iPhone 13" },
    technician: { displayName: "Іван" },
    usedItems: [
      { catalogItemId: NumberLong("1"), itemName: "Екран OLED для iPhone 13" },
      { catalogItemId: NumberLong("7"), itemName: "Заміна екрана смартфона" }
    ],
    photos: ["reviews/671-1/before.jpg", "reviews/671-1/after.jpg"],
    comments: [
      { authorName: "Андрій", text: "Скільки коштувало, якщо не секрет?", createdAt: ISODate("2026-10-05T09:12:00Z") },
      { authorName: "Олена Коваленко", text: "4700 грн разом із роботою.", createdAt: ISODate("2026-10-05T10:40:00Z") }
    ],
    commentsCount: NumberInt(2),
    staffReply: { authorName: "Сервісний центр", text: "Дякуємо за відгук! Гарантія на екран — 6 місяців.", repliedAt: ISODate("2026-10-04T12:00:00Z") },
    createdAt: ISODate("2026-10-04T08:30:00Z"),
    updatedAt: ISODate("2026-10-05T10:40:00Z"),
    version: NumberInt(3)
  },

  {
    _id: ids.reviewItemScreenA,
    reviewType: "item",
    rating: NumberInt(4),
    title: "Якісний екран, але недешевий",
    text: "Кольори та яскравість на рівні оригіналу. Ціна кусається.",
    author: { customerId: NumberLong("1"), displayName: "Олена Коваленко" },
    catalogItemId: NumberLong("1"),
    itemName: "Екран OLED для iPhone 13",
    pros: ["OLED-матриця", "Гарантія 6 місяців"],
    cons: ["Висока ціна"],
    comments: [
      { authorName: "Марія", text: "А True Tone після заміни працює?", createdAt: ISODate("2026-10-06T14:00:00Z") }
    ],
    commentsCount: NumberInt(1),
    discussionId: ids.discussionScreen,
    createdAt: ISODate("2026-10-05T18:00:00Z"),
    version: NumberInt(2)
  },

  {
    _id: ids.reviewItemSsd,
    reviewType: "item",
    rating: NumberInt(5),
    title: "Ноутбук ожив",
    text: "Після встановлення SSD система завантажується за 10 секунд.",
    author: { customerId: NumberLong("2"), displayName: "Андрій Шевченко" },
    catalogItemId: NumberLong("4"),
    itemName: "SSD Kingston NV2 500 ГБ",
    pros: ["Швидкість", "Гарантія 3 роки"],
    photos: ["reviews/671-3/benchmark.png"],
    createdAt: ISODate("2026-10-25T11:15:00Z"),
    version: NumberInt(1)
  },

  {
    _id: ids.reviewItemScreenB,
    reviewType: "item",
    rating: NumberInt(2),
    text: "Через місяць на екрані з'явилася зелена смуга.",
    author: { customerId: NumberLong("4"), displayName: "Тарас Мельник" },
    catalogItemId: NumberLong("1"),
    itemName: "Екран OLED для iPhone 13",
    cons: ["Дефект матриці через місяць"],
    staffReply: { authorName: "Сервісний центр", text: "Це гарантійний випадок, замінимо безкоштовно.", repliedAt: ISODate("2026-11-06T09:00:00Z") },
    discussionId: ids.discussionDefect,
    createdAt: ISODate("2026-11-05T20:45:00Z"),
    updatedAt: ISODate("2026-11-06T09:00:00Z"),
    version: NumberInt(2)
  },

  {
    _id: ids.reviewRepair5,
    reviewType: "repair",
    rating: NumberInt(4),
    title: "Чесно сказали, що ремонт недоцільний",
    text: "Матриця коштує майже як новий телевізор. Не нав'язували ремонт, узяли лише за діагностику.",
    author: { customerId: NumberLong("4"), displayName: "Тарас Мельник" },
    repairOrderId: NumberLong("5"),
    device: { type: "TV", brand: "LG", model: "43UP7500" },
    createdAt: ISODate("2026-10-14T10:00:00Z"),
    version: NumberInt(1)
  },

  {
    _id: ids.reviewItemBattery,
    reviewType: "item",
    rating: NumberInt(5),
    text: "Тримає заряд як новий.",
    author: { customerId: NumberLong("3"), displayName: "Марія Бондаренко" },
    catalogItemId: NumberLong("3"),
    itemName: "Акумулятор для Samsung Galaxy S22",
    createdAt: ISODate("2026-11-08T16:20:00Z"),
    version: NumberInt(1)
  },

  {
    _id: ids.reviewItemPaste,
    reviewType: "item",
    rating: NumberInt(4),
    title: "Температура впала на 15 градусів",
    text: "Після чистки й заміни термопасти ноутбук перестав вимикатися.",
    author: { customerId: NumberLong("2"), displayName: "Андрій Шевченко" },
    catalogItemId: NumberLong("5"),
    itemName: "Термопаста Arctic MX-4, 4 г",
    pros: ["Помітний ефект", "Невисока ціна"],
    createdAt: ISODate("2026-10-23T09:05:00Z"),
    version: NumberInt(1)
  }
]);

// discussions
upsertAll("discussions", [
  {
    _id: ids.discussionScreen,
    topic: "Чи оригінальний цей екран і чи працює True Tone?",
    reviewId: ids.reviewItemScreenA,
    catalogItemId: NumberLong("1"),
    itemName: "Екран OLED для iPhone 13",
    status: "open",
    startedBy: { customerId: NumberLong("3"), displayName: "Марія Бондаренко" },
    messagesCount: NumberInt(4),
    lastMessage: {
      messageId: ids.msg4,
      authorName: "Марія Бондаренко",
      text: "Дякую, запишуся на заміну.",
      createdAt: ISODate("2026-10-07T12:30:00Z")
    },
    createdAt: ISODate("2026-10-06T14:05:00Z")
  },
  {
    _id: ids.discussionDefect,
    topic: "Зелена смуга на екрані після заміни",
    reviewId: ids.reviewItemScreenB,
    catalogItemId: NumberLong("1"),
    itemName: "Екран OLED для iPhone 13",
    status: "closed",
    startedBy: { customerId: NumberLong("4"), displayName: "Тарас Мельник" },
    messagesCount: NumberInt(2),
    lastMessage: {
      messageId: ids.msg6,
      authorName: "Сервісний центр",
      text: "Екран замінено за гарантією.",
      createdAt: ISODate("2026-11-09T15:00:00Z")
    },
    createdAt: ISODate("2026-11-05T21:00:00Z"),
    closedAt: ISODate("2026-11-09T15:00:00Z")
  }
]);

// discussionMessages
upsertAll("discussionMessages", [
  {
    _id: ids.msg1,
    discussionId: ids.discussionScreen,
    author: { customerId: NumberLong("3"), displayName: "Марія Бондаренко", role: "customer" },
    text: "Підкажіть, це оригінальний екран чи копія? Чи працюватиме True Tone?",
    createdAt: ISODate("2026-10-06T14:05:00Z")
  },
  {
    _id: ids.msg2,
    discussionId: ids.discussionScreen,
    author: { displayName: "Сервісний центр", role: "staff" },
    text: "Це якісний OLED-аналог. True Tone переносимо програматором зі старого модуля.",
    replyToMessageId: ids.msg1,
    createdAt: ISODate("2026-10-06T15:20:00Z")
  },
  {
    _id: ids.msg3,
    discussionId: ids.discussionScreen,
    author: { customerId: NumberLong("1"), displayName: "Олена Коваленко", role: "customer" },
    text: "Підтверджую, у мене True Tone працює.",
    replyToMessageId: ids.msg1,
    attachments: [{ fileName: "true-tone.jpg", url: "reviews/672-1/true-tone.jpg" }],
    createdAt: ISODate("2026-10-07T08:10:00Z")
  },
  {
    _id: ids.msg4,
    discussionId: ids.discussionScreen,
    author: { customerId: NumberLong("3"), displayName: "Марія Бондаренко", role: "customer" },
    text: "Дякую, запишуся на заміну.",
    createdAt: ISODate("2026-10-07T12:30:00Z")
  },
  {
    _id: ids.msg5,
    discussionId: ids.discussionDefect,
    author: { customerId: NumberLong("4"), displayName: "Тарас Мельник", role: "customer" },
    text: "Смуга з'явилася сама, телефон не падав.",
    attachments: [{ fileName: "green-line.jpg", url: "reviews/672-2/green-line.jpg" }],
    createdAt: ISODate("2026-11-05T21:00:00Z")
  },
  {
    _id: ids.msg6,
    discussionId: ids.discussionDefect,
    author: { displayName: "Сервісний центр", role: "staff" },
    text: "Екран замінено за гарантією.",
    replyToMessageId: ids.msg5,
    createdAt: ISODate("2026-11-09T15:00:00Z"),
    editedAt: ISODate("2026-11-09T15:05:00Z")
  }
]);

// Перерахунок itemRatingSummaries; $merge робить операцію ідемпотентною
db.reviews.aggregate([
  { $match: { reviewType: "item", isDeleted: { $ne: true } } },
  { $sort: { createdAt: 1 } },
  {
    $group: {
      _id: "$catalogItemId",
      itemName: { $last: "$itemName" },
      reviewsCount: { $sum: NumberInt(1) },
      averageRating: { $avg: "$rating" },
      r1: { $sum: { $cond: [{ $eq: ["$rating", 1] }, NumberInt(1), NumberInt(0)] } },
      r2: { $sum: { $cond: [{ $eq: ["$rating", 2] }, NumberInt(1), NumberInt(0)] } },
      r3: { $sum: { $cond: [{ $eq: ["$rating", 3] }, NumberInt(1), NumberInt(0)] } },
      r4: { $sum: { $cond: [{ $eq: ["$rating", 4] }, NumberInt(1), NumberInt(0)] } },
      r5: { $sum: { $cond: [{ $eq: ["$rating", 5] }, NumberInt(1), NumberInt(0)] } },
      lastReviewAt: { $max: "$createdAt" }
    }
  },
  {
    $project: {
      itemName: 1,
      reviewsCount: 1,
      averageRating: { $round: ["$averageRating", 2] },
      ratingHistogram: { r1: "$r1", r2: "$r2", r3: "$r3", r4: "$r4", r5: "$r5" },
      lastReviewAt: 1,
      updatedAt: "$$NOW"
    }
  },
  { $merge: { into: "itemRatingSummaries", on: "_id", whenMatched: "replace", whenNotMatched: "insert" } }
]);
print("itemRatingSummaries: recalculated");

// Контроль: кількість документів
["reviews", "discussions", "discussionMessages", "itemRatingSummaries"].forEach(function (name) {
  print(`  ${name}: ${db.getCollection(name).countDocuments({})} documents`);
});
