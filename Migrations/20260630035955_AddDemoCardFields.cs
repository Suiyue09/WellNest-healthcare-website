using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Healthcare_Industry.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoCardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DemoCardExpiry",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DemoCardLastFour",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DemoCardholderName",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SavedDemoCardExpiry",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SavedDemoCardLastFour",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SavedDemoCardholderName",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DemoCardExpiry",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DemoCardLastFour",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DemoCardholderName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SavedDemoCardExpiry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SavedDemoCardLastFour",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SavedDemoCardholderName",
                table: "AspNetUsers");
        }
    }
}
