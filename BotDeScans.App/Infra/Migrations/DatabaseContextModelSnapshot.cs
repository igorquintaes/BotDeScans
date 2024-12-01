﻿// <auto-generated />
using System;
using BotDeScans.App.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BotDeScans.App.Infra.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.11");

            modelBuilder.Entity("BotDeScans.App.Models.Title", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("DiscordRoleId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Titles");
                });

            modelBuilder.Entity("BotDeScans.App.Models.TitleReference", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Key")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TitleId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("TitleId", "Key")
                        .IsUnique();

                    b.ToTable("TitleReferences");
                });

            modelBuilder.Entity("BotDeScans.App.Models.TitleReference", b =>
                {
                    b.HasOne("BotDeScans.App.Models.Title", "Title")
                        .WithMany("References")
                        .HasForeignKey("TitleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Title");
                });

            modelBuilder.Entity("BotDeScans.App.Models.Title", b =>
                {
                    b.Navigation("References");
                });
#pragma warning restore 612, 618
        }
    }
}