using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamaWMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeRollNumberUniquePerFabric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rolls_FabricId",
                table: "Rolls");

            migrationBuilder.DropIndex(
                name: "IX_Rolls_Number",
                table: "Rolls");

            migrationBuilder.CreateIndex(
                name: "IX_Rolls_FabricId_Number",
                table: "Rolls",
                columns: new[] { "FabricId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rolls_FabricId_Number",
                table: "Rolls");

            migrationBuilder.CreateIndex(
                name: "IX_Rolls_FabricId",
                table: "Rolls",
                column: "FabricId");

            migrationBuilder.CreateIndex(
                name: "IX_Rolls_Number",
                table: "Rolls",
                column: "Number",
                unique: true);
        }
    }
}
