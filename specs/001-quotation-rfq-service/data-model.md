# Data Model: Unified Quotation and RFQ Management Service

**Feature**: 001-quotation-rfq-service
**Date**: 2025-11-28
**Phase**: Phase 1 - Data Model Design

## Overview

This document defines the complete data model for the Quotation Service, including all entities, relationships, constraints, indexes, and validation rules. The model is designed for PostgreSQL 18 with Entity Framework Core 10.0.0.

## Entity Relationship Diagram

```
┌─────────────────┐       ┌──────────────────┐       ┌────────────────────┐
│   Customer      │───┬───│      RFQ         │───────│  FileReference     │
│                 │   │   │                  │       │                    │
│  Id (PK)        │   │   │  Id (PK)         │       │  Id (PK)           │
│  Email (UNQ)    │   │   │  CustomerId (FK) │       │  RfqId (FK)        │
│  PhoneNumber    │   │   │  Channel         │       │  QuotationId (FK)  │
│  Name           │   │   │  Status          │       │  UploadServiceId   │
│  MergedFrom[]   │   │   │  CreatedAt       │       │  FileName          │
│  MergeHistory   │   │   │  UpdatedAt       │       └────────────────────┘
└─────────────────┘   │   │  AssignedStaffId │
                      │   └──────────────────┘
                      │            │
                      │            │
                      │   ┌────────┴───────────┐
                      │   │   InternalNote     │
                      │   │                    │
                      │   │  Id (PK)           │
                      │   │  RfqId (FK)        │
                      │   │  QuotationId (FK)  │
                      │   │  AuthorUserId      │
                      │   │  Content           │
                      │   │  CreatedAt         │
                      │   └────────────────────┘
                      │
                      ├───┬───────────────────────────┐
                      │   │       Quotation           │
                      │   │                           │
                      │   │  Id (PK)                  │
                      │   │  CustomerId (FK)          │
                      │   │  SourceRfqId (FK, NULL)   │
                      │   │  CurrentVersionId (FK)    │
                      │   │  Status                   │
                      │   │  ValidityPeriod           │
                      │   │  CreatedAt                │
                      │   │  RowVersion               │
                      │   └───────┬───────────────────┘
                      │           │
                      │           │
                      │   ┌───────┴──────────────────┐
                      │   │   QuotationVersion       │
                      │   │                          │
                      │   │  Id (PK)                 │
                      │   │  QuotationId (FK)        │
                      │   │  VersionNumber           │
                      │   │  CreatedByUserId         │
                      │   │  CreatedAt               │
                      │   │  ChangeSummary           │
                      │   │  TotalPrice              │
                      │   │  DeliveryExpectations    │
                      │   └──────┬───────────────────┘
                      │          │
                      │          │
                      │   ┌──────┴──────────────────────┐
                      │   │   QuotationLineItem         │
                      │   │                             │
                      │   │  Id (PK)                    │
                      │   │  VersionId (FK)             │
                      │   │  LineNumber                 │
                      │   │  MaterialId (from ext svc)  │
                      │   │  Quantity                   │
                      │   │  UnitPrice                  │
                      │   │  LineTotal                  │
                      │   │  ManufacturingProcess       │
                      │   └─────────────────────────────┘
                      │
                      │   ┌────────────────────────┐
                      └───│  DiscountStructure     │
                          │                        │
                          │  Id (PK)               │
                          │  QuotationVersionId (FK)│
                          │  DiscountType          │
                          │  DiscountValue         │
                          │  Conditions            │
                          │  AuthorizationReason   │
                          └────────────────────────┘

┌──────────────────────────┐       ┌───────────────────┐
│   MaterialReference      │       │    StaffRole      │
│   (Cached from ext svc)  │       │                   │
│  Id (PK)                 │       │  Id (PK)          │
│  MaterialServiceId       │       │  RoleName         │
│  MaterialName            │       │  Permissions[]    │
│  Properties (JSONB)      │       │                   │
│  AvailabilityStatus      │       └───────────────────┘
│  CachedAt                │
│  ExpiresAt               │
└──────────────────────────┘

┌──────────────────────────────────┐
│       AuditLogEntry              │
│                                  │
│  Id (PK)                         │
│  EntityType (RFQ/Quotation)      │
│  EntityId                        │
│  UserId                          │
│  ActionType                      │
│  Timestamp                       │
│  ChangedFields (JSONB)           │
└──────────────────────────────────┘
```

## Entity Definitions

### 1. Customer

Represents customer information associated with RFQs and quotations, with support for cross-channel customer linking.

