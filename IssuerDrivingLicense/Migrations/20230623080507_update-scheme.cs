using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssuerDrivingLicense.Migrations
{
    /// <inheritdoc />
    public partial class updatescheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "DriverLicenses",
                newName: "UnDistinguishingSign");

            migrationBuilder.RenameColumn(
                name: "LicenseType",
                table: "DriverLicenses",
                newName: "IssuingCountry");

            migrationBuilder.RenameColumn(
                name: "IssuedAt",
                table: "DriverLicenses",
                newName: "IssueDate");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "DriverLicenses",
                newName: "IssuingAuthority");

            migrationBuilder.AlterColumn<string>(
                name: "Issuedby",
                table: "DriverLicenses",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AdministrativeNumber",
                table: "DriverLicenses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "DriverLicenses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DrivingPrivileges",
                table: "DriverLicenses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiryDate",
                table: "DriverLicenses",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "FamilyName",
                table: "DriverLicenses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GivenName",
                table: "DriverLicenses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdministrativeNumber",
                table: "DriverLicenses");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "DriverLicenses");

            migrationBuilder.DropColumn(
                name: "DrivingPrivileges",
                table: "DriverLicenses");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "DriverLicenses");

            migrationBuilder.DropColumn(
                name: "FamilyName",
                table: "DriverLicenses");

            migrationBuilder.DropColumn(
                name: "GivenName",
                table: "DriverLicenses");

            migrationBuilder.RenameColumn(
                name: "UnDistinguishingSign",
                table: "DriverLicenses",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "IssuingCountry",
                table: "DriverLicenses",
                newName: "LicenseType");

            migrationBuilder.RenameColumn(
                name: "IssuingAuthority",
                table: "DriverLicenses",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "IssueDate",
                table: "DriverLicenses",
                newName: "IssuedAt");

            migrationBuilder.AlterColumn<string>(
                name: "Issuedby",
                table: "DriverLicenses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
