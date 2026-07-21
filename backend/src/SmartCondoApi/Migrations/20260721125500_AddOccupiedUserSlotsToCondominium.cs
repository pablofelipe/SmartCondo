using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCondoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOccupiedUserSlotsToCondominium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OccupiedUserSlots",
                table: "Condominiums",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: a slot is occupied by every registered profile regardless of e-mail confirmation
            // status (Enabled), matching the rule going forward - see the authorization evolution notes for
            // Step 5. Existing condominiums must start from their real current occupancy, not zero, or every
            // one of them would appear to have full vacancy up to MaxUsers on top of who is already there.
            migrationBuilder.Sql(
                """
                UPDATE "Condominiums" c
                SET "OccupiedUserSlots" = (
                    SELECT COUNT(*) FROM "UserProfiles" up WHERE up."CondominiumId" = c."Id"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OccupiedUserSlots",
                table: "Condominiums");
        }
    }
}