**Table Name**: `customers`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique customer identifier |
| Email | VARCHAR(320) | UNIQUE, INDEX | Customer email address (RFC 5321 max length) |
| PhoneNumber | VARCHAR(20) | INDEX | Customer phone number (E.164 format) |
| Name | VARCHAR(200) | NOT NULL, INDEX (Full-text) | Customer full name |
| ContactInfo | JSONB | NULL | Additional contact information (addresses, etc.) |
| MergedFromIds | UUID[] | NULL | Array of customer IDs that were merged into this record |
| MergeHistory | JSONB | NULL | History of customer merge operations with timestamps and staff IDs |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Record creation timestamp |
| UpdatedAt | TIMESTAMPTZ | NOT NULL | Last update timestamp |
| IsDeleted | BOOLEAN | NOT NULL, DEFAULT FALSE | Soft delete flag |
| DeletedAt | TIMESTAMPTZ | NULL | Deletion timestamp (for 7-year retention tracking) |

**Indexes**:
- PRIMARY KEY on `Id`
- UNIQUE INDEX on `Email` (case-insensitive)
- INDEX on `PhoneNumber`
- GIN INDEX on `Name` (for fuzzy matching with trigram similarity)
- INDEX on `IsDeleted` (for filtered queries)

**Validation Rules**:
- Email must match RFC 5322 pattern
- PhoneNumber must match E.164 format (if provided)
- Name length: 1-200 characters
- Email or PhoneNumber must be provided (at least one)

**Entity Framework Configuration**:
```csharp
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Email).HasMaxLength(320).IsRequired(false);
        builder.HasIndex(c => c.Email).IsUnique();

        builder.Property(c => c.PhoneNumber).HasMaxLength(20).IsRequired(false);
        builder.HasIndex(c => c.PhoneNumber);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(c => c.Name);

        builder.Property(c => c.ContactInfo).HasColumnType("jsonb");
        builder.Property(c => c.MergeHistory).HasColumnType("jsonb");

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(c => c.UpdatedAt).IsRequired();
        builder.Property(c => c.IsDeleted).HasDefaultValue(false);

        builder.HasQueryFilter(c => !c.IsDeleted); // Global query filter
    }
}
```

---

### 2. RFQ (Request for Quotation)

Represents a customer request for a quotation from any channel.

**Table Name**: `rfqs`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique RFQ identifier |
| CustomerId | UUID | FOREIGN KEY (customers.Id), NOT NULL, INDEX | Associated customer |
| ChannelSource | INT | NOT NULL, INDEX | Enum: Website=1, LINE=2, WhatsApp=3, FacebookMessenger=4, Instagram=5, Email=6, InStore=7, Unknown=99 |
| Status | INT | NOT NULL, INDEX | Enum: New=1, InProgress=2, Qualified=3, Converted=4, Abandoned=5 |
| RequestDetails | JSONB | NULL | Flexible storage for channel-specific RFQ details |
| AssignedStaffUserId | VARCHAR(50) | NULL, INDEX | Staff user ID assigned to this RFQ |
| ConvertedToQuotationId | UUID | FOREIGN KEY (quotations.Id), NULL | Link to quotation if converted |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW(), INDEX | RFQ creation timestamp |
| UpdatedAt | TIMESTAMPTZ | NOT NULL | Last update timestamp |
| IsDeleted | BOOLEAN | NOT NULL, DEFAULT FALSE | Soft delete flag |
| DeletedAt | TIMESTAMPTZ | NULL | Deletion timestamp |

**Indexes**:
- PRIMARY KEY on `Id`
- INDEX on `CustomerId`
- INDEX on `ChannelSource`
- INDEX on `Status`
- COMPOSITE INDEX on `(ChannelSource, Status, CreatedAt)` for filtered queries
- INDEX on `AssignedStaffUserId`
- INDEX on `CreatedAt` (for time-based queries)
- INDEX on `IsDeleted`

**Validation Rules**:
- ChannelSource must be valid enum value
- Status must be valid enum value
- AssignedStaffUserId must exist in identity system (validated at service layer)

