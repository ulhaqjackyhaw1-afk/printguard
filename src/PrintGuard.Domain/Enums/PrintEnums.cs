namespace PrintGuard.Domain.Enums;

public enum PrintJobStatus
{
    RECEIVED,
    HELD,
    AUTHENTICATING,
    AUTHORIZED,
    RELEASING,
    PRINTING,
    COMPLETED,
    DENIED,
    CANCELLED,
    FAILED
}

public enum QueueType
{
    HOLD,
    RELEASE,
    DIRECT
}

public enum AuthMethod
{
    PASSWORD,
    REMEMBER_ME
}

public enum AuthResult
{
    SUCCESS,
    DENIED,
    EXPIRED
}
