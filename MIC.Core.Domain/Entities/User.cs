using Ardalis.GuardClauses;
using System;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

// NEW: user roles for authorization and seeding
public enum UserRole
{
	Admin = 0,
	User = 1,
	Guest = 2
}

// NEW: Supported languages for multilingual support
public enum UserLanguage
{
	English = 0,
	French = 1,
	Spanish = 2,
	Arabic = 3,
	Chinese = 4
}

/// <summary>
/// Represents an application user account.
/// </summary>
public sealed class User : BaseEntity
{
	public string Username { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string PasswordHash { get; set; } = string.Empty;

	// REQUIRED for Argon2 hashing
	public string Salt { get; set; } = string.Empty;

	// Full display name for UI
	public string? FullName { get; set; }

	// Role for basic authorization
	public UserRole Role { get; set; } = UserRole.User;

	public bool IsActive { get; set; } = true;

	public DateTimeOffset? LastLoginAt { get; set; }

	public new DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset UpdatedAt { get; set; }
	
	// Job position for organizational intelligence
	public string? JobPosition { get; set; }
	
	// Department for organizational intelligence
	public string? Department { get; set; }
	
	// Seniority level for organizational intelligence
	public string? SeniorityLevel { get; set; }
	
	// Language preference for multilingual support
	public UserLanguage Language { get; set; } = UserLanguage.English;

	public User() => UpdatedAt = CreatedAt;

	public void SetCredentials(string username, string email)
	{
		var sanitizedUsername = Guard.Against.NullOrWhiteSpace(username).Trim();
		var sanitizedEmail = Guard.Against.NullOrWhiteSpace(email).Trim();

		Username = sanitizedUsername;
		Email = sanitizedEmail;
		Touch();
	}

	public void SetPasswordHash(string passwordHash, string salt)
	{
		PasswordHash = Guard.Against.NullOrWhiteSpace(passwordHash);
		Salt = Guard.Against.NullOrWhiteSpace(salt);
		Touch();
	}

	public void SetRole(UserRole role)
	{
		Role = role;
		Touch();
	}

	public void SetLanguage(UserLanguage language)
	{
		Language = language;
		Touch();
	}

	public void UpdateProfile(string? fullName = null, string? jobPosition = null, string? department = null, string? seniorityLevel = null)
	{
		FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
		JobPosition = string.IsNullOrWhiteSpace(jobPosition) ? null : jobPosition.Trim();
		Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
		SeniorityLevel = string.IsNullOrWhiteSpace(seniorityLevel) ? null : seniorityLevel.Trim();
		Touch();
	}

	public void RecordLogin(DateTimeOffset loginTime)
	{
		LastLoginAt = loginTime;
		UpdatedAt = loginTime;
	}

	public void Deactivate()
	{
		IsActive = false;
		Touch();
	}

	public void Activate()
	{
		IsActive = true;
		Touch();
	}

	private void Touch()
	{
		UpdatedAt = DateTimeOffset.UtcNow;
	}
}
