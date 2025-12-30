using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB.Entities;

namespace Shared.FPMS_DB
{
    public class FPMSDbContext : DbContext
    {

        public FPMSDbContext(DbContextOptions<FPMSDbContext> options)
            : base(options) { }

        public DbSet<BatchStatus> BatchStatus { get; set; }
        public DbSet<Dispenser> Dispenser { get; set; }
        public DbSet<DispenserNozzle> DispenserNozzle { get; set; }
        public DbSet<PriceUpdateBatch> PriceUpdateBatch { get; set; }
        public DbSet<PriceUpdateLog> PriceUpdateLog { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Transaction> Transaction { get; set; }
        public DbSet<SiteDetail> SiteDetail { get; set; }
        public DbSet<DispenserActionType> DispenserActionType { get; set; }
        public DbSet<DispenserActionLog> DispenserActionLog { get; set; }
        public DbSet<User> User { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Common default properties
            void ConfigureCommon<TEntity>() where TEntity : class
            {
                modelBuilder.Entity<TEntity>().Property<DateTime>("CreatedAt")
                    .HasDefaultValueSql("datetime('now')");
                modelBuilder.Entity<TEntity>().Property<bool>("IsActive")
                    .HasDefaultValue(true);
            }

            ConfigureCommon<Transaction>();
            ConfigureCommon<DispenserNozzle>();
            ConfigureCommon<SiteDetail>();
            ConfigureCommon<Product>();
            ConfigureCommon<PriceUpdateLog>();
            ConfigureCommon<PriceUpdateBatch>();
            ConfigureCommon<BatchStatus>();
            ConfigureCommon<DispenserActionLog>();
            ConfigureCommon<DispenserActionLog>();
            ConfigureCommon<Dispenser>();
            ConfigureCommon<User>();

            // Configure Dispenser defaults
            modelBuilder.Entity<Dispenser>(entity =>
            {
                entity.Property(d => d.IsOnline).HasDefaultValue(true);
                entity.Property(d => d.IsLocked).HasDefaultValue(false);

                // Relationship with Nozzles
                entity.HasMany(d => d.Nozzles)
                      .WithOne(n => n.Dispenser)
                      .HasForeignKey(n => n.DispenserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure DispenserNozzle defaults
            modelBuilder.Entity<DispenserNozzle>(entity =>
            {
                entity.Property(n => n.IsEnable)
                      .HasDefaultValue(true)
                      .IsRequired();

                entity.Property(n => n.ProductId)
                      .IsRequired(false);

                entity.Property(n => n.LastTotalLiter)
                      .IsRequired(false);

                entity.Property(n => n.LastTotalCash)
                      .IsRequired(false);
            });
        }

    }
    }
