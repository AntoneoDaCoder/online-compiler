using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerAPIApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddedMetricsFieldToSubmissionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_submissions_users_created_by",
                table: "submissions");

            migrationBuilder.RenameIndex(
                name: "ix_submissions_createdby",
                table: "submissions",
                newName: "IX_submissions_created_by");

            migrationBuilder.AddColumn<long>(
                name: "cpu_time_us",
                table: "submissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "peak_memory_bytes",
                table: "submissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "wall_time_ms",
                table: "submissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddForeignKey(
                name: "FK_submissions_users_created_by",
                table: "submissions",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_submissions_users_created_by",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "cpu_time_us",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "peak_memory_bytes",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "wall_time_ms",
                table: "submissions");

            migrationBuilder.RenameIndex(
                name: "IX_submissions_created_by",
                table: "submissions",
                newName: "ix_submissions_createdby");

            migrationBuilder.AddForeignKey(
                name: "FK_submissions_users_created_by",
                table: "submissions",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
