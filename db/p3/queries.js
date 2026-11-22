/* Проєкт №3 - MongoDB ReviewsDb. Запити перевірки та aggregation pipeline; запускати після seed.js.
   Дані не змінюються: CRUD-сценарій працює з тимчасовим документом і видаляє його. */

db = db.getSiblingDB("ReviewsDb");

function section(title) {
  print("\n--- " + title + " ---");
}
function show(cursorOrArray) {
  cursorOrArray.forEach(function (doc) {
    print(EJSON.stringify(doc));
  });
}

section("1. ГНУЧКА СХЕМА: документи однієї колекції мають різні набори полів");
db.reviews.find().sort({ _id: 1 }).forEach(function (doc) {
  print(`${doc.reviewType.padEnd(6)} | ${Object.keys(doc).filter((k) => k !== "_id").join(", ")}`);
});

section("2. ПАГІНАЦІЯ: відгуки про позицію каталогу 1, новіші спочатку (ix_reviews_catalogItemId_createdAt)");
const page = 1, pageSize = 2;
show(
  db.reviews
    .find({ reviewType: "item", catalogItemId: NumberLong("1") }, { itemName: 1, rating: 1, "author.displayName": 1, createdAt: 1 })
    .sort({ createdAt: -1 })
    .skip((page - 1) * pageSize)
    .limit(pageSize)
);

section("3. ВБУДОВАНІ ДАНІ: пошук за полем вбудованого об'єкта та елементом вбудованого масиву");
print("відгуки клієнта 4 (author.customerId): " + db.reviews.countDocuments({ "author.customerId": NumberLong("4") }));
show(
  db.reviews.find(
    { comments: { $elemMatch: { authorName: "Марія" } } },
    { title: 1, "comments.$": 1 }
  )
);
print("ремонти, де використано позицію каталогу 1 (usedItems.catalogItemId): " +
  db.reviews.countDocuments({ "usedItems.catalogItemId": NumberLong("1") }));

section("4. ПОШУК: повнотекстовий індекс ix_reviews_text");
show(db.reviews.find({ $text: { $search: "екран" } }, { title: 1, score: { $meta: "textScore" } }).sort({ score: { $meta: "textScore" } }));

section("5. ПОСИЛАННЯ: відгук -> обговорення -> повідомлення ($lookup по двох колекціях)");
show(
  db.reviews.aggregate([
    { $match: { discussionId: { $exists: true } } },
    { $lookup: { from: "discussions", localField: "discussionId", foreignField: "_id", as: "discussion" } },
    { $unwind: "$discussion" },
    {
      $lookup: {
        from: "discussionMessages",
        let: { discussionId: "$discussion._id" },
        pipeline: [
          { $match: { $expr: { $eq: ["$discussionId", "$$discussionId"] } } },
          { $sort: { createdAt: 1 } },
          { $project: { _id: 0, author: "$author.displayName", text: 1 } }
        ],
        as: "messages"
      }
    },
    { $project: { _id: 0, itemName: 1, rating: 1, topic: "$discussion.topic", status: "$discussion.status", messages: 1 } }
  ])
);

section("6. АГРЕГАЦІЯ: середній рейтинг і кількість відгуків за позиціями каталогу ($group)");
show(
  db.reviews.aggregate([
    { $match: { reviewType: "item" } },
    { $group: { _id: "$catalogItemId", itemName: { $last: "$itemName" }, reviews: { $sum: 1 }, avgRating: { $avg: "$rating" } } },
    { $sort: { avgRating: -1, reviews: -1, _id: 1 } }
  ])
);

section("7. АГРЕГАЦІЯ: найактивніші коментатори ($unwind вбудованого масиву)");
show(
  db.reviews.aggregate([
    { $unwind: "$comments" },
    { $group: { _id: "$comments.authorName", comments: { $sum: 1 }, lastCommentAt: { $max: "$comments.createdAt" } } },
    { $sort: { comments: -1, _id: 1 } }
  ])
);

section("8. АГРЕГАЦІЯ: зведення за типом відгуку та розподіл оцінок одним запитом ($facet)");
show(
  db.reviews.aggregate([
    {
      $facet: {
        byType: [{ $group: { _id: "$reviewType", count: { $sum: 1 }, avgRating: { $avg: "$rating" } } }, { $sort: { _id: 1 } }],
        byRating: [{ $group: { _id: "$rating", count: { $sum: 1 } } }, { $sort: { _id: -1 } }],
        withPhotos: [{ $match: { photos: { $exists: true } } }, { $count: "count" }]
      }
    }
  ])
);

