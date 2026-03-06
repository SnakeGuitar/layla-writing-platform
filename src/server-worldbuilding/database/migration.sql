BEGIN TRANSACTION;
CREATE TABLE [Projects] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Synopsis] nvarchar(max) NOT NULL,
    [LiteraryGenre] nvarchar(max) NOT NULL,
    [CoverImageUrl] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
);

CREATE TABLE [ProjectRoles] (
    [ProjectId] uniqueidentifier NOT NULL,
    [AppUserId] nvarchar(450) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProjectRoles] PRIMARY KEY ([ProjectId], [AppUserId]),
    CONSTRAINT [FK_ProjectRoles_AspNetUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProjectRoles_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ProjectRoles_AppUserId] ON [ProjectRoles] ([AppUserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260225180803_AddProjectEntities', N'10.0.3');

COMMIT;
GO

