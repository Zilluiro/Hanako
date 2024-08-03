using Hanako.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hanako.Extensions;

internal static class IServiceProviderExtensions
{
    public static T GetValidatedService<T>(this IServiceProvider provider)
    {
        var foundService = provider.GetService<T>();
        return Guard.IsNotNull(foundService)!;
    }
}
