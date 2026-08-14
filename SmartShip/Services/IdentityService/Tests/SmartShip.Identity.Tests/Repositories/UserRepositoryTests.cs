using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartShip.Identity.Domain.Entities;
using SmartShip.Identity.Infrastructure.Data;
using SmartShip.Identity.Infrastructure.Repositories;
using Xunit;

namespace SmartShip.Identity.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IdentityDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new IdentityDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = "hashed_password",
            Role = "CUSTOMER",
            IsActive = true
        };

        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == "john@example.com");

        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("CUSTOMER", result.Role);
    }

    [Fact]
    public async Task GetByEmailAsync_UserExists_ShouldReturnUser()
    {
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = "hashed_password",
            Role = "CUSTOMER"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByEmailAsync("john@example.com");

        Assert.NotNull(result);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_UserDoesNotExist_ShouldReturnNull()
    {
        var result = await _repository.GetByEmailAsync("missing@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_UserExists_ShouldReturnUser()
    {
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = "hashed_password",
            Role = "CUSTOMER"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_UserDoesNotExist_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsByEmailAsync_UserExists_ShouldReturnTrue()
    {
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = "hashed_password"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsByEmailAsync("john@example.com");

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByEmailAsync_UserDoesNotExist_ShouldReturnFalse()
    {
        var result = await _repository.ExistsByEmailAsync("missing@example.com");

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsActiveByIdAsync_ActiveUser_ShouldReturnTrue()
    {
        var user = new User
        {
            Name = "Active User",
            Email = "active@example.com",
            Phone = "9876543210",
            PasswordHash = "hashed_password",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsActiveByIdAsync(user.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsActiveByIdAsync_InactiveUser_ShouldReturnFalse()
    {
        var user = new User
        {
            Name = "Inactive User",
            Email = "inactive@example.com",
            Phone = "9876543210",
            PasswordHash = "hashed_password",
            IsActive = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsActiveByIdAsync(user.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task Update_ShouldUpdateUser()
    {
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = "hashed_password",
            Role = "CUSTOMER",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        user.Name = "Jane Doe";
        user.Role = "ADMIN";

        _repository.Update(user);
        await _context.SaveChangesAsync();

        var result = await _context.Users
            .AsNoTracking()
            .FirstAsync(x => x.Id == user.Id);

        Assert.Equal("Jane Doe", result.Name);
        Assert.Equal("ADMIN", result.Role);
    }

    [Fact]
    public async Task Delete_ShouldRemoveUser()
    {
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = "hashed_password",
            Role = "CUSTOMER"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _repository.Delete(user);
        await _context.SaveChangesAsync();

        var result = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == user.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailForAdminAsync_UserExists_ShouldReturnUser()
    {
        var user = new User
        {
            Name = "Super Admin",
            Email = "admin@smartship.com",
            Phone = "9999999999",
            PasswordHash = "hashed_password",
            Role = "ADMIN",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result =
            await _repository.GetByEmailForAdminAsync("admin@smartship.com");

        Assert.NotNull(result);
        Assert.Equal("ADMIN", result.Role);
        Assert.Equal("admin@smartship.com", result.Email);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}