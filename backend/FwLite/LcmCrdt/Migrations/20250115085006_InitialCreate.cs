using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Commits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    ParentHash = table.Column<string>(type: "TEXT", nullable: false),
                    Counter = table.Column<long>(type: "INTEGER", nullable: false),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OriginDomain = table.Column<string>(type: "TEXT", nullable: true),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FwProjectId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeEntities",
                columns: table => new
                {
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    CommitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Change = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeEntities", x => new { x.CommitId, x.Index });
                    table.ForeignKey(
                        name: "FK_ChangeEntities_Commits_CommitId",
                        column: x => x.CommitId,
                        principalTable: "Commits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TypeName = table.Column<string>(type: "TEXT", nullable: false),
                    Entity = table.Column<string>(type: "jsonb", nullable: false),
                    References = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityIsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsRoot = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Snapshots_Commits_CommitId",
                        column: x => x.CommitId,
                        principalTable: "Commits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplexFormType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "jsonb", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplexFormType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplexFormType_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Entry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LexemeForm = table.Column<string>(type: "jsonb", nullable: false),
                    CitationForm = table.Column<string>(type: "jsonb", nullable: false),
                    LiteralMeaning = table.Column<string>(type: "jsonb", nullable: false),
                    Note = table.Column<string>(type: "jsonb", nullable: false),
                    ComplexFormTypes = table.Column<string>(type: "jsonb", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entry_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PartOfSpeech",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "jsonb", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Predefined = table.Column<bool>(type: "INTEGER", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartOfSpeech", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartOfSpeech_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SemanticDomain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "jsonb", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Predefined = table.Column<bool>(type: "INTEGER", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanticDomain", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SemanticDomain_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WritingSystem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WsId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Abbreviation = table.Column<string>(type: "TEXT", nullable: false),
                    Font = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Exemplars = table.Column<string>(type: "jsonb", nullable: false),
                    Order = table.Column<double>(type: "REAL", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingSystem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WritingSystem_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Sense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<double>(type: "REAL", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Definition = table.Column<string>(type: "jsonb", nullable: false),
                    Gloss = table.Column<string>(type: "jsonb", nullable: false),
                    PartOfSpeech = table.Column<string>(type: "TEXT", nullable: false),
                    PartOfSpeechId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SemanticDomains = table.Column<string>(type: "jsonb", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sense", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sense_Entry_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sense_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComplexFormComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ComplexFormEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComplexFormHeadword = table.Column<string>(type: "TEXT", nullable: true),
                    ComponentEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComponentSenseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ComponentHeadword = table.Column<string>(type: "TEXT", nullable: true),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplexFormComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplexFormComponents_Entry_ComplexFormEntryId",
                        column: x => x.ComplexFormEntryId,
                        principalTable: "Entry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplexFormComponents_Entry_ComponentEntryId",
                        column: x => x.ComponentEntryId,
                        principalTable: "Entry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplexFormComponents_Sense_ComponentSenseId",
                        column: x => x.ComponentSenseId,
                        principalTable: "Sense",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplexFormComponents_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExampleSentence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<double>(type: "REAL", nullable: false),
                    Sentence = table.Column<string>(type: "jsonb", nullable: false),
                    Translation = table.Column<string>(type: "jsonb", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    SenseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExampleSentence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExampleSentence_Sense_SenseId",
                        column: x => x.SenseId,
                        principalTable: "Sense",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExampleSentence_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplexFormComponents_ComplexFormEntryId_ComponentEntryId",
                table: "ComplexFormComponents",
                columns: new[] { "ComplexFormEntryId", "ComponentEntryId" },
                unique: true,
                filter: "ComponentSenseId IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComplexFormComponents_ComplexFormEntryId_ComponentEntryId_ComponentSenseId",
                table: "ComplexFormComponents",
                columns: new[] { "ComplexFormEntryId", "ComponentEntryId", "ComponentSenseId" },
                unique: true,
                filter: "ComponentSenseId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComplexFormComponents_ComponentEntryId",
                table: "ComplexFormComponents",
                column: "ComponentEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplexFormComponents_ComponentSenseId",
                table: "ComplexFormComponents",
                column: "ComponentSenseId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplexFormComponents_SnapshotId",
                table: "ComplexFormComponents",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplexFormType_SnapshotId",
                table: "ComplexFormType",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entry_SnapshotId",
                table: "Entry",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExampleSentence_SenseId",
                table: "ExampleSentence",
                column: "SenseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExampleSentence_SnapshotId",
                table: "ExampleSentence",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartOfSpeech_SnapshotId",
                table: "PartOfSpeech",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemanticDomain_SnapshotId",
                table: "SemanticDomain",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sense_EntryId",
                table: "Sense",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Sense_SnapshotId",
                table: "Sense",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_CommitId_EntityId",
                table: "Snapshots",
                columns: new[] { "CommitId", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritingSystem_SnapshotId",
                table: "WritingSystem",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritingSystem_WsId_Type",
                table: "WritingSystem",
                columns: new[] { "WsId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeEntities");

            migrationBuilder.DropTable(
                name: "ComplexFormComponents");

            migrationBuilder.DropTable(
                name: "ComplexFormType");

            migrationBuilder.DropTable(
                name: "ExampleSentence");

            migrationBuilder.DropTable(
                name: "PartOfSpeech");

            migrationBuilder.DropTable(
                name: "ProjectData");

            migrationBuilder.DropTable(
                name: "SemanticDomain");

            migrationBuilder.DropTable(
                name: "WritingSystem");

            migrationBuilder.DropTable(
                name: "Sense");

            migrationBuilder.DropTable(
                name: "Entry");

            migrationBuilder.DropTable(
                name: "Snapshots");

            migrationBuilder.DropTable(
                name: "Commits");
        }
    }
}
