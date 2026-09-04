# Error Handling & the Result Pattern

## The contract

Every failed response uses `ApiErrorResponse`:

```json
{
  "success": false,
  "errors": [
    { "errorCode": "BOOK_ISBN_INVALID", "errorMessage": "...", "field": "isbn",
      "line": 14, "required": true, "supportedValues": "ONLY DIGITS ..." }
  ],
  "correlationId": "..."
}
```

`line` is only set for bulk-import rows. **All** detectable validation errors are
returned together — never stop at the first.

## New code: `Result`

`src/Library.Application/Common/Results/Result.cs` — `Result` /
`Result<T>` carry either success or a non-empty `ApiError` list.

```csharp
if (errors.Count > 0) return Result.Failure<BookResponse>(errors);
return Result.Success(Map(book));
```

Controllers: `return result.ToActionResult(this);` /
`result.ToCreatedResult(this, nameof(GetById), new { id = result.Value?.Id });`
(`ResultActionExtensions`). Status is derived from the leading error code:
`*_NOT_FOUND` → 404, `*_DUPLICATE` / conflict → 409, `FEATURE_DISABLED` → 403,
any row-level error → 422, else 400.

## Old code / uncaught: the middleware

`GlobalExceptionHandlingMiddleware` maps `ValidationException` → 422 (full list),
`KeyNotFoundException` → 404, `ArgumentException` → 400,
`InvalidOperationException` → 409, anything else → 500 with a fixed support
message (never a stack trace). Full detail goes to the log streams.

## Error codes

`Common/Errors/ErrorCodes.cs` — stable string constants, never renamed.
`SupportedValues` holds the human-readable accepted-value hints. Add a new code
+ hint here, then use it in a validator or service.

## Validators

`Common/Validation/` — `FieldRules` (primitive checks: ISBN, year, email,
required, enum) and `BookValidator` / `MemberValidator` / `BookCopyValidator`
operating on a `…Candidate` struct, so an API request and an Excel row run the
exact same rules.
