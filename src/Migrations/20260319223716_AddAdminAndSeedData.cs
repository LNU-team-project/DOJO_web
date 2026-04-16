using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.Identity;

#nullable disable

namespace DOJO2.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAndSeedData : Migration
    {
        private const string AdminsTable = "admins";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_admins_login",
                table: AdminsTable,
                column: "login",
                unique: true);

            var passwordHasher = new PasswordHasher<string>();
            var hashedPassword = passwordHasher.HashPassword(null, "24062006");

            migrationBuilder.InsertData(
                table: AdminsTable,
                columns: new[] { "login", "password" },
                values: new object[] { "sxolixs", hashedPassword });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_admins_login",
                table: AdminsTable);

            migrationBuilder.DeleteData(
                table: AdminsTable,
                keyColumn: "login",
                keyValue: "sxolixs");
        }
    }
}
