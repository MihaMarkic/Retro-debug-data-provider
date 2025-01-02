using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using Righthand.RetroDbgDataProvider.Services.Implementation;

namespace Righthand.RetroDbgDataProvider;

/// <summary>
/// Registrar for DI.
/// </summary>
public static class IoCRegistrar
{
    /// <summary>
    /// Registers custom services.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDebugDataProvider(this IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IOsDependent, WindowsDependent>();
        }
        else
        {
            services.AddSingleton<IOsDependent, NonWindowsDependent>();
        }
        return services
            .AddSingleton<IKickAssemblerCompiler, KickAssemblerCompiler>()
            .AddSingleton<IKickAssemblerByteDumpParser, KickAssemblerByteDumpParser>()
            .AddSingleton<IKickAssemblerDbgParser, KickAssemblerDbgParser>()
            .AddSingleton<IKickAssemblerProgramInfoBuilder, KickAssemblerProgramInfoBuilder>()
            .AddScoped<IKickAssemblerSourceCodeParser, KickAssemblerSourceCodeParser>()
            .AddSingleton<IFileService, FileService>();

    }
}