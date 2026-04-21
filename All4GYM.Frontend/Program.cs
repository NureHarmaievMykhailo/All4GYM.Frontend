var builder = WebApplication.CreateBuilder(args);
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];

if (string.IsNullOrEmpty(apiBaseUrl))
{
    throw new Exception("ApiSettings:BaseUrl is not configured!");
}

// HttpClient з BaseAddress
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Сервіси
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();

// Cookie authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.None;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();