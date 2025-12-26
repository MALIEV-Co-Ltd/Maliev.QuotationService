# QuotationService Specification - Permission-Based Authorization Migration

## Permissions to Define

### Quotation Operations
```
quotation.quotations.create      - Create new quotations
quotation.quotations.read        - Read quotation details
quotation.quotations.update      - Update quotation information
quotation.quotations.delete      - Delete quotations
quotation.quotations.approve     - Approve quotations
quotation.quotations.send        - Send quotations to customers
quotation.quotations.convert     - Convert quotation to order
quotation.quotations.revise      - Create revised versions
quotation.quotations.expire      - Mark quotations as expired
```

### Line Item Operations
```
quotation.line-items.create      - Create quotation line items
quotation.line-items.read        - Read line item details
quotation.line-items.update      - Update line items
quotation.line-items.delete      - Delete line items
```

### Template Operations
```
quotation.templates.create       - Create quotation templates
quotation.templates.read         - Read templates
quotation.templates.use          - Use templates for quotations
```

## Predefined Roles

### quotation-admin
**Permissions**: All quotation.* permissions

### quotation-manager
**Permissions**: create, read, update, approve, send, convert, revise, line-items.*, templates.*

### quotation-creator
**Permissions**: create, read, update, send, line-items.create, line-items.read, line-items.update, templates.read, templates.use

### quotation-viewer
**Permissions**: read, line-items.read, templates.read

## Success Criteria
- [ ] ~16 permissions registered
- [ ] 4 predefined roles registered
