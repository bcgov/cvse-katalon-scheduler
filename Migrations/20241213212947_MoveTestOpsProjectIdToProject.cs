using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KatalonScheduler.Migrations
{
    /// <inheritdoc />
    public partial class MoveTestOpsProjectIdToProject : Migration
    {
        /// <inheritdoc />
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add TestOpsProjectId to Projects table
    migrationBuilder.AddColumn<long>(
        name: "TestOpsProjectId",
        table: "Projects",
        type: "INTEGER",
        nullable: true);

    // Copy TestOpsProjectId from Organization to its Projects
    migrationBuilder.Sql(@"
        UPDATE Projects 
        SET TestOpsProjectId = (
            SELECT TestOpsProjectId 
            FROM Organizations 
            WHERE Organizations.Id = Projects.OrganizationId
        )");

    // Remove TestOpsProjectId from Organizations
    migrationBuilder.DropColumn(
        name: "TestOpsProjectId",
        table: "Organizations");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Add TestOpsProjectId back to Organizations
    migrationBuilder.AddColumn<int>(
        name: "TestOpsProjectId",
        table: "Organizations",
        type: "INTEGER",
        nullable: false,
        defaultValue: 0);

    // Copy first project's TestOpsProjectId back to Organization
    migrationBuilder.Sql(@"
        UPDATE Organizations 
        SET TestOpsProjectId = (
            SELECT TestOpsProjectId 
            FROM Projects 
            WHERE Projects.OrganizationId = Organizations.Id 
            LIMIT 1
        )");

    // Remove TestOpsProjectId from Projects
    migrationBuilder.DropColumn(
        name: "TestOpsProjectId",
        table: "Projects");
}
    }
}
