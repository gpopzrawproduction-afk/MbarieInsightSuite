using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIC.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlertName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Context = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AssetType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    HealthScore = table.Column<double>(type: "REAL", nullable: true),
                    LastMonitoredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Specifications = table.Column<string>(type: "TEXT", nullable: false),
                    AssociatedMetrics = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContextName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    DecisionMaker = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Deadline = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ContextData = table.Column<string>(type: "TEXT", nullable: false),
                    ConsideredOptions = table.Column<string>(type: "TEXT", nullable: false),
                    SelectedOption = table.Column<string>(type: "TEXT", nullable: true),
                    AIRecommendation = table.Column<string>(type: "TEXT", nullable: true),
                    AIConfidence = table.Column<double>(type: "REAL", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessTokenEncrypted = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    RefreshTokenEncrypted = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GrantedScopes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ImapServer = table.Column<string>(type: "TEXT", nullable: true),
                    ImapPort = table.Column<int>(type: "INTEGER", nullable: false),
                    SmtpServer = table.Column<string>(type: "TEXT", nullable: true),
                    SmtpPort = table.Column<int>(type: "INTEGER", nullable: false),
                    UseSsl = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordEncrypted = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSyncAttemptAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalEmailsSynced = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAttachmentsSynced = table.Column<int>(type: "INTEGER", nullable: false),
                    DeltaLink = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    HistoryId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastSyncError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "INTEGER", nullable: false),
                    SyncIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    InitialSyncDays = table.Column<int>(type: "INTEGER", nullable: false),
                    SyncAttachments = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxAttachmentSizeMB = table.Column<int>(type: "INTEGER", nullable: false),
                    FoldersToSync = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    StorageUsedBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    UnreadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiresResponseCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FromAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FromName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ToRecipients = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CcRecipients = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    BccRecipients = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    SentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BodyText = table.Column<string>(type: "TEXT", nullable: false),
                    BodyHtml = table.Column<string>(type: "TEXT", nullable: true),
                    BodyPreview = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFlagged = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDraft = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasAttachments = table.Column<bool>(type: "INTEGER", nullable: false),
                    Folder = table.Column<int>(type: "INTEGER", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    AIPriority = table.Column<int>(type: "INTEGER", nullable: false),
                    AICategory = table.Column<int>(type: "INTEGER", nullable: false),
                    Sentiment = table.Column<int>(type: "INTEGER", nullable: false),
                    ContainsActionItems = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresResponse = table.Column<bool>(type: "INTEGER", nullable: false),
                    SuggestedResponseBy = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AISummary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ExtractedKeywords = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ActionItems = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    AIConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    IsAIProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    AIProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUrgent = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmailAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConversationId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    InReplyTo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    KnowledgeEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    FullContent = table.Column<string>(type: "TEXT", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    AISummary = table.Column<string>(type: "TEXT", nullable: true),
                    RelevanceScore = table.Column<double>(type: "REAL", nullable: false),
                    LastAccessed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MetricName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(256)", nullable: false),
                    Salt = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    JobPosition = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    SeniorityLevel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SizeInBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    EmailMessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ExtractedText = table.Column<string>(type: "TEXT", nullable: true),
                    IsProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: true),
                    WordCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ProcessingError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AISummary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ExtractedKeywords = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    DocumentCategory = table.Column<int>(type: "INTEGER", nullable: true),
                    ClassificationConfidence = table.Column<double>(type: "REAL", nullable: true),
                    KnowledgeEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EmbeddingId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsIndexed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAttachments_EmailMessages_EmailMessageId",
                        column: x => x.EmailMessageId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Query = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Response = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Context = table.Column<string>(type: "TEXT", nullable: true),
                    AIProvider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModelUsed = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TokenCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Cost = table.Column<decimal>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Local"),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SettingsJson = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SettingsVersion = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettingHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SettingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    NewValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingHistory_Settings_SettingId",
                        column: x => x.SettingId,
                        principalTable: "Settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_CreatedAt",
                table: "Alerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Severity",
                table: "Alerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Severity_Status",
                table: "Alerts",
                columns: new[] { "Severity", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Source",
                table: "Alerts",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Status",
                table: "Alerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TriggeredAt",
                table: "Alerts",
                column: "TriggeredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetType",
                table: "Assets",
                column: "AssetType");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_LastMonitoredAt",
                table: "Assets",
                column: "LastMonitoredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Location",
                table: "Assets",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Status",
                table: "Assets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistories_SessionId",
                table: "ChatHistories",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistories_Timestamp",
                table: "ChatHistories",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistories_UserId",
                table: "ChatHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistories_UserId_Timestamp",
                table: "ChatHistories",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_Deadline",
                table: "Decisions",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_DecisionMaker",
                table: "Decisions",
                column: "DecisionMaker");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_Priority",
                table: "Decisions",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_Status",
                table: "Decisions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_EmailAddress",
                table: "EmailAccounts",
                column: "EmailAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_IsActive",
                table: "EmailAccounts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_IsPrimary",
                table: "EmailAccounts",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_LastSyncedAt",
                table: "EmailAccounts",
                column: "LastSyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_Provider",
                table: "EmailAccounts",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_Status",
                table: "EmailAccounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_UserId",
                table: "EmailAccounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_UserId_EmailAddress",
                table: "EmailAccounts",
                columns: new[] { "UserId", "EmailAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_UserId_IsActive",
                table: "EmailAccounts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_UserId_IsPrimary",
                table: "EmailAccounts",
                columns: new[] { "UserId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_EmailMessageId",
                table: "EmailAttachments",
                column: "EmailMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_EmailMessageId_Type",
                table: "EmailAttachments",
                columns: new[] { "EmailMessageId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_IsIndexed",
                table: "EmailAttachments",
                column: "IsIndexed");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_IsProcessed",
                table: "EmailAttachments",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_KnowledgeEntryId",
                table: "EmailAttachments",
                column: "KnowledgeEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_Status",
                table: "EmailAttachments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_Type",
                table: "EmailAttachments",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_AICategory",
                table: "EmailMessages",
                column: "AICategory");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_AIPriority",
                table: "EmailMessages",
                column: "AIPriority");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_ConversationId",
                table: "EmailMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_EmailAccountId",
                table: "EmailMessages",
                column: "EmailAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_EmailAccountId_ReceivedDate",
                table: "EmailMessages",
                columns: new[] { "EmailAccountId", "ReceivedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Folder",
                table: "EmailMessages",
                column: "Folder");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_FromAddress",
                table: "EmailMessages",
                column: "FromAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_IsRead",
                table: "EmailMessages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_MessageId",
                table: "EmailMessages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_ReceivedDate",
                table: "EmailMessages",
                column: "ReceivedDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_RequiresResponse",
                table: "EmailMessages",
                column: "RequiresResponse");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_SentDate",
                table: "EmailMessages",
                column: "SentDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId",
                table: "EmailMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId_CreatedAt",
                table: "EmailMessages",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId_IsRead_Folder",
                table: "EmailMessages",
                columns: new[] { "UserId", "IsRead", "Folder" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId_ReceivedDate",
                table: "EmailMessages",
                columns: new[] { "UserId", "ReceivedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId_RequiresResponse",
                table: "EmailMessages",
                columns: new[] { "UserId", "RequiresResponse" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeEntries_CreatedAt",
                table: "KnowledgeEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeEntries_SourceId",
                table: "KnowledgeEntries",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeEntries_UserId",
                table: "KnowledgeEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_Category",
                table: "Metrics",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_MetricName",
                table: "Metrics",
                column: "MetricName");

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_Severity",
                table: "Metrics",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_Source",
                table: "Metrics",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_Timestamp",
                table: "Metrics",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SettingHistory_SettingId_ChangedAt",
                table: "SettingHistory",
                columns: new[] { "SettingId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Settings_SyncStatus",
                table: "Settings",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_UserId_Category_Key",
                table: "Settings",
                columns: new[] { "UserId", "Category", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "ChatHistories");

            migrationBuilder.DropTable(
                name: "Decisions");

            migrationBuilder.DropTable(
                name: "EmailAccounts");

            migrationBuilder.DropTable(
                name: "EmailAttachments");

            migrationBuilder.DropTable(
                name: "KnowledgeEntries");

            migrationBuilder.DropTable(
                name: "Metrics");

            migrationBuilder.DropTable(
                name: "SettingHistory");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "EmailMessages");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
