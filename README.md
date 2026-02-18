# Invoice Generator

A comprehensive Windows desktop application for managing clients, generating invoices, and sending them via email.

## Features

### 1. Client Manager
- Add, edit, and delete client records
- Store client information including name, billing address, and contact email
- Quick client lookup and selection during invoice creation

### 2. Invoice Builder
- Select client from dropdown with auto-populated fields
- Enter invoice details (invoice number, date range)
- Import line items from CSV files
- Preview invoice before generation
- Generate Word documents and PDFs automatically

### 3. File Output Engine
- Replaces placeholders in Word templates with invoice data
- Auto-saves documents in both .docx and .pdf formats
- Organized folder structure: `/Invoices/ClientName/YYYY/MM/`
- Original template never modified (copy created for each invoice)

### 4. Invoice History & Email
- Browse past invoices with search and filter capabilities
- Open stored DOCX and PDF files directly from history
- Send invoices via email with optional custom subject/body
- SMTP email support with secure credential storage

## Installation & Setup

### Prerequisites
- Windows 10 or later
- .NET 6.0 Runtime or later
- Microsoft Word (for template creation) - optional
- LibreOffice (for PDF conversion) - recommended

### Building from Source

1. Clone or extract the project
2. Open the solution in Visual Studio 2022 or later
3. Build the solution (Ctrl+Shift+B)
4. Run the application

### First-Time Setup

1. **Configure Settings:**
   - Click "Settings" in the navigation menu
   - Set the path to your Word template file (.docx)
   - Set the root folder for invoice storage
   - Configure email SMTP server details

2. **Add Clients:**
   - Click "Client Manager"
   - Click "Add Client" to create client records
   - Fill in display name, email, billing address

3. **Prepare Invoice Template:**
   - Create a Word template with placeholders in the format: `{{PLACEHOLDER_NAME}}`
   - Common placeholders: `{{CLIENT_NAME}}`, `{{ADDRESS}}`, `{{INVOICE_NUMBER}}`, `{{DATE_FROM}}`, `{{DATE_TO}}`

4. **Import CSV Format:**
   - Create CSV files with columns: `Description`, `Quantity`, `UnitRate`
   - Example:
     ```
     Description,Quantity,UnitRate
     Consulting Services,10,150.00
     Software License,1,500.00
     ```

## Usage

### Generating Invoices

1. Navigate to "Invoice Builder"
2. Select a client (auto-populates details)
3. Enter invoice number and date range
4. Click "Import CSV" to load line items
5. Click "Preview" to verify all details
6. Click "Generate Invoice" to create DOCX and PDF files

### Sending Invoices

1. Go to "Invoice History"
2. Select an invoice from the list
3. Click "Send Email"
4. Customize email subject/body if needed
5. Click "Send" to send via SMTP

## Directory Structure

```
InvoiceGenerator/
├── Models/                 # Data models (Client, Invoice, etc.)
├── Views/                 # XAML UI components
├── Services/              # Business logic and integrations
├── ViewModels/            # MVVM ViewModels (future)
├── Utilities/             # Helper functions
├── Properties/            # Project properties
├── App.xaml               # Application startup
├── MainWindow.xaml        # Main application window
└── InvoiceGenerator.csproj# Project file
```

## Dependencies

- **DocumentFormat.OpenXml** - Word document manipulation
- **EntityFrameworkCore.SQLite** - Local database
- **MailKit** - SMTP email sending
- **CsvHelper** - CSV file parsing
- **SelectPdf** - PDF conversion (alternative to LibreOffice)

## Email Configuration

### Gmail Setup:
1. Enable 2-factor authentication on your Gmail account
2. Create an App Password (not your main password)
3. Use SMTP: `smtp.gmail.com`, Port: `587`, TLS: Yes

### Outlook Setup:
1. Use SMTP: `smtp-mail.outlook.com`, Port: `587`, TLS: Yes
2. Use your Outlook email and password

## Security

- Email passwords are stored securely in Windows Credential Manager
- Passwords are NOT stored in plain text configuration files
- Database is stored locally in your AppData folder

## Troubleshooting

### Email Not Sending
- Check email credentials in Settings
- Use "Test Email Connection" button to verify SMTP configuration
- Ensure firewall allows outgoing SMTP connections

### PDF Conversion Issues
- Ensure LibreOffice is installed at: `C:\Program Files\LibreOffice\program\soffice.exe`
- Or configure SelectPdf library in settings

### Database Errors
- Delete `AppData\Roaming\InvoiceGenerator\InvoiceGenerator.db` to reset database
- Close application and reopen to regenerate

## License

This project is provided as-is for personal use.

## Support

For issues or feature requests, please document the error and contact support.
