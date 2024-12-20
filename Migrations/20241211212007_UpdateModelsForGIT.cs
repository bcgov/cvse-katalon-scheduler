using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KatalonScheduler.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsForGIT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitUrl",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitUrl",
                table: "Projects");
        }
    }
}
