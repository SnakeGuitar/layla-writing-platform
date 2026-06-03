namespace Layla.Core.Contracts.Admin;

public class SystemReportDto
{
    public DateTime GeneratedAt { get; set; }
    public int TotalUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int BannedUsers { get; set; }
    public int TotalProjects { get; set; }
    public int ProjectsModifiedToday { get; set; }
    public int PublicProjects { get; set; }
    public int[] NewUsersPerMonth { get; set; } = new int[12];
}
