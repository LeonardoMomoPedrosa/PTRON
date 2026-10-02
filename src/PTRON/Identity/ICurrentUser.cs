namespace PTRON.Identity;

/// <summary>The signed-in user for this request or Blazor circuit. Empty when anonymous.</summary>
public interface ICurrentUser
{
    string UserId { get; }
}
