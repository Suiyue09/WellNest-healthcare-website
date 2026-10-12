using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Healthcare_Industry.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CodAmount",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNote",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCodCollected",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsCodCollected",
                table: "Orders");
        }
    }
}
