using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TimeSheet.Infrastructure.Persistence;

public class TimeSheetDbContextFactory : IDesignTimeDbContextFactory<TimeSheetDbContext>
{
  public TimeSheetDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<TimeSheetDbContext>();
    optionsBuilder.UseSqlite("Data Source=timesheet.db");
    
    return new TimeSheetDbContext(optionsBuilder.Options);
  }
}
