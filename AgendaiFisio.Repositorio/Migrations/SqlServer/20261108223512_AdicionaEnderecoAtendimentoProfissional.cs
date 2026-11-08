using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaiFisio.Repositorio.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AdicionaEnderecoAtendimentoProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EnderecoId",
                table: "Profissionais",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_EnderecoId",
                table: "Profissionais",
                column: "EnderecoId",
                unique: true,
                filter: "[EnderecoId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Profissionais_Enderecos_EnderecoId",
                table: "Profissionais",
                column: "EnderecoId",
                principalTable: "Enderecos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Profissionais_Enderecos_EnderecoId",
                table: "Profissionais");

            migrationBuilder.DropIndex(
                name: "IX_Profissionais_EnderecoId",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "EnderecoId",
                table: "Profissionais");
        }
    }
}