**Entity Framework Configuration**:
```csharp
public class RfqConfiguration : IEntityTypeConfiguration<Rfq>
{
    public void Configure(EntityTypeBuilder<Rfq> builder)
    {
        builder.ToTable("rfqs");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ChannelSource).IsRequired();
        builder.HasIndex(r => r.ChannelSource);

        builder.Property(r => r.Status).IsRequired();
        builder.HasIndex(r => r.Status);

        builder.HasIndex(r => new { r.ChannelSource, r.Status, r.CreatedAt });

        builder.Property(r => r.RequestDetails).HasColumnType("jsonb");

        builder.Property(r => r.AssignedStaffUserId).HasMaxLength(50);
        builder.HasIndex(r => r.AssignedStaffUserId);

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).IsRequired();
        builder.Property(r => r.IsDeleted).HasDefaultValue(false);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Quotation>()
            .WithMany()
            .HasForeignKey(r => r.ConvertedToQuotationId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
```

---

### 3. Quotation

Represents a formal quotation issued to a customer.

**Table Name**: `quotations`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique quotation identifier |
| CustomerId | UUID | FOREIGN KEY (customers.Id), NOT NULL, INDEX | Associated customer |
| SourceRfqId | UUID | FOREIGN KEY (rfqs.Id), NULL, INDEX | Link to source RFQ (if created from RFQ) |
| CurrentVersionId | UUID | FOREIGN KEY (quotation_versions.Id), NULL | Current active version |
| Status | INT | NOT NULL, INDEX | Enum: Draft=1, PendingApproval=2, Approved=3, CustomerReview=4, Accepted=5, Expired=6, Cancelled=7 |
| ValidityPeriodStart | DATE | NOT NULL | Quotation validity start date |
| ValidityPeriodEnd | DATE | NOT NULL, INDEX | Quotation validity end date |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW(), INDEX | Quotation creation timestamp |
| UpdatedAt | TIMESTAMPTZ | NOT NULL | Last update timestamp |
| RowVersion | BYTEA | NOT NULL, CONCURRENCY TOKEN | Optimistic concurrency control (EF Core timestamp) |
| IsDeleted | BOOLEAN | NOT NULL, DEFAULT FALSE | Soft delete flag |
| DeletedAt | TIMESTAMPTZ | NULL | Deletion timestamp |

**Indexes**:
- PRIMARY KEY on `Id`
- INDEX on `CustomerId`
- INDEX on `SourceRfqId`
- INDEX on `CurrentVersionId`
- INDEX on `Status`
- COMPOSITE INDEX on `(Status, ValidityPeriodEnd, CreatedAt)` for expiration checks and status queries
- INDEX on `ValidityPeriodEnd` (for expiration job)
- INDEX on `CreatedAt`
- INDEX on `IsDeleted`

**Validation Rules**:
- ValidityPeriodEnd must be >= ValidityPeriodStart
- ValidityPeriodEnd must be in the future for new quotations
- Status transitions must follow state machine rules

**Entity Framework Configuration**:
```csharp
public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("quotations");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Status).IsRequired();
        builder.HasIndex(q => q.Status);

        builder.Property(q => q.ValidityPeriodStart).IsRequired();
        builder.Property(q => q.ValidityPeriodEnd).IsRequired();
        builder.HasIndex(q => q.ValidityPeriodEnd);

        builder.HasIndex(q => new { q.Status, q.ValidityPeriodEnd, q.CreatedAt });

        builder.Property(q => q.RowVersion).IsRowVersion(); // Optimistic concurrency

        builder.Property(q => q.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(q => q.UpdatedAt).IsRequired();
        builder.Property(q => q.IsDeleted).HasDefaultValue(false);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Rfq>()
            .WithMany()
            .HasForeignKey(q => q.SourceRfqId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne<QuotationVersion>()
            .WithMany()
            .HasForeignKey(q => q.CurrentVersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasQueryFilter(q => !q.IsDeleted);
    }
}
```

---

### 4. QuotationVersion

Represents a specific version of a quotation (immutable once created).

**Table Name**: `quotation_versions`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique version identifier |
| QuotationId | UUID | FOREIGN KEY (quotations.Id), NOT NULL, INDEX | Parent quotation |
| VersionNumber | INT | NOT NULL | Version number (1, 2, 3...) |
| CreatedByUserId | VARCHAR(50) | NOT NULL | User who created this version |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Version creation timestamp |
| ChangeSummary | VARCHAR(500) | NULL | Summary of changes from previous version |
| TotalPrice | DECIMAL(18,2) | NOT NULL | Total quotation price (calculated from line items) |
| CurrencyCode | VARCHAR(3) | NOT NULL | ISO 4217 currency code (e.g., THB, USD) |
| DeliveryExpectations | TEXT | NULL | Delivery timeline and expectations |
| SpecialTerms | TEXT | NULL | Special terms and conditions for this quotation |

