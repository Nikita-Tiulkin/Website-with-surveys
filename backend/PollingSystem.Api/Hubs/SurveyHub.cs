using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PollingSystem.Api.Hubs;

[Authorize]
public sealed class SurveyHub : Hub
{
}
