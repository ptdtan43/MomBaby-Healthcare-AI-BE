using System.ComponentModel.DataAnnotations;

namespace MomOi.API.DTOs.Auth
{
    /// <summary>
    /// Password re-confirmation required to permanently delete the account.
    /// </summary>
    public class DeleteAccountDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu để xác nhận.")]
        public string Password { get; set; } = string.Empty;
    }
}
