/* Проєкт №3 - контекст Reviews, MongoDB, база ReviewsDb. Колекції, JSON Schema-валідація, індекси.
   Скрипт ідемпотентний. catalogItemId, repairOrderId, customerId - ідентифікатори з інших контекстів:
   звичайні поля без зв'язків на рівні бази. */

db = db.getSiblingDB("ReviewsDb");

function ensureCollection(name, jsonSchema) {
  const options = { validator: { $jsonSchema: jsonSchema }, validationLevel: "strict", validationAction: "error" };
  if (db.getCollectionNames().includes(name)) {
    db.runCommand({ collMod: name, ...options });
    print(`collection ${name}: exists, validator updated`);
  } else {
    db.createCollection(name, options);
    print(`collection ${name}: created`);
  }
}

const authorSchema = {
  bsonType: "object",
  required: ["customerId", "displayName"],
  properties: {
    customerId: { bsonType: "long", description: "Id клієнта в RepairsDb (логічне посилання, без FK)" },
    displayName: { bsonType: "string", minLength: 1, maxLength: 200 }
  }
};

// reviews: гібрид - вбудовані author, comments[], staffReply, usedItems[] і посилання discussionId;
// набір полів залежить від типу відгуку
ensureCollection("reviews", {
  bsonType: "object",
  required: ["reviewType", "rating", "text", "author", "createdAt", "version"],
  properties: {
    reviewType: { enum: ["repair", "item"], description: "repair - відгук про ремонт, item - про позицію каталогу" },
    rating: { bsonType: "int", minimum: 1, maximum: 5 },
    title: { bsonType: "string", maxLength: 200 },
    text: { bsonType: "string", minLength: 1, maxLength: 5000 },
    author: authorSchema,

    catalogItemId: { bsonType: "long", description: "Id позиції в CatalogDb (логічне посилання, без FK)" },
    itemName: { bsonType: "string", description: "денормалізована назва позиції; власник - Catalog, оновлюється подіями" },
    pros: { bsonType: "array", items: { bsonType: "string" } },
    cons: { bsonType: "array", items: { bsonType: "string" } },

    repairOrderId: { bsonType: "long", description: "Id замовлення в RepairsDb (логічне посилання, без FK)" },
    device: {
      bsonType: "object",
      required: ["type", "model"],
      description: "знімок пристрою на момент відгуку; не оновлюється",
      properties: { type: { bsonType: "string" }, brand: { bsonType: "string" }, model: { bsonType: "string" } }
    },
    technician: { bsonType: "object", required: ["displayName"], properties: { displayName: { bsonType: "string" } } },
    usedItems: {
      bsonType: "array",
      description: "вбудований масив: запчастини й послуги, використані в ремонті (знімок назв)",
      items: {
        bsonType: "object",
        required: ["catalogItemId", "itemName"],
        properties: { catalogItemId: { bsonType: "long" }, itemName: { bsonType: "string" } }
      }
    },

    photos: { bsonType: "array", items: { bsonType: "string" } },
    comments: {
      bsonType: "array",
      maxItems: 20,
      description: "вбудований масив: останні коментарі (обмежений, щоб документ не ріс безмежно)",
      items: {
        bsonType: "object",
        required: ["authorName", "text", "createdAt"],
        properties: {
          authorName: { bsonType: "string" },
          text: { bsonType: "string", minLength: 1, maxLength: 1000 },
          isStaff: { bsonType: "bool" },
          createdAt: { bsonType: "date" }
        }
      }
    },
    commentsCount: { bsonType: "int", minimum: 0 },
    staffReply: {
      bsonType: "object",
      required: ["authorName", "text", "repliedAt"],
      properties: { authorName: { bsonType: "string" }, text: { bsonType: "string" }, repliedAt: { bsonType: "date" } }
    },
    discussionId: { bsonType: "objectId", description: "посилання (reference) на discussions._id" },

    createdAt: { bsonType: "date" },
    updatedAt: { bsonType: "date" },
    isDeleted: { bsonType: "bool", description: "м'яке видалення" },
    version: { bsonType: "int", minimum: 1, description: "токен оптимістичної конкурентності" }
  },
  oneOf: [
    { properties: { reviewType: { enum: ["item"] } }, required: ["catalogItemId", "itemName"] },
    { properties: { reviewType: { enum: ["repair"] } }, required: ["repairOrderId", "device"] }
  ]
});

// discussions: посилання reviewId на відгук, вбудована копія останнього повідомлення
ensureCollection("discussions", {
  bsonType: "object",
  required: ["topic", "reviewId", "status", "startedBy", "messagesCount", "createdAt"],
  properties: {
    topic: { bsonType: "string", minLength: 1, maxLength: 300 },
    reviewId: { bsonType: "objectId", description: "посилання (reference) на reviews._id" },
    catalogItemId: { bsonType: "long" },
    itemName: { bsonType: "string", description: "денормалізована назва позиції каталогу" },
    status: { enum: ["open", "closed"] },
    startedBy: authorSchema,
    messagesCount: { bsonType: "int", minimum: 0 },
    lastMessage: {
      bsonType: "object",
      required: ["messageId", "authorName", "text", "createdAt"],
      description: "вбудована копія останнього повідомлення (щоб список тем не робив $lookup)",
      properties: {
        messageId: { bsonType: "objectId" },
        authorName: { bsonType: "string" },
        text: { bsonType: "string" },
        createdAt: { bsonType: "date" }
      }
    },
    createdAt: { bsonType: "date" },
    closedAt: { bsonType: "date" }
  }
});

