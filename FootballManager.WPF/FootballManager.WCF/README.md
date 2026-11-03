# FootballManager WCF

This project hosts the SOAP WCF service with CoreWCF on .NET 10. It communicates with the WPF client over `http://localhost:5152/FootballManagerService`. The service listens on localhost only, so it is for this computer's development/demo use.

On first run, the service creates Data\FootballManager.accdb if needed, creates any missing C2 DSD tables and relationships, and ensures usernames have a unique index. The local Microsoft ACE OLE DB provider is required.

The public Register operation accepts only a username and password and always creates a Player. Login returns a short lived session token and the server-confirmed role. Admin operations check that token against the current user record and require an active Admin role on every request.

To create the first Admin locally, stop the service and run:

dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --create-admin

The tool asks for a username and password in the terminal. The password is masked while typing, hashed with the same password hasher used for registration and login, and is never placed in source code. It refuses to create another Admin if one already exists. Do not share or commit the .accdb file if it contains real accounts.

To add the two non-Admin demo accounts required for the C2 package, run this once after an Admin exists:

```powershell
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj -- --seed-demo-users
```

The tool creates `DemoPlayer` and `DemoManager`, generates random passwords, stores only password hashes, and prints the passwords once. Save them privately for the demonstration. It does not change an existing account or create another Admin.

WSDL: http://localhost:5152/FootballManagerService?wsdl.
