using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenCandlesOfBlazor.Data;

namespace TenCandlesOfBlazor.Components.Account;

// This is a server-side AuthenticationStateProvider that revalidates the security stamp for the connected user
// every 30 minutes an interactive circuit is connected.
internal sealed class IdentityRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
  private readonly IServiceScopeFactory scopeFactory;
  private readonly IOptions<IdentityOptions> options;

  public IdentityRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityOptions> options)
    : base(loggerFactory)
  {
    this.scopeFactory = scopeFactory;
    this.options = options;
  }

  protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

  protected override async Task<bool> ValidateAuthenticationStateAsync(
      AuthenticationState authenticationState, CancellationToken cancellationToken)
  {
    // Get the user manager from a new scope to ensure it fetches fresh data
    await using var scope = scopeFactory.CreateAsyncScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    return await this.ValidateSecurityStampAsync(userManager, authenticationState.User);
  }

  private async Task<bool> ValidateSecurityStampAsync(UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
  {
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
      return false;
    }
    else if (!userManager.SupportsUserSecurityStamp)
    {
      return true;
    }
    else
    {
      var principalStamp = principal.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType);
      var userStamp = await userManager.GetSecurityStampAsync(user);
      return principalStamp == userStamp;
    }
  }
}
