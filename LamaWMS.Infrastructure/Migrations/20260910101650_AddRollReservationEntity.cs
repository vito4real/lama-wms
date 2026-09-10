using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamaWMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRollReservationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RollReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RollId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductFabricId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservedLength = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RollReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RollReservations_ProductFabrics_ProductFabricId",
                        column: x => x.ProductFabricId,
                        principalTable: "ProductFabrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RollReservations_Rolls_RollId",
                        column: x => x.RollId,
                        principalTable: "Rolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RollReservations_ProductFabricId",
                table: "RollReservations",
                column: "ProductFabricId");

            migrationBuilder.CreateIndex(
                name: "IX_RollReservations_RollId",
                table: "RollReservations",
                column: "RollId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RollReservations");
        }
    }
}
