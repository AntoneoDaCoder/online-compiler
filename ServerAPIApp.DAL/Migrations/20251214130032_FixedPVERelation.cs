using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerAPIApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixedPVERelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_problem_versions_AspNetUsers_CreatorId",
                table: "problem_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_problem_versions_AspNetUsers_PublisherId",
                table: "problem_versions");

            migrationBuilder.DropIndex(
                name: "IX_problem_versions_CreatorId",
                table: "problem_versions");

            migrationBuilder.DropIndex(
                name: "IX_problem_versions_PublisherId",
                table: "problem_versions");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "problem_versions");

            migrationBuilder.DropColumn(
                name: "PublisherId",
                table: "problem_versions");

            migrationBuilder.AlterColumn<Guid>(
                name: "published_by",
                table: "problem_versions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_problem_versions_published_by",
                table: "problem_versions",
                column: "published_by");

            migrationBuilder.AddForeignKey(
                name: "FK_problem_versions_AspNetUsers_created_by",
                table: "problem_versions",
                column: "created_by",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_problem_versions_AspNetUsers_published_by",
                table: "problem_versions",
                column: "published_by",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_problem_versions_AspNetUsers_created_by",
                table: "problem_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_problem_versions_AspNetUsers_published_by",
                table: "problem_versions");

            migrationBuilder.DropIndex(
                name: "IX_problem_versions_published_by",
                table: "problem_versions");

            migrationBuilder.AlterColumn<Guid>(
                name: "published_by",
                table: "problem_versions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "problem_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublisherId",
                table: "problem_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_problem_versions_CreatorId",
                table: "problem_versions",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_problem_versions_PublisherId",
                table: "problem_versions",
                column: "PublisherId");

            migrationBuilder.AddForeignKey(
                name: "FK_problem_versions_AspNetUsers_CreatorId",
                table: "problem_versions",
                column: "CreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_problem_versions_AspNetUsers_PublisherId",
                table: "problem_versions",
                column: "PublisherId",
                principalTable: "AspNetUsers",
                principalColumn: "id");
        }
    }
}
