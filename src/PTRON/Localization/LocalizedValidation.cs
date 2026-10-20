using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Resources;

namespace PTRON.Localization;

/// <summary>Looks up UI texts by key outside of dependency injection (data annotation attributes).</summary>
public static class StaticText
{
    private static readonly ResourceManager Manager =
        new("PTRON.Resources.SharedResource", typeof(SharedResource).Assembly);

    public static string Get(string key) => Manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);
}

public sealed class LRequiredAttribute : RequiredAttribute
{
    public override string FormatErrorMessage(string name) => StaticText.Get(ErrorMessageString);
}

public sealed class LEmailAttribute : DataTypeAttribute
{
    private static readonly EmailAddressAttribute Inner = new();

    public LEmailAttribute() : base(DataType.EmailAddress)
    {
    }

    public override bool IsValid(object? value) => Inner.IsValid(value);

    public override string FormatErrorMessage(string name) => StaticText.Get(ErrorMessageString);
}

public sealed class LStringLengthAttribute : StringLengthAttribute
{
    public LStringLengthAttribute(int maximumLength) : base(maximumLength)
    {
    }

    public override string FormatErrorMessage(string name) => StaticText.Get(ErrorMessageString);
}

public sealed class LMinLengthAttribute : MinLengthAttribute
{
    public LMinLengthAttribute(int length) : base(length)
    {
    }

    public override string FormatErrorMessage(string name) => StaticText.Get(ErrorMessageString);
}

public sealed class LCompareAttribute : CompareAttribute
{
    public LCompareAttribute(string otherProperty) : base(otherProperty)
    {
    }

    public override string FormatErrorMessage(string name) => StaticText.Get(ErrorMessageString);
}

public sealed class LRangeAttribute : RangeAttribute
{
    public LRangeAttribute(int minimum, int maximum) : base(minimum, maximum)
    {
    }

    public LRangeAttribute(double minimum, double maximum) : base(minimum, maximum)
    {
    }

    public LRangeAttribute(Type type, string minimum, string maximum) : base(type, minimum, maximum)
    {
    }

    public override string FormatErrorMessage(string name) => StaticText.Get(ErrorMessageString);
}
