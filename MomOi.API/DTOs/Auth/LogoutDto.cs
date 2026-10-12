namespace MomOi.API.DTOs.Auth
{
    /// <summary>
    /// Optional logout body. With a refresh token only that device is signed out;
    /// without one every device of the user is signed out.
    /// </summary>
    public class LogoutDto
    {
        public string? RefreshToken { get; set; }
    }
}
