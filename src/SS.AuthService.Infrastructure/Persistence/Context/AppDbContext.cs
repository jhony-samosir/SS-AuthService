using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Infrastructure.Persistence.Context;

public partial class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public virtual DbSet<AuthSession> AuthSessions { get; set; }

    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }

    public virtual DbSet<LoginAttempt> LoginAttempts { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<MfaRecoveryCode> MfaRecoveryCodes { get; set; }

    public virtual DbSet<PasswordHistory> PasswordHistories { get; set; }

    public virtual DbSet<PasswordReset> PasswordResets { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleMenu> RoleMenus { get; set; }

    public virtual DbSet<SocialAccount> SocialAccounts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_sessions_pkey");

            entity.ToTable("auth_sessions");

            entity.HasIndex(e => e.PublicId, "auth_sessions_public_id_key").IsUnique();

            entity.HasIndex(e => e.RefreshTokenHash, "auth_sessions_refresh_token_hash_key").IsUnique();

            entity.HasIndex(e => new { e.ExpiresAt, e.IsRevoked }, "idx_auth_sessions_expires_revoked");

            entity.HasIndex(e => e.UserId, "idx_auth_sessions_user_id");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(255)
                .HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.IsRevoked).HasColumnName("is_revoked");
            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("public_id");
            entity.Property(e => e.RefreshTokenHash)
                .HasMaxLength(255)
                .HasColumnName("refresh_token_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("email_verifications_pkey");

            entity.ToTable("email_verifications");

            entity.HasIndex(e => e.VerificationTokenHash, "email_verifications_verification_token_hash_key").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_email_verifications_user_id");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VerificationTokenHash)
                .HasMaxLength(255)
                .HasColumnName("verification_token_hash");
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("login_attempts_pkey");

            entity.ToTable("login_attempts");

            entity.HasIndex(e => e.AttemptedAt, "idx_login_attempts_attempted_at");

            entity.HasIndex(e => e.IpAddress, "idx_login_attempts_ip_address");

            entity.HasIndex(e => e.UserId, "idx_login_attempts_user_id");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AttemptedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("attempted_at");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(255)
                .HasColumnName("device_info");
            entity.Property(e => e.EmailAttempted)
                .HasMaxLength(255)
                .HasColumnName("email_attempted");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.IsSuccess).HasColumnName("is_success");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("menus_pkey");

            entity.ToTable("menus");

            entity.HasIndex(e => e.Id, "idx_menus_active_only").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.ParentId, "idx_menus_parent_id");

            entity.HasIndex(e => e.Path, "menus_path_key").IsUnique();

            entity.HasIndex(e => e.PublicId, "menus_public_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.Icon)
                .HasMaxLength(50)
                .HasColumnName("icon");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.Path)
                .HasMaxLength(255)
                .HasColumnName("path");
            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("public_id");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(d => d.ParentId);
        });

        modelBuilder.Entity<MfaRecoveryCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mfa_recovery_codes_pkey");

            entity.ToTable("mfa_recovery_codes");

            entity.HasIndex(e => e.UserId, "idx_mfa_recovery_codes_user_id");

            entity.HasIndex(e => e.CodeHash, "mfa_recovery_codes_code_hash_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CodeHash)
                .HasMaxLength(255)
                .HasColumnName("code_hash");
            entity.Property(e => e.IsUsed).HasColumnName("is_used");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<PasswordHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("password_histories_pkey");

            entity.ToTable("password_histories");

            entity.HasIndex(e => e.UserId, "idx_password_histories_user_id");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("password_resets_pkey");

            entity.ToTable("password_resets");

            entity.HasIndex(e => e.UserId, "idx_password_resets_user_id");

            entity.HasIndex(e => e.ResetTokenHash, "password_resets_reset_token_hash_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed).HasColumnName("is_used");
            entity.Property(e => e.ResetTokenHash)
                .HasMaxLength(255)
                .HasColumnName("reset_token_hash");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Id, "idx_roles_active_only").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.HasIndex(e => e.PublicId, "roles_public_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("public_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        });

        modelBuilder.Entity<RoleMenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_menu_pkey");

            entity.ToTable("role_menu");

            entity.HasIndex(e => new { e.RoleId, e.MenuId }, "idx_role_menu_role_menu");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CanCreate).HasColumnName("can_create");
            entity.Property(e => e.CanDelete).HasColumnName("can_delete");
            entity.Property(e => e.CanRead)
                .HasDefaultValue(true)
                .HasColumnName("can_read");
            entity.Property(e => e.CanUpdate).HasColumnName("can_update");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.MenuId).HasColumnName("menu_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Menu)
                .WithMany(p => p.RoleMenus)
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Role)
                .WithMany(p => p.RoleMenus)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SocialAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("social_accounts_pkey");

            entity.ToTable("social_accounts");

            entity.HasIndex(e => e.UserId, "idx_social_accounts_user_id");

            entity.HasIndex(e => new { e.Provider, e.ProviderAccountId }, "uq_provider_account").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasColumnName("provider");
            entity.Property(e => e.ProviderAccountId)
                .HasMaxLength(255)
                .HasColumnName("provider_account_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Id, "idx_users_active_only").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.IsActive, "idx_users_is_active");

            entity.HasIndex(e => e.RoleId, "idx_users_role_id");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.PublicId, "users_public_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmailVerifiedAt).HasColumnName("email_verified_at");
            entity.Property(e => e.FailedLoginAttempts).HasColumnName("failed_login_attempts");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LockedUntil).HasColumnName("locked_until");
            entity.Property(e => e.MfaEnabled).HasColumnName("mfa_enabled");
            entity.Property(e => e.MfaSecret)
                .HasMaxLength(255)
                .HasColumnName("mfa_secret");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PrivacyPolicyAcceptedAt).HasColumnName("privacy_policy_accepted_at");
            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("public_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.TosAcceptedAt).HasColumnName("tos_accepted_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
