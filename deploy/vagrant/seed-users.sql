-- ============================================================================
-- Seed login users for the Layla demo (run against the SQL Server in VM "data").
-- Idempotent: re-running deletes and recreates the two accounts.
--
--   admin@layla.local / Admin123!   -> roles: Admin, Writer
--   gvera@layla.local / patito      -> role:  Writer
--
-- PasswordHash values are ASP.NET Core Identity v3 (PBKDF2-HMACSHA256, 100k iter).
-- Identity reads PRF/iter/salt from the hash itself, so they verify regardless of
-- the server's current PasswordHasher defaults.
-- ============================================================================
SET NOCOUNT ON;
USE LaylaCore;

DECLARE @adminEmail nvarchar(256) = N'admin@layla.local';
DECLARE @userEmail  nvarchar(256) = N'gvera@layla.local';

-- Clean previous runs (remove role links first, then the users) ---------------
DELETE ur
  FROM AspNetUserRoles ur
  INNER JOIN AspNetUsers u ON u.Id = ur.UserId
  WHERE u.NormalizedEmail IN (UPPER(@adminEmail), UPPER(@userEmail));

DELETE FROM AspNetUsers
  WHERE NormalizedEmail IN (UPPER(@adminEmail), UPPER(@userEmail));

DECLARE @adminId nvarchar(450) = LOWER(CONVERT(nvarchar(36), NEWID()));
DECLARE @userId  nvarchar(450) = LOWER(CONVERT(nvarchar(36), NEWID()));

-- Admin -----------------------------------------------------------------------
INSERT INTO AspNetUsers
  (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
   PasswordHash, SecurityStamp, ConcurrencyStamp,
   PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
   LockoutEnd, LockoutEnabled, AccessFailedCount,
   DisplayName, Bio, AvatarUrl, CreatedAt, TokenVersion)
VALUES
  (@adminId, @adminEmail, UPPER(@adminEmail), @adminEmail, UPPER(@adminEmail), 1,
   N'AQAAAAEAAYagAAAAEEu1i92kmhYown5La6vTFK6OnRdz1VGI0ez2iJlt3HEp4P8Qjx4mUVrh1njVy++hoA==',
   CONVERT(nvarchar(36), NEWID()), CONVERT(nvarchar(36), NEWID()),
   NULL, 0, 0, NULL, 1, 0,
   N'Admin', NULL, NULL, GETUTCDATE(), 1);

-- User: gvera -----------------------------------------------------------------
INSERT INTO AspNetUsers
  (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
   PasswordHash, SecurityStamp, ConcurrencyStamp,
   PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
   LockoutEnd, LockoutEnabled, AccessFailedCount,
   DisplayName, Bio, AvatarUrl, CreatedAt, TokenVersion)
VALUES
  (@userId, @userEmail, UPPER(@userEmail), @userEmail, UPPER(@userEmail), 1,
   N'AQAAAAEAAYagAAAAEODkHD/JKU0mgoAGs7bDIuZgcgaZRPLtRyUx2BRGMXoAzydNCGghxar2LyJcJ7Yzww==',
   CONVERT(nvarchar(36), NEWID()), CONVERT(nvarchar(36), NEWID()),
   NULL, 0, 0, NULL, 1, 0,
   N'gvera', NULL, NULL, GETUTCDATE(), 1);

-- Role assignments (roles are seeded at server-core startup) -------------------
INSERT INTO AspNetUserRoles (UserId, RoleId)
  SELECT @adminId, Id FROM AspNetRoles WHERE NormalizedName IN (N'ADMIN', N'WRITER');

INSERT INTO AspNetUserRoles (UserId, RoleId)
  SELECT @userId, Id FROM AspNetRoles WHERE NormalizedName = N'WRITER';

-- Confirmation ----------------------------------------------------------------
SELECT u.Email, u.EmailConfirmed, STRING_AGG(r.Name, ', ') AS Roles
  FROM AspNetUsers u
  LEFT JOIN AspNetUserRoles ur ON ur.UserId = u.Id
  LEFT JOIN AspNetRoles r ON r.Id = ur.RoleId
  WHERE u.NormalizedEmail IN (UPPER(@adminEmail), UPPER(@userEmail))
  GROUP BY u.Email, u.EmailConfirmed;
