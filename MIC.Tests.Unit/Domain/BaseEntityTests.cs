using System;
using FluentAssertions;
using MIC.Core.Domain.Abstractions;
using Xunit;

namespace MIC.Tests.Unit.Domain;

/// <summary>
/// Tests for BaseEntity covering identity, domain events, soft delete, and modification tracking.
/// Uses a concrete test stub since BaseEntity is abstract.
/// </summary>
public class BaseEntityTests
{
    private class TestEntity : BaseEntity
    {
        public void RaiseDomainEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private record TestDomainEvent : IDomainEvent;

    #region Default State

    [Fact]
    public void NewEntity_HasNonEmptyId()
    {
        var entity = new TestEntity();
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void NewEntity_HasCreatedAtSet()
    {
        var before = DateTime.UtcNow;
        var entity = new TestEntity();
        entity.CreatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void NewEntity_IsNotDeleted()
    {
        var entity = new TestEntity();
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void NewEntity_HasNullModifiedAt()
    {
        var entity = new TestEntity();
        entity.ModifiedAt.Should().BeNull();
    }

    [Fact]
    public void NewEntity_HasEmptyDomainEvents()
    {
        var entity = new TestEntity();
        entity.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Domain Events

    [Fact]
    public void AddDomainEvent_AddsEventToCollection()
    {
        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();

        entity.RaiseDomainEvent(domainEvent);

        entity.DomainEvents.Should().HaveCount(1);
        entity.DomainEvents.Should().Contain(domainEvent);
    }

    [Fact]
    public void AddDomainEvent_ThrowsOnNull()
    {
        var entity = new TestEntity();
        var act = () => entity.RaiseDomainEvent(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var entity = new TestEntity();
        entity.RaiseDomainEvent(new TestDomainEvent());
        entity.RaiseDomainEvent(new TestDomainEvent());

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region MarkAsModified

    [Fact]
    public void MarkAsModified_SetsTimestamp()
    {
        var entity = new TestEntity();
        var before = DateTime.UtcNow;

        entity.MarkAsModified("admin");

        entity.ModifiedAt.Should().NotBeNull();
        entity.ModifiedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void MarkAsModified_SetsModifiedBy()
    {
        var entity = new TestEntity();

        entity.MarkAsModified("admin");

        entity.LastModifiedBy.Should().Be("admin");
    }

    [Fact]
    public void MarkAsModified_NullModifiedBy_SetsNull()
    {
        var entity = new TestEntity();

        entity.MarkAsModified(null);

        entity.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public void MarkAsModified_WhitespaceModifiedBy_SetsNull()
    {
        var entity = new TestEntity();

        entity.MarkAsModified("   ");

        entity.LastModifiedBy.Should().BeNull();
    }

    #endregion

    #region MarkAsDeleted

    [Fact]
    public void MarkAsDeleted_SetsIsDeletedTrue()
    {
        var entity = new TestEntity();

        entity.MarkAsDeleted("admin");

        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void MarkAsDeleted_SetsModifiedBy()
    {
        var entity = new TestEntity();

        entity.MarkAsDeleted("admin");

        entity.LastModifiedBy.Should().Be("admin");
        entity.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsDeleted_WithNullDeletedBy()
    {
        var entity = new TestEntity();

        entity.MarkAsDeleted();

        entity.IsDeleted.Should().BeTrue();
        entity.LastModifiedBy.Should().BeNull();
    }

    #endregion

    #region Restore

    [Fact]
    public void Restore_SetsIsDeletedFalse()
    {
        var entity = new TestEntity();
        entity.MarkAsDeleted("admin");

        entity.Restore("admin");

        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_SetsModifiedBy()
    {
        var entity = new TestEntity();
        entity.MarkAsDeleted("admin");

        entity.Restore("restorer");

        entity.LastModifiedBy.Should().Be("restorer");
        entity.ModifiedAt.Should().NotBeNull();
    }

    #endregion

    #region SetModifiedNow

    [Fact]
    public void SetModifiedNow_SetsTimestamp()
    {
        var entity = new TestEntity();
        var before = DateTime.UtcNow;

        entity.SetModifiedNow();

        entity.ModifiedAt.Should().NotBeNull();
        entity.ModifiedAt!.Value.Should().BeOnOrAfter(before);
    }

    #endregion
}
