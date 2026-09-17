using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "borrow_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: true),
                    SuggestedTitle = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    SuggestedAuthor = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BorrowRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_borrow_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_borrow_requests_books_BookId",
                        column: x => x.BookId,
                        principalTable: "books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_borrow_requests_members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_borrow_requests_BookId",
                table: "borrow_requests",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_borrow_requests_IsDeleted",
                table: "borrow_requests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_borrow_requests_MemberId_Status",
                table: "borrow_requests",
                columns: new[] { "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_borrow_requests_Status",
                table: "borrow_requests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "borrow_requests");
        }
    }
}
