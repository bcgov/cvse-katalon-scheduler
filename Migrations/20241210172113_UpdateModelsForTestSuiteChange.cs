using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KatalonScheduler.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsForTestSuiteChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastResult",
                table: "ScheduledTests",
                newName: "NextRun");

            migrationBuilder.AddColumn<string>(
                name: "DayOfWeek",
                table: "ScheduledTests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Hour",
                table: "ScheduledTests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Minute",
                table: "ScheduledTests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TestCaseId",
                table: "ScheduledTests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestSuiteId",
                table: "ScheduledTests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTests_TestCaseId",
                table: "ScheduledTests",
                column: "TestCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTests_TestSuiteId",
                table: "ScheduledTests",
                column: "TestSuiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledTests_TestCases_TestCaseId",
                table: "ScheduledTests",
                column: "TestCaseId",
                principalTable: "TestCases",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledTests_TestSuites_TestSuiteId",
                table: "ScheduledTests",
                column: "TestSuiteId",
                principalTable: "TestSuites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledTests_TestCases_TestCaseId",
                table: "ScheduledTests");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledTests_TestSuites_TestSuiteId",
                table: "ScheduledTests");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledTests_TestCaseId",
                table: "ScheduledTests");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledTests_TestSuiteId",
                table: "ScheduledTests");

            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "ScheduledTests");

            migrationBuilder.DropColumn(
                name: "Hour",
                table: "ScheduledTests");

            migrationBuilder.DropColumn(
                name: "Minute",
                table: "ScheduledTests");

            migrationBuilder.DropColumn(
                name: "TestCaseId",
                table: "ScheduledTests");

            migrationBuilder.DropColumn(
                name: "TestSuiteId",
                table: "ScheduledTests");

            migrationBuilder.RenameColumn(
                name: "NextRun",
                table: "ScheduledTests",
                newName: "LastResult");
        }
    }
}
