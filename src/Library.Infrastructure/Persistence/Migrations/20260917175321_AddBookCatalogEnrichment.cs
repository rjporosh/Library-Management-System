using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookCatalogEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudiobookUrl",
                table: "books",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "books",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EbookUrl",
                table: "books",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Edition",
                table: "books",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalBuyUrl",
                table: "books",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalPdfUrl",
                table: "books",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAudiobook",
                table: "books",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasEbook",
                table: "books",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudiobookUrl",
                table: "books");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "books");

            migrationBuilder.DropColumn(
                name: "EbookUrl",
                table: "books");

            migrationBuilder.DropColumn(
                name: "Edition",
                table: "books");

            migrationBuilder.DropColumn(
                name: "ExternalBuyUrl",
                table: "books");

            migrationBuilder.DropColumn(
                name: "ExternalPdfUrl",
                table: "books");

            migrationBuilder.DropColumn(
                name: "HasAudiobook",
                table: "books");

            migrationBuilder.DropColumn(
                name: "HasEbook",
                table: "books");
        }
    }
}
