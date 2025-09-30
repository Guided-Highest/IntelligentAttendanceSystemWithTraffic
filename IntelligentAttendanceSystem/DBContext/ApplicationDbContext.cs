using IntelligentAttendanceSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace IntelligentAttendanceSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<UserShift> UserShifts { get; set; }
        public DbSet<SystemDevice> SystemDevices { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SystemDevice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IPAddress).IsRequired().HasMaxLength(15);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DeviceType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.MacAddress).HasMaxLength(17);
                entity.Property(e => e.Gateway).HasMaxLength(17);
                entity.Property(e => e.SubnetMask).HasMaxLength(15);
            });

            // Shift configuration
            builder.Entity<Shift>(entity =>
            {
                entity.HasIndex(s => s.ShiftCode).IsUnique();
                entity.Property(s => s.StartTime).HasColumnType("time");
                entity.Property(s => s.OffTime).HasColumnType("time");
                entity.Property(s => s.RelaxTime).HasColumnType("time");

                // Calculate TotalHours
                entity.Ignore(s => s.TotalHours);
                entity.Ignore(s => s.LateThreshold);
                entity.Ignore(s => s.DisplayName);
            });

            // UserShift configuration - FIXED
            builder.Entity<UserShift>(entity =>
            {
                entity.HasIndex(us => new { us.UserId, us.EffectiveDate });

                // Relationship with User
                entity.HasOne(us => us.User)
                      .WithMany(u => u.UserShifts)
                      .HasForeignKey(us => us.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relationship with Shift - FIXED: Use UserShifts navigation property
                entity.HasOne(us => us.Shift)
                      .WithMany(s => s.UserShifts)
                      .HasForeignKey(us => us.ShiftId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            // Ignore computed properties in ApplicationUser
            builder.Entity<ApplicationUser>()
                .Ignore(u => u.CurrentShift)
                .Ignore(u => u.CurrentShiftId);
            builder.Entity<ApplicationUser>()
        .HasIndex(e => new { e.CredentialType, e.CredentialNumber })
        .IsUnique();

            // Configure relationships
            builder.Entity<Attendance>()
                .HasOne(a => a.User)
                .WithMany(u => u.Attendances)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint for user and date
            builder.Entity<Attendance>()
                .HasIndex(a => new { a.UserId, a.Date })
                .IsUnique();

            // Seed data
            //builder.Entity<ApplicationUser>().HasData(
            //    new ApplicationUser
            //    {
            //        Id = "1",
            //        UserName = "admin@ams.com",
            //        NormalizedUserName = "ADMIN@AMS.COM",
            //        Email = "admin@ams.com",
            //        NormalizedEmail = "ADMIN@AMS.COM",
            //        EmailConfirmed = true,
            //        PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(null, "Admin123!"),
            //        SecurityStamp = string.Empty,
            //        FullName = "System Administrator",
            //        UserType = UserType.Admin,
            //        IsActive = true,
            //        CreatedDate = DateTime.UtcNow
            //    }
            //);
            // Seed default shifts
            builder.Entity<Shift>().HasData(
                new Shift
                {
                    ShiftId = 1,
                    ShiftName = "Morning Shift",
                    ShiftCode = "MORN",
                    StartTime = new TimeSpan(9, 0, 0), // 9:00 AM
                    RelaxTime = new TimeSpan(0, 15, 0), // 15 minutes
                    OffTime = new TimeSpan(17, 0, 0),   // 5:00 PM
                    Description = "Standard morning shift",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Shift
                {
                    ShiftId = 2,
                    ShiftName = "Evening Shift",
                    ShiftCode = "EVEN",
                    StartTime = new TimeSpan(14, 0, 0), // 2:00 PM
                    RelaxTime = new TimeSpan(0, 15, 0), // 15 minutes
                    OffTime = new TimeSpan(22, 0, 0),   // 10:00 PM
                    Description = "Evening shift",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Shift
                {
                    ShiftId = 3,
                    ShiftName = "Night Shift",
                    ShiftCode = "NIGHT",
                    StartTime = new TimeSpan(22, 0, 0), // 10:00 PM
                    RelaxTime = new TimeSpan(0, 15, 0), // 15 minutes
                    OffTime = new TimeSpan(6, 0, 0),    // 6:00 AM (next day)
                    Description = "Night shift",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }
            );
        }
    }
}