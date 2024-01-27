namespace EcoAssetHub.Domain.Exceptions;

public class DomainException : Exception
{
    // Property to hold the list of error messages
    public List<string> ErrorMessages { get; }

    public DomainException()
    {
        ErrorMessages = new List<string>();
    }

    public DomainException(string message)
        : base(message)
    {
        ErrorMessages = new List<string> { message };
    }

    public DomainException(IEnumerable<string> messages)
        : base(string.Join(", ", messages))
    {
        ErrorMessages = new List<string>(messages);
    }

    public DomainException(string message, Exception inner)
        : base(message, inner)
    {
        ErrorMessages = new List<string> { message };
    }

    // Optionally, you can add a constructor that accepts an inner exception and a list of messages
    public DomainException(IEnumerable<string> messages, Exception inner)
        : base(string.Join(", ", messages), inner)
    {
        ErrorMessages = new List<string>(messages);
    }
}
