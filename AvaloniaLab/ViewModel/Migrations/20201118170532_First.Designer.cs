﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ViewModel;

namespace ViewModel.Migrations
{
    [DbContext(typeof(ImagesLibraryContext))]
    [Migration("20201118170532_First")]
    partial class First
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("ViewModel.ImageRecognized", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ImageName")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ImageRecognizedDetailsId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ImageTypeId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ImageRecognizedDetailsId");

                    b.HasIndex("ImageTypeId");

                    b.ToTable("Images");
                });

            modelBuilder.Entity("ViewModel.ImageRecognizedDetails", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("BinaryFile")
                        .HasColumnType("BLOB");

                    b.HasKey("Id");

                    b.ToTable("Details");
                });

            modelBuilder.Entity("ViewModel.PredictionResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("PredictionStringResult")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("TypesOfImages");
                });

            modelBuilder.Entity("ViewModel.ImageRecognized", b =>
                {
                    b.HasOne("ViewModel.ImageRecognizedDetails", "ImageRecognizedDetails")
                        .WithMany()
                        .HasForeignKey("ImageRecognizedDetailsId");

                    b.HasOne("ViewModel.PredictionResult", "ImageType")
                        .WithMany("RecognizedImages")
                        .HasForeignKey("ImageTypeId");

                    b.Navigation("ImageRecognizedDetails");

                    b.Navigation("ImageType");
                });

            modelBuilder.Entity("ViewModel.PredictionResult", b =>
                {
                    b.Navigation("RecognizedImages");
                });
#pragma warning restore 612, 618
        }
    }
}