**Indexes**:
- PRIMARY KEY on `Id`
- UNIQUE INDEX on `(QuotationId, VersionNumber)` (prevents duplicate version numbers)
- INDEX on `QuotationId`
- INDEX on `CreatedAt`

**Validation Rules**:
- VersionNumber must be >= 1
- TotalPrice must be >= 0
- CurrencyCode must be valid ISO 4217 code
- ChangeSummary max length: 500 characters

**Entity Framework Configuration**:
```csharp
public class QuotationVersionConfiguration : IEntityTypeConfiguration<QuotationVersion>
{
    public void Configure(EntityTypeBuilder<QuotationVersion> builder)
    {
        builder.ToTable("quotation_versions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.VersionNumber).IsRequired();
        builder.HasIndex(v => new { v.QuotationId, v.VersionNumber }).IsUnique();

        builder.Property(v => v.CreatedByUserId).HasMaxLength(50).IsRequired();
        builder.Property(v => v.CreatedAt).HasDefaultValueSql("NOW()");

        builder.Property(v => v.ChangeSummary).HasMaxLength(500);

        builder.Property(v => v.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(v => v.CurrencyCode).HasMaxLength(3).IsRequired();

        builder.HasOne<Quotation>()
            .WithMany()
            .HasForeignKey(v => v.QuotationId)
            .OnDelete(DeleteBehavior.Cascade); // Delete versions when quotation is deleted
    }
}
```

---

### 5. QuotationLineItem

Represents a single line item in a quotation version.

**Table Name**: `quotation_line_items`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique line item identifier |
| VersionId | UUID | FOREIGN KEY (quotation_versions.Id), NOT NULL, INDEX | Parent version |
| LineNumber | INT | NOT NULL | Line number within version (1, 2, 3...) |
| MaterialServiceId | UUID | NOT NULL | Material identifier from Material Service |
| MaterialName | VARCHAR(200) | NOT NULL | Cached material name (for display) |
| MaterialProperties | JSONB | NULL | Cached material properties from Material Service |
| ManufacturingProcess | VARCHAR(100) | NOT NULL | Selected manufacturing process |
| Quantity | DECIMAL(10,2) | NOT NULL | Quantity ordered |
| UnitOfMeasure | VARCHAR(20) | NOT NULL | Unit of measure (e.g., kg, m, pcs) |
| UnitPrice | DECIMAL(18,2) | NOT NULL | Price per unit |
| LineTotal | DECIMAL(18,2) | NOT NULL | Total for this line (Quantity × UnitPrice) |
| Notes | VARCHAR(500) | NULL | Item-specific notes or specifications |

**Indexes**:
- PRIMARY KEY on `Id`
- UNIQUE INDEX on `(VersionId, LineNumber)` (prevents duplicate line numbers)
- INDEX on `VersionId`
- INDEX on `MaterialServiceId` (for material-based queries)

**Validation Rules**:
- LineNumber must be >= 1
- Quantity must be > 0
- UnitPrice must be >= 0
- LineTotal must equal Quantity × UnitPrice (calculated field)
- MaterialName max length: 200 characters
- Notes max length: 500 characters

**Entity Framework Configuration**:
```csharp
public class QuotationLineItemConfiguration : IEntityTypeConfiguration<QuotationLineItem>
{
    public void Configure(EntityTypeBuilder<QuotationLineItem> builder)
    {
        builder.ToTable("quotation_line_items");
        builder.HasKey(li => li.Id);

        builder.Property(li => li.LineNumber).IsRequired();
        builder.HasIndex(li => new { li.VersionId, li.LineNumber }).IsUnique();

        builder.Property(li => li.MaterialServiceId).IsRequired();
        builder.HasIndex(li => li.MaterialServiceId);

        builder.Property(li => li.MaterialName).HasMaxLength(200).IsRequired();
        builder.Property(li => li.MaterialProperties).HasColumnType("jsonb");

        builder.Property(li => li.ManufacturingProcess).HasMaxLength(100).IsRequired();

        builder.Property(li => li.Quantity).HasColumnType("decimal(10,2)").IsRequired();
        builder.Property(li => li.UnitOfMeasure).HasMaxLength(20).IsRequired();
        builder.Property(li => li.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(li => li.LineTotal).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(li => li.Notes).HasMaxLength(500);

        builder.HasOne<QuotationVersion>()
            .WithMany()
            .HasForeignKey(li => li.VersionId)
            .OnDelete(DeleteBehavior.Cascade); // Delete line items when version is deleted
    }
}
```

---

### 6. DiscountStructure

