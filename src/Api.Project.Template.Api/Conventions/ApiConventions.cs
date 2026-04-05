using Microsoft.AspNetCore.Mvc;

namespace Api.Project.Template.Api.Conventions;

public static class ApiConventions
{
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static void Default()
    {
    }
}
