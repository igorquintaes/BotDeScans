using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotDeScans.App.Infra.Migrations
{
    /// <inheritdoc />
    public partial class TitleUniqueName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Titles_Name",
                table: "Titles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Titles_Name",
                table: "Titles");
        }
    }
}
