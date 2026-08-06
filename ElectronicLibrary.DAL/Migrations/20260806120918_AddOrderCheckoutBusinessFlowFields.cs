using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicLibrary.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCheckoutBusinessFlowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "OrderItems",
                newName: "UnitPrice");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "OrderDate",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "CouponCodeSnapshot",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CouponDiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CouponDiscountTypeSnapshot",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CouponDiscountValueSnapshot",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ListingDiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BookId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BookTitleSnapshot",
                table: "OrderItems",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ConditionSnapshot",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "OrderItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EffectiveUnitPrice",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FormatSnapshot",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LineDiscount",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineSubtotal",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SellerId",
                table: "OrderItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SellerStoreNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Listings",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_OrderDate",
                table: "Orders",
                columns: new[] { "UserId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SellerId_Status",
                table: "OrderItems",
                columns: new[] { "SellerId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId_OrderDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_SellerId_Status",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CouponCodeSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CouponDiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CouponDiscountTypeSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CouponDiscountValueSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ListingDiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SubtotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TotalDiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "BookTitleSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ConditionSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "EffectiveUnitPrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "FormatSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "LineDiscount",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "LineSubtotal",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SellerStoreNameSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Listings");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "OrderItems",
                newName: "Price");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OrderDate",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");
        }
    }
}
