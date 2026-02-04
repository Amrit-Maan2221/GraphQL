using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Standard EF Core Registration
builder.Services.AddPooledDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddFiltering()
    .AddSorting()
    .RegisterDbContextFactory<AppDbContext>();

var app = builder.Build();

app.MapGraphQL();
app.Run();

// --- DATA LAYER ---

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public int Pages { get; set; }
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Personalization: Ensuring strings use varchar instead of nvarchar
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(string)))
            {
                property.SetIsUnicode(false); // Forces varchar instead of nvarchar
            }
        }
    }
}

// --- GRAPHQL TYPES ---

public class Query
{
    [UseFiltering]
    [UseSorting]
    public IQueryable<Book> GetBooks(AppDbContext context)
        => context.Books;
}

public class Mutation
{
    public async Task<Book> AddBookAsync(
        string title, string author, int pages,
        AppDbContext context)
    {
        var book = new Book { Title = title, Author = author, Pages = pages };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        return book;
    }
}