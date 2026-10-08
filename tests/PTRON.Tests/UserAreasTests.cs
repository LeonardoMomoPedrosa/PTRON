using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PTRON.Data;
using PTRON.Identity;
using PTRON.Services;
using Xunit;

namespace PTRON.Tests;

public sealed class UserAreasTests : IAsyncLifetime
{
    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), "ptron-uploads-" + Guid.NewGuid().ToString("N"));
    private SqliteConnection _connection = null!;
    private DbContextOptions<AppDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_webRoot);
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        await using var db = new AppDbContext(_options, ReservedUsers.LeoId);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        if (Directory.Exists(_webRoot))
            Directory.Delete(_webRoot, recursive: true);
    }

    [Fact]
    public async Task Photo_is_stored_under_the_user_folder_with_a_guid_name()
    {
        var images = new ImageUploadService(new TempHost(_webRoot), new FixedUser("ana-1"));
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var path = await images.SaveAsync(stream, "foto.JPG", stream.Length);

        Assert.StartsWith("/uploads/ana-1/", path);
        var fileName = path["/uploads/ana-1/".Length..];
        Assert.Matches("^[0-9a-f]{32}\\.jpg$", fileName);
        Assert.True(File.Exists(Path.Combine(_webRoot, "uploads", "ana-1", fileName)));
    }

    [Fact]
    public void Delete_removes_only_the_owner_file()
    {
        var ana = new ImageUploadService(new TempHost(_webRoot), new FixedUser("ana-1"));
        var leo = new ImageUploadService(new TempHost(_webRoot), new FixedUser(ReservedUsers.LeoId));
        var anaFile = Write("uploads", "ana-1", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.png");
        var leoFile = Write("uploads", ReservedUsers.LeoId, "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.png");

        ana.Delete("/uploads/ana-1/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.png");
        ana.Delete("/uploads/leo/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.png");

        Assert.False(File.Exists(anaFile));
        Assert.True(File.Exists(leoFile));

        leo.Delete("/uploads/leo/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.png");
        Assert.False(File.Exists(leoFile));
    }

    [Fact]
    public void Leo_can_delete_a_legacy_upload_and_another_user_cannot()
    {
        var legacy = Write("uploads", "legado.png");
        var secret = Write("secret.txt");

        new ImageUploadService(new TempHost(_webRoot), new FixedUser("ana-1"))
            .Delete("/uploads/legado.png");
        Assert.True(File.Exists(legacy));

        new ImageUploadService(new TempHost(_webRoot), new FixedUser(ReservedUsers.LeoId))
            .Delete("/uploads/../secret.txt");
        Assert.True(File.Exists(secret));

        new ImageUploadService(new TempHost(_webRoot), new FixedUser(ReservedUsers.LeoId))
            .Delete("/uploads/legado.png");
        Assert.False(File.Exists(legacy));
        Assert.True(File.Exists(secret));
    }

    [Fact]
    public async Task Save_without_a_user_is_rejected()
    {
        var images = new ImageUploadService(new TempHost(_webRoot), new FixedUser(""));
        await using var stream = new MemoryStream(new byte[] { 1 });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => images.SaveAsync(stream, "foto.png", stream.Length));
        Assert.Equal("Não há usuário autenticado para salvar a imagem.", error.Message);
    }

    [Fact]
    public async Task Sql_console_runs_for_leo_and_refuses_everyone_else()
    {
        var leo = new SqlConsoleService(new UserFactory(_options, ReservedUsers.LeoId), new FixedUser(ReservedUsers.LeoId));
        var tables = await leo.GetTableNamesAsync();
        Assert.Contains("AspNetUsers", tables);

        var query = await leo.ExecuteAsync("SELECT 1");
        Assert.True(query.IsQuery);
        Assert.Single(query.Rows);

        var other = new SqlConsoleService(new UserFactory(_options, "ana-1"), new FixedUser("ana-1"));
        var denied = await Assert.ThrowsAsync<InvalidOperationException>(() => other.ExecuteAsync("SELECT 1"));
        Assert.Equal("O console SQL é exclusivo do usuário reservado.", denied.Message);
        var listed = await Assert.ThrowsAsync<InvalidOperationException>(() => other.GetTableNamesAsync());
        Assert.Equal("O console SQL é exclusivo do usuário reservado.", listed.Message);

        var anonymous = new SqlConsoleService(new UserFactory(_options, ""), new FixedUser(""));
        await Assert.ThrowsAsync<InvalidOperationException>(() => anonymous.ExecuteAsync("SELECT 1"));
    }

    private string Write(params string[] parts)
    {
        var path = Path.Combine(new[] { _webRoot }.Concat(parts).ToArray());
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[] { 9 });
        return path;
    }

    private sealed class FixedUser : ICurrentUser
    {
        public FixedUser(string userId) => UserId = userId;
        public string UserId { get; }
    }

    private sealed class UserFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly string _userId;

        public UserFactory(DbContextOptions<AppDbContext> options, string userId)
        {
            _options = options;
            _userId = userId;
        }

        public AppDbContext CreateDbContext() => new(_options, _userId);
    }

    private sealed class TempHost : IWebHostEnvironment
    {
        public TempHost(string webRoot) => WebRootPath = webRoot;
        public string ApplicationName { get; set; } = "PTRON.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
