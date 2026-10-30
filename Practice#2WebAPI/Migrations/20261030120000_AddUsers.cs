using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Practice_2WebAPI.Data;

#nullable disable
namespace Practice_2WebAPI.Migrations;

[DbContext(typeof(WebAPIContext))]
[Migration("20261030120000_AddUsers")]
public partial class AddUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "Users", columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            Login = table.Column<string>(type: "TEXT", nullable: false),
            PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
            Role = table.Column<string>(type: "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Users", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_Users_Login", table: "Users", column: "Login", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("Users");
}
