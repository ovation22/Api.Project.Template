namespace Api.Project.Template.Infrastructure.Data;

public class ContextRepository : EFRepository
{
    public ContextRepository(Context context) : base(context)
    {
    }
}
