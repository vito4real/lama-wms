using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamaWMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteRollTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RollTransactions_Rolls_RollId",
                table: "RollTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_RollTransactions_Rolls_RollId",
                table: "RollTransactions",
                column: "RollId",
                principalTable: "Rolls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RollTransactions_Rolls_RollId",
                table: "RollTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_RollTransactions_Rolls_RollId",
                table: "RollTransactions",
                column: "RollId",
                principalTable: "Rolls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
