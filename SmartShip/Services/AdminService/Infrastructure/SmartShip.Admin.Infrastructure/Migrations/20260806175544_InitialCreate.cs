using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartShip.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotalShipments = table.Column<int>(type: "int", nullable: false),
                    ActiveShipments = table.Column<int>(type: "int", nullable: false),
                    DeliveredToday = table.Column<int>(type: "int", nullable: false),
                    TotalCustomers = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hubs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hubs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportType = table.Column<int>(type: "int", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "DashboardMetrics",
                columns: new[] { "Id", "ActiveShipments", "DeliveredToday", "LastUpdatedAt", "TotalCustomers", "TotalShipments" },
                values: new object[] { 1, 0, 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, 0 });

            migrationBuilder.InsertData(
                table: "Hubs",
                columns: new[] { "Id", "City", "ContactPhone", "Country", "CreatedAt", "IsActive", "Name", "State" },
                values: new object[,]
                {
                    { 101, "Bengaluru", "9800000003", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Bangalore Hub", "Karnataka" },
                    { 102, "Hyderabad", "9800000004", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Hyderabad Hub", "Telangana" },
                    { 103, "Chennai", "9800000005", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Chennai Hub", "Tamil Nadu" },
                    { 104, "Kolkata", "9800000006", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Kolkata Hub", "West Bengal" },
                    { 105, "Jalandhar", "9800000007", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Jalandhar Hub", "Punjab" },
                    { 106, "Lucknow", "9800000008", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Lucknow Hub", "Uttar Pradesh" },
                    { 107, "Pune", "9800000009", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Pune Hub", "Maharashtra" },
                    { 108, "Ahmedabad", "9800000010", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Ahmedabad Hub", "Gujarat" },
                    { 109, "Jaipur", "9800000011", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Jaipur Hub", "Rajasthan" },
                    { 110, "Chandigarh", "9800000012", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Chandigarh Hub", "Chandigarh" },
                    { 111, "Indore", "9800000013", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Indore Hub", "Madhya Pradesh" },
                    { 112, "Nagpur", "9800000014", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Nagpur Hub", "Maharashtra" },
                    { 113, "Patna", "9800000015", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Patna Hub", "Bihar" },
                    { 114, "Bhopal", "9800000016", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Bhopal Hub", "Madhya Pradesh" },
                    { 115, "Kochi", "9800000017", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Kochi Hub", "Kerala" },
                    { 116, "Guwahati", "9800000018", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Guwahati Hub", "Assam" },
                    { 117, "Coimbatore", "9800000019", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Coimbatore Hub", "Tamil Nadu" },
                    { 118, "Visakhapatnam", "9800000020", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Visakhapatnam Hub", "Andhra Pradesh" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardMetrics");

            migrationBuilder.DropTable(
                name: "Hubs");

            migrationBuilder.DropTable(
                name: "Reports");
        }
    }
}
