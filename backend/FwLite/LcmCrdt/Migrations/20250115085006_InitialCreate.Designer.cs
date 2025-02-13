﻿// <auto-generated />
using System;
using System.Collections.Generic;
using LcmCrdt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace LcmCrdt.Migrations
{
    [DbContext(typeof(LcmCrdtDbContext))]
    [Migration("20250115085006_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.12");

            modelBuilder.Entity("LcmCrdt.ProjectData", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ClientId")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("FwProjectId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("OriginDomain")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ProjectData");
                });

            modelBuilder.Entity("MiniLcm.Models.ComplexFormComponent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ComplexFormEntryId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ComplexFormHeadword")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ComponentEntryId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ComponentHeadword")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("ComponentSenseId")
                        .HasColumnType("TEXT")
                        .HasColumnName("ComponentSenseId");

                    b.Property<DateTimeOffset?>("DeletedAt")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("SnapshotId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ComponentEntryId");

                    b.HasIndex("ComponentSenseId");

                    b.HasIndex("SnapshotId")
                        .IsUnique();

                    b.HasIndex("ComplexFormEntryId", "ComponentEntryId")
                        .IsUnique()
                        .HasFilter("ComponentSenseId IS NULL");

                    b.HasIndex("ComplexFormEntryId", "ComponentEntryId", "ComponentSenseId")
                        .IsUnique()
                        .HasFilter("ComponentSenseId IS NOT NULL");

                    b.ToTable("ComplexFormComponents", (string)null);
                });

            modelBuilder.Entity("MiniLcm.Models.ComplexFormType", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("DeletedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Guid?>("SnapshotId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SnapshotId")
                        .IsUnique();

                    b.ToTable("ComplexFormType");
                });

            modelBuilder.Entity("MiniLcm.Models.Entry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("CitationForm")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("ComplexFormTypes")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<DateTimeOffset?>("DeletedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("LexemeForm")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("LiteralMeaning")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("Note")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Guid?>("SnapshotId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SnapshotId")
                        .IsUnique();

                    b.ToTable("Entry");
                });

            modelBuilder.Entity("MiniLcm.Models.ExampleSentence", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("DeletedAt")
                        .HasColumnType("TEXT");

                    b.Property<double>("Order")
                        .HasColumnType("REAL");

                    b.Property<string>("Reference")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("SenseId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Sentence")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Guid?>("SnapshotId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Translation")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.HasIndex("SenseId");

                    b.HasIndex("SnapshotId")
                        .IsUnique();

                    b.ToTable("ExampleSentence");
                });

            modelBuilder.Entity("MiniLcm.Models.PartOfSpeech", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("DeletedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<bool>("Predefined")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("SnapshotId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SnapshotId")
                        .IsUnique();

                    b.ToTable("PartOfSpeech");
                });

            modelBuilder.Entity("MiniLcm.Models.SemanticDomain", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("DeletedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<bool>("Predefined")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("SnapshotId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SnapshotId")
                        .IsUnique();

                    b.ToTable("SemanticDomain");
                });

            modelBuilder.Entity("MiniLcm.Models.Sense", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Definition")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<DateTimeOffset?>("DeletedAt")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("EntryId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Gloss")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<double>("Order")
                        .HasColumnType("REAL");

                    b.Property<string>("PartOfSpeech")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("PartOfSpeechId")
                        .HasColumnType("TEXT");

                    b.Property<string>("SemanticDomains")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Guid?>("SnapshotId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EntryId");

                    b.HasIndex("SnapshotId")
                        .IsUnique();

                    b.ToTable("Sense");
                });

            modelBuilder.Entity("MiniLcm.Models.WritingSystem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Abbreviation")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("DeletedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Exemplars")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("Font")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("Order")
                        .HasColumnType("REAL");

                    b.Property<Guid?>("SnapshotId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.Property<string>("WsId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SnapshotId")
                        .IsUnique();

                    b.HasIndex("WsId", "Type")
                        .IsUnique();

                    b.ToTable("WritingSystem");
                });

            modelBuilder.Entity("SIL.Harmony.Commit", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ClientId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Metadata")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("ParentHash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ComplexProperty<Dictionary<string, object>>("HybridDateTime", "SIL.Harmony.Commit.HybridDateTime#HybridDateTime", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<long>("Counter")
                                .HasColumnType("INTEGER")
                                .HasColumnName("Counter");

                            b1.Property<DateTime>("DateTime")
                                .HasColumnType("TEXT")
                                .HasColumnName("DateTime");
                        });

                    b.HasKey("Id");

                    b.ToTable("Commits", (string)null);
                });

            modelBuilder.Entity("SIL.Harmony.Core.ChangeEntity<SIL.Harmony.Changes.IChange>", b =>
                {
                    b.Property<Guid>("CommitId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Index")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Change")
                        .HasColumnType("jsonb");

                    b.Property<Guid>("EntityId")
                        .HasColumnType("TEXT");

                    b.HasKey("CommitId", "Index");

                    b.ToTable("ChangeEntities", (string)null);
                });

            modelBuilder.Entity("SIL.Harmony.Db.ObjectSnapshot", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("CommitId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Entity")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Guid>("EntityId")
                        .HasColumnType("TEXT");

                    b.Property<bool>("EntityIsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsRoot")
                        .HasColumnType("INTEGER");

                    b.Property<string>("References")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TypeName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CommitId", "EntityId")
                        .IsUnique();

                    b.ToTable("Snapshots", (string)null);
                });

            modelBuilder.Entity("MiniLcm.Models.ComplexFormComponent", b =>
                {
                    b.HasOne("MiniLcm.Models.Entry", null)
                        .WithMany("Components")
                        .HasForeignKey("ComplexFormEntryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MiniLcm.Models.Entry", null)
                        .WithMany("ComplexForms")
                        .HasForeignKey("ComponentEntryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MiniLcm.Models.Sense", null)
                        .WithMany()
                        .HasForeignKey("ComponentSenseId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SIL.Harmony.Db.ObjectSnapshot", null)
                        .WithOne()
                        .HasForeignKey("MiniLcm.Models.ComplexFormComponent", "SnapshotId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("MiniLcm.Models.ComplexFormType", b =>
                {
                    b.HasOne("SIL.Harmony.Db.ObjectSnapshot", null)
                        .WithOne()
                        .HasForeignKey("MiniLcm.Models.ComplexFormType", "SnapshotId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("MiniLcm.Models.Entry", b =>
                {
                    b.HasOne("SIL.Harmony.Db.ObjectSnapshot", null)
                        .WithOne()
                        .HasForeignKey("MiniLcm.Models.Entry", "SnapshotId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("MiniLcm.Models.ExampleSentence", b =>
                {
                    b.HasOne("MiniLcm.Models.Sense", null)
                        .WithMany("ExampleSentences")
                        .HasForeignKey("SenseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SIL.Harmony.Db.ObjectSnapshot", null)
                        .WithOne()
                        .HasForeignKey("MiniLcm.Models.ExampleSentence", "SnapshotId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("MiniLcm.Models.PartOfSpeech", b =>
                {
                    b.HasOne("SIL.Harmony.Db.ObjectSnapshot", null)
                        .WithOne()
                        .HasForeignKey("MiniLcm.Models.PartOfSpeech", "SnapshotId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("MiniLcm.Models.SemanticDomain", b =>
                {
                    b.HasOne("SIL.Harmony.Db.ObjectSnapshot", null)
                        .WithOne()
                        .HasForeignKey("MiniLcm.Models.SemanticDomain", "SnapshotId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("MiniLcm.Models.Sense", b =>
                {
                    b.HasOne("MiniLcm.Models.Entry", null)
                        .WithMany("Senses")
                        .HasForeignKey("EntryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SIL.Harmony.Db.ObjectSnapshot", null)
                        .WithOne()
                        .HasForeignKey("MiniLcm.Models.Sense", "SnapshotId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("MiniLcm.Models.WritingSystem", b =>
                {
                    b.HasOne("SIL.Harmony.Db.ObjectSnapshot", null)
                        .WithOne()
                        .HasForeignKey("MiniLcm.Models.WritingSystem", "SnapshotId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("SIL.Harmony.Core.ChangeEntity<SIL.Harmony.Changes.IChange>", b =>
                {
                    b.HasOne("SIL.Harmony.Commit", null)
                        .WithMany("ChangeEntities")
                        .HasForeignKey("CommitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SIL.Harmony.Db.ObjectSnapshot", b =>
                {
                    b.HasOne("SIL.Harmony.Commit", "Commit")
                        .WithMany("Snapshots")
                        .HasForeignKey("CommitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Commit");
                });

            modelBuilder.Entity("MiniLcm.Models.Entry", b =>
                {
                    b.Navigation("ComplexForms");

                    b.Navigation("Components");

                    b.Navigation("Senses");
                });

            modelBuilder.Entity("MiniLcm.Models.Sense", b =>
                {
                    b.Navigation("ExampleSentences");
                });

            modelBuilder.Entity("SIL.Harmony.Commit", b =>
                {
                    b.Navigation("ChangeEntities");

                    b.Navigation("Snapshots");
                });
#pragma warning restore 612, 618
        }
    }
}