Represents pricing discounts applied to a quotation version.

**Table Name**: `discount_structures`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique discount identifier |
| QuotationVersionId | UUID | FOREIGN KEY (quotation_versions.Id), NOT NULL, INDEX | Associated version |
| DiscountType | INT | NOT NULL | Enum: Percentage=1, FixedAmount=2, VolumeBased=3 |
| DiscountValue | DECIMAL(18,2) | NOT NULL | Discount value (percentage or amount depending on type) |
| Conditions | VARCHAR(500) | NULL | Conditions for discount application |
| AuthorizationReason | VARCHAR(500) | NULL | Reason or authorization for discount |
| AuthorizedByUserId | VARCHAR(50) | NULL | User who authorized this discount |
| AppliedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | When discount was applied |

**Indexes**:
- PRIMARY KEY on `Id`
- INDEX on `QuotationVersionId`
- INDEX on `DiscountType`

**Validation Rules**:
- DiscountType must be valid enum value
- DiscountValue must be > 0
- For Percentage type, DiscountValue must be <= 100
- Conditions max length: 500 characters
- AuthorizationReason max length: 500 characters

**Entity Framework Configuration**:
```csharp
public class DiscountStructureConfiguration : IEntityTypeConfiguration<DiscountStructure>
{
    public void Configure(EntityTypeBuilder<DiscountStructure> builder)
    {
        builder.ToTable("discount_structures");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DiscountType).IsRequired();
        builder.HasIndex(d => d.DiscountType);

        builder.Property(d => d.DiscountValue).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(d => d.Conditions).HasMaxLength(500);
        builder.Property(d => d.AuthorizationReason).HasMaxLength(500);
        builder.Property(d => d.AuthorizedByUserId).HasMaxLength(50);

        builder.Property(d => d.AppliedAt).HasDefaultValueSql("NOW()");

        builder.HasOne<QuotationVersion>()
            .WithMany()
            .HasForeignKey(d => d.QuotationVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

### 7. InternalNote

Represents staff commentary on an RFQ or quotation.

**Table Name**: `internal_notes`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique note identifier |
| RfqId | UUID | FOREIGN KEY (rfqs.Id), NULL, INDEX | Associated RFQ (mutually exclusive with QuotationId) |
| QuotationId | UUID | FOREIGN KEY (quotations.Id), NULL, INDEX | Associated Quotation (mutually exclusive with RfqId) |
| AuthorUserId | VARCHAR(50) | NOT NULL | User who created the note |
| Content | TEXT | NOT NULL | Note content |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW(), INDEX | Note creation timestamp |
| IsDeleted | BOOLEAN | NOT NULL, DEFAULT FALSE | Soft delete flag |
| DeletedAt | TIMESTAMPTZ | NULL | Deletion timestamp |

**Indexes**:
- PRIMARY KEY on `Id`
- INDEX on `RfqId`
- INDEX on `QuotationId`
- INDEX on `CreatedAt`
- INDEX on `IsDeleted`

**Validation Rules**:
- Exactly one of RfqId or QuotationId must be non-null (CHECK constraint)
- Content must not be empty (min length: 1)
- AuthorUserId must exist in identity system (validated at service layer)

**Entity Framework Configuration**:
```csharp
public class InternalNoteConfiguration : IEntityTypeConfiguration<InternalNote>
{
    public void Configure(EntityTypeBuilder<InternalNote> builder)
    {
        builder.ToTable("internal_notes");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.AuthorUserId).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Content).HasColumnType("text").IsRequired();

        builder.Property(n => n.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(n => n.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(n => n.RfqId);
        builder.HasIndex(n => n.QuotationId);
        builder.HasIndex(n => n.CreatedAt);

        // CHECK constraint: exactly one of RfqId or QuotationId must be non-null
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_InternalNote_OneAssociation",
            "(rfq_id IS NOT NULL AND quotation_id IS NULL) OR (rfq_id IS NULL AND quotation_id IS NOT NULL)"
        ));

        builder.HasOne<Rfq>()
            .WithMany()
            .HasForeignKey(n => n.RfqId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne<Quotation>()
            .WithMany()
            .HasForeignKey(n => n.QuotationId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasQueryFilter(n => !n.IsDeleted);
    }
}
```

---

### 8. FileReference

Represents a file uploaded by customers or staff, stored in the Upload Service.

**Table Name**: `file_references`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique file reference identifier |
| RfqId | UUID | FOREIGN KEY (rfqs.Id), NULL, INDEX | Associated RFQ (mutually exclusive with QuotationId) |
| QuotationId | UUID | FOREIGN KEY (quotations.Id), NULL, INDEX | Associated Quotation (mutually exclusive with RfqId) |
| UploadServiceFileId | UUID | NOT NULL, UNIQUE | File identifier from Upload Service |
| FileName | VARCHAR(255) | NOT NULL | Original file name |
| FileType | VARCHAR(100) | NOT NULL | MIME type (e.g., application/pdf, image/jpeg) |
| UploadedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | File upload timestamp |
| UploadedByUserId | VARCHAR(50) | NOT NULL | User who uploaded the file |

**Indexes**:
- PRIMARY KEY on `Id`
- UNIQUE INDEX on `UploadServiceFileId` (prevents duplicate file references)
- INDEX on `RfqId`
- INDEX on `QuotationId`
- INDEX on `UploadedAt`

**Validation Rules**:
- Exactly one of RfqId or QuotationId must be non-null (CHECK constraint)
- FileName max length: 255 characters
- FileType must be valid MIME type
- UploadServiceFileId must exist in Upload Service (validated at service layer)

**Entity Framework Configuration**:
```csharp
public class FileReferenceConfiguration : IEntityTypeConfiguration<FileReference>
{
    public void Configure(EntityTypeBuilder<FileReference> builder)
    {
        builder.ToTable("file_references");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.UploadServiceFileId).IsRequired();
        builder.HasIndex(f => f.UploadServiceFileId).IsUnique();

