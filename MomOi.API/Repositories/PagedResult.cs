using System;
using System.Collections.Generic;

namespace MomOi.API.Repositories
{
    /// <summary>
    /// Kết quả của một truy vấn có phân trang.
    ///
    /// Vì sao cần lớp này thay vì chỉ trả về danh sách?
    /// Client cần biết TỔNG số bản ghi mới vẽ được thanh phân trang ("trang 3/47").
    /// Mà tổng số đó phải lấy bằng một câu COUNT(*) riêng chạy trên database —
    /// không suy ra được từ số phần tử của trang hiện tại.
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>Các bản ghi của riêng trang hiện tại.</summary>
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

        /// <summary>Trang hiện tại, đánh số từ 1.</summary>
        public int Page { get; init; }

        /// <summary>Số bản ghi tối đa trên một trang.</summary>
        public int PageSize { get; init; }

        /// <summary>Tổng số bản ghi khớp điều kiện lọc (trên toàn bộ các trang).</summary>
        public int TotalItems { get; init; }

        /// <summary>Tổng số trang. Là giá trị suy ra (derived), không lưu.</summary>
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;
    }
}
