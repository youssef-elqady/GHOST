using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GHOST.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerRewardsAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    OldValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AppliedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Discounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Discounts_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Discounts_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Discounts_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiftCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RewardType = table.Column<int>(type: "INTEGER", nullable: false),
                    CashValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UsedById = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftCards_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCards_Users_UsedById",
                        column: x => x.UsedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Gifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    RedeemedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gifts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gifts_Users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gifts_Users_RedeemedById",
                        column: x => x.RedeemedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DaysOfWeek = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    DiscountType = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ApplicableDeviceTypes = table.Column<string>(type: "TEXT", nullable: false),
                    MinimumDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumUsage = table.Column<int>(type: "INTEGER", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorId",
                table: "AuditLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_ApprovedById",
                table: "Discounts",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_CustomerId",
                table: "Discounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_RequestedById",
                table: "Discounts",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_SessionId",
                table: "Discounts",
                column: "SessionId",
                unique: true,
                filter: "\"SessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_Code",
                table: "GiftCards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_CreatedById",
                table: "GiftCards",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_UsedById",
                table: "GiftCards",
                column: "UsedById");

            migrationBuilder.CreateIndex(
                name: "IX_Gifts_CustomerId_Period_IssuedAt",
                table: "Gifts",
                columns: new[] { "CustomerId", "Period", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Gifts_IssuedById",
                table: "Gifts",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_Gifts_RedeemedById",
                table: "Gifts",
                column: "RedeemedById");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_CreatedById",
                table: "LoyaltyTransactions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_CustomerId_CreatedAt",
                table: "LoyaltyTransactions",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CreatedById",
                table: "Offers",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Discounts");

            migrationBuilder.DropTable(
                name: "GiftCards");

            migrationBuilder.DropTable(
                name: "Gifts");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "Offers");
        }
    }
}
