using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TenderSystem.Models;

public partial class TenderSystemContext : DbContext
{
    public TenderSystemContext()
    {
    }

    public TenderSystemContext(DbContextOptions<TenderSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bank> Banks { get; set; }

    public virtual DbSet<BlogContent> BlogContents { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

    public virtual DbSet<TenderApplication> TenderApplications { get; set; }

    public virtual DbSet<TenderDetail> TenderDetails { get; set; }

    public virtual DbSet<UserList> UserLists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("name = dbConn");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bank>(entity =>
        {
            entity.HasKey(e => e.BankId).HasName("PK__Bank__AA08CB13AFED9446");

            entity.ToTable("Bank");

            entity.Property(e => e.BankId).ValueGeneratedNever();
            entity.Property(e => e.AccountHolderName).HasMaxLength(100);
            entity.Property(e => e.AccountNumber).HasMaxLength(100);
            entity.Property(e => e.AccountType).HasMaxLength(50);
            entity.Property(e => e.BankName).HasMaxLength(100);

            entity.HasOne(d => d.Userbank).WithMany(p => p.Banks)
                .HasForeignKey(d => d.UserbankId)
                .HasConstraintName("FK__Bank__UserbankId__5535A963");
        });

        modelBuilder.Entity<BlogContent>(entity =>
        {
            entity.HasKey(e => e.Bid).HasName("PK__BlogCont__C6DE0CC1BFDBE17A");

            entity.ToTable("BlogContent");

            entity.Property(e => e.Bid)
                .ValueGeneratedNever()
                .HasColumnName("BId");

            entity.HasOne(d => d.UploadUser).WithMany(p => p.BlogContents)
                .HasForeignKey(d => d.UploadUserId)
                .HasConstraintName("FK__BlogConte__Uploa__6D0D32F4");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("PK__Chat__A9FBE7C6D29B7BD9");

            entity.ToTable("Chat");

            entity.Property(e => e.ChatId).ValueGeneratedNever();
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Receiver).WithMany(p => p.ChatReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Chat__ReceiverId__70DDC3D8");

            entity.HasOne(d => d.Sender).WithMany(p => p.ChatSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Chat__SenderId__6FE99F9F");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyId).HasName("PK__Company__2D971CAC5A397031");

            entity.ToTable("Company");

            entity.HasIndex(e => e.PanNumber, "UQ__Company__7C38BFC888E92F52").IsUnique();

            entity.HasIndex(e => e.RegistrationNumber, "UQ__Company__E88646021FA0CB4D").IsUnique();

            entity.HasIndex(e => e.OfficeEmail, "UQ__Company__FCEC3C727FED691E").IsUnique();

            entity.Property(e => e.CompanyId).ValueGeneratedNever();
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.CompanyType).HasMaxLength(50);
            entity.Property(e => e.CompanyWebsiteUrl).HasMaxLength(255);
            entity.Property(e => e.FullAddress).HasMaxLength(50);
            entity.Property(e => e.OfficeEmail).HasMaxLength(100);
            entity.Property(e => e.PanNumber).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(50);
            entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.RegistrationNumber).HasMaxLength(50);

            entity.HasOne(d => d.Userbid).WithMany(p => p.Companies)
                .HasForeignKey(d => d.UserbidId)
                .HasConstraintName("FK__Company__Userbid__5165187F");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A382B74128D");

            entity.ToTable("Payment");

            entity.Property(e => e.PaymentId).ValueGeneratedNever();
            entity.Property(e => e.PaymentAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(20);

            entity.HasOne(d => d.PayByUserNavigation).WithMany(p => p.PaymentPayByUserNavigations)
                .HasForeignKey(d => d.PayByUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__PayByUs__6754599E");

            entity.HasOne(d => d.PayCompany).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PayCompanyId)
                .HasConstraintName("FK__Payment__PayComp__656C112C");

            entity.HasOne(d => d.PayTender).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PayTenderId)
                .HasConstraintName("FK__Payment__PayTend__6477ECF3");

            entity.HasOne(d => d.PayToUserNavigation).WithMany(p => p.PaymentPayToUserNavigations)
                .HasForeignKey(d => d.PayToUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__PayToUs__66603565");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.RatingId).HasName("PK__Rating__FCCDF87CBB3469F1");

            entity.ToTable("Rating");

            entity.Property(e => e.RatingId).ValueGeneratedNever();
            entity.Property(e => e.Rate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Rating1)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("Rating");
            entity.Property(e => e.RatingDescription).HasMaxLength(500);

            entity.HasOne(d => d.RatingByNavigation).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.RatingBy)
                .HasConstraintName("FK__Rating__RatingBy__75A278F5");

            entity.HasOne(d => d.RatingForNavigation).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.RatingFor)
                .HasConstraintName("FK__Rating__RatingFo__76969D2E");
        });

        modelBuilder.Entity<TenderApplication>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("PK__TenderAp__C93A4C9997520343");

            entity.ToTable("TenderApplication");

            entity.Property(e => e.ApplicationId).ValueGeneratedNever();
            entity.Property(e => e.ApplicationStatus).HasMaxLength(10);
            entity.Property(e => e.ProposedBudget).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProposedDuration).HasMaxLength(50);

            entity.HasOne(d => d.CompanyApply).WithMany(p => p.TenderApplications)
                .HasForeignKey(d => d.CompanyApplyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TenderApp__Compa__60A75C0F");

            entity.HasOne(d => d.TenderApplly).WithMany(p => p.TenderApplications)
                .HasForeignKey(d => d.TenderAppllyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TenderApp__Tende__5FB337D6");
        });

        modelBuilder.Entity<TenderDetail>(entity =>
        {
            entity.HasKey(e => e.TenderId).HasName("PK__TenderDe__B21B4268465644CF");

            entity.Property(e => e.TenderId).ValueGeneratedNever();
            entity.Property(e => e.AwardStatus).HasMaxLength(20);
            entity.Property(e => e.BudgetEstimation).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsVerified).HasMaxLength(20);
            entity.Property(e => e.IssuedBy).HasMaxLength(100);
            entity.Property(e => e.ProjectDuration).HasMaxLength(50);
            entity.Property(e => e.TenderStatus).HasMaxLength(20);
            entity.Property(e => e.TenderType).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.AwardCompany).WithMany(p => p.TenderDetails)
                .HasForeignKey(d => d.AwardCompanyId)
                .HasConstraintName("FK__TenderDet__Award__59063A47");

            entity.HasOne(d => d.PublishedByUser).WithMany(p => p.TenderDetails)
                .HasForeignKey(d => d.PublishedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TenderDet__Publi__59FA5E80");
        });

        modelBuilder.Entity<UserList>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserList__1788CC4C3019809F");

            entity.ToTable("UserList");

            entity.HasIndex(e => e.EmailAddress, "UQ__UserList__49A147408BF816A1").IsUnique();

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.District).HasMaxLength(50);
            entity.Property(e => e.EmailAddress).HasMaxLength(50);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.MiddleName).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Province).HasMaxLength(50);
            entity.Property(e => e.UserRole).HasMaxLength(40);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
