using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Reviews.Domain.Common;
using Reviews.Domain.Entities;
using Reviews.Domain.Enums;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence.Serializers;

namespace Reviews.Infrastructure.Persistence;

public static class MongoMappings
{
    private static readonly Lock Sync = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Sync)
        {
            if (_registered)
                return;

            RegisterConventions();
            RegisterSerializers();
            RegisterClassMaps();
            _registered = true;
        }
    }

    private static void RegisterConventions()
    {
        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new IgnoreIfNullConvention(true)
        };

        ConventionRegistry.Register(
            "Reviews",
            pack,
            type => type.Namespace?.StartsWith("Reviews.", StringComparison.Ordinal) == true);
    }

    private static void RegisterSerializers()
    {
        BsonSerializer.TryRegisterSerializer(new DateTimeSerializer(DateTimeKind.Utc));
        BsonSerializer.TryRegisterSerializer(new LowerCaseEnumSerializer<ReviewType>());
    }

    private static void RegisterClassMaps()
    {
        BsonClassMap.TryRegisterClassMap<BaseEntity>(cm =>
        {
            cm.AutoMap();
            cm.SetIdMember(cm.GetMemberMap(e => e.Id));
        });

        BsonClassMap.TryRegisterClassMap<Review>(cm =>
        {
            cm.AutoMap();
            cm.MapField("_pros").SetElementName("pros").SetShouldSerializeMethod(r => ((Review)r).Pros.Count > 0);
            cm.MapField("_cons").SetElementName("cons").SetShouldSerializeMethod(r => ((Review)r).Cons.Count > 0);
            cm.MapField("_comments").SetElementName("comments")
                .SetShouldSerializeMethod(r => ((Review)r).Comments.Count > 0);
            cm.GetMemberMap(r => r.CommentsCount).SetIgnoreIfNull(false).SetIgnoreIfDefault(true);
            cm.GetMemberMap(r => r.IsDeleted).SetIgnoreIfNull(false).SetIgnoreIfDefault(true);
        });

        BsonClassMap.TryRegisterClassMap<Comment>(cm =>
        {
            cm.AutoMap();
            cm.MapCreator(c => new Comment(c.AuthorName, c.Text, c.IsStaff, c.CreatedAt));
            cm.GetMemberMap(c => c.IsStaff).SetIgnoreIfNull(false).SetIgnoreIfDefault(true).SetDefaultValue(false);
        });

        BsonClassMap.TryRegisterClassMap<Author>(cm =>
        {
            cm.AutoMap();
            cm.MapCreator(a => new Author(a.CustomerId, a.DisplayName));
        });

        BsonClassMap.TryRegisterClassMap<Device>(cm =>
        {
            cm.AutoMap();
            cm.MapCreator(d => new Device(d.Type, d.Brand, d.Model));
            cm.GetMemberMap(d => d.Brand).SetDefaultValue((object?)null);
        });

        BsonClassMap.TryRegisterClassMap<StaffReply>(cm =>
        {
            cm.AutoMap();
            cm.MapCreator(r => new StaffReply(r.AuthorName, r.Text, r.RepliedAt));
        });
    }
}
