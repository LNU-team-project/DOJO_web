using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DOJO2.Migrations
{
    /// <inheritdoc />
    public partial class AddAndrewAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var passwordHasher = new PasswordHasher<string>();
            var hashedPassword = passwordHasher.HashPassword(null, "121305");

            migrationBuilder.InsertData(
                table: "admins",
                columns: new[] { "login", "password" },
                values: new object[] { "Andrew", hashedPassword });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "admins",
                keyColumn: "login",
                keyValue: "Andrew");
        }
    }
}
