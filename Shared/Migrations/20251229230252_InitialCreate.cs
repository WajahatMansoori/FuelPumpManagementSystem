using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BatchStatus",
                columns: table => new
                {
                    BatchStatusId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchStatusName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchStatus", x => x.BatchStatusId);
                });

            migrationBuilder.CreateTable(
                name: "Dispenser",
                columns: table => new
                {
                    DispenserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiEndPoint = table.Column<string>(type: "TEXT", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispenser", x => x.DispenserId);
                });

            migrationBuilder.CreateTable(
                name: "DispenserActionLog",
                columns: table => new
                {
                    DispenserActionLogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DispenserId = table.Column<int>(type: "INTEGER", nullable: false),
                    DispenserActionTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ApiRequest = table.Column<string>(type: "TEXT", nullable: true),
                    ApiResponse = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    IsErrorOccured = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispenserActionLog", x => x.DispenserActionLogId);
                });

            migrationBuilder.CreateTable(
                name: "DispenserActionType",
                columns: table => new
                {
                    DispenserActionTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DispenserActionTypeName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispenserActionType", x => x.DispenserActionTypeId);
                });

            migrationBuilder.CreateTable(
                name: "PriceUpdateBatch",
                columns: table => new
                {
                    PriceUpdateBatchId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchExecutionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalDispensor = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BatchStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceUpdateBatch", x => x.PriceUpdateBatchId);
                });

            migrationBuilder.CreateTable(
                name: "PriceUpdateLog",
                columns: table => new
                {
                    PriceUpdateLogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DispensorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ApiRequest = table.Column<string>(type: "TEXT", nullable: true),
                    ApiResponse = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    IsErrorOccured = table.Column<bool>(type: "INTEGER", nullable: false),
                    PriceUpdateBatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRecallAndResolve = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceUpdateLog", x => x.PriceUpdateLogId);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductName = table.Column<string>(type: "TEXT", nullable: true),
                    ProductPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProductColorCode = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.ProductId);
                });

            migrationBuilder.CreateTable(
                name: "SiteDetail",
                columns: table => new
                {
                    SiteDetailId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteName = table.Column<string>(type: "TEXT", nullable: true),
                    SiteAddress = table.Column<string>(type: "TEXT", nullable: true),
                    SitePhone = table.Column<string>(type: "TEXT", nullable: true),
                    SiteLogo = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteDetail", x => x.SiteDetailId);
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DispenserId = table.Column<int>(type: "INTEGER", nullable: false),
                    NozzleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Liter = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProductTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.TransactionId);
                });

            migrationBuilder.CreateTable(
                name: "DispenserNozzle",
                columns: table => new
                {
                    DispenserNozzleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DispenserId = table.Column<int>(type: "INTEGER", nullable: false),
                    NozzleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsEnable = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    LastTotalLiter = table.Column<decimal>(type: "TEXT", nullable: true),
                    LastTotalCash = table.Column<decimal>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispenserNozzle", x => x.DispenserNozzleId);
                    table.ForeignKey(
                        name: "FK_DispenserNozzle_Dispenser_DispenserId",
                        column: x => x.DispenserId,
                        principalTable: "Dispenser",
                        principalColumn: "DispenserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispenserNozzle_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "ProductId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispenserNozzle_DispenserId",
                table: "DispenserNozzle",
                column: "DispenserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispenserNozzle_ProductId",
                table: "DispenserNozzle",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatchStatus");

            migrationBuilder.DropTable(
                name: "DispenserActionLog");

            migrationBuilder.DropTable(
                name: "DispenserActionType");

            migrationBuilder.DropTable(
                name: "DispenserNozzle");

            migrationBuilder.DropTable(
                name: "PriceUpdateBatch");

            migrationBuilder.DropTable(
                name: "PriceUpdateLog");

            migrationBuilder.DropTable(
                name: "SiteDetail");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "Dispenser");

            migrationBuilder.DropTable(
                name: "Product");
        }
    }
}
