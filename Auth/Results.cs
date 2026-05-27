namespace Auth.Results
{
    public record RegisterResult(Guid UserId);
    public record LoginResult(Guid UserId,
                              string Email,
                              string FirstName,
                              string LastName,
                              string AccessToken,
                              int AccessTokenExpiresIn,
                              string RefreshToken,
                              int RefreshTokenExpiresIn);
    public record RefreshTokenResult(string AccessToken,
                                     int AccessTokenExpiresIn,
                                     string RefreshToken,
                                     int RefreshTokenExpiresIn);
}
