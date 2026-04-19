using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerAPIApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_problems_users_initiator_id",
                table: "problems");

            migrationBuilder.DropIndex(
                name: "IX_problems_initiator_id",
                table: "problems");

            migrationBuilder.DropColumn(
                name: "initiator_id",
                table: "problems");

            migrationBuilder.AddColumn<string>(
                name: "deletion_job_id",
                table: "problems",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "problem_deletion_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_problem_deletion_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_problem_deletion_requests_problems_problem_id",
                        column: x => x.problem_id,
                        principalTable: "problems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_problem_deletion_requests_users_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_problems_deletion_job_id",
                table: "problems",
                column: "deletion_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_problem_deletion_requests_InitiatorId",
                table: "problem_deletion_requests",
                column: "InitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_problem_deletion_requests_problem_id",
                table: "problem_deletion_requests",
                column: "problem_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "problem_deletion_requests");

            migrationBuilder.DropIndex(
                name: "ix_problems_deletion_job_id",
                table: "problems");

            migrationBuilder.DropColumn(
                name: "deletion_job_id",
                table: "problems");

            migrationBuilder.AddColumn<Guid>(
                name: "initiator_id",
                table: "problems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_problems_initiator_id",
                table: "problems",
                column: "initiator_id");

            migrationBuilder.AddForeignKey(
                name: "FK_problems_users_initiator_id",
                table: "problems",
                column: "initiator_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
