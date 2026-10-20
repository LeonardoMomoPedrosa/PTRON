using System.Globalization;
using System.Runtime.CompilerServices;

namespace PTRON.Tests;

internal static class TestCulture
{
    // The app starts in pt-BR (Program.cs); tests that do not choose a language run the same way.
    [ModuleInitializer]
    internal static void Init()
    {
        var ptBr = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = ptBr;
        CultureInfo.DefaultThreadCurrentUICulture = ptBr;
        CultureInfo.CurrentCulture = ptBr;
        CultureInfo.CurrentUICulture = ptBr;
    }
}
