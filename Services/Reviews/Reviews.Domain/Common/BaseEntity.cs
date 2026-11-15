using MongoDB.Bson;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.Common;

public abstract class BaseEntity
{
    public ObjectId Id { get; protected set; } = ObjectId.GenerateNewId();

    public DateTime CreatedAt { get; protected set; } = Clock.UtcNow;

    public DateTime? UpdatedAt { get; protected set; }

    public int Version { get; protected set; } = 1;

    protected void Touch() => UpdatedAt = Clock.UtcNow;

    public void BumpVersion() => Version++;

    public void EnsureVersion(int expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConflictException(
                $"{GetType().Name} {Id} has version {Version}, but version {expectedVersion} was expected");
    }
}
