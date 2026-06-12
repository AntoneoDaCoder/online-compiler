using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerAPIApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class TempMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_problems_isdeleted",
                table: "problems");

            migrationBuilder.RenameIndex(
                name: "ix_problem_version_problemid",
                table: "problem_versions",
                newName: "IX_problem_versions_ProblemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_problem_versions_ProblemId",
                table: "problem_versions",
                newName: "ix_problem_version_problemid");

            migrationBuilder.CreateIndex(
                name: "ix_problems_isdeleted",
                table: "problems",
                column: "is_deleted");
        }
    }
}
