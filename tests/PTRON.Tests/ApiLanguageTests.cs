using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using PTRON.Api;
using PTRON.Localization;
using PTRON.Services;
using Xunit;

namespace PTRON.Tests;

internal sealed class Scope : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _ui = CultureInfo.CurrentUICulture;
    private Scope(string code)
    {
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(code);
    }
    public static Scope Use(string code) => new(code);
    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _ui;
    }
}

public class ApiLanguageTests
{
    private static ExceptionContext Context(Exception ex) => new(
        new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
        new List<IFilterMetadata>()) { Exception = ex };

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en")]
    [InlineData("es")]
    public void Not_found_is_404_in_every_language(string language)
    {
        using var _ = Scope.Use(language);
        var context = Context(new NotFoundException(StaticText.Get("Tipo não encontrado.")));

        new ApiExceptionFilter().OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(404, result.StatusCode);
        var dto = Assert.IsType<ErrorDto>(result.Value);
        Assert.Equal(language == "pt-BR", dto.Error == "Tipo não encontrado.");
    }

    [Fact]
    public void Other_domain_errors_are_400()
    {
        var context = Context(new InvalidOperationException("x"));

        new ApiExceptionFilter().OnException(context);

        Assert.Equal(400, Assert.IsType<ObjectResult>(context.Result).StatusCode);
    }

    [Fact]
    public void Dto_validation_messages_follow_the_language()
    {
        var attribute = new LRequiredAttribute { ErrorMessage = "Informe o nome." };
        string Message(string l)
        {
            using var _ = Scope.Use(l);
            return attribute.FormatErrorMessage("Nome");
        }

        Assert.Equal("Informe o nome.", Message("pt-BR"));
        Assert.Equal("Enter the name.", Message("en"));
        Assert.Equal("Ingresa el nombre.", Message("es"));
    }
}
