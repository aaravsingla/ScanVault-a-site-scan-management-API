namespace ScanVault.Api.Errors;

/// <summary>Thrown by services when a requested record does not exist. Becomes HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Thrown by services when input breaks a business rule. Becomes HTTP 400.</summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}
