using System;

namespace Hanako.Exceptions;

internal class FailedHandshakeException : Exception
{
    public FailedHandshakeException(string? message) : base(message)
    {
    }
}
