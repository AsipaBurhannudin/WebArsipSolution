namespace WebArsip.Core.DTOs
{
    public class BaseQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<T> Items { get; set; } = new();
    }
}