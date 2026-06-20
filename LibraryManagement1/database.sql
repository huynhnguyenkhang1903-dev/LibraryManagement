CREATE TABLE [Authors] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Bio] nvarchar(max) NULL,
    [BirthDate] datetime2 NULL,
    CONSTRAINT [PK_Authors] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(250) NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Publishers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Address] nvarchar(200) NULL,
    [Phone] nvarchar(20) NULL,
    CONSTRAINT [PK_Publishers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Readers] (
    [Id] int NOT NULL IDENTITY,
    [ReaderCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [BirthDate] datetime2 NOT NULL,
    [Address] nvarchar(200) NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Readers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Books] (
    [Id] int NOT NULL IDENTITY,
    [BookCode] nvarchar(50) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [CategoryId] int NOT NULL,
    [AuthorId] int NOT NULL,
    [PublisherId] int NOT NULL,
    [PublishYear] int NOT NULL,
    [Quantity] int NOT NULL,
    [AvailableQuantity] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_Books] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Books_Authors_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Authors] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Books_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Books_Publishers_PublisherId] FOREIGN KEY ([PublisherId]) REFERENCES [Publishers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [BorrowTickets] (
    [Id] int NOT NULL IDENTITY,
    [TicketCode] nvarchar(50) NOT NULL,
    [ReaderId] int NOT NULL,
    [BorrowDate] datetime2 NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedByUserId] int NULL,
    CONSTRAINT [PK_BorrowTickets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BorrowTickets_Readers_ReaderId] FOREIGN KEY ([ReaderId]) REFERENCES [Readers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BorrowTickets_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id])
);
GO


CREATE TABLE [BorrowDetails] (
    [Id] int NOT NULL IDENTITY,
    [BorrowTicketId] int NOT NULL,
    [BookId] int NOT NULL,
    [ReturnDate] datetime2 NULL,
    [FineAmount] decimal(18,2) NOT NULL,
    [Note] nvarchar(250) NULL,
    CONSTRAINT [PK_BorrowDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BorrowDetails_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [Books] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BorrowDetails_BorrowTickets_BorrowTicketId] FOREIGN KEY ([BorrowTicketId]) REFERENCES [BorrowTickets] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [FineReceipts] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptCode] nvarchar(50) NOT NULL,
    [ReaderId] int NOT NULL,
    [BorrowTicketId] int NOT NULL,
    [FineAmount] decimal(18,2) NOT NULL,
    [AmountPaid] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    CONSTRAINT [PK_FineReceipts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FineReceipts_BorrowTickets_BorrowTicketId] FOREIGN KEY ([BorrowTicketId]) REFERENCES [BorrowTickets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FineReceipts_Readers_ReaderId] FOREIGN KEY ([ReaderId]) REFERENCES [Readers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE INDEX [IX_Books_AuthorId] ON [Books] ([AuthorId]);
GO


CREATE INDEX [IX_Books_CategoryId] ON [Books] ([CategoryId]);
GO


CREATE INDEX [IX_Books_PublisherId] ON [Books] ([PublisherId]);
GO


CREATE INDEX [IX_BorrowDetails_BookId] ON [BorrowDetails] ([BookId]);
GO


CREATE INDEX [IX_BorrowDetails_BorrowTicketId] ON [BorrowDetails] ([BorrowTicketId]);
GO


CREATE INDEX [IX_BorrowTickets_CreatedByUserId] ON [BorrowTickets] ([CreatedByUserId]);
GO


CREATE INDEX [IX_BorrowTickets_ReaderId] ON [BorrowTickets] ([ReaderId]);
GO


CREATE INDEX [IX_FineReceipts_BorrowTicketId] ON [FineReceipts] ([BorrowTicketId]);
GO


CREATE INDEX [IX_FineReceipts_ReaderId] ON [FineReceipts] ([ReaderId]);
GO


