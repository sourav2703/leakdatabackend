using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreat2e : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Databases_Searches_SearchId",
                table: "Databases");

            migrationBuilder.DropForeignKey(
                name: "FK_Records_Databases_DatabaseId",
                table: "Records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Searches",
                table: "Searches");

            migrationBuilder.DropIndex(
                name: "idx_searches_query",
                table: "Searches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Records",
                table: "Records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Databases",
                table: "Databases");

            migrationBuilder.RenameTable(
                name: "Searches",
                newName: "searches");

            migrationBuilder.RenameTable(
                name: "Records",
                newName: "records");

            migrationBuilder.RenameTable(
                name: "Databases",
                newName: "databases");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "searches",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Query",
                table: "searches",
                newName: "query");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "searches",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "QueryType",
                table: "searches",
                newName: "query_type");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "searches",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "searches",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "records",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RawData",
                table: "records",
                newName: "raw_data");

            migrationBuilder.RenameColumn(
                name: "DatabaseId",
                table: "records",
                newName: "database_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "records",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "idx_records_database_id",
                table: "records",
                newName: "IX_records_database_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "databases",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SearchId",
                table: "databases",
                newName: "search_id");

            migrationBuilder.RenameColumn(
                name: "RecordCount",
                table: "databases",
                newName: "record_count");

            migrationBuilder.RenameColumn(
                name: "InfoLeak",
                table: "databases",
                newName: "info_leak");

            migrationBuilder.RenameColumn(
                name: "DatabaseName",
                table: "databases",
                newName: "database_name");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "databases",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "idx_databases_search_id",
                table: "databases",
                newName: "IX_databases_search_id");

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "searches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "limit",
                table: "searches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_searches",
                table: "searches",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_records",
                table: "records",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_databases",
                table: "databases",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_search",
                table: "databases",
                column: "search_id",
                principalTable: "searches",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_database",
                table: "records",
                column: "database_id",
                principalTable: "databases",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_search",
                table: "databases");

            migrationBuilder.DropForeignKey(
                name: "fk_database",
                table: "records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_searches",
                table: "searches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_records",
                table: "records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_databases",
                table: "databases");

            migrationBuilder.DropColumn(
                name: "language",
                table: "searches");

            migrationBuilder.DropColumn(
                name: "limit",
                table: "searches");

            migrationBuilder.RenameTable(
                name: "searches",
                newName: "Searches");

            migrationBuilder.RenameTable(
                name: "records",
                newName: "Records");

            migrationBuilder.RenameTable(
                name: "databases",
                newName: "Databases");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Searches",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "query",
                table: "Searches",
                newName: "Query");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Searches",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "query_type",
                table: "Searches",
                newName: "QueryType");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "Searches",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Searches",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Records",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "raw_data",
                table: "Records",
                newName: "RawData");

            migrationBuilder.RenameColumn(
                name: "database_id",
                table: "Records",
                newName: "DatabaseId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Records",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_records_database_id",
                table: "Records",
                newName: "idx_records_database_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Databases",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "search_id",
                table: "Databases",
                newName: "SearchId");

            migrationBuilder.RenameColumn(
                name: "record_count",
                table: "Databases",
                newName: "RecordCount");

            migrationBuilder.RenameColumn(
                name: "info_leak",
                table: "Databases",
                newName: "InfoLeak");

            migrationBuilder.RenameColumn(
                name: "database_name",
                table: "Databases",
                newName: "DatabaseName");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Databases",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_databases_search_id",
                table: "Databases",
                newName: "idx_databases_search_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Searches",
                table: "Searches",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Records",
                table: "Records",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Databases",
                table: "Databases",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "idx_searches_query",
                table: "Searches",
                column: "Query");

            migrationBuilder.AddForeignKey(
                name: "FK_Databases_Searches_SearchId",
                table: "Databases",
                column: "SearchId",
                principalTable: "Searches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Databases_DatabaseId",
                table: "Records",
                column: "DatabaseId",
                principalTable: "Databases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