section("9. ДЕНОРМАЛІЗАЦІЯ: готовий рейтинг позицій читається без агрегації (itemRatingSummaries)");
show(db.itemRatingSummaries.find({}, { itemName: 1, averageRating: 1, reviewsCount: 1 }).sort({ averageRating: -1, reviewsCount: -1 }));

section("10. ПЛАН ЗАПИТУ: відгуки про позицію використовують індекс, а не перегляд колекції");
try {
  const plan = db.reviews.find({ reviewType: "item", catalogItemId: NumberLong("1") }).sort({ createdAt: -1 }).explain("queryPlanner");
  const usedIndexes = JSON.stringify(plan.queryPlanner.winningPlan).match(/"indexName":"[^"]+"/g);
  print(usedIndexes ? usedIndexes.join(", ") : "індекс не використано");
} catch (error) {
  print("explain недоступний: " + error.message);
}

section("11. CRUD з оптимістичною конкурентністю (тимчасовий документ, наприкінці видаляється)");
const tempId = ObjectId("67ffffffffffffffffffffff");
db.reviews.deleteOne({ _id: tempId });
db.reviews.insertOne({
  _id: tempId,
  reviewType: "item",
  rating: NumberInt(3),
  text: "Тимчасовий відгук для перевірки",
  author: { customerId: NumberLong("2"), displayName: "Андрій Шевченко" },
  catalogItemId: NumberLong("2"),
  itemName: "Акумулятор для iPhone 13",
  comments: [],
  commentsCount: NumberInt(0),
  createdAt: new Date(),
  version: NumberInt(1)
});
print("CREATE: вставлено, version = 1");

const addComment = {
  $push: { comments: { $each: [{ authorName: "Олена", text: "Корисно, дякую", createdAt: new Date() }], $slice: -20 } },
  $inc: { commentsCount: NumberInt(1), version: NumberInt(1) },
  $set: { updatedAt: new Date() }
};
const firstWriter = db.reviews.updateOne({ _id: tempId, version: 1 }, addComment);
print(`UPDATE (актуальна версія 1): modified = ${firstWriter.modifiedCount}`);
const secondWriter = db.reviews.updateOne({ _id: tempId, version: 1 }, addComment);
print(`UPDATE (застаріла версія 1): modified = ${secondWriter.modifiedCount}  <- конфлікт, чужі зміни не затерто`);

const afterUpdate = db.reviews.findOne({ _id: tempId }, { version: 1, commentsCount: 1 });
print(`READ: version = ${afterUpdate.version}, commentsCount = ${afterUpdate.commentsCount}`);
print(`DELETE: deleted = ${db.reviews.deleteOne({ _id: tempId }).deletedCount}`);

section("12. ВАЛІДАЦІЯ: JSON Schema та унікальний індекс відхиляють некоректні документи");
function expectRejected(description, action) {
  try {
    action();
    print(`ПОМИЛКА ТЕСТУ: ${description} - документ прийнято`);
  } catch (error) {
    print(`відхилено: ${description} (${error.codeName || error.code || error.name})`);
  }
}
expectRejected("оцінка 9 поза межами 1..5", function () {
  db.reviews.insertOne({
    reviewType: "item", rating: NumberInt(9), text: "x", author: { customerId: NumberLong("1"), displayName: "x" },
    catalogItemId: NumberLong("1"), itemName: "x", createdAt: new Date(), version: NumberInt(1)
  });
});
expectRejected("відгук про ремонт без repairOrderId і device", function () {
  db.reviews.insertOne({
    reviewType: "repair", rating: NumberInt(5), text: "x", author: { customerId: NumberLong("1"), displayName: "x" },
    createdAt: new Date(), version: NumberInt(1)
  });
});
expectRejected("другий відгук на те саме замовлення 1", function () {
  db.reviews.insertOne({
    reviewType: "repair", rating: NumberInt(5), text: "x", author: { customerId: NumberLong("1"), displayName: "x" },
    repairOrderId: NumberLong("1"), device: { type: "Smartphone", model: "iPhone 13" }, createdAt: new Date(), version: NumberInt(1)
  });
});

section("13. КОНТРОЛЬ: після всіх сценаріїв кількість документів не змінилася");
["reviews", "discussions", "discussionMessages", "itemRatingSummaries"].forEach(function (name) {
  print(`  ${name}: ${db.getCollection(name).countDocuments({})}`);
});