// discussionMessages: посилання discussionId - повідомлень необмежено багато
ensureCollection("discussionMessages", {
  bsonType: "object",
  required: ["discussionId", "author", "text", "createdAt"],
  properties: {
    discussionId: { bsonType: "objectId", description: "посилання (reference) на discussions._id" },
    author: {
      bsonType: "object",
      required: ["displayName", "role"],
      properties: {
        customerId: { bsonType: "long" },
        displayName: { bsonType: "string" },
        role: { enum: ["customer", "staff"] }
      }
    },
    text: { bsonType: "string", minLength: 1, maxLength: 5000 },
    replyToMessageId: { bsonType: "objectId", description: "посилання на повідомлення цієї ж колекції" },
    attachments: {
      bsonType: "array",
      items: { bsonType: "object", required: ["fileName", "url"], properties: { fileName: { bsonType: "string" }, url: { bsonType: "string" } } }
    },
    createdAt: { bsonType: "date" },
    editedAt: { bsonType: "date" }
  }
});

// itemRatingSummaries: денормалізований рейтинг позиції каталогу (_id = catalogItemId)
ensureCollection("itemRatingSummaries", {
  bsonType: "object",
  required: ["itemName", "reviewsCount", "averageRating", "ratingHistogram", "updatedAt"],
  properties: {
    _id: { bsonType: "long", description: "catalogItemId - Id позиції в CatalogDb" },
    itemName: { bsonType: "string", description: "денормалізована назва позиції каталогу" },
    reviewsCount: { bsonType: "int", minimum: 0 },
    averageRating: { bsonType: ["double", "int"], minimum: 1, maximum: 5 },
    ratingHistogram: {
      bsonType: "object",
      required: ["r1", "r2", "r3", "r4", "r5"],
      description: "вбудований об'єкт: скільки відгуків з кожною оцінкою"
    },
    lastReviewAt: { bsonType: "date" },
    updatedAt: { bsonType: "date" }
  }
});

// Запит: «відгуки про позицію каталогу, новіші спочатку» (сторінка товару, з пагінацією).
db.reviews.createIndex(
  { catalogItemId: 1, createdAt: -1 },
  { name: "ix_reviews_catalogItemId_createdAt", partialFilterExpression: { reviewType: "item" } }
);

// Правило + запит: на одне замовлення на ремонт - один відгук; пошук відгуку за замовленням.
db.reviews.createIndex(
  { repairOrderId: 1 },
  { name: "ux_reviews_repairOrderId", unique: true, partialFilterExpression: { repairOrderId: { $exists: true } } }
);

// Правило: клієнт залишає один відгук на позицію каталогу; захищає від дублю при одночасних запитах.
db.reviews.createIndex(
  { catalogItemId: 1, "author.customerId": 1 },
  { name: "ux_reviews_catalogItemId_customerId", unique: true, partialFilterExpression: { reviewType: "item" } }
);

// Запит: «мої відгуки» - усі відгуки клієнта, новіші спочатку (поле вбудованого об'єкта).
db.reviews.createIndex({ "author.customerId": 1, createdAt: -1 }, { name: "ix_reviews_author_createdAt" });

// Запит: «найкращі / найгірші відгуки» - фільтр за оцінкою з сортуванням за датою.
db.reviews.createIndex({ rating: 1, createdAt: -1 }, { name: "ix_reviews_rating_createdAt" });

// Запит: повнотекстовий пошук у заголовку й тексті відгуку ($text).
db.reviews.createIndex(
  { title: "text", text: "text" },
  { name: "ix_reviews_text", default_language: "none", weights: { title: 3, text: 1 } }
);

// Запит: «обговорення до відгуку» (перехід від відгуку до теми).
db.discussions.createIndex({ reviewId: 1 }, { name: "ix_discussions_reviewId" });

// Запит: «відкриті теми, спочатку з найсвіжішою активністю» (список обговорень для персоналу).
db.discussions.createIndex({ status: 1, "lastMessage.createdAt": -1 }, { name: "ix_discussions_status_lastActivity" });

// Запит: «повідомлення теми в хронологічному порядку» з пагінацією.
db.discussionMessages.createIndex({ discussionId: 1, createdAt: 1 }, { name: "ix_messages_discussionId_createdAt" });

// Запит: «рейтинг позицій» - топ позицій каталогу за середньою оцінкою.
db.itemRatingSummaries.createIndex({ averageRating: -1, reviewsCount: -1 }, { name: "ix_summaries_rating" });

print("reviews: collections and indexes are up to date");
db.getCollectionNames().sort().forEach(function (name) {
  print(`  ${name}: ${db.getCollection(name).getIndexes().length} indexes`);
});
