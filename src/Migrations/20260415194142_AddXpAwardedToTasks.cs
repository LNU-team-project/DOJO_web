using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DOJO2.Migrations
{
    /// <inheritdoc />
    public partial class AddXpAwardedToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "xp_awarded",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xp_awarded",
                table: "tasks");
        }
    }
}
