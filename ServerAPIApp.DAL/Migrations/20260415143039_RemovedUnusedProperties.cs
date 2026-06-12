using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerAPIApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemovedUnusedProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_created_by",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_createdby",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_createdby",
                table: "users",
                column: "created_by");

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_created_by",
                table: "users",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
