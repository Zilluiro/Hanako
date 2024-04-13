using Hanako.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hanako;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<RtmpServer>();
        builder.Services.AddTransient<Handshaker>();

        var host = builder.Build();
        host.Run();
    }
}
