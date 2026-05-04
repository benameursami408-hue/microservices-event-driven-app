using AuthService.Application.DTOs;
using AuthService.Application.Exceptions;
using AuthService.Application.Interfaces;
using AuthService.Application.Outbox;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using SharedEvents.Events;

namespace AuthService.Tests.Services;

public class AdminUsersServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesEmail_HashesPassword_AndPublishesUserCreatedEvent()
    {
        var repository = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var outbox = new FakeOutboxWriter();
        var service = new AdminUsersService(repository, hasher, outbox);

        var created = await service.CreateAsync(new CreateUserDto
        {
            FirstName = "  Salma ",
            LastName = "  Trabelsi ",
            Email = "  SALMA@EXAMPLE.COM ",
            PhoneNumber = "+216 22 000 000",
            Address = " Tunis ",
            Password = " SavTest!123 ",
            Role = UserRole.SAV,
            IsActive = true
        });

        Assert.Equal("salma@example.com", created.Email);
        Assert.Equal("hashed:SavTest!123", repository.Users.Single().Password);
        Assert.Single(outbox.Events);
        var evt = Assert.IsType<UserCreatedEvent>(outbox.Events.Single());
        Assert.Equal(created.Id, evt.UserId);
        Assert.Equal("SAV", evt.Role);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateEmail_CaseInsensitive()
    {
        var repository = new FakeUserRepository(new User
        {
            Id = 10,
            FirstName = "Existing",
            LastName = "Admin",
            Email = "admin@example.com",
            PhoneNumber = "+216 22 000 000",
            Address = "Tunis",
            Password = "hashed",
            Role = UserRole.ADMIN,
            IsActive = true
        });
        var service = new AdminUsersService(repository, new FakePasswordHasher(), new FakeOutboxWriter());

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreateUserDto
        {
            FirstName = "New",
            LastName = "Admin",
            Email = "ADMIN@example.com",
            PhoneNumber = "+216 22 111 111",
            Address = "Tunis",
            Password = "Admin123!",
            Role = UserRole.ADMIN,
            IsActive = true
        }));
    }

    [Fact]
    public async Task UpdateAsync_KeepsExistingPassword_WhenPasswordIsBlank()
    {
        var existing = new User
        {
            Id = 5,
            FirstName = "Old",
            LastName = "Name",
            Email = "old@example.com",
            PhoneNumber = "+216 22 000 000",
            Address = "Old address",
            Password = "original-hash",
            Role = UserRole.SAV,
            IsActive = true
        };
        var repository = new FakeUserRepository(existing);
        var service = new AdminUsersService(repository, new FakePasswordHasher(), new FakeOutboxWriter());

        var updated = await service.UpdateAsync(5, new UpdateUserDto
        {
            FirstName = "New",
            LastName = "Name",
            Email = " NEW@EXAMPLE.COM ",
            PhoneNumber = "+216 22 222 222",
            Address = "New address",
            Password = "   ",
            Role = UserRole.ST,
            IsActive = false
        });

        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal("original-hash", repository.Users.Single().Password);
        Assert.Equal(UserRole.ST, updated.Role);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public void Delete_RejectsDeletingCurrentLoggedInAdmin()
    {
        var repository = new FakeUserRepository(new User
        {
            Id = 99,
            FirstName = "Root",
            LastName = "Admin",
            Email = "root@example.com",
            PhoneNumber = "+216 22 000 000",
            Address = "Tunis",
            Password = "hashed",
            Role = UserRole.ADMIN,
            IsActive = true
        });
        var service = new AdminUsersService(repository, new FakePasswordHasher(), new FakeOutboxWriter());

        Assert.Throws<BadRequestException>(() => service.Delete(99, currentUserId: 99));
        Assert.Single(repository.Users);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string passwordHash, string inputPassword) => passwordHash == Hash(inputPassword);
    }

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public List<IIntegrationEvent> Events { get; } = new();

        public Task EnqueueAsync(IIntegrationEvent evt, CancellationToken cancellationToken = default)
        {
            Events.Add(evt);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; }
        private long _nextId = 1;

        public FakeUserRepository(params User[] users)
        {
            Users = users.ToList();
            if (Users.Count > 0) _nextId = Users.Max(x => x.Id) + 1;
        }

        public List<User> GetAll() => Users.ToList();
        public IQueryable<User> Query() => Users.AsQueryable();
        public User? GetById(long id) => Users.FirstOrDefault(x => x.Id == id);
        public User? GetByEmail(string email) => FindByEmail(email);
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult(FindByEmail(email));
        public User? GetByPhoneNumber(string phoneNumber) => Users.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
        public User Create(User user) => Add(user);
        public Task<User> AddAsync(User user) => Task.FromResult(Add(user));

        public User Update(User user)
        {
            var index = Users.FindIndex(x => x.Id == user.Id);
            if (index >= 0) Users[index] = user;
            return user;
        }

        public void Delete(long id) => Users.RemoveAll(x => x.Id == id);

        private User Add(User user)
        {
            user.Id = _nextId++;
            Users.Add(user);
            return user;
        }

        private User? FindByEmail(string email)
            => Users.FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));
    }
}