        builder.Property(f => f.FileName).HasMaxLength(255).IsRequired();
        builder.Property(f => f.FileType).HasMaxLength(100).IsRequired();

        builder.Property(f => f.UploadedAt).HasDefaultValueSql("NOW()");
        builder.Property(f => f.UploadedByUserId).HasMaxLength(50).IsRequired();

        builder.HasIndex(f => f.RfqId);
        builder.HasIndex(f => f.QuotationId);

        // CHECK constraint: exactly one of RfqId or QuotationId must be non-null
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_FileReference_OneAssociation",
            "(rfq_id IS NOT NULL AND quotation_id IS NULL) OR (rfq_id IS NULL AND quotation_id IS NOT NULL)"
        ));

        builder.HasOne<Rfq>()
            .WithMany()
            .HasForeignKey(f => f.RfqId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne<Quotation>()
            .WithMany()
            .HasForeignKey(f => f.QuotationId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}
```

---

### 9. MaterialReference

Represents cached material data obtained from the Material Service.

**Table Name**: `material_references`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique material reference identifier |
| MaterialServiceId | UUID | NOT NULL, UNIQUE | Material identifier from Material Service |
| MaterialName | VARCHAR(200) | NOT NULL | Material name |
| MechanicalProperties | JSONB | NULL | Cached mechanical properties (tensile strength, hardness, etc.) |
| SupportedProcesses | TEXT[] | NULL | Array of supported manufacturing processes |
| AvailabilityStatus | INT | NOT NULL | Enum: Available=1, LimitedStock=2, Discontinued=3 |
| CachedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | When this data was cached |
| ExpiresAt | TIMESTAMPTZ | NOT NULL | Cache expiration timestamp (CachedAt + 1 hour) |

**Indexes**:
- PRIMARY KEY on `Id`
- UNIQUE INDEX on `MaterialServiceId` (prevents duplicate material references)
- INDEX on `ExpiresAt` (for cache cleanup job)
- INDEX on `AvailabilityStatus`

**Validation Rules**:
- MaterialName max length: 200 characters
- AvailabilityStatus must be valid enum value
- ExpiresAt must be > CachedAt

**Entity Framework Configuration**:
```csharp
public class MaterialReferenceConfiguration : IEntityTypeConfiguration<MaterialReference>
{
    public void Configure(EntityTypeBuilder<MaterialReference> builder)
    {
        builder.ToTable("material_references");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MaterialServiceId).IsRequired();
        builder.HasIndex(m => m.MaterialServiceId).IsUnique();

        builder.Property(m => m.MaterialName).HasMaxLength(200).IsRequired();

        builder.Property(m => m.MechanicalProperties).HasColumnType("jsonb");
        builder.Property(m => m.SupportedProcesses).HasColumnType("text[]");

        builder.Property(m => m.AvailabilityStatus).IsRequired();
        builder.HasIndex(m => m.AvailabilityStatus);

        builder.Property(m => m.CachedAt).HasDefaultValueSql("NOW()");
        builder.Property(m => m.ExpiresAt).IsRequired();
        builder.HasIndex(m => m.ExpiresAt);
    }
}
```

---

### 10. AuditLogEntry

Represents a recorded change to an RFQ or quotation.

**Table Name**: `audit_log_entries`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique audit log entry identifier |
| EntityType | INT | NOT NULL, INDEX | Enum: RFQ=1, Quotation=2, QuotationVersion=3 |
| EntityId | UUID | NOT NULL, INDEX | Identifier of the changed entity |
| UserId | VARCHAR(50) | NOT NULL, INDEX | User who performed the action |
| ActionType | INT | NOT NULL, INDEX | Enum: Create=1, Update=2, StatusChange=3, NoteAdded=4, Assignment=5, Delete=6 |
| Timestamp | TIMESTAMPTZ | NOT NULL, DEFAULT NOW(), INDEX | Action timestamp |
| ChangedFields | JSONB | NULL | Before/after values of changed fields |
| IpAddress | VARCHAR(45) | NULL | IP address of user (IPv6 max length: 45) |
| UserAgent | VARCHAR(500) | NULL | Browser/client user agent |

**Indexes**:
- PRIMARY KEY on `Id`
- INDEX on `EntityType`
- INDEX on `EntityId`
- COMPOSITE INDEX on `(EntityType, EntityId, Timestamp)` for entity audit trail queries
- INDEX on `UserId`
- INDEX on `ActionType`
- INDEX on `Timestamp` (for time-based queries)

**Validation Rules**:
- EntityType must be valid enum value
- ActionType must be valid enum value
- UserId max length: 50 characters
- IpAddress must be valid IPv4 or IPv6 address
- UserAgent max length: 500 characters

**Entity Framework Configuration**:
```csharp
public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType).IsRequired();
        builder.HasIndex(a => a.EntityType);

        builder.Property(a => a.EntityId).IsRequired();
        builder.HasIndex(a => a.EntityId);

        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.Timestamp });

        builder.Property(a => a.UserId).HasMaxLength(50).IsRequired();
        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.ActionType).IsRequired();
        builder.HasIndex(a => a.ActionType);

        builder.Property(a => a.Timestamp).HasDefaultValueSql("NOW()");
        builder.HasIndex(a => a.Timestamp);

        builder.Property(a => a.ChangedFields).HasColumnType("jsonb");

        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasMaxLength(500);
    }
}
```

---

### 11. StaffRole

Represents a user role with specific permissions for system operations.

**Table Name**: `staff_roles`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique role identifier |
| RoleName | VARCHAR(50) | NOT NULL, UNIQUE | Role name (e.g., Sales Staff, Manager, Analyst, Administrator) |
| Permissions | TEXT[] | NOT NULL | Array of permission strings (e.g., "rfq:create", "quotation:approve", "analytics:view") |
| Description | VARCHAR(500) | NULL | Role description |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Role creation timestamp |
| UpdatedAt | TIMESTAMPTZ | NOT NULL | Last update timestamp |

**Indexes**:
- PRIMARY KEY on `Id`
- UNIQUE INDEX on `RoleName` (case-insensitive)

**Validation Rules**:
- RoleName max length: 50 characters, required
- Permissions array must not be empty
- Description max length: 500 characters

**Entity Framework Configuration**:
```csharp
public class StaffRoleConfiguration : IEntityTypeConfiguration<StaffRole>
{
    public void Configure(EntityTypeBuilder<StaffRole> builder)
    {
        builder.ToTable("staff_roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoleName).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.RoleName).IsUnique();

        builder.Property(r => r.Permissions).HasColumnType("text[]").IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).IsRequired();
    }
}
```

---

## Enumerations

### RfqChannel
```csharp
public enum RfqChannel
{
    Website = 1,
    LINE = 2,
    WhatsApp = 3,
    FacebookMessenger = 4,
    Instagram = 5,
    Email = 6,
    InStore = 7,
    Unknown = 99
}
```

### RfqStatus
```csharp
public enum RfqStatus
{
    New = 1,
    InProgress = 2,
    Qualified = 3,
    Converted = 4,
    Abandoned = 5
}
```

### QuotationStatus
```csharp
public enum QuotationStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    CustomerReview = 4,
    Accepted = 5,
    Expired = 6,
    Cancelled = 7
}
```

### DiscountType
```csharp
public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2,
    VolumeBased = 3
}
```

### MaterialAvailabilityStatus
```csharp
public enum MaterialAvailabilityStatus
{
    Available = 1,
    LimitedStock = 2,
    Discontinued = 3
}
```

### AuditEntityType
```csharp
public enum AuditEntityType
{
    RFQ = 1,
    Quotation = 2,
    QuotationVersion = 3
}
```

### AuditActionType
```csharp
public enum AuditActionType
{
    Create = 1,
    Update = 2,
    StatusChange = 3,
    NoteAdded = 4,
    Assignment = 5,
    Delete = 6
}
```

## Database Migrations

### Initial Migration Commands

```bash
# Create initial migration
dotnet ef migrations add InitialCreate --project Maliev.QuotationService.Data --startup-project Maliev.QuotationService.Api

