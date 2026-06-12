using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerAPIApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemovedCompositeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_problem_version_problemid_version",
                table: "problem_versions");

            migrationBuilder.CreateIndex(
                name: "ix_problem_version_problemid",
                table: "problem_versions",
                column: "ProblemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_problem_version_problemid",
                table: "problem_versions");

            migrationBuilder.CreateIndex(
                name: "ux_problem_version_problemid_version",
                table: "problem_versions",
                columns: new[] { "ProblemId", "version" },
                unique: true);
        }
    }
}
