# Claude Code Prompt — DevPad C# & SQL Formatter

Paste everything below this line directly into Claude Code:

---

## Context

I have a DevPad desktop application built with Avalonia UI (.NET 8).
It currently has these tools: JSON, XML, JWT, Base64, SQL.
Each tool follows the same pattern:
- Left panel: raw input (TextBox or AvaloniaEdit)
- Right panel: formatted output (AvaloniaEdit with syntax highlighting)
- Top buttons: tool-specific actions (Format, Copy, Copy Output, Clear)

## Task

Add a new **C#** formatter tool to DevPad, and improve the existing **SQL** formatter tool.
The goal is to restore formatting on code that has been mangled by copy-paste through Microsoft Teams or similar tools.

---

## PART 1 — Add C# Formatter Tool

### 1.1 Install NuGet Package
Add `CSharpier.Core` to the project:
```
dotnet add package CSharpier.Core
```

### 1.2 Create CSharp Tool View
Create a new view file: `Views/CSharpView.axaml` and `Views/CSharpView.axaml.cs`
Follow the exact same structure as the existing JSON or XML tool view.

**Input panel:**
- AvaloniaEdit TextEditor
- Syntax highlighting: C# (use AvaloniaEdit's built-in C# highlighting definition)
- Placeholder hint: "Paste your C# code here..."

**Output panel:**
- AvaloniaEdit TextEditor (read-only)
- Syntax highlighting: C#
- Line numbers: enabled
- Same styling as JSON output panel

**Top action buttons:**
- **Format** (primary button style) — triggers CSharpier formatting
- **Copy** — copies input
- **Copy Output** — copies formatted output  
- **Clear** — clears both panels

### 1.3 Create CSharp ViewModel
Create `ViewModels/CSharpViewModel.cs`

Formatting logic using CSharpier:
```csharp
using CSharpier;

public async Task FormatAsync()
{
    try
    {
        var result = await CodeFormatter.FormatAsync(
            InputText,
            new CodeFormatterOptions
            {
                IndentSize = 4,
                UseTabs = false,
                Width = 120,
                EndOfLine = EndOfLine.LF
            }
        );

        if (result.CompilationErrors.Any())
        {
            OutputText = "// Format error — check your C# syntax\n\n" 
                        + string.Join("\n", result.CompilationErrors.Select(e => $"// Line {e.GetLineSpan().StartLinePosition.Line + 1}: {e.GetMessage()}"));
        }
        else
        {
            OutputText = result.Code;
        }
    }
    catch (Exception ex)
    {
        OutputText = $"// Unexpected error: {ex.Message}";
    }
}
```

**Auto-format on paste:**
Wire up the input TextEditor's TextChanged event.
After 600ms debounce (user stopped typing), auto-trigger FormatAsync().
This means user pastes mangled code → formatted output appears automatically.

### 1.4 Register C# Tool in Sidebar
In `MainViewModel.cs` or wherever tools are registered:
- Add CSharp tool entry
- Icon: use `</>` or `{ }` styled icon similar to existing tools
- Accent color for C# tool: `#9CDCFE` (VS Code C# blue)
- Label: "C#"
- Position: after SQL in the sidebar list

### 1.5 Sidebar Icon for C#
Use this SVG path or text-based icon in the sidebar:
```xml
<!-- Simple C# icon using text -->
<TextBlock Text="C#" FontWeight="Bold" FontSize="12"/>
```
Or use an appropriate path icon if your icon system supports it.

---

## PART 2 — Improve Existing SQL Formatter

### 2.1 Install or Verify SQL Formatter Package
Check if project already has a SQL formatting library.
If not, add:
```
dotnet add package SqlFormatter.Net
```
Alternative: `Microsoft.SqlServer.TransactSql.ScriptDom` (heavier but more accurate)
Prefer `SqlFormatter.Net` for simplicity.

### 2.2 SQL Formatting Style Rules
Apply these exact formatting rules to the SQL output:

**Keywords:** ALL UPPERCASE
```sql
-- Wrong
select id, name from users where id = 1

-- Correct  
SELECT
    id,
    name
FROM
    users
WHERE
    id = 1
```

**Clause layout — each on its own line:**
```sql
SELECT
    u.id,
    u.name,
    u.email,
    a.city,
    a.country
FROM
    users u
    INNER JOIN addresses a ON a.user_id = u.id
WHERE
    u.is_active = 1
    AND u.created_at >= '2024-01-01'
GROUP BY
    u.id,
    u.name
ORDER BY
    u.name ASC
```

**Subqueries — indented:**
```sql
SELECT
    u.id,
    u.name
FROM
    users u
WHERE
    u.id IN (
        SELECT
            order_user_id
        FROM
            orders
        WHERE
            status = 'ACTIVE'
    )
```

**INSERT statements:**
```sql
INSERT INTO users (
    id,
    name,
    email,
    created_at
)
VALUES (
    1,
    'Yasin',
    'yasin@example.com',
    GETDATE()
)
```

