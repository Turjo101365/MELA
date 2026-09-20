# MELA

## Password reset email configuration

Copy the values from `.env.example` into your environment or User Secrets. ASP.NET Core maps `Email__Host` to `Email:Host` (and the same pattern for the remaining values). Never place real credentials in `appsettings.json` or source control.

For local PowerShell testing, set the values for the current terminal, for example:

```powershell
$env:Email__Host = "smtp.example.com"
$env:Email__Port = "587"
$env:Email__UseSsl = "true"
$env:Email__UserName = "your-username"
$env:Email__Password = "your-app-password"
$env:Email__FromAddress = "no-reply@example.com"
$env:Email__FromName = "MELA Fair"
$env:Email__PublicBaseUrl = "http://localhost:5173"
dotnet run --project .\MelaFair.Web --launch-profile http
```

Use an SMTP provider's app password where applicable. The reset link is valid for 20 minutes and is single-use.
