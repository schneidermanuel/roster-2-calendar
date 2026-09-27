using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RosterSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkType",
                table: "SyncConfigs",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Html")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkType",
                table: "SyncConfigs");
        }
    }
}
