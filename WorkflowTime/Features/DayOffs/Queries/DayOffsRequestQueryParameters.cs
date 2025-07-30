using WorkflowTime.Enums;

namespace WorkflowTime.Features.DayOffs.Queries
{
    public class DayOffsRequestQueryParameters
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public DayOffRequestOrderBy SortBy { get; set; } = DayOffRequestOrderBy.RequestDate;
        public List<DayOffRequestStatus>? DayOffRequestStatuses { get; set; }
        public string SortOrder { get; set; } = "asc";
        public Guid? UserId { get; set; }
    }
}
