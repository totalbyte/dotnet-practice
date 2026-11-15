namespace Catalog.Domain.Exceptions;

public sealed class ConflictException : DomainException
{
    public ConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
