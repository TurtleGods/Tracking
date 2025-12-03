using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Controllers;

namespace Tracking.Api.Tests;

public sealed class HealthControllerTests
{
    [Fact]
    public void Get_ReturnsOkStatus()
    {
        var controller = new HealthController();

        var result = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equivalent(new { status = "ok" }, ok.Value);
    }
}