# Update database (local development only)
dotnet ef database update --project Maliev.QuotationService.Data --startup-project Maliev.QuotationService.Api
```

**IMPORTANT**: Migrations are NOT auto-applied on startup in production. Manual migration required via deployment pipeline.

### Migration Strategy

1. **Development**: Apply migrations locally via `dotnet ef database update`
2. **Testing**: Testcontainers applies migrations automatically via `dbContext.Database.Migrate()` in IntegrationTestWebAppFactory
3. **Production**: Migrations applied via Kubernetes InitContainer or manual deployment script

## Constraints Summary

### Foreign Key Constraints
- Customer → RFQ (restrict delete if RFQs exist)
- Customer → Quotation (restrict delete if quotations exist)
- RFQ → Quotation (set null on RFQ delete if converted)
- Quotation → QuotationVersion (restrict delete of current version)
- QuotationVersion → QuotationLineItem (cascade delete line items)
- QuotationVersion → DiscountStructure (cascade delete discounts)
- RFQ/Quotation → InternalNote (cascade delete notes)
- RFQ/Quotation → FileReference (cascade delete file references)

### Check Constraints
- InternalNote: Exactly one of RfqId or QuotationId must be non-null
- FileReference: Exactly one of RfqId or QuotationId must be non-null
- QuotationVersion.VersionNumber >= 1
- QuotationLineItem.Quantity > 0
- QuotationLineItem.UnitPrice >= 0
- DiscountStructure.DiscountValue > 0
- MaterialReference.ExpiresAt > CachedAt

### Unique Constraints
- Customer.Email (case-insensitive)
- QuotationVersion (QuotationId, VersionNumber)
- QuotationLineItem (VersionId, LineNumber)
- MaterialReference.MaterialServiceId
- FileReference.UploadServiceFileId
- StaffRole.RoleName (case-insensitive)

## Performance Considerations

### Indexing Strategy

**High-Priority Indexes** (frequent queries):
- Customers: Email, PhoneNumber, Name (full-text)
- RFQs: (ChannelSource, Status, CreatedAt), CustomerId, AssignedStaffUserId
- Quotations: (Status, ValidityPeriodEnd, CreatedAt), CustomerId
- AuditLogEntries: (EntityType, EntityId, Timestamp), UserId

**Cache-Friendly Queries**:
- Material data cached for 1 hour (reduce Material Service calls)
- Customer match suggestions cached for 5 minutes (session-based)

**Pagination**:
- Default page size: 20
- Max page size: 100
- Use offset/limit for simple queries
- Use keyset pagination for high-volume queries (cursor-based on CreatedAt)

### Query Optimization

- Use `AsNoTracking()` for read-only queries
- Eager load related entities with `.Include()` to avoid N+1
- Project to DTOs in database query to reduce data transfer
- Use compiled queries for frequently executed queries

## Data Retention & Archival

### Soft Delete Strategy

All user-facing entities (Customer, RFQ, Quotation, InternalNote, FileReference) support soft delete:
- `IsDeleted` flag (default: false)
- `DeletedAt` timestamp
- Global query filter excludes soft-deleted records by default
- Analytics queries can opt-in to include deleted records

### 7-Year Retention Policy

- All RFQ, Quotation, and AuditLogEntry data retained for 7 years from creation date (FR-031)
- Background job (external to this service) purges data where `CreatedAt < NOW() - INTERVAL '7 years'`
- Deleted records remain accessible for 7 years, then permanently purged
- Material references cached for 1 hour, then eligible for cleanup (not subject to 7-year retention)

## Conclusion

This data model provides a comprehensive foundation for the Quotation Service, supporting all functional requirements while maintaining flexibility for future enhancements. The model leverages PostgreSQL-specific features (JSONB, full-text search, array types) for optimal performance and developer experience. Entity Framework Core configurations ensure proper mapping, constraints, and indexes for production deployment.

**Next Phase**: Proceed to API Contract Design (Phase 1 continuation)
