using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOkpTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "okp_dictionary");

            migrationBuilder.AddColumn<string>(
                name: "agent_type",
                table: "agents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "applied_workflows",
                table: "agents",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "okpd2_code",
                table: "agents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "okpd2_codes",
                table: "agents",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "tech_process_id",
                table: "agents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "workflow_applied",
                table: "agents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "workflow_status",
                table: "agents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "okpd2_dictionary",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    parent_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_okpd2_dictionary", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pending_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    agent_id = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    workflow_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_pending_messages_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tech_processes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    source_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    source_sheet_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    source_checksum = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    imported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tech_processes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tech_process_operations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tech_process_id = table.Column<int>(type: "integer", nullable: false),
                    op_no = table.Column<int>(type: "integer", nullable: true),
                    op_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    blank_code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    setup_time_min = table.Column<double>(type: "double precision", nullable: true),
                    process_time_min = table.Column<double>(type: "double precision", nullable: true),
                    service_time_min = table.Column<double>(type: "double precision", nullable: true),
                    equipment_code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tool_codes = table.Column<string>(type: "text", nullable: true),
                    profession_code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    extra = table.Column<string>(type: "text", nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tech_process_operations", x => x.id);
                    table.ForeignKey(
                        name: "FK_tech_process_operations_tech_processes_tech_process_id",
                        column: x => x.tech_process_id,
                        principalTable: "tech_processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agents_tech_process_id",
                table: "agents",
                column: "tech_process_id");

            migrationBuilder.CreateIndex(
                name: "IX_okpd2_dictionary_code",
                table: "okpd2_dictionary",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_okpd2_dictionary_level",
                table: "okpd2_dictionary",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "IX_okpd2_dictionary_parent_code",
                table: "okpd2_dictionary",
                column: "parent_code");

            migrationBuilder.CreateIndex(
                name: "IX_pending_messages_agent_id",
                table: "pending_messages",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_pending_messages_status",
                table: "pending_messages",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_tech_process_operations_tech_process_id",
                table: "tech_process_operations",
                column: "tech_process_id");

            migrationBuilder.CreateIndex(
                name: "IX_tech_processes_code",
                table: "tech_processes",
                column: "code");

            migrationBuilder.AddForeignKey(
                name: "FK_agents_tech_processes_tech_process_id",
                table: "agents",
                column: "tech_process_id",
                principalTable: "tech_processes",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agents_tech_processes_tech_process_id",
                table: "agents");

            migrationBuilder.DropTable(
                name: "okpd2_dictionary");

            migrationBuilder.DropTable(
                name: "pending_messages");

            migrationBuilder.DropTable(
                name: "tech_process_operations");

            migrationBuilder.DropTable(
                name: "tech_processes");

            migrationBuilder.DropIndex(
                name: "IX_agents_tech_process_id",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "agent_type",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "applied_workflows",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "okpd2_code",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "okpd2_codes",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "tech_process_id",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "workflow_applied",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "workflow_status",
                table: "agents");

            migrationBuilder.CreateTable(
                name: "okp_dictionary",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_okp_dictionary", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_okp_dictionary_name",
                table: "okp_dictionary",
                column: "name");
        }
    }
}
