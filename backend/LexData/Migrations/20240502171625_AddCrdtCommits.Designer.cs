﻿// <auto-generated />
using System;
using System.Collections.Generic;
using System.Text.Json;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LexData.Migrations
{
    [DbContext(typeof(LexBoxDbContext))]
    [Migration("20240502171625_AddCrdtCommits")]
    partial class AddCrdtCommits
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzBlobTrigger", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("TriggerName")
                        .HasColumnType("text")
                        .HasColumnName("trigger_name");

                    b.Property<string>("TriggerGroup")
                        .HasColumnType("text")
                        .HasColumnName("trigger_group");

                    b.Property<byte[]>("BlobData")
                        .HasColumnType("bytea")
                        .HasColumnName("blob_data");

                    b.HasKey("SchedulerName", "TriggerName", "TriggerGroup");

                    b.ToTable("qrtz_blob_triggers", "quartz");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzCalendar", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("CalendarName")
                        .HasColumnType("text")
                        .HasColumnName("calendar_name");

                    b.Property<byte[]>("Calendar")
                        .IsRequired()
                        .HasColumnType("bytea")
                        .HasColumnName("calendar");

                    b.HasKey("SchedulerName", "CalendarName");

                    b.ToTable("qrtz_calendars", "quartz");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzCronTrigger", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("TriggerName")
                        .HasColumnType("text")
                        .HasColumnName("trigger_name");

                    b.Property<string>("TriggerGroup")
                        .HasColumnType("text")
                        .HasColumnName("trigger_group");

                    b.Property<string>("CronExpression")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("cron_expression");

                    b.Property<string>("TimeZoneId")
                        .HasColumnType("text")
                        .HasColumnName("time_zone_id");

                    b.HasKey("SchedulerName", "TriggerName", "TriggerGroup");

                    b.ToTable("qrtz_cron_triggers", "quartz");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzFiredTrigger", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("EntryId")
                        .HasColumnType("text")
                        .HasColumnName("entry_id");

                    b.Property<long>("FiredTime")
                        .HasColumnType("bigint")
                        .HasColumnName("fired_time");

                    b.Property<string>("InstanceName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("instance_name");

                    b.Property<bool>("IsNonConcurrent")
                        .HasColumnType("bool")
                        .HasColumnName("is_nonconcurrent");

                    b.Property<string>("JobGroup")
                        .HasColumnType("text")
                        .HasColumnName("job_group");

                    b.Property<string>("JobName")
                        .HasColumnType("text")
                        .HasColumnName("job_name");

                    b.Property<int>("Priority")
                        .HasColumnType("integer")
                        .HasColumnName("priority");

                    b.Property<bool?>("RequestsRecovery")
                        .HasColumnType("bool")
                        .HasColumnName("requests_recovery");

                    b.Property<long>("ScheduledTime")
                        .HasColumnType("bigint")
                        .HasColumnName("sched_time");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("state");

                    b.Property<string>("TriggerGroup")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("trigger_group");

                    b.Property<string>("TriggerName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("trigger_name");

                    b.HasKey("SchedulerName", "EntryId");

                    b.HasIndex("InstanceName")
                        .HasDatabaseName("idx_qrtz_ft_trig_inst_name");

                    b.HasIndex("JobGroup")
                        .HasDatabaseName("idx_qrtz_ft_job_group");

                    b.HasIndex("JobName")
                        .HasDatabaseName("idx_qrtz_ft_job_name");

                    b.HasIndex("RequestsRecovery")
                        .HasDatabaseName("idx_qrtz_ft_job_req_recovery");

                    b.HasIndex("TriggerGroup")
                        .HasDatabaseName("idx_qrtz_ft_trig_group");

                    b.HasIndex("TriggerName")
                        .HasDatabaseName("idx_qrtz_ft_trig_name");

                    b.HasIndex("SchedulerName", "TriggerName", "TriggerGroup")
                        .HasDatabaseName("idx_qrtz_ft_trig_nm_gp");

                    b.ToTable("qrtz_fired_triggers", "quartz");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzJobDetail", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("JobName")
                        .HasColumnType("text")
                        .HasColumnName("job_name");

                    b.Property<string>("JobGroup")
                        .HasColumnType("text")
                        .HasColumnName("job_group");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<bool>("IsDurable")
                        .HasColumnType("bool")
                        .HasColumnName("is_durable");

                    b.Property<bool>("IsNonConcurrent")
                        .HasColumnType("bool")
                        .HasColumnName("is_nonconcurrent");

                    b.Property<bool>("IsUpdateData")
                        .HasColumnType("bool")
                        .HasColumnName("is_update_data");

                    b.Property<string>("JobClassName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("job_class_name");

                    b.Property<byte[]>("JobData")
                        .HasColumnType("bytea")
                        .HasColumnName("job_data");

                    b.Property<bool>("RequestsRecovery")
                        .HasColumnType("bool")
                        .HasColumnName("requests_recovery");

                    b.HasKey("SchedulerName", "JobName", "JobGroup");

                    b.HasIndex("RequestsRecovery")
                        .HasDatabaseName("idx_qrtz_j_req_recovery");

                    b.ToTable("qrtz_job_details", "quartz");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzLock", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("LockName")
                        .HasColumnType("text")
                        .HasColumnName("lock_name");

                    b.HasKey("SchedulerName", "LockName");

                    b.ToTable("qrtz_locks", "quartz");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzPausedTriggerGroup", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("TriggerGroup")
                        .HasColumnType("text")
                        .HasColumnName("trigger_group");

                    b.HasKey("SchedulerName", "TriggerGroup");

                    b.ToTable("qrtz_paused_trigger_grps", "quartz");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzSchedulerState", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("InstanceName")
                        .HasColumnType("text")
                        .HasColumnName("instance_name");

                    b.Property<long>("CheckInInterval")
                        .HasColumnType("bigint")
                        .HasColumnName("checkin_interval");

                    b.Property<long>("LastCheckInTime")
                        .HasColumnType("bigint")
                        .HasColumnName("last_checkin_time");

                    b.HasKey("SchedulerName", "InstanceName");

                    b.ToTable("qrtz_scheduler_state", "quartz");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzSimplePropertyTrigger", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("TriggerName")
                        .HasColumnType("text")
                        .HasColumnName("trigger_name");

                    b.Property<string>("TriggerGroup")
                        .HasColumnType("text")
                        .HasColumnName("trigger_group");

                    b.Property<bool?>("BooleanProperty1")
                        .HasColumnType("bool")
                        .HasColumnName("bool_prop_1");

                    b.Property<bool?>("BooleanProperty2")
                        .HasColumnType("bool")
                        .HasColumnName("bool_prop_2");

                    b.Property<decimal?>("DecimalProperty1")
                        .HasColumnType("numeric")
                        .HasColumnName("dec_prop_1");

                    b.Property<decimal?>("DecimalProperty2")
                        .HasColumnType("numeric")
                        .HasColumnName("dec_prop_2");

                    b.Property<int?>("IntegerProperty1")
                        .HasColumnType("integer")
                        .HasColumnName("int_prop_1");

                    b.Property<int?>("IntegerProperty2")
                        .HasColumnType("integer")
                        .HasColumnName("int_prop_2");

                    b.Property<long?>("LongProperty1")
                        .HasColumnType("bigint")
                        .HasColumnName("long_prop_1");

                    b.Property<long?>("LongProperty2")
                        .HasColumnType("bigint")
                        .HasColumnName("long_prop_2");

                    b.Property<string>("StringProperty1")
                        .HasColumnType("text")
                        .HasColumnName("str_prop_1");

                    b.Property<string>("StringProperty2")
                        .HasColumnType("text")
                        .HasColumnName("str_prop_2");

                    b.Property<string>("StringProperty3")
                        .HasColumnType("text")
                        .HasColumnName("str_prop_3");

                    b.Property<string>("TimeZoneId")
                        .HasColumnType("text")
                        .HasColumnName("time_zone_id");

                    b.HasKey("SchedulerName", "TriggerName", "TriggerGroup");

                    b.ToTable("qrtz_simprop_triggers", "quartz");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzSimpleTrigger", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("TriggerName")
                        .HasColumnType("text")
                        .HasColumnName("trigger_name");

                    b.Property<string>("TriggerGroup")
                        .HasColumnType("text")
                        .HasColumnName("trigger_group");

                    b.Property<long>("RepeatCount")
                        .HasColumnType("bigint")
                        .HasColumnName("repeat_count");

                    b.Property<long>("RepeatInterval")
                        .HasColumnType("bigint")
                        .HasColumnName("repeat_interval");

                    b.Property<long>("TimesTriggered")
                        .HasColumnType("bigint")
                        .HasColumnName("times_triggered");

                    b.HasKey("SchedulerName", "TriggerName", "TriggerGroup");

                    b.ToTable("qrtz_simple_triggers", "quartz");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzTrigger", b =>
                {
                    b.Property<string>("SchedulerName")
                        .HasColumnType("text")
                        .HasColumnName("sched_name");

                    b.Property<string>("TriggerName")
                        .HasColumnType("text")
                        .HasColumnName("trigger_name");

                    b.Property<string>("TriggerGroup")
                        .HasColumnType("text")
                        .HasColumnName("trigger_group");

                    b.Property<string>("CalendarName")
                        .HasColumnType("text")
                        .HasColumnName("calendar_name");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<long?>("EndTime")
                        .HasColumnType("bigint")
                        .HasColumnName("end_time");

                    b.Property<byte[]>("JobData")
                        .HasColumnType("bytea")
                        .HasColumnName("job_data");

                    b.Property<string>("JobGroup")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("job_group");

                    b.Property<string>("JobName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("job_name");

                    b.Property<short?>("MisfireInstruction")
                        .HasColumnType("smallint")
                        .HasColumnName("misfire_instr");

                    b.Property<long?>("NextFireTime")
                        .HasColumnType("bigint")
                        .HasColumnName("next_fire_time");

                    b.Property<long?>("PreviousFireTime")
                        .HasColumnType("bigint")
                        .HasColumnName("prev_fire_time");

                    b.Property<int?>("Priority")
                        .HasColumnType("integer")
                        .HasColumnName("priority");

                    b.Property<long>("StartTime")
                        .HasColumnType("bigint")
                        .HasColumnName("start_time");

                    b.Property<string>("TriggerState")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("trigger_state");

                    b.Property<string>("TriggerType")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("trigger_type");

                    b.HasKey("SchedulerName", "TriggerName", "TriggerGroup");

                    b.HasIndex("NextFireTime")
                        .HasDatabaseName("idx_qrtz_t_next_fire_time");

                    b.HasIndex("TriggerState")
                        .HasDatabaseName("idx_qrtz_t_state");

                    b.HasIndex("NextFireTime", "TriggerState")
                        .HasDatabaseName("idx_qrtz_t_nft_st");

                    b.HasIndex("SchedulerName", "JobName", "JobGroup");

                    b.ToTable("qrtz_triggers", "quartz");
                });

            modelBuilder.Entity("LexCore.Entities.DraftProject", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("ProjectManagerId")
                        .HasColumnType("uuid");

                    b.Property<int>("RetentionPolicy")
                        .HasColumnType("integer");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("UpdatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("ProjectManagerId");

                    b.ToTable("DraftProjects");
                });

            modelBuilder.Entity("LexCore.Entities.FlexProjectMetadata", b =>
                {
                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<int?>("LexEntryCount")
                        .HasColumnType("integer");

                    b.HasKey("ProjectId");

                    b.ToTable("FlexProjectMetadata");
                });

            modelBuilder.Entity("LexCore.Entities.OrgMember", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<Guid>("OrgId")
                        .HasColumnType("uuid");

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("UpdatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("OrgId");

                    b.HasIndex("UserId", "OrgId")
                        .IsUnique();

                    b.ToTable("OrgMembers", (string)null);
                });

            modelBuilder.Entity("LexCore.Entities.Organization", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("UpdatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Orgs", (string)null);
                });

            modelBuilder.Entity("LexCore.Entities.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<DateTimeOffset?>("DeletedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("LastCommit")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("MigratedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("ParentId")
                        .HasColumnType("uuid");

                    b.Property<int>("ProjectOrigin")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(1);

                    b.Property<int>("ResetStatus")
                        .HasColumnType("integer");

                    b.Property<int>("RetentionPolicy")
                        .HasColumnType("integer");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("UpdatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("ParentId");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("LexCore.Entities.ProjectUsers", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("UpdatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.HasIndex("UserId", "ProjectId")
                        .IsUnique();

                    b.ToTable("ProjectUsers");
                });

            modelBuilder.Entity("LexCore.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<bool>("CanCreateProjects")
                        .HasColumnType("boolean");

                    b.Property<Guid?>("CreatedById")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("Email")
                        .HasColumnType("text")
                        .UseCollation("case_insensitive");

                    b.Property<bool>("EmailVerified")
                        .HasColumnType("boolean");

                    b.Property<string>("GoogleId")
                        .HasColumnType("text");

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset>("LastActive")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("LocalizationCode")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasDefaultValue("en");

                    b.Property<bool>("Locked")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int?>("PasswordStrength")
                        .HasColumnType("integer");

                    b.Property<string>("Salt")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("UpdatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("Username")
                        .HasColumnType("text")
                        .UseCollation("case_insensitive");

                    b.HasKey("Id");

                    b.HasIndex("CreatedById");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("LexData.Entities.CrdtCommit", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ClientId")
                        .HasColumnType("uuid");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ParentHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.ComplexProperty<Dictionary<string, object>>("HybridDateTime", "LexData.Entities.CrdtCommit.HybridDateTime#HybridDateTime", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<long>("Counter")
                                .HasColumnType("bigint");

                            b1.Property<DateTimeOffset>("DateTime")
                                .HasColumnType("timestamp with time zone");
                        });

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("CrdtCommits", (string)null);
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzBlobTrigger", b =>
                {
                    b.HasOne("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzTrigger", "Trigger")
                        .WithMany("BlobTriggers")
                        .HasForeignKey("SchedulerName", "TriggerName", "TriggerGroup")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Trigger");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzCronTrigger", b =>
                {
                    b.HasOne("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzTrigger", "Trigger")
                        .WithMany("CronTriggers")
                        .HasForeignKey("SchedulerName", "TriggerName", "TriggerGroup")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Trigger");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzSimplePropertyTrigger", b =>
                {
                    b.HasOne("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzTrigger", "Trigger")
                        .WithMany("SimplePropertyTriggers")
                        .HasForeignKey("SchedulerName", "TriggerName", "TriggerGroup")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Trigger");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzSimpleTrigger", b =>
                {
                    b.HasOne("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzTrigger", "Trigger")
                        .WithMany("SimpleTriggers")
                        .HasForeignKey("SchedulerName", "TriggerName", "TriggerGroup")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Trigger");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzTrigger", b =>
                {
                    b.HasOne("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzJobDetail", "JobDetail")
                        .WithMany("Triggers")
                        .HasForeignKey("SchedulerName", "JobName", "JobGroup")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("JobDetail");
                });

            modelBuilder.Entity("LexCore.Entities.DraftProject", b =>
                {
                    b.HasOne("LexCore.Entities.User", "ProjectManager")
                        .WithMany()
                        .HasForeignKey("ProjectManagerId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("ProjectManager");
                });

            modelBuilder.Entity("LexCore.Entities.FlexProjectMetadata", b =>
                {
                    b.HasOne("LexCore.Entities.Project", null)
                        .WithOne("FlexProjectMetadata")
                        .HasForeignKey("LexCore.Entities.FlexProjectMetadata", "ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("LexCore.Entities.OrgMember", b =>
                {
                    b.HasOne("LexCore.Entities.Organization", "Organization")
                        .WithMany("Members")
                        .HasForeignKey("OrgId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("LexCore.Entities.User", "User")
                        .WithMany("Organizations")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Organization");

                    b.Navigation("User");
                });

            modelBuilder.Entity("LexCore.Entities.Project", b =>
                {
                    b.HasOne("LexCore.Entities.Project", null)
                        .WithMany()
                        .HasForeignKey("ParentId");
                });

            modelBuilder.Entity("LexCore.Entities.ProjectUsers", b =>
                {
                    b.HasOne("LexCore.Entities.Project", "Project")
                        .WithMany("Users")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("LexCore.Entities.User", "User")
                        .WithMany("Projects")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");

                    b.Navigation("User");
                });

            modelBuilder.Entity("LexCore.Entities.User", b =>
                {
                    b.HasOne("LexCore.Entities.User", "CreatedBy")
                        .WithMany("UsersICreated")
                        .HasForeignKey("CreatedById")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("CreatedBy");
                });

            modelBuilder.Entity("LexData.Entities.CrdtCommit", b =>
                {
                    b.HasOne("LexCore.Entities.FlexProjectMetadata", null)
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsMany("Crdt.Core.ChangeEntity<System.Text.Json.JsonElement>", "ChangeEntities", b1 =>
                        {
                            b1.Property<Guid>("CrdtCommitId")
                                .HasColumnType("uuid");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("integer");

                            b1.Property<JsonElement>("Change")
                                .HasColumnType("jsonb");

                            b1.Property<Guid>("CommitId")
                                .HasColumnType("uuid");

                            b1.Property<Guid>("EntityId")
                                .HasColumnType("uuid");

                            b1.Property<int>("Index")
                                .HasColumnType("integer");

                            b1.HasKey("CrdtCommitId", "Id");

                            b1.ToTable("CrdtCommits");

                            b1.ToJson("ChangeEntities");

                            b1.WithOwner()
                                .HasForeignKey("CrdtCommitId");
                        });

                    b.Navigation("ChangeEntities");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzJobDetail", b =>
                {
                    b.Navigation("Triggers");
                });

            modelBuilder.Entity("AppAny.Quartz.EntityFrameworkCore.Migrations.QuartzTrigger", b =>
                {
                    b.Navigation("BlobTriggers");

                    b.Navigation("CronTriggers");

                    b.Navigation("SimplePropertyTriggers");

                    b.Navigation("SimpleTriggers");
                });

            modelBuilder.Entity("LexCore.Entities.Organization", b =>
                {
                    b.Navigation("Members");
                });

            modelBuilder.Entity("LexCore.Entities.Project", b =>
                {
                    b.Navigation("FlexProjectMetadata");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("LexCore.Entities.User", b =>
                {
                    b.Navigation("Organizations");

                    b.Navigation("Projects");

                    b.Navigation("UsersICreated");
                });
#pragma warning restore 612, 618
        }
    }
}
