using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicLibrary.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBookBusinessFlowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_Isbn",
                table: "Books");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Books",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Books",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Books",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedById",
                table: "Books",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Books",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "Books",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_Isbn",
                table: "Books",
                column: "Isbn",
                unique: true,
                filter: "[Isbn] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_Isbn",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Books");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Isbn",
                table: "Books",
                column: "Isbn",
                unique: true,
                filter: "[Isbn] IS NOT NULL");
        }
    }
}
