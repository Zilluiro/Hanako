using System;

namespace Hanako.Exceptions;

internal class NetworkException : Exception
{
    public NetworkException(string? message) : base(message)
    {
    }
}
