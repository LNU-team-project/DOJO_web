using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DOJO2.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleOccurrenceExceptions : Migration
    {
        private const string ExclusionsTableName = "schedule_occurrence_exceptions";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: ExclusionsTableName,
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    schedule_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    occurrence_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_occurrence_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_schedule_occurrence_exceptions_schedules_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_occurrence_exceptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_occurrence_exceptions_occurrence_at",
                table: ExclusionsTableName,
                column: "occurrence_at");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_occurrence_exceptions_schedule_id",
                table: ExclusionsTableName,
                column: "schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_occurrence_exceptions_schedule_id_occurrence_at",
                table: ExclusionsTableName,
                columns: new[] { "schedule_id", "occurrence_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_occurrence_exceptions_user_id",
                table: ExclusionsTableName,
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: ExclusionsTableName);
        }
    }
}
