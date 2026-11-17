using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamaWMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFurnitureLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FurnitureLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Complectation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Designation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FunctionalPurpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeclarationNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeclarationValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DeclarationValidTo = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FabricGroup = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ManufactureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Copies = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FurnitureLabels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FurnitureLabelPrintEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FurnitureLabelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Copies = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FurnitureLabelPrintEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FurnitureLabelPrintEvents_FurnitureLabels_FurnitureLabelId",
                        column: x => x.FurnitureLabelId,
                        principalTable: "FurnitureLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FurnitureLabelPrintEvents_FurnitureLabelId",
                table: "FurnitureLabelPrintEvents",
                column: "FurnitureLabelId");

            migrationBuilder.CreateIndex(
                name: "IX_FurnitureLabels_CreatedAt",
                table: "FurnitureLabels",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FurnitureLabelPrintEvents");

            migrationBuilder.DropTable(
                name: "FurnitureLabels");
        }
    }
}
