using Api.Project.Template.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data;

public class ContextFixture : IDisposable
{
    public Context Context { get; }

    public ContextFixture()
    {
        var options = new DbContextOptionsBuilder<Context>()
            .UseInMemoryDatabase("Database")
            .Options;

        Context = new Context(options);
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
