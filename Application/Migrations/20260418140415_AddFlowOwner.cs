using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Migrations
{
    /// <inheritdoc />
    public partial class AddFlowOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CurrentNodeId",
                table: "UserSessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // ── Step 1: add both columns as NULLABLE ─────────────────────────────────
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Flows",
                type: "uuid",
                nullable: true);          // <-- nullable, not false

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Offers",
                type: "uuid",
                nullable: true);          // <-- nullable, not false

            // ── Step 2: assign every orphan row to your real UserProfile ─────────────
            // Run this SQL first to find your UserProfile.Id:
            //   SELECT up."Id", au."Email" FROM "UserProfiles" up
            //   JOIN "Users" au ON au."Id" = up."ApplicationUserId"::text;
            // Then paste the UUID below.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE default_owner uuid;
                BEGIN
                    SELECT ""Id"" INTO default_owner FROM ""UserProfiles"" LIMIT 1;
                    IF default_owner IS NOT NULL THEN
                        UPDATE ""Flows""  SET ""OwnerId"" = default_owner WHERE ""OwnerId"" IS NULL;
                        UPDATE ""Offers"" SET ""OwnerId"" = default_owner WHERE ""OwnerId"" IS NULL;
                    END IF;
                END $$;
            ");

            // ── Step 3: tighten to NOT NULL now every row has a valid value ───────────
            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Flows",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Offers",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // ── Step 4: indexes and FK constraints ───────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_Flows_OwnerId",
                table: "Flows",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_OwnerId",
                table: "Offers",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Flows_UserProfiles_OwnerId",
                table: "Flows",
                column: "OwnerId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_UserProfiles_OwnerId",
                table: "Offers",
                column: "OwnerId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flows_UserProfiles_OwnerId",
                table: "Flows");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_UserProfiles_OwnerId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_OwnerId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Flows_OwnerId",
                table: "Flows");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Flows");

            migrationBuilder.AlterColumn<Guid>(
                name: "CurrentNodeId",
                table: "UserSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
