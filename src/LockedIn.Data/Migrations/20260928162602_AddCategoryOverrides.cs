using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LockedIn.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryOverrides",
                columns: table => new
                {
                    AppName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false, collation: "NOCASE"),
                    Category = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryOverrides", x => x.AppName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryOverrides");
        }
    }
}
