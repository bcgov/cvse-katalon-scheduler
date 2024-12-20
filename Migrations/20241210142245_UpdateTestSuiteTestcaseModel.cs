using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KatalonScheduler.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTestSuiteTestcaseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RelativePath",
                table: "TestSuites",
                newName: "FilePath");

            migrationBuilder.RenameColumn(
                name: "Path",
                table: "TestCases",
                newName: "FilePath");

            migrationBuilder.AddColumn<int>(
                name: "TestSuiteId",
                table: "TestCases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_TestSuiteId",
                table: "TestCases",
                column: "TestSuiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestCases_TestSuites_TestSuiteId",
                table: "TestCases",
                column: "TestSuiteId",
                principalTable: "TestSuites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestCases_TestSuites_TestSuiteId",
                table: "TestCases");

            migrationBuilder.DropIndex(
                name: "IX_TestCases_TestSuiteId",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "TestSuiteId",
                table: "TestCases");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "TestSuites",
                newName: "RelativePath");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "TestCases",
                newName: "Path");
        }
    }
}
