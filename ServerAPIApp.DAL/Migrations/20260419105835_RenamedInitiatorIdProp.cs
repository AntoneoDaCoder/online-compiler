using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerAPIApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenamedInitiatorIdProp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_problem_deletion_requests_users_InitiatorId",
                table: "problem_deletion_requests");

            migrationBuilder.RenameColumn(
                name: "InitiatorId",
                table: "problem_deletion_requests",
                newName: "initiator_id");

            migrationBuilder.RenameIndex(
                name: "IX_problem_deletion_requests_InitiatorId",
                table: "problem_deletion_requests",
                newName: "IX_problem_deletion_requests_initiator_id");

            migrationBuilder.AddForeignKey(
                name: "FK_problem_deletion_requests_users_initiator_id",
                table: "problem_deletion_requests",
                column: "initiator_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_problem_deletion_requests_users_initiator_id",
                table: "problem_deletion_requests");

            migrationBuilder.RenameColumn(
                name: "initiator_id",
                table: "problem_deletion_requests",
                newName: "InitiatorId");

            migrationBuilder.RenameIndex(
                name: "IX_problem_deletion_requests_initiator_id",
                table: "problem_deletion_requests",
                newName: "IX_problem_deletion_requests_InitiatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_problem_deletion_requests_users_InitiatorId",
                table: "problem_deletion_requests",
                column: "InitiatorId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
