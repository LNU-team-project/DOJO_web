using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DOJO2.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleItems : Migration
    {
        private const string SchedulesTableName = "schedules";
        private const string SmallIntColumnType = "smallint";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: SchedulesTableName,
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<short>(type: SmallIntColumnType, nullable: false, defaultValue: (short)60),
                    priority = table.Column<short>(type: SmallIntColumnType, nullable: false, defaultValue: (short)2),
                    recurrence_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "none"),
                    recurrence_interval = table.Column<short>(type: SmallIntColumnType, nullable: false, defaultValue: (short)1),
                    weekly_days_mask = table.Column<short>(type: SmallIntColumnType, nullable: false, defaultValue: (short)0),
                    recurrence_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedules", x => x.id);
                    table.CheckConstraint("chk_schedule_interval", "recurrence_interval > 0");
                    table.CheckConstraint("chk_schedule_priority", "priority BETWEEN 1 AND 3");
                    table.CheckConstraint("chk_schedule_week_mask", "weekly_days_mask BETWEEN 0 AND 127");
                    table.ForeignKey(
                        name: "FK_schedules_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schedules_is_active",
                table: SchedulesTableName,
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_schedules_start_at",
                table: SchedulesTableName,
                column: "start_at");

            migrationBuilder.CreateIndex(
                name: "IX_schedules_user_id",
                table: SchedulesTableName,
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: SchedulesTableName);
        }
    }
}
