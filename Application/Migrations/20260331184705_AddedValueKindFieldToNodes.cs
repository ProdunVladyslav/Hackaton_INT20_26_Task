using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Migrations
{
    /// <inheritdoc />
    public partial class AddedValueKindFieldToNodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NodeOffers_Offers_OfferId",
                table: "NodeOffers");

            migrationBuilder.AddColumn<int>(
                name: "ValueKind",
                table: "Nodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NodeOffers_Offers_OfferId",
                table: "NodeOffers",
                column: "OfferId",
                principalTable: "Offers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NodeOffers_Offers_OfferId",
                table: "NodeOffers");

            migrationBuilder.DropColumn(
                name: "ValueKind",
                table: "Nodes");

            migrationBuilder.AddForeignKey(
                name: "FK_NodeOffers_Offers_OfferId",
                table: "NodeOffers",
                column: "OfferId",
                principalTable: "Offers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
