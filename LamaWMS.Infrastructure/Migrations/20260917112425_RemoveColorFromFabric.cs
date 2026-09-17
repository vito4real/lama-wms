using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamaWMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveColorFromFabric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Fabrics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Fabrics",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
