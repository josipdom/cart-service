namespace Cart.Application.Exceptions;

public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("The cart was changed by another request. Reload it and try again.", innerException);
