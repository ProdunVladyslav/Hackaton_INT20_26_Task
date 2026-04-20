using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewNodeTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "DigitalContent",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "PhysicalWellnessKitName",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Offers");

            migrationBuilder.RenameColumn(
                name: "PhysicalWellnessKitItems",
                table: "Offers",
                newName: "Body");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "UserSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConvertedAt",
                table: "SessionOffers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreDelta",
                table: "Options",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Offers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "CalendarUrl",
                table: "Offers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Headline",
                table: "Offers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedOwnerId",
                table: "NodeOffers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalendarProvider",
                table: "NodeOffers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "NodeOffers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Hot");

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompanySize = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TerminalNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TerminalNodeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "New"),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeToCompleteSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leads_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leads_Nodes_TerminalNodeId",
                        column: x => x.TerminalNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leads_UserProfiles_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Leads_UserSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "UserSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NodeLeadCaptures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeLeadCaptures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeLeadCaptures_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeRedirects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Headline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RedirectUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AutoRedirectAfterSeconds = table.Column<int>(type: "integer", nullable: true),
                    Tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeRedirects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeRedirects_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeLeadCaptureFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeLeadCaptureId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Placeholder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeLeadCaptureFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeLeadCaptureFields_NodeLeadCaptures_NodeLeadCaptureId",
                        column: x => x.NodeLeadCaptureId,
                        principalTable: "NodeLeadCaptures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeRedirectLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeRedirectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeRedirectLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeRedirectLinks_NodeRedirects_NodeRedirectId",
                        column: x => x.NodeRedirectId,
                        principalTable: "NodeRedirects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NodeOffers_AssignedOwnerId",
                table: "NodeOffers",
                column: "AssignedOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_AssignedToId",
                table: "Leads",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_FlowId",
                table: "Leads",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_FlowId_Email",
                table: "Leads",
                columns: new[] { "FlowId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_FlowId_Status",
                table: "Leads",
                columns: new[] { "FlowId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_FlowId_Tier",
                table: "Leads",
                columns: new[] { "FlowId", "Tier" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_SessionId",
                table: "Leads",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TerminalNodeId",
                table: "Leads",
                column: "TerminalNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeLeadCaptureFields_NodeLeadCaptureId_DisplayOrder",
                table: "NodeLeadCaptureFields",
                columns: new[] { "NodeLeadCaptureId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NodeLeadCaptureFields_NodeLeadCaptureId_FieldType",
                table: "NodeLeadCaptureFields",
                columns: new[] { "NodeLeadCaptureId", "FieldType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeLeadCaptures_NodeId",
                table: "NodeLeadCaptures",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeRedirectLinks_NodeRedirectId_DisplayOrder",
                table: "NodeRedirectLinks",
                columns: new[] { "NodeRedirectId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NodeRedirects_NodeId",
                table: "NodeRedirects",
                column: "NodeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NodeOffers_UserProfiles_AssignedOwnerId",
                table: "NodeOffers",
                column: "AssignedOwnerId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NodeOffers_UserProfiles_AssignedOwnerId",
                table: "NodeOffers");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "NodeLeadCaptureFields");

            migrationBuilder.DropTable(
                name: "NodeRedirectLinks");

            migrationBuilder.DropTable(
                name: "NodeLeadCaptures");

            migrationBuilder.DropTable(
                name: "NodeRedirects");

            migrationBuilder.DropIndex(
                name: "IX_NodeOffers_AssignedOwnerId",
                table: "NodeOffers");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "ConvertedAt",
                table: "SessionOffers");

            migrationBuilder.DropColumn(
                name: "ScoreDelta",
                table: "Options");

            migrationBuilder.DropColumn(
                name: "CalendarUrl",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "Headline",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "AssignedOwnerId",
                table: "NodeOffers");

            migrationBuilder.DropColumn(
                name: "CalendarProvider",
                table: "NodeOffers");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "NodeOffers");

            migrationBuilder.RenameColumn(
                name: "Body",
                table: "Offers",
                newName: "PhysicalWellnessKitItems");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Offers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Offers",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DigitalContent",
                table: "Offers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Offers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhysicalWellnessKitName",
                table: "Offers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Offers",
                type: "numeric(18,2)",
                nullable: true);
        }
    }
}
