﻿using System;
using System.Threading.Tasks;
using Google.Authenticator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Our.Umbraco.TwoFactorAuth.Extensions;
using Our.Umbraco.TwoFactorAuth.Models;
using Our.Umbraco.TwoFactorAuth.Options;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Our.Umbraco.TwoFactorAuth.Authentication;

internal class UmbracoUserAppAuthenticator : ITwoFactorProvider
{
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly TwoFactorAuthenticator _twoFactorAuthenticator;
    private readonly TwoFactorAuthSettings _twoFactorAuth;

    public const string Name = Constants.Authentication.AppAuthenticatorProvider;

    public string ProviderName => Name;
    public UmbracoUserAppAuthenticator(IUserService userService, IWebHostEnvironment webHostEnvironment, TwoFactorAuthenticator twoFactorAuthenticator, IOptionsMonitor<TwoFactorAuthSettings> twoFactorAuth)
    {
        _userService = userService;
        _webHostEnvironment = webHostEnvironment;
        _twoFactorAuthenticator = twoFactorAuthenticator;
        _twoFactorAuth = twoFactorAuth.CurrentValue;
    }

    public Task<object> GetSetupDataAsync(Guid userOrMemberKey, string secret)
    {
        var user = _userService.GetByKey(userOrMemberKey);
        
        var issuer = GetIssuer();
        

        var setupInfo = _twoFactorAuthenticator.GenerateSetupCode(issuer, user?.Username, secret, false);
        return Task.FromResult<object>(new TwoFactorAuthInfo
        {
            QrCodeSetupImageUrl = setupInfo.QrCodeSetupImageUrl,
            Secret = secret
        });
    }

    public bool ValidateTwoFactorPIN(string secret, string token)
    {
        return _twoFactorAuthenticator.ValidateTwoFactorPIN(secret, token);
    }

    public bool ValidateTwoFactorSetup(string secret, string token) => ValidateTwoFactorPIN(secret, token);

    private string GetIssuer()
    {
        var environment = _webHostEnvironment.EnvironmentName;
        var environmentName = environment != null && !environment.Equals(Environments.Production, StringComparison.CurrentCultureIgnoreCase) ? environment : string.Empty;
        return _twoFactorAuth.ShowEnvironment ? _twoFactorAuth.ApplicationName.ConcatWithSeparator(" - ", environmentName) : _twoFactorAuth.ApplicationName;
    }
}