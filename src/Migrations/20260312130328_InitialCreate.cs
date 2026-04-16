using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DOJO2.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        private const string UsersTable = "users";
        private const string GoalsTable = "goals";
        private const string TasksTable = "tasks";
        private const string PomodorosTable = "pomodoros";
        private const string IntegerType = "integer";
        private const string SmallIntType = "smallint";
        private const string TimestampWithTimeZoneType = "timestamp with time zone";
        private const string CharacterVarying255Type = "character varying(255)";
        private const string ValueGenerationStrategyAnnotation = "Npgsql:ValueGenerationStrategy";
        private const string NowSqlExpression = "NOW()";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: UsersTable,
                columns: table => new
                {
                    id = table.Column<int>(type: IntegerType, nullable: false)
                        .Annotation(ValueGenerationStrategyAnnotation, NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    email = table.Column<string>(type: CharacterVarying255Type, maxLength: 255, nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    exp_points = table.Column<int>(type: IntegerType, nullable: false, defaultValue: 0),
                    level = table.Column<int>(type: IntegerType, nullable: false, defaultValue: 1),
                    current_streak = table.Column<int>(type: IntegerType, nullable: false, defaultValue: 0),
                    last_completion_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: TimestampWithTimeZoneType, nullable: false, defaultValueSql: NowSqlExpression)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: GoalsTable,
                columns: table => new
                {
                    id = table.Column<int>(type: IntegerType, nullable: false)
                        .Annotation(ValueGenerationStrategyAnnotation, NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<int>(type: IntegerType, nullable: false),
                    title = table.Column<string>(type: CharacterVarying255Type, maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    priority = table.Column<short>(type: SmallIntType, nullable: false, defaultValue: (short)2),
                    progress = table.Column<short>(type: SmallIntType, nullable: false, defaultValue: (short)0),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: TimestampWithTimeZoneType, nullable: false, defaultValueSql: NowSqlExpression),
                    updated_at = table.Column<DateTime>(type: TimestampWithTimeZoneType, nullable: false, defaultValueSql: NowSqlExpression)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goals", x => x.id);
                    table.CheckConstraint("chk_goal_priority", "priority BETWEEN 1 AND 3");
                    table.CheckConstraint("chk_goal_progress", "progress BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_goals_users_user_id",
                        column: x => x.user_id,
                        principalTable: UsersTable,
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: TasksTable,
                columns: table => new
                {
                    id = table.Column<int>(type: IntegerType, nullable: false)
                        .Annotation(ValueGenerationStrategyAnnotation, NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<int>(type: IntegerType, nullable: false),
                    goal_id = table.Column<int>(type: IntegerType, nullable: true),
                    parent_task_id = table.Column<int>(type: IntegerType, nullable: true),
                    title = table.Column<string>(type: CharacterVarying255Type, maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    priority = table.Column<short>(type: SmallIntType, nullable: false, defaultValue: (short)2),
                    completed_at = table.Column<DateTime>(type: TimestampWithTimeZoneType, nullable: true),
                    created_at = table.Column<DateTime>(type: TimestampWithTimeZoneType, nullable: false, defaultValueSql: NowSqlExpression)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                    table.CheckConstraint("chk_no_self_parent", "id <> parent_task_id");
                    table.CheckConstraint("chk_task_priority", "priority BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_tasks_goals_goal_id",
                        column: x => x.goal_id,
                        principalTable: GoalsTable,
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tasks_tasks_parent_task_id",
                        column: x => x.parent_task_id,
                        principalTable: TasksTable,
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tasks_users_user_id",
                        column: x => x.user_id,
                        principalTable: UsersTable,
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    id = table.Column<int>(type: IntegerType, nullable: false)
                        .Annotation(ValueGenerationStrategyAnnotation, NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    task_id = table.Column<int>(type: IntegerType, nullable: false),
                    file_name = table.Column<string>(type: CharacterVarying255Type, maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: TimestampWithTimeZoneType, nullable: false, defaultValueSql: NowSqlExpression)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_attachments_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: TasksTable,
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: PomodorosTable,
                columns: table => new
                {
                    id = table.Column<int>(type: IntegerType, nullable: false)
                        .Annotation(ValueGenerationStrategyAnnotation, NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<int>(type: IntegerType, nullable: false),
                    task_id = table.Column<int>(type: IntegerType, nullable: true),
                    start_time = table.Column<DateTime>(type: TimestampWithTimeZoneType, nullable: false),
                    end_time = table.Column<DateTime>(type: TimestampWithTimeZoneType, nullable: true),
                    duration_minutes = table.Column<short>(type: SmallIntType, nullable: true),
                    work_cycles = table.Column<short>(type: SmallIntType, nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pomodoros", x => x.id);
                    table.CheckConstraint("chk_pomodoro_duration", "duration_minutes > 0");
                    table.CheckConstraint("chk_pomodoro_end_after_start", "end_time IS NULL OR end_time > start_time");
                    table.CheckConstraint("chk_pomodoro_work_cycles", "work_cycles > 0");
                    table.ForeignKey(
                        name: "FK_pomodoros_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: TasksTable,
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pomodoros_users_user_id",
                        column: x => x.user_id,
                        principalTable: UsersTable,
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_task_id",
                table: "attachments",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_goals_is_completed",
                table: GoalsTable,
                column: "is_completed");

            migrationBuilder.CreateIndex(
                name: "IX_goals_user_id",
                table: GoalsTable,
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pomodoros_task_id",
                table: PomodorosTable,
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_pomodoros_user_id",
                table: PomodorosTable,
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_goal_id",
                table: TasksTable,
                column: "goal_id");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_is_completed",
                table: TasksTable,
                column: "is_completed");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_parent_task_id",
                table: TasksTable,
                column: "parent_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_user_id",
                table: TasksTable,
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: UsersTable,
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_user_name",
                table: UsersTable,
                column: "user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachments");

            migrationBuilder.DropTable(
                name: PomodorosTable);

            migrationBuilder.DropTable(
                name: TasksTable);

            migrationBuilder.DropTable(
                name: GoalsTable);

            migrationBuilder.DropTable(
                name: UsersTable);
        }
    }
}
