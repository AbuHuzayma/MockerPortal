using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApiMocker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MockApis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Environment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockApis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MockEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MockApiId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockEndpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockEndpoints_MockApis_MockApiId",
                        column: x => x.MockApiId,
                        principalTable: "MockApis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MockEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseHeaders = table.Column<string>(type: "jsonb", nullable: true),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    DelayMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockResponses_MockEndpoints_MockEndpointId",
                        column: x => x.MockEndpointId,
                        principalTable: "MockEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockMatchRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MockResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Field = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Operator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpectedValue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockMatchRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockMatchRules_MockResponses_MockResponseId",
                        column: x => x.MockResponseId,
                        principalTable: "MockResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MockApis_Code",
                table: "MockApis",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_MockApiId",
                table: "MockEndpoints",
                column: "MockApiId");

            migrationBuilder.CreateIndex(
                name: "IX_MockMatchRules_MockResponseId",
                table: "MockMatchRules",
                column: "MockResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_MockResponses_MockEndpointId",
                table: "MockResponses",
                column: "MockEndpointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MockMatchRules");

            migrationBuilder.DropTable(
                name: "MockResponses");

            migrationBuilder.DropTable(
                name: "MockEndpoints");

            migrationBuilder.DropTable(
                name: "MockApis");
        }
    }
}
