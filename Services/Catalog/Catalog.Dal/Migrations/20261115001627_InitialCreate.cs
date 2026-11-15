using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.CheckConstraint("CK_Categories_DisplayOrder", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_Categories_NotOwnParent", "[ParentCategoryId] IS NULL OR [ParentCategoryId] <> [Id]");
                    table.CheckConstraint("CK_Categories_Slug", "LEN([Slug]) > 0 AND [Slug] NOT LIKE N'% %'");
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BrandId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItems", x => x.Id);
                    table.CheckConstraint("CK_CatalogItems_ItemType", "[ItemType] IN (N'Part', N'Service')");
                    table.CheckConstraint("CK_CatalogItems_Price", "[Price] > 0");
                    table.CheckConstraint("CK_CatalogItems_ServiceHasNoBrand", "[ItemType] = N'Part' OR [BrandId] IS NULL");
                    table.ForeignKey(
                        name: "FK_CatalogItems_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogItemCategories",
                columns: table => new
                {
                    CatalogItemId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItemCategories", x => new { x.CatalogItemId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_CatalogItemCategories_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogItemCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogItemDetails",
                columns: table => new
                {
                    CatalogItemId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    WarrantyMonths = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Specifications = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItemDetails", x => x.CatalogItemId);
                    table.CheckConstraint("CK_CatalogItemDetails_Duration", "[EstimatedDurationMinutes] IS NULL OR [EstimatedDurationMinutes] > 0");
                    table.CheckConstraint("CK_CatalogItemDetails_Specifications", "[Specifications] IS NULL OR ISJSON([Specifications]) = 1");
                    table.CheckConstraint("CK_CatalogItemDetails_WarrantyMonths", "[WarrantyMonths] BETWEEN 0 AND 60");
                    table.ForeignKey(
                        name: "FK_CatalogItemDetails_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Brands",
                columns: new[] { "Id", "Country", "CreatedAt", "Name" },
                values: new object[,]
                {
                    { 1, "США", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Apple" },
                    { 2, "Південна Корея", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Samsung" },
                    { 3, "США", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kingston" },
                    { 4, "Швейцарія", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Arctic" },
                    { 5, "Китай", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lenovo" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "DisplayOrder", "Name", "ParentCategoryId", "Slug" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Запчастини", null, "parts" },
                    { 2, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Послуги", null, "services" },
                    { 3, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Екрани та дисплеї", 1, "displays" },
                    { 4, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Акумулятори", 1, "batteries" },
                    { 5, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Накопичувачі", 1, "storage" },
                    { 6, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Витратні матеріали", 1, "consumables" },
                    { 7, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, "Для смартфонів", 1, "for-smartphones" },
                    { 8, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, "Для ноутбуків", 1, "for-laptops" },
                    { 9, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Діагностика", 2, "diagnostics" },
                    { 10, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Ремонт смартфонів", 2, "smartphone-repair" },
                    { 11, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Ремонт ноутбуків", 2, "laptop-repair" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItemCategories_CategoryId_AddedAt",
                table: "CatalogItemCategories",
                columns: new[] { "CategoryId", "AddedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UX_CatalogItemCategories_CatalogItemId_Primary",
                table: "CatalogItemCategories",
                column: "CatalogItemId",
                unique: true,
                filter: "[IsPrimary] = 1")
                .Annotation("SqlServer:Include", new[] { "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_BrandId",
                table: "CatalogItems",
                column: "BrandId",
                filter: "[BrandId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_ItemType_Price",
                table: "CatalogItems",
                columns: new[] { "ItemType", "Price" },
                filter: "[IsActive] = 1 AND [IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "Name", "Sku" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_Name",
                table: "CatalogItems",
                column: "Name",
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "Sku", "Price", "ItemType" });

            migrationBuilder.CreateIndex(
                name: "UQ_CatalogItems_Sku",
                table: "CatalogItems",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId_DisplayOrder",
                table: "Categories",
                columns: new[] { "ParentCategoryId", "DisplayOrder" })
                .Annotation("SqlServer:Include", new[] { "Name", "Slug" });

            migrationBuilder.CreateIndex(
                name: "UQ_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogItemCategories");

            migrationBuilder.DropTable(
                name: "CatalogItemDetails");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "CatalogItems");

            migrationBuilder.DropTable(
                name: "Brands");
        }
    }
}
