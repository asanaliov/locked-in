using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LockedIn.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryFromTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CategoryFromTitle",
                table: "UsageSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Older sessions didn't record this. For the default browsers, a session whose category differs
            // from the browser's own setting (its override, or Neutral) can only have come from a title keyword.
            migrationBuilder.Sql("""
                UPDATE UsageSessions
                SET CategoryFromTitle = 1
                WHERE AppName IN ('chrome', 'msedge', 'firefox', 'brave', 'opera', 'vivaldi', 'arc')
                  AND Category <> COALESCE(
                      (SELECT o.Category FROM CategoryOverrides o WHERE o.AppName = UsageSessions.AppName),
                      'Neutral');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryFromTitle",
                table: "UsageSessions");
        }
    }
}
