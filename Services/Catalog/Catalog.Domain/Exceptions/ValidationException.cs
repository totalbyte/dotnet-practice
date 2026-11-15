namespace Catalog.Domain.Exceptions;

public sealed class ValidationException : DomainException
{
    public ValidationException(string propertyName, string message)
        : this(new Dictionary<string, string[]> { [propertyName] = [message] })
    {
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
