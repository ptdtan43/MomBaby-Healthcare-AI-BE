using System;
using System.ComponentModel.DataAnnotations;

namespace MomOi.API.Models
{
    /// <summary>
    /// Thuộc tính chung của mọi entity: khoá chính và dấu thời gian kiểm toán.
    /// CreatedAt/UpdatedAt được AppDbContext tự gán trong SaveChangesAsync,
    /// service không cần (và không nên) tự gán.
    /// </summary>
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
