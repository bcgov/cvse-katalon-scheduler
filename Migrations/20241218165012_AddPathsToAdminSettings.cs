using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KatalonScheduler.Migrations
{
    /// <inheritdoc />
    public partial class AddPathsToAdminSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseRepositoryPath",
                table: "AdminSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "C:\\KatalonProjects");

            migrationBuilder.AddColumn<string>(
                name: "KatalonPath",
                table: "AdminSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "C:\\Katalon\\Katalon_Studio_Engine_Windows_64-10.0.1\\katalonc.exe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseRepositoryPath",
                table: "AdminSettings");

            migrationBuilder.DropColumn(
                name: "KatalonPath",
                table: "AdminSettings");
        }
    }
}
