﻿// <auto-generated />
using System;
using KatalonScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace KatalonScheduler.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241213192455_AddTestExecutionLogs")]
    partial class AddTestExecutionLogs
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("AdminSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("GitRepositoryPath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("TEXT");

                    b.Property<string>("OrganizationId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("AdminSettings");
                });

            modelBuilder.Entity("Organization", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("KatalonOrganizationId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("TestOpsProjectId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Organizations");
                });

            modelBuilder.Entity("Project", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("GitRepositoryPath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("GitUrl")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastScanned")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("OrganizationId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProjectPath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("OrganizationId");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("ScheduledTest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("DayOfWeek")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Hour")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<string>("JobId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastRun")
                        .HasColumnType("TEXT");

                    b.Property<string>("LastRunStatus")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Minute")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("NextRun")
                        .HasColumnType("TEXT");

                    b.Property<int>("ProjectId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Schedule")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SelectedProfile")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("TestCaseId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TestSuiteId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TestSuitePath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.HasIndex("TestCaseId");

                    b.HasIndex("TestSuiteId");

                    b.ToTable("ScheduledTests");
                });

            modelBuilder.Entity("TestCase", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ProjectId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TestSuiteId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.HasIndex("TestSuiteId");

                    b.ToTable("TestCases");
                });

            modelBuilder.Entity("TestExecutionLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ErrorMessage")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ExecutionDetails")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ExecutionTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("JobId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ScheduledTestId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ScheduledTestId");

                    b.ToTable("TestExecutionLogs");
                });

            modelBuilder.Entity("TestSuite", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ProjectId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("TestSuites");
                });

            modelBuilder.Entity("Project", b =>
                {
                    b.HasOne("Organization", "Organization")
                        .WithMany("Projects")
                        .HasForeignKey("OrganizationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Organization");
                });

            modelBuilder.Entity("ScheduledTest", b =>
                {
                    b.HasOne("Project", "Project")
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TestCase", "TestCase")
                        .WithMany()
                        .HasForeignKey("TestCaseId");

                    b.HasOne("TestSuite", "TestSuite")
                        .WithMany("ScheduledTests")
                        .HasForeignKey("TestSuiteId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");

                    b.Navigation("TestCase");

                    b.Navigation("TestSuite");
                });

            modelBuilder.Entity("TestCase", b =>
                {
                    b.HasOne("Project", "Project")
                        .WithMany("TestCases")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TestSuite", "TestSuite")
                        .WithMany("TestCases")
                        .HasForeignKey("TestSuiteId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");

                    b.Navigation("TestSuite");
                });

            modelBuilder.Entity("TestExecutionLog", b =>
                {
                    b.HasOne("ScheduledTest", "ScheduledTest")
                        .WithMany()
                        .HasForeignKey("ScheduledTestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ScheduledTest");
                });

            modelBuilder.Entity("TestSuite", b =>
                {
                    b.HasOne("Project", "Project")
                        .WithMany("TestSuites")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Organization", b =>
                {
                    b.Navigation("Projects");
                });

            modelBuilder.Entity("Project", b =>
                {
                    b.Navigation("TestCases");

                    b.Navigation("TestSuites");
                });

            modelBuilder.Entity("TestSuite", b =>
                {
                    b.Navigation("ScheduledTests");

                    b.Navigation("TestCases");
                });
#pragma warning restore 612, 618
        }
    }
}
