namespace AzureKeyVaultStudio.Exceptions;

public class KeyVaultItemNotFoundException : Exception
{
    public KeyVaultItemNotFoundException()
    {
    }

    public KeyVaultItemNotFoundException(string message)
        : base(message)
    {
    }

    public KeyVaultItemNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class KeyVaultItemNotFailedToUpdate : Exception
{
    public KeyVaultItemNotFailedToUpdate()
    {
    }

    public KeyVaultItemNotFailedToUpdate(string message)
        : base(message)
    {
    }

    public KeyVaultItemNotFailedToUpdate(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class KeyVaultInsufficientPrivilegesException : Exception
{
    public KeyVaultInsufficientPrivilegesException()
    {
    }

    public KeyVaultInsufficientPrivilegesException(string message)
        : base(message)
    {
    }

    public KeyVaultInsufficientPrivilegesException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class KeyVaultInvalidSecretPrivilegesException : Exception
{
    public KeyVaultInvalidSecretPrivilegesException()
    {
    }

    public KeyVaultInvalidSecretPrivilegesException(string message)
        : base(message)
    {
    }

    public KeyVaultInvalidSecretPrivilegesException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class SubscriptionInsufficientPrivileges : Exception
{
    public SubscriptionInsufficientPrivileges()
    {
    }

    public SubscriptionInsufficientPrivileges(string message)
        : base(message)
    {
    }

    public SubscriptionInsufficientPrivileges(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class AuthenticationRequiredException : Exception
{
    public AuthenticationRequiredException(string message) : base(message) { }
    public AuthenticationRequiredException(string message, Exception innerException) : base(message, innerException) { }
}
