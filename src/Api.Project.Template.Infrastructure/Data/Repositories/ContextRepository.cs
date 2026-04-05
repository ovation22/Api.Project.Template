using Api.Project.Template.Application.Abstractions.Logging;

namespace Api.Project.Template.Infrastructure.Data.Repositories;

public class ContextRepository : EFRepository
{
    public ContextRepository(ApiProjectTemplateContext context, ILoggerAdapter<EFRepository> logger) : base(context, logger)
    {
    }
}
