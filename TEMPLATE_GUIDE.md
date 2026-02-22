# Invoice Template Guide

This guide explains how to set up your Word template for use with Invoice Generator.

## Creating the Template

1. **Open Microsoft Word** and open your existing completed invoice (e.g., `edcl066.docx`) as a reference for layout.

2. **Save a copy** as your template (e.g., `Invoice_Template.docx`).

3. **Replace the actual values** with `{{PLACEHOLDER}}` markers as described below.

## Header / Invoice Details

Replace the static invoice info with these placeholders:

```
Invoice Number: {{INVOICE_NUMBER}}
Invoice Period:  {{DATE_FROM}} – {{DATE_TO}}
```

Or use `{{INVOICE_PERIOD}}` for a combined "dd/MM/yyyy - dd/MM/yyyy" range.

## Client Information

Replace the client name and address block with:

```
{{CUSTOMER_NAME}}
{{CUSTOMER_STREET}}
{{CUSTOMER_CITY}}, {{CUSTOMER_POSTCODE}}
{{CLIENT_EMAIL}}
```

## Line Items Table

Your template must have a **Word table** for line items. The table should have:

1. A **header row** with static column titles (these stay as-is):

| Date | Day | Duration | No of Carers | Care Description | Rate (£) |
|------|-----|----------|-------------|-----------------|----------|

2. A single **template data row** directly below the header, containing these placeholders:

| {{DATE}} | {{DAY}} | {{DURATION}} | {{NO_OF_CARERS}} | {{CARE_DESCRIPTION}} | {{RATE}} |
|----------|---------|-------------|-------------------|---------------------|----------|

**How it works:** When the invoice is generated, the app finds this template row, clones it once per CSV line item (filling in each row's values), and removes the original placeholder row. The result is a fully populated table with one row per visit.

## Totals

Below or after the table, add a total placeholder:

```
Total: £{{TOTAL}}
```

## Placeholder Reference

| Placeholder | Replaced With |
|-------------|---------------|
| `{{INVOICE_NUMBER}}` | Invoice number (e.g., "EDCL080") |
| `{{INVOICE_NO}}` | Invoice number (alias) |
| `{{INVOICE_PERIOD}}` | "dd/MM/yyyy - dd/MM/yyyy" date range |
| `{{DATE_FROM}}` | Period start date (dd/MM/yyyy) |
| `{{DATE_TO}}` | Period end date (dd/MM/yyyy) |
| `{{DATE_GENERATED}}` | Date invoice was generated |
| `{{CUSTOMER_NAME}}` | Client display name |
| `{{CLIENT_NAME}}` | Client display name (alias) |
| `{{CUSTOMER_STREET}}` | Client street address |
| `{{CUSTOMER_CITY}}` | Client city |
| `{{CUSTOMER_POSTCODE}}` | Client postcode |
| `{{CUSTOMER_ADDRESS}}` | Client billing address (legacy) |
| `{{CLIENT_ADDRESS}}` | Client billing address (legacy) |
| `{{CLIENT_EMAIL}}` | Client email address |
| `{{EMAIL}}` | Client email (alias) |
| **Line Item Placeholders** (in template table row): |  |
| `{{DATE}}` | Visit date (dd/MM/yyyy) |
| `{{DAY}}` | Day of week |
| `{{DURATION}}` | Visit duration (e.g., "30 minutes") |
| `{{NO_OF_CARERS}}` | Number of carers |
| `{{CARE_DESCRIPTION}}` | Description of care |
| `{{RATE}}` | Rate for this visit (£) |
| **Totals:** |  |
| `{{TOTAL}}` | Sum of all line item rates |
| `{{TOTAL_AMOUNT}}` | Same as TOTAL (alias) |
| `{{SUBTOTAL}}` | Same as TOTAL (alias) |

## CSV File Format

Your CSV files should have these exact column headers:

```csv
Date,Day,Duration,No of Carers,Care Description,Rate (£)
01/01/2026,Thursday,30 minutes,1,Morning care call,14.99
01/01/2026,Thursday,45 minutes,1,Evening care call,17.99
```

## Step-by-Step Template Creation

1. Open your reference invoice in Word
2. **Save As** → `Invoice_Template.docx`
3. Replace the invoice number with `{{INVOICE_NUMBER}}`
4. Replace dates with `{{DATE_FROM}}` and `{{DATE_TO}}`
5. Replace client name/address with `{{CUSTOMER_NAME}}`, `{{CUSTOMER_STREET}}`, etc.
6. In the line items table, **delete all data rows** except one
7. In that single remaining data row, put the placeholders: `{{DATE}}`, `{{DAY}}`, `{{DURATION}}`, `{{NO_OF_CARERS}}`, `{{CARE_DESCRIPTION}}`, `{{RATE}}`
8. Replace the total value with `{{TOTAL}}`
9. Save the template
10. In the app's Settings, set the template path to this file

## Tips

- **Keep placeholders exact**: Use exactly `{{NAME}}` with double curly braces
- **Word may split placeholders**: If a placeholder doesn't get replaced, select it, delete it, and re-type it in one go (don't copy-paste character by character)
- **Table formatting is preserved**: The cloned rows inherit all formatting (borders, fonts, alignment) from the template row
- **Test first**: Generate one invoice to verify everything works before processing a batch
- **Backup template**: Keep a copy of your template before first use
