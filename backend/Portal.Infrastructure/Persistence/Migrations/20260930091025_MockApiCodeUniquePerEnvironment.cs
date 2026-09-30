using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MockApiCodeUniquePerEnvironment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MockApis_Code",
                table: "MockApis");

            migrationBuilder.CreateIndex(
                name: "IX_MockApis_Environment_Code",
                table: "MockApis",
                columns: new[] { "Environment", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MockApis_Environment_Code",
                table: "MockApis");

            migrationBuilder.CreateIndex(
                name: "IX_MockApis_Code",
                table: "MockApis",
                column: "Code",
                unique: true);
        }
    }
}
