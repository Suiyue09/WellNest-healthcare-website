using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Healthcare_Industry.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryEarning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryEarning",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryEarning",
                table: "Orders");
        }
    }
}
