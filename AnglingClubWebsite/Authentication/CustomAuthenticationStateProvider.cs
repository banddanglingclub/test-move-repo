using AnglingClubShared;
using AnglingClubShared.DTOs;
using AnglingClubShared.Models.Auth;
using AnglingClubWebsite.Extensions;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace AnglingClubWebsite.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly ISessionStorageService _sessionStorageService;
        private readonly IMessenger _messenger;
        private readonly ILogger<CustomAuthenticationStateProvider> _logger;

        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(
            ILocalStorageService localStorageService,
            IMessenger messenger,
            ILogger<CustomAuthenticationStateProvider> logger,
            ISessionStorageService sessionStorageService)
        {
            _localStorageService = localStorageService;
            _messenger = messenger;
            _logger = logger;
            _sessionStorageService = sessionStorageService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var userSession = await _localStorageService.ReadEncryptedItem<AuthenticateResponse>(Constants.AUTH_KEY);

                if (userSession == null)
                {
                    return await Task.FromResult(new AuthenticationState(_anonymous));
                }

                var expired = userSession.Expiration < DateTime.UtcNow;

                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> {
                    new Claim(ClaimTypes.Name, userSession.Id!),
                    new Claim("Token", userSession.Token!),
                    new Claim(ClaimTypes.Expired, expired.ToString()),
                }, "JwtAuth"));

                return await Task.FromResult(new AuthenticationState(claimsPrincipal));
            }
            catch
            {
                return await Task.FromResult(new AuthenticationState(_anonymous));
            }
        }

        public async Task UpdateAuthenticationState(AuthenticateResponse? userSession, bool rememberMe)
        {
            var userSessionAsString = JsonSerializer.Serialize(userSession, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            //_logger.LogWarning($"[UpdateAuthenticationState] called with userSession = {userSessionAsString} and rememberMe = {rememberMe}");

            ClaimsPrincipal claimsPrincipal;

            if (userSession != null)
            {
                claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> {
                    new Claim("Id", userSession.Id!),
                    new Claim("Token", userSession.Token!),
                    //new Claim(ClaimTypes.GivenName, userSession.FirstName!),
                    //new Claim(ClaimTypes.Surname, userSession.LastName!),
                }));

                userSession.Expiration = DateTime.Now.AddSeconds(userSession.ExpiresIn);

                if (rememberMe)
                {
                    
                    await _localStorageService.SetItemAsync(Constants.AUTH_KEY, userSession); // TODO Ang to Blazor Migration - remove after migration
                    //await _localStorageService.SaveItemEncrypted(Constants.AUTH_KEY, userSession); // TODO Ang to Blazor Migration - re-instate after migration
                }
                else
                {
                    // TODO Ang to Blazor Migration - this needs to be added at some point an retained
                    //await _sessionStorageService.SaveItemEncrypted(Constants.AUTH_KEY, userSession);
                }

                // TODO Ang to Blazor Migration - put the following back once migration is complete
                //_messenger.Send(new LoggedIn(new ClientMemberDto(new JwtSecurityTokenHandler().ReadJwtToken(userSession.Token))));
            }
            else
            {
                claimsPrincipal = _anonymous;

                await _localStorageService.RemoveItemAsync(Constants.AUTH_KEY);

                var anonUser = new LoggedIn(new ClientMemberDto());

                // TODO Ang to Blazor Migration - put the following back once migration is complete
                //_messenger.Send(anonUser);
            }


            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        public async Task<string> GetToken()
        {
            var result = string.Empty;

            try
            {
                var userSession = await _localStorageService.ReadEncryptedItem<AuthenticateResponse>(Constants.AUTH_KEY);

                if (userSession != null)
                {
                    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(userSession.Token);

                    var expiry = jwt.ValidTo;

                    if (DateTime.UtcNow < expiry)
                    {
                        result = userSession.Token;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetToken: {ex.Message}");
                throw;
            }

            return result;
        }
    }
}
