using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KatalonScheduler.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminSettingsGitPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GitRepositoryPath",
                table: "AdminSettings",
                newName: "GitExecutablePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GitExecutablePath",
                table: "AdminSettings",
                newName: "GitRepositoryPath");
        }
    }
}
