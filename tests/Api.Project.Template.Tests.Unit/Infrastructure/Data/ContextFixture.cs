using Api.Project.Template.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data;

public class ContextFixture : IDisposable
{
    public ApiProjectTemplateContext Context { get; }

    public ContextFixture()
    {
        var options = new DbContextOptionsBuilder<ApiProjectTemplateContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Context = new ApiProjectTemplateContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
