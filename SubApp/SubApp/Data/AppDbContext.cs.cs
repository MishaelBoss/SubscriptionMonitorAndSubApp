using Avalonia.Platform;
using Microsoft.EntityFrameworkCore;
using SubApp.Models;
using System;
using System.IO;

namespace SubApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Mailbox> Mailboxes { get; set; }
    public DbSet<ParsedEmail> ParsedEmails { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string dbName = "db.sqlite3";
        string dbPath;

        if (OperatingSystem.IsAndroid())
        {
            dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dbName);

            if (!File.Exists(dbPath))
            {
                var assetUri = new Uri("avares://SubApp/Data/Sqlite/db.sqlite3");
                using var assetStream = AssetLoader.Open(assetUri);
                using var fileStream = File.Create(dbPath);
                assetStream.CopyTo(fileStream);
            }
        }
        else
        {
            dbPath = Path.Combine("Data", "Sqlite", dbName);
        }

        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Profile>()
            .HasOne(p => p.User)
            .WithOne()
            .HasForeignKey<Profile>(p => p.UserId);

        modelBuilder.Entity<Profile>().ToTable("accounts_profile");
        modelBuilder.Entity<User>().ToTable("auth_user");
        modelBuilder.Entity<Mailbox>().ToTable("mail_parser_mailbox");
        modelBuilder.Entity<ParsedEmail>().ToTable("mail_parser_parsedemail");
        modelBuilder.Entity<Subscription>().ToTable("subscriptions_subscription");
    }
}
