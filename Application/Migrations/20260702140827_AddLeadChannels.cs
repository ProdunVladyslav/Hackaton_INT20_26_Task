using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LeadChannelId",
                table: "UserSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeadChannelId",
                table: "Leads",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeadChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShortCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadChannels_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_LeadChannelId",
                table: "UserSessions",
                column: "LeadChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_FlowId_LeadChannelId",
                table: "Leads",
                columns: new[] { "FlowId", "LeadChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_LeadChannelId",
                table: "Leads",
                column: "LeadChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadChannels_FlowId",
                table: "LeadChannels",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadChannels_ShortCode",
                table: "LeadChannels",
                column: "ShortCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_LeadChannels_LeadChannelId",
                table: "Leads",
                column: "LeadChannelId",
                principalTable: "LeadChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSessions_LeadChannels_LeadChannelId",
                table: "UserSessions",
                column: "LeadChannelId",
                principalTable: "LeadChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_LeadChannels_LeadChannelId",
                table: "Leads");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSessions_LeadChannels_LeadChannelId",
                table: "UserSessions");

            migrationBuilder.DropTable(
                name: "LeadChannels");

            migrationBuilder.DropIndex(
                name: "IX_UserSessions_LeadChannelId",
                table: "UserSessions");

            migrationBuilder.DropIndex(
                name: "IX_Leads_FlowId_LeadChannelId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_LeadChannelId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "LeadChannelId",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "LeadChannelId",
                table: "Leads");
        }
    }
}
