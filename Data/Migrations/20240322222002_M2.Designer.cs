﻿// <auto-generated />
using System;
using DockerMVC.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace SignalrChat.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20240322222002_M2")]
    partial class M2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.3");

            modelBuilder.Entity("comp4870project.Model.SavedMessage", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ConversationId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Language")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("MessageId")
                        .HasColumnType("TEXT");

                    b.Property<bool>("OriginalMessage")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SenderName")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("SentDate")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("Messages");
                });
#pragma warning restore 612, 618
        }
    }
}
