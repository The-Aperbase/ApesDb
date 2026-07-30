using System.Security.Claims;
using System.Text.Encodings.Web;
using ApesDb.Auth.Services.UserProvisioning;
using ApesDb.Domain;
using ApesDb.Shared.Services.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApesDb.Api.Tests.Infrastructure.Authentication;

public sealed class FakeAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiTests";
    public const string HeaderName = "X-ApesDb-Test-User";

    private readonly ApplicationDbContext _dbContext;
    private readonly IAllowedUserService _allowedUserService;
    private readonly IUserProvisioningService _userProvisioningService;

    public FakeAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApplicationDbContext dbContext,
        IAllowedUserService allowedUserService,
        IUserProvisioningService userProvisioningService
    )
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
        _allowedUserService = allowedUserService;
        _userProvisioningService = userProvisioningService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values))
        {
            return AuthenticateResult.NoResult();
        }

        var testUser = TestUsers.Find(values.ToString());
        if (testUser is null)
        {
            return AuthenticateResult.Fail("The requested API test identity does not exist.");
        }

        if (!await _allowedUserService.IsAllowedAsync(testUser.Email, Context.RequestAborted))
        {
            return AuthenticateResult.NoResult();
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Auth0Subject == testUser.Auth0Subject,
            Context.RequestAborted
        );
        if (user is null)
        {
            await _userProvisioningService.EnsureUserFromPrincipalAsync(
                TestUsers.CreateAuth0Principal(testUser),
                Context.RequestAborted
            );
            user = await _dbContext.Users.SingleAsync(
                candidate => candidate.Auth0Subject == testUser.Auth0Subject,
                Context.RequestAborted
            );
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Auth0Subject),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new("ApesDbUserId", user.Id.ToString()),
        };

        if (user.PictureUrl is not null)
        {
            claims.Add(new Claim("picture", user.PictureUrl));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
