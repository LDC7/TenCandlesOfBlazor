using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenCandlesOfBlazor.Components;
using TenCandlesOfBlazor.Components.Account;
using TenCandlesOfBlazor.Data;

namespace TenCandlesOfBlazor;

public sealed class Program
{
  public static async Task Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    builder.Services
      .AddRazorComponents()
      .AddInteractiveServerComponents();

    builder.Services
      .AddCascadingAuthenticationState()
      .AddScoped<IdentityUserAccessor>()
      .AddScoped<IdentityRedirectManager>()
      .AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

    builder.Services
      .AddAuthentication(options =>
      {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
      })
      .AddIdentityCookies();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services
      .AddDbContext<ApplicationDbContext>(options =>
      {
        options.UseSqlServer(connectionString);
      })
      .AddDatabaseDeveloperPageExceptionFilter();

    builder.Services
      .AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
      .AddEntityFrameworkStores<ApplicationDbContext>()
      .AddSignInManager()
      .AddDefaultTokenProviders();

    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
      app.UseMigrationsEndPoint();
    }
    else
    {
      app.UseExceptionHandler("/Error");
      // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
      app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
      .AddInteractiveServerRenderMode();

    // Add additional endpoints required by the Identity /Account Razor components.
    app.MapAdditionalIdentityEndpoints();

    await app.RunAsync();
  }
}
