# FootballManager WCF and website

This project hosts the SOAP WCF service with CoreWCF on .NET 10 and the local FootballManager website. The WPF client communicates with the SOAP service at `http://localhost:5152/FootballManagerService`. Open `http://localhost:5152/` in a browser for the website. Both clients use the same service logic and Access database. The service listens on localhost only, so it is for this computer's development and demonstration use.

The website's static files are in `wwwroot`. Its `/api` routes are hosted by the same WCF project and call the same business-service methods. The WPF client remains a separate application and calls WCF using the shared contracts and DTOs; it never connects directly to Access.

On first run, the service creates `Data/FootballManager.accdb` if needed, creates missing DSD tables and relationships, and ensures usernames have a unique index. Microsoft ACE OLE DB is required.

## Start the application

Open PowerShell in the solution folder and run:

```powershell
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj
```

Keep the service window open. Open `http://localhost:5152/` for the website. To run WPF as well, open another PowerShell window and run:

```powershell
dotnet run --project .\FootballManager.WPF\FootballManager.WPF.csproj
```

WSDL: `http://localhost:5152/FootballManagerService?wsdl`.

## Demo database and accounts

The packaged database contains eight tables, three accounts, and sample C2 football content. The three demo accounts are one `Admin`, one `Manager`, and one `Player`; their plaintext passwords are kept in a separate private file outside the source ZIP. Password hashes only are stored in Access.

For a new local database, create the first Admin, create the other two accounts, add the sample content, then prepare fresh random demo passwords:

```powershell
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --create-admin
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --seed-demo-users
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --seed-demo-content
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --prepare-demo-accounts
```

`--seed-demo-content` is safe to repeat. It inserts three fictional teams, twelve fictional player records, and their active roster links only when missing. Matches, statistics, and transfers remain empty because they belong to later cycles. `--prepare-demo-accounts` assigns new random passwords to the existing Admin, `DemoManager`, and `DemoPlayer`, stores password hashes, and prints the passwords once. Keep that output private and outside the submission ZIP.

The public Register operation accepts only a username and password and always creates a `Player`. The browser stores the short-lived session in an `HttpOnly`, same-site cookie. WCF checks the active user's current role for each protected action. The website's Admin communication card is an account directory based on `Users`; it does not claim to send messages because the approved schema has no contact details or message storage.

## Submission

Submit the requirements document, source ZIP, Access database, and demonstration video. Do not include `bin`, `obj`, `.vs`, `*.csproj.user` files, plaintext passwords, or the earlier API project in the source archive.
