namespace PTRON.Models;

/// <summary>Root entity owned by one user. Child rows inherit isolation through their parent.</summary>
public interface IUserOwned
{
    string UserId { get; set; }
}
