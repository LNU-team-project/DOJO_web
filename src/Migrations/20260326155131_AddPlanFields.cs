using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DOJO2.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanFields : Migration
    {
        private const string TasksTableName = "tasks";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_plan",
                table: TasksTableName,
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_at",
                table: TasksTableName,
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tasks_is_plan",
                table: TasksTableName,
                column: "is_plan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tasks_is_plan",
                table: TasksTableName);

            migrationBuilder.DropColumn(
                name: "is_plan",
                table: TasksTableName);

            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: TasksTableName);
        }
    }
}
