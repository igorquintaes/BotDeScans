using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotDeScans.App.Infra.Migrations
{
    /// <inheritdoc />
    public partial class SkipSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkipSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Step = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkipSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkipSteps_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkipSteps_TitleId",
                table: "SkipSteps",
                column: "TitleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkipSteps");
        }
    }
}