**UPDATE statements:**
```sql
UPDATE
    users
SET
    name = 'Yasin',
    email = 'yasin@example.com',
    updated_at = GETDATE()
WHERE
    id = 1
```

### 2.3 SQL Formatter Implementation
In `ViewModels/SqlViewModel.cs`, update the Format method:

```csharp
private string FormatSql(string rawSql)
{
    // Using SqlFormatter.Net
    var formatter = new SqlFormatter.SqlFormatter();
    var formatted = formatter.Format(rawSql, new SqlFormatter.SqlFormatterOptions
    {
        Uppercase = true,           // Keywords uppercase
        LinesBetweenQueries = 2,    // Blank lines between multiple queries
        IndentStyle = IndentStyle.Standard,
        IndentWidth = 4
    });
    return formatted;
}
```

If SqlFormatter.Net doesn't support all rules above, implement a post-processor:
```csharp
private string PostProcessSql(string formatted)
{
    // Ensure common keywords are uppercase
    var keywords = new[] { 
        "select", "from", "where", "join", "inner join", "left join", 
        "right join", "on", "group by", "order by", "having", 
        "insert into", "update", "delete", "set", "values",
        "and", "or", "not", "in", "exists", "between", "like",
        "null", "is null", "is not null", "asc", "desc",
        "distinct", "top", "count", "sum", "avg", "min", "max",
        "case", "when", "then", "else", "end", "as", "with"
    };
    
    var result = formatted;
    foreach (var keyword in keywords)
    {
        result = Regex.Replace(
            result, 
            $@"\b{keyword}\b", 
            keyword.ToUpper(), 
            RegexOptions.IgnoreCase
        );
    }
    return result;
}
```

### 2.4 SQL Tool Buttons Update
Ensure SQL tool has these buttons:
- **Format** (primary) — formats with rules above
- **Copy** — copies raw input
- **Copy Output** — copies formatted SQL
- **Clear** — clears both

---

## PART 3 — Shared Styling Requirements

### 3.1 C# Syntax Highlighting Colors
For AvaloniaEdit C# highlighting, use these colors (VS Code Dark+ inspired):
- Keywords (`class`, `public`, `void`, `async`, etc.): `#569CD6` (blue)
- Strings: `#CE9178` (orange-brown)
- Comments: `#6A9955` (green)
- Numbers: `#B5CEA8` (light green)
- Method names: `#DCDCAA` (yellow)
- Types/Classes: `#4EC9B0` (teal)
- Properties: `#9CDCFE` (light blue)
- Punctuation/operators: `#D4D4D4` (white-gray)

Apply these via AvaloniaEdit's `IHighlightingDefinition` or load an `.xshd` highlighting file.

### 3.2 Auto-Format Debounce Pattern (reuse across tools)
Create a shared helper if not already exists:
```csharp
public class DebounceHelper
{
    private CancellationTokenSource? _cts;
    
    public async Task Debounce(Func<Task> action, int milliseconds = 600)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        try
        {
            await Task.Delay(milliseconds, _cts.Token);
            await action();
        }
        catch (TaskCanceledException)
        {
            // Debounced — do nothing
        }
    }
}
```

### 3.3 Error State Styling
When formatting fails (invalid C# / invalid SQL):
- Output panel background: subtle red tint `#2D1B1B`
- Output border: `#DA3633`  
- Error message displayed in output with `//` comment prefix for C#, `--` for SQL
- After user starts editing input again, error state clears immediately

---

## PART 4 — Sidebar Update

After adding C# tool, sidebar order should be:
1. JSON (accent: `#58A6FF` blue)
2. XML (accent: `#F0883E` orange)
3. JWT (accent: `#BC8CFF` purple)
4. Base64 (accent: `#3FB950` green)
5. SQL (accent: `#FFA657` amber)
6. **C#** (accent: `#9CDCFE` light blue) ← NEW

Each tool's accent color should appear on:
- Left border of selected sidebar item
- Active tab underline (TEXT/TREE or similar)
- Primary action button background when hovered

---

## Testing Checklist

After implementation, test with these cases:

**C# test — paste this mangled code and verify output:**
```
public class UserService{private readonly IUserRepository _repo;public UserService(IUserRepository repo){_repo=repo;}public async Task<User> GetUserAsync(int id){var user=await _repo.GetByIdAsync(id);if(user==null){throw new NotFoundException($"User {id} not found");}return user;}}
```

Expected: properly formatted class with Allman braces, proper indentation, spacing.

**SQL test — paste this mangled query and verify output:**
```
select u.id,u.name,u.email,a.city from users u inner join addresses a on a.user_id=u.id where u.is_active=1 and u.created_at>='2024-01-01' order by u.name asc
```

Expected: uppercase keywords, each clause on own line, columns indented.

---

## Important Notes

- Do NOT break any existing tools (JSON, XML, JWT, Base64)
- Follow existing MVVM pattern exactly as other tools do
- C# tool should feel identical in UX to JSON tool — same layout, same button behavior
- All formatting is LOCAL — no API calls, no internet required
- If CSharpier package has issues, fallback to basic indentation cleanup rather than crashing
