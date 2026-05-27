namespace MembershipProfile
{
    /// <summary>User profile model</summary>
    //public class UserProfile
    //{
    //    public Guid Id { get; }
    //    public string Email { get; }
    //    public string FirstName { get; }
    //    public string LastName { get; }
    //    public string Role { get; }
    //    public DateTime CreatedAt { get; }
    //    public bool IsActive { get; }
    //
    //    private UserProfile(Guid id, string email, string firstName, string lastName, string role, DateTime createdAt, bool isActive)
    //    {
    //        Id = id;
    //        Email = email;
    //        FirstName = firstName;
    //        LastName = lastName;
    //        Role = role;
    //        CreatedAt = createdAt;
    //        IsActive = isActive;
    //    }
    //    public static UserProfile New(Guid id, string email, string firstName, string lastName, string role, DateTime createdAt)
    //    {
    //        return new UserProfile(id, email, firstName, lastName, role, createdAt, true);
    //    }
    //    public static UserProfile Empty() => new UserProfile(Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty, DateTime.MinValue, false);
    //
    //    public string FullName => $"{FirstName} {LastName}".Trim();
    //}
}
