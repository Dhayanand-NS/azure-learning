using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);


//builder.Services. AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));


/// This is the same as above 2 lines, but for understanding the authentication flow, with event handlers added to log each step of the authentication process to the console.///////////////////////////

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.SaveTokens = true;
        options.Scope.Add(builder.Configuration["AzureAd:Scope:ApiScope"]); 

        // 🔴 STEP 1 - Your app → Entra ID
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            Console.WriteLine("\n=================================");
            Console.WriteLine("🔴 STEP 1: Redirecting to Entra ID");
            Console.WriteLine($"URL: {context.ProtocolMessage.IssuerAddress}");
            Console.WriteLine($"ClientID: {context.ProtocolMessage.ClientId}");
            Console.WriteLine($"RedirectURI: {context.ProtocolMessage.RedirectUri}");
            Console.WriteLine($"Scope: {context.ProtocolMessage.Scope}");
            Console.WriteLine("=================================\n");
            return Task.CompletedTask;
        };

        // 🔑 STEP 2 - Auth Code arrives before exchange
        options.Events.OnAuthorizationCodeReceived = context =>
        {
            Console.WriteLine("\n=================================");
            Console.WriteLine("🔑 STEP 2: Auth Code Received!");
            Console.WriteLine($"Auth Code: {context.ProtocolMessage.Code}");
            Console.WriteLine("⚡ About to exchange for token...");
            Console.WriteLine("=================================\n");
            return Task.CompletedTask;
        };
        ////In between, the authorization code is exchanged for tokens, but that happens under the hood in the OpenID Connect middleware, so we don't have a direct event for it. The next event we can tap into is when the raw ID token is received from Entra ID after the exchange.

        // 🟡 STEP 3 - Entra ID → Your app (raw token arrives)
        options.Events.OnMessageReceived = context =>
        {
            Console.WriteLine("\n=================================");
            Console.WriteLine("🟡 STEP 3: Raw Token Received!");
            Console.WriteLine($"RAW ID TOKEN: {context.ProtocolMessage.IdToken}");
            Console.WriteLine("👆 Copy this and paste at jwt.ms to decode!");
            Console.WriteLine("=================================\n");
            return Task.CompletedTask;
        };

        // 🟢 STEP 4 - Token decoded into claims
        options.Events.OnTokenValidated = context =>
        {
            Console.WriteLine("\n=================================");
            Console.WriteLine("🟢 STEP 4: Token Validated! Claims:");
            foreach (var claim in context.Principal.Claims)
            {
                Console.WriteLine($"   {claim.Type} = {claim.Value}");
            }
            Console.WriteLine("=================================\n");
            return Task.CompletedTask;
        };

        // 🔵 STEP 5 - Session cookie created
        options.Events.OnTicketReceived = context =>
        {
            Console.WriteLine("\n=================================");
            Console.WriteLine("🔵 STEP 5: Cookie Created! User logged in!");
            Console.WriteLine($"User: {context.Principal.Identity.Name}");
            Console.WriteLine("=================================\n");
            var accessToken = context.Properties.GetTokenValue("access_token");
            var idToken = context.Properties.GetTokenValue("id_token");
            Console.WriteLine($"Access Token in ticket: {accessToken ?? "NULL!"}");
            Console.WriteLine($"ID Token in ticket: {idToken ?? "NULL!"}");
            Console.WriteLine("=================================\n");
            return Task.CompletedTask;
        };

        // ❌ If anything fails
        options.Events.OnAuthenticationFailed = context =>
        {
            Console.WriteLine("\n=================================");
            Console.WriteLine($"❌ AUTH FAILED: {context.Exception.Message}");
            Console.WriteLine("=================================\n");
            return Task.CompletedTask;
        };
    }).EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();



/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});


//builder.Services.AddHttpClient();

// Ignore SSL errors for localhost development only!, its a self signed certificate, in production you should use a valid certificate from a trusted CA.
builder.Services.AddHttpClient("default", client => { })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            // Ignore SSL errors for localhost development only!
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    });

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

app.Run();
