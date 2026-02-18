# Sample Invoice Template Guide

This guide explains how to set up your Word template for use with Invoice Generator.

## Creating the Template

1. **Open Microsoft Word** and create a new blank document

2. **Add Your Header/Logo** (optional):
   - Insert your company logo and name
   - Add any company details

3. **Add Invoice Title**:
   ```
   INVOICE
   ```

4. **Add Invoice Details Section** with placeholders:
   ```
   Invoice Number: {{INVOICE_NUMBER}}
   Invoice Period: {{INVOICE_PERIOD}}
   Date From: {{DATE_FROM}}
   Date To: {{DATE_TO}}
   ```

5. **Add Client Information Section** with placeholders:
   ```
   BILL TO:
   {{CUSTOMER_NAME}}
   {{CUSTOMER_STREET}}
   {{CUSTOMER_CITY}}, {{CUSTOMER_POSTCODE}}
   {{CLIENT_EMAIL}}
   ```

6. **Create Line Items Table**:
   Create a table with the following headers:
   - Description
   - Quantity
   - Unit Rate
   - Amount
   
   Add placeholder rows:
   ```
   {{LINE_DESCRIPTION}} | {{LINE_QUANTITY}} | {{LINE_RATE}} | {{LINE_AMOUNT}}
   ```

7. **Add Summary Section**:
   ```
   Subtotal: {{SUBTOTAL}}
   Tax (if applicable): {{TAX}}
   ─────────────────
   Total: {{TOTAL}}
   ```

8. **Add Footer** (optional):
   - Payment terms
   - Thank you message
   - Contact information

## Placeholder Reference

When creating your template, use these placeholders:

| Placeholder | Description |
|-------------|-------------|
| {{CLIENT_NAME}} | Client display name (legacy placeholder) |
| {{CLIENT_ADDRESS}} | Client billing address (legacy placeholder) |
| {{ADDRESS}} | Client billing address (legacy placeholder) |
| {{CLIENT_EMAIL}} | Client email address |
| {{EMAIL}} | Client email address (legacy placeholder) |
| {{CUSTOMER_NAME}} | Customer name |
| {{CUSTOMER_STREET}} | Customer street address |
| {{CUSTOMER_CITY}} | Customer city |
| {{CUSTOMER_POSTCODE}} | Customer postcode |
| {{CUSTOMER_ADDRESS}} | Customer address line (legacy alias for billing address) |
| {{INVOICE_NUMBER}} | Invoice number (e.g., INV-0042) |
| {{INVOICE_NO}} | Invoice number (alias) |
| {{INVOICE_PERIOD}} | Invoice period in DD/MM/YYYY - DD/MM/YYYY |
| {{DATE_FROM}} | Invoice period start date (YYYY-MM-DD) |
| {{DATE_TO}} | Invoice period end date (YYYY-MM-DD) |
| {{LINE_DESCRIPTION}} | Line item description |
| {{LINE_QUANTITY}} | Line item quantity |
| {{LINE_RATE}} | Line item unit rate |
| {{LINE_AMOUNT}} | Line item total amount |
| {{SUBTOTAL}} | Sum of all line items |
| {{TAX}} | Tax amount (if applicable) |
| {{TOTAL}} | Grand total |

## Saving the Template

1. Save the document as **Invoice_Template.docx** in a location of your choice
2. Note the full file path (e.g., `C:\Users\YourName\Documents\Invoice_Template.docx`)
3. In the Settings window of Invoice Generator, set this path as your template

## Tips

- **Keep placeholders simple**: Use exactly the placeholder names listed above
- **Use a table for line items**: The template engine works best with table cells
- **Test first**: Create one invoice first to verify all placeholders work
- **Backup template**: Keep a backup of your original template before first use
- **Styled document**: You can use any formatting, colors, and fonts - only the placeholders get replaced

## Example Template Structure

```
═══════════════════════════════════════════
YOUR COMPANY NAME
Your Company Address | Phone | Email
═══════════════════════════════════════════

INVOICE

Invoice #: {{INVOICE_NUMBER}}
Period: {{INVOICE_PERIOD}}

─────────────────────────────────────────
BILL TO:
{{CUSTOMER_NAME}}
{{CUSTOMER_STREET}}
{{CUSTOMER_CITY}}, {{CUSTOMER_POSTCODE}}
{{CLIENT_EMAIL}}

─────────────────────────────────────────
LINE ITEMS:

Description         | Qty  | Rate      | Amount
────────────────────────────────────────────
{{LINE_DESCRIPTION}} | {{LINE_QUANTITY}} | ${{LINE_RATE}} | ${{LINE_AMOUNT}}

                                    ─────────
                                    Total: ${{TOTAL}}

Thank you for your business!
```

For further assistance, refer to the Invoice Generator documentation.
