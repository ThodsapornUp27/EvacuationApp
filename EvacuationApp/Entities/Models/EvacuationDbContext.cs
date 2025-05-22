using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EvacuationApp.Entities.Models;

public partial class EvacuationDbContext : DbContext
{
    public EvacuationDbContext()
    {
    }

    public EvacuationDbContext(DbContextOptions<EvacuationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<Zone> Zones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Vehicle");

            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.VehicleId).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Zone>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.PeopleNumber).HasColumnName("People_Number");
            entity.Property(e => e.UrgencyLevel).HasColumnName("Urgency_Level");
            entity.Property(e => e.ZonesId).ValueGeneratedOnAdd();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
