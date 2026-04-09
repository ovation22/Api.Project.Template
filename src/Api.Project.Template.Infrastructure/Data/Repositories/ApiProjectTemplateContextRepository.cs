using Api.Project.Template.Application.Abstractions.Logging;

namespace Api.Project.Template.Infrastructure.Data.Repositories;

public class ApiProjectTemplateContextRepository(ApiProjectTemplateContext context, ILoggerAdapter<EFRepository> logger) : EFRepository(context, logger)
{
}
