using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Healthcare_Industry.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimDeadline",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryProofImageUrl",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimDeadline",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryProofImageUrl",
                table: "Orders");
        }
    }
}
