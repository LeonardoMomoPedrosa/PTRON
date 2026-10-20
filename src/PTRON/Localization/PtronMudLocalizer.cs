using System.Globalization;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace PTRON.Localization;

/// <summary>Translations for the ready-made MudBlazor texts. English is built into MudBlazor.</summary>
public sealed class PtronMudLocalizer : MudLocalizer
{
    private static readonly Dictionary<string, Dictionary<string, string>> Texts = new()
    {
        ["pt"] = new()
        {
            ["MudDataGridPager_RowsPerPage"] = "Linhas por página:",
            ["MudDataGridPager_InfoFormat"] = "{first_item}-{last_item} de {all_items}",
            ["MudTablePager_RowsPerPage"] = "Linhas por página:",
            ["MudTablePager_InfoFormat"] = "{first_item}-{last_item} de {all_items}",
            ["MudTablePager_FirstPage"] = "Primeira página",
            ["MudTablePager_PreviousPage"] = "Página anterior",
            ["MudTablePager_NextPage"] = "Próxima página",
            ["MudTablePager_LastPage"] = "Última página",
            ["MudAlert_Close"] = "Fechar",
            ["MudDialog_Close"] = "Fechar",
            ["MudSelect_Clear"] = "Limpar",
            ["MudAutocomplete_Clear"] = "Limpar",
        },
        ["es"] = new()
        {
            ["MudDataGridPager_RowsPerPage"] = "Filas por página:",
            ["MudDataGridPager_InfoFormat"] = "{first_item}-{last_item} de {all_items}",
            ["MudTablePager_RowsPerPage"] = "Filas por página:",
            ["MudTablePager_InfoFormat"] = "{first_item}-{last_item} de {all_items}",
            ["MudTablePager_FirstPage"] = "Primera página",
            ["MudTablePager_PreviousPage"] = "Página anterior",
            ["MudTablePager_NextPage"] = "Página siguiente",
            ["MudTablePager_LastPage"] = "Última página",
            ["MudAlert_Close"] = "Cerrar",
            ["MudDialog_Close"] = "Cerrar",
            ["MudSelect_Clear"] = "Borrar",
            ["MudAutocomplete_Clear"] = "Borrar",
        },
    };

    public override LocalizedString this[string key]
    {
        get
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (Texts.TryGetValue(lang, out var table) && table.TryGetValue(key, out var value))
            {
                return new LocalizedString(key, value, resourceNotFound: false);
            }

            return new LocalizedString(key, key, resourceNotFound: true);
        }
    }
}
