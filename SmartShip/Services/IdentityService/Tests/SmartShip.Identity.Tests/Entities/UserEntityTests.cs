using SmartShip.Identity.Domain.Entities;
using Xunit;

namespace SmartShip.Identity.Tests.Entities;

public class UserEntityTests
{
    [Fact] // tells that following method is a test that should be executed.
    public void User_Create_WithValidData_ShouldInitializeCorrectly()
    {   
        // AAA pattern
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        var phone = "9876543210";
        var role = "CUSTOMER";

        // Act
        var user = new User // object initializer syntax
        {
            Name = name,
            Email = email,
            Phone = phone,
            PasswordHash = "hashed_password_123",
            Role = role,
            IsActive = true
        };

        // Assert
        Assert.Equal(name, user.Name);
        Assert.Equal(email, user.Email);
        Assert.Equal(phone, user.Phone);
        Assert.Equal(role, user.Role);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void User_DefaultRole_ShouldBeCustomer()
    {
        var user = new User();

        Assert.Equal("CUSTOMER", user.Role);
    }

    [Fact]
    public void User_DefaultIsActive_ShouldBeTrue()
    {
        var user = new User();

        Assert.True(user.IsActive);
    }

    [Fact]
    public void User_SetAsInactive_ShouldUpdateActiveStatus()
    {
        var user = new User { IsActive = true };

        user.IsActive = false;

        Assert.False(user.IsActive);
    }

    [Theory]
    [InlineData("CUSTOMER")]
    [InlineData("ADMIN")]
    [InlineData("VENDOR")]
    public void User_WithDifferentRoles_ShouldBeAccepted(string role)
    {
        var user = new User { Role = role };

        Assert.Equal(role, user.Role);
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.co.uk")]
    [InlineData("admin+tag@company.com")]
    public void User_WithValidEmails_ShouldAcceptAll(string email)
    {
        var user = new User { Email = email };

        Assert.Equal(email, user.Email);
        Assert.Contains("@", user.Email);
    }

    [Theory]
    [InlineData("9876543210")]
    [InlineData("+1-987-654-3210")]
    [InlineData("(987)-654-3210")]
    public void User_WithValidPhoneNumbers_ShouldBeAccepted(string phone)
    {
        var user = new User { Phone = phone };

        Assert.Equal(phone, user.Phone);
        Assert.NotEmpty(user.Phone);
    }

    [Fact]
    public void User_CreatedAt_ShouldBeSet()
    {
        var beforeCreation = DateTime.Now;

        var user = new User();
        var afterCreation = DateTime.Now;

        Assert.NotEqual(default(DateTime), user.CreatedAt);
        Assert.True(user.CreatedAt >= beforeCreation && user.CreatedAt <= afterCreation);
    }

    [Fact]
    public void User_PasswordHash_ShouldBeStored()
    {
        var passwordHash = "bcrypt_hashed_value_here";

        var user = new User { PasswordHash = passwordHash };

        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.NotEmpty(user.PasswordHash);
    }

    [Fact]
    public void User_Multiple_ShouldBeDifferentInstances()
    {
        var user1 = new User { Id = 1, Email = "user1@example.com" };
        var user2 = new User { Id = 2, Email = "user2@example.com" };

        Assert.NotEqual(user1.Id, user2.Id);
        Assert.NotEqual(user1.Email, user2.Email);
        Assert.NotSame(user1, user2);
    }
}
