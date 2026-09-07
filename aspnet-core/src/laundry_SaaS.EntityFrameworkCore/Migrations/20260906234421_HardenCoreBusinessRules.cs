using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace laundry_SaaS.Migrations
{
    /// <inheritdoc />
    public partial class HardenCoreBusinessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppServicePrices_TenantId_LaundryItemTypeId_LaundryServiceId",
                table: "AppServicePrices");

            migrationBuilder.DropIndex(
                name: "IX_AppPickupTasks_TenantId_OrderId",
                table: "AppPickupTasks");

            migrationBuilder.DropIndex(
                name: "IX_AppLaundryStaffProfiles_TenantId_UserId",
                table: "AppLaundryStaffProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AppLaundryServices_TenantId_Code",
                table: "AppLaundryServices");

            migrationBuilder.DropIndex(
                name: "IX_AppLaundryItemTypes_TenantId_Code",
                table: "AppLaundryItemTypes");

            migrationBuilder.DropIndex(
                name: "IX_AppLaundries_TenantId",
                table: "AppLaundries");

            migrationBuilder.DropIndex(
                name: "IX_AppDrivers_TenantId_UserId",
                table: "AppDrivers");

            migrationBuilder.CreateIndex(
                name: "IX_AppServicePrices_TenantId_LaundryItemTypeId_LaundryServiceId",
                table: "AppServicePrices",
                columns: new[] { "TenantId", "LaundryItemTypeId", "LaundryServiceId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppPickupTasks_TenantId_OrderId",
                table: "AppPickupTasks",
                columns: new[] { "TenantId", "OrderId" },
                unique: true,
                filter: "[Status] IN (0, 1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaundryStaffProfiles_TenantId_UserId",
                table: "AppLaundryStaffProfiles",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaundryServices_TenantId_Code",
                table: "AppLaundryServices",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaundryItemTypes_TenantId_Code",
                table: "AppLaundryItemTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaundries_TenantId",
                table: "AppLaundries",
                column: "TenantId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppDrivers_TenantId_UserId",
                table: "AppDrivers",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_CustomerId_Default",
                table: "AppCustomerAddresses",
                column: "CustomerId",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppServicePrices_TenantId_LaundryItemTypeId_LaundryServiceId",
                table: "AppServicePrices");

            migrationBuilder.DropIndex(
                name: "IX_AppPickupTasks_TenantId_OrderId",
                table: "AppPickupTasks");

            migrationBuilder.DropIndex(
                name: "IX_AppLaundryStaffProfiles_TenantId_UserId",
                table: "AppLaundryStaffProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AppLaundryServices_TenantId_Code",
                table: "AppLaundryServices");

            migrationBuilder.DropIndex(
                name: "IX_AppLaundryItemTypes_TenantId_Code",
                table: "AppLaundryItemTypes");

            migrationBuilder.DropIndex(
                name: "IX_AppLaundries_TenantId",
                table: "AppLaundries");

            migrationBuilder.DropIndex(
                name: "IX_AppDrivers_TenantId_UserId",
                table: "AppDrivers");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAddresses_CustomerId_Default",
                table: "AppCustomerAddresses");

            migrationBuilder.CreateIndex(
                name: "IX_AppServicePrices_TenantId_LaundryItemTypeId_LaundryServiceId",
                table: "AppServicePrices",
                columns: new[] { "TenantId", "LaundryItemTypeId", "LaundryServiceId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppPickupTasks_TenantId_OrderId",
                table: "AppPickupTasks",
                columns: new[] { "TenantId", "OrderId" },
                unique: true,
                filter: "[Status] IN (0, 1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaundryStaffProfiles_TenantId_UserId",
                table: "AppLaundryStaffProfiles",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaundryServices_TenantId_Code",
                table: "AppLaundryServices",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaundryItemTypes_TenantId_Code",
                table: "AppLaundryItemTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaundries_TenantId",
                table: "AppLaundries",
                column: "TenantId",
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppDrivers_TenantId_UserId",
                table: "AppDrivers",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }
    }
}
