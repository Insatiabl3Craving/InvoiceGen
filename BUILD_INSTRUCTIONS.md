# Build and Run Instructions

## Prerequisites
- Windows 10 or later
- .NET 6.0 SDK or later ([Download](https://dotnet.microsoft.com/en-us/download/dotnet/6.0))
- Visual Studio 2022 or Visual Studio Code with C# extensions
- LibreOffice (for PDF conversion) - [Download](https://www.libreoffice.org/download/)

## Building from Command Line

1. Open PowerShell or Command Prompt
2. Navigate to the project directory:
   ```
   cd "c:\Users\samue\Desktop\Invoice Generator"
   ```

3. Restore NuGet packages:
   ```
   dotnet restore
   ```

4. Build the project:
   ```
   dotnet build -c Release
   ```

5. Run the application:
   ```
   dotnet run --project InvoiceGenerator\InvoiceGenerator.csproj
   ```

## Building with Visual Studio

1. Open `InvoiceGenerator.sln` in Visual Studio 2022
2. Right-click on the solution and select "Restore NuGet Packages"
3. Press `Ctrl+Shift+B` to build
4. Press `F5` to run with debugging (or `Ctrl+F5` without debugging)

## Building with Visual Studio Code

1. Open the folder in VS Code
2. Install C# extensions if not already installed
3. Open a terminal in VS Code (Ctrl+`)
4. Run:
   ```
   dotnet restore
   dotnet build
   dotnet run --project InvoiceGenerator/InvoiceGenerator.csproj
   ```

## First-Time Setup After Running

1. Click on "Settings" in the navigation menu
2. Configure the following:
   - **Word Template Path**: Point to your .docx template file
   - **Invoices Folder**: Select where to save generated invoices
   - **SMTP Server**: (e.g., smtp.gmail.com for Gmail)
   - **SMTP Port**: Usually 587 for TLS
   - **From Email Address**: Your email address
   - **Email Password**: Your email password (securely stored in Windows Credential Manager)
   - **Use TLS**: Check if using Port 587
3. Click "Test Email Connection" to verify settings
4. Click "Save Settings"

## Creating a Word Template

1. Open Microsoft Word
2. Design your invoice layout
3. Use placeholders in the format `{{PLACEHOLDER_NAME}}` for dynamic content

   Example placeholders:
   - `{{CLIENT_NAME}}` - Client display name
   - `{{ADDRESS}}` - Client billing address
   - `{{EMAIL}}` - Client email
   - `{{INVOICE_NUMBER}}` - Invoice number
   - `{{DATE_FROM}}` - Start date of invoice period
   - `{{DATE_TO}}` - End date of invoice period
   - For line items, use dynamic table cells with similar placeholders

4. Save as `.docx` file
5. Point to this file in Settings

## Creating a Test CSV File

Create a file named `test_invoice.csv` with the following content:

```
Description,Quantity,UnitRate
Consulting Services,10,150.00
Software License,1,500.00
Technical Support,5,75.00
```

## Email Configuration Examples

### Gmail
- SMTP Server: `smtp.gmail.com`
- Port: `587`
- Use TLS: ✓ Checked
- Email: Your Gmail address
- Password: [Create an App Password](https://support.google.com/accounts/answer/185833)

### Outlook/Hotmail
- SMTP Server: `smtp-mail.outlook.com`
- Port: `587`
- Use TLS: ✓ Checked
- Email: Your Outlook email
- Password: Your password

## Troubleshooting

### Build Fails - "No .NET SDKs were found"
- Install [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- Verify installation: `dotnet --version`

### PDF Conversion Fails
- Install [LibreOffice](https://www.libreoffice.org/download/)
- Verify installation at: `C:\Program Files\LibreOffice\program\soffice.exe`
- Restart the application after installing

### Email Won't Send
- Click "Test Email Connection" in Settings
- Verify SMTP credentials are correct
- Check firewall allows outgoing SMTP connections
- For Gmail, use an App Password, not your main password

### Database Errors
- Delete: `C:\Users\<YourUsername>\AppData\Roaming\InvoiceGenerator\InvoiceGenerator.db`
- Restart the application to regenerate the database

## Creating an Installer

To create a Windows installer, use Wix Toolset or NSIS. The compiled executable will be in:
```
InvoiceGenerator\bin\Release\net6.0-windows\InvoiceGenerator.exe
```
