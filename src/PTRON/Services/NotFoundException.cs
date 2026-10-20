namespace PTRON.Services;

/// <summary>A record that does not exist (or belongs to someone else). The API answers 404.</summary>
public sealed class NotFoundException : InvalidOperationException
{
    public NotFoundException(string message) : base(message)
    {
    }
}
