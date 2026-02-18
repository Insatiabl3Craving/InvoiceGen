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
   Date From: {{DATE_FROM}}
   Date To: {{DATE_TO}}
   ```

5. **Add Client Information Section** with placeholders:
   ```
   BILL TO:
   {{CLIENT_NAME}}
   {{ADDRESS}}
   {{EMAIL}}
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
| {{CLIENT_NAME}} | Client display name |
| {{ADDRESS}} | Client billing address |
| {{EMAIL}} | Client email address |
| {{INVOICE_NUMBER}} | Invoice number (e.g., INV-0042) |
| {{DATE_FROM}} | Invoice period start date |
| {{DATE_TO}} | Invoice period end date |
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
Period: {{DATE_FROM}} to {{DATE_TO}}

─────────────────────────────────────────
BILL TO:
{{CLIENT_NAME}}
{{ADDRESS}}
{{EMAIL}}

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
