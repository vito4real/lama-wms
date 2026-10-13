using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamaWMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTrackingToWarehouseOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "RollTransactions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "RollReservations",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RollTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "RollReservations");
        }
    }
}
