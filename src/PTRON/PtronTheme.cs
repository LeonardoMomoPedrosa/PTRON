using MudBlazor;

namespace PTRON;

/// <summary>
/// Colors sampled from the official logo: navy wordmark and bright top faces.
/// </summary>
public static class PtronTheme
{
    public const string Navy = "#01295A";
    public const string Blue = "#007AFC";

    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Navy,
            AppbarBackground = "#FFFFFF",
            AppbarText = Navy,
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#3D92FF",
            AppbarBackground = "#121418",
            AppbarText = "#E8F1FF",
        },
    };
}
