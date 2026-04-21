using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerAPIApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddedIsApprovedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_approved",
                table: "problem_deletion_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_approved",
                table: "problem_deletion_requests");
        }
    }
}
