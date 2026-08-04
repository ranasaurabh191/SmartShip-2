

namespace SmartShip.Admin.Application.DTOs
{
    public record ReportDto(int Id, string Title, string ReportType, DateTime FromDate, DateTime ToDate, DateTime GeneratedAt, object Data);

}
