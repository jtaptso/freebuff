# Design Document - Corsuite Administration Web Tool

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      Blazor Web App (.NET 10)                   │
│  ┌─────────────────────┐  ┌─────────────────────────────────┐   │
│  │   Client (Blazor)   │  │        Server (ASP.NET Core)    │   │
│  │   ─────────────────  │  │        ─────────────────────── │   │
│  │   - Home Page       │◄─┤   - REST API Endpoints          │   │
│  │   - Module Cards    │  │   - SAP B1 Integration Service  │   │
│  │   - Forms/Dialogs   │  │   - File Management Service     │   │
│  │   - Toast Alerts    │  │   - Authentication Handler      │   │
│  └─────────────────────┘  └─────────────────────────────────┘   │
│                                    │                            │
│                                    ▼                            │
│                    ┌─────────────────────────────────┐         │
│                    │      SAP Business One           │         │
│                    │   - DI API (SAPbobsCOM)         │         │
│                    │   - Add-on Registry             │         │
│                    │   - Module Storage              │         │
│                    └─────────────────────────────────┘         │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Project Structure

```
CorsuiteAdmin/
├── CorsuiteAdmin.Web/              # Blazor Web App
│   ├── Pages/
│   │   ├── Index.razor             # Home - Module List
│   │   ├── AddModule.razor         # Add DLL Form
│   │   ├── EditModule.razor        # Edit DLL Form
│   │   ├── ModuleDetails.razor     # Module Details View
│   │   └── _Host.cshtml            # App Host
│   ├── Components/
│   │   ├── ModuleCard.razor        # Module display card
│   │   ├── ConfirmDialog.razor     # Confirmation modal
│   │   ├── SearchBar.razor         # Search/filter component
│   │   └── ToastNotification.razor # Toast messages
│   ├── Services/
│   │   └── ModuleService.cs        # API client service
│   ├── wwwroot/
│   │   ├── css/
│   │   │   └── app.css             # Custom styles
│   │   └── images/
│   └── Program.cs
│
├── CorsuiteAdmin.Api/              # ASP.NET Core Web API
│   ├── Controllers/
│   │   └── ModulesController.cs    # CRUD endpoints
│   ├── Services/
│   │   ├── ISapB1Service.cs        # SAP B1 interface
│   │   ├── SapB1Service.cs         # SAP B1 implementation
│   │   └── IModuleService.cs       # Module management interface
│   ├── Models/
│   │   ├── DllModule.cs            # Module entity
│   │   └── SapConnectionInfo.cs    # Connection config
│   ├── Data/
│   │   └── AppDbContext.cs         # EF Core DbContext
│   └── Program.cs
│
└── CorsuiteAdmin.Shared/           # Shared models
    └── DTOs/
        └── ModuleDto.cs            # Data transfer objects
```

---

## 3. UI/UX Design

### 3.1 Color Palette

| Role | Color | Hex |
|------|-------|-----|
| Primary | Deep Blue | #1E3A5F |
| Secondary | Teal | #2A9D8F |
| Accent | Orange | #E76F51 |
| Background | Light Gray | #F8F9FA |
| Surface | White | #FFFFFF |
| Text Primary | Dark Gray | #212529 |
| Text Secondary | Medium Gray | #6C757D |
| Success | Green | #28A745 |
| Warning | Yellow | #FFC107 |
| Error | Red | #DC3545 |

### 3.2 Typography

- **Font Family:** 'Segoe UI', system-ui, sans-serif
- **Headings:**
  - H1: 2rem (32px), Bold
  - H2: 1.5rem (24px), SemiBold
  - H3: 1.25rem (20px), Medium
- **Body:** 1rem (16px), Regular
- **Small:** 0.875rem (14px)

### 3.3 Layout Structure

```
┌────────────────────────────────────────────────────────────┐
│  HEADER (Navbar)                                           │
│  [Logo] Corsuite Admin    [Connection Status] [Settings]  │
├────────────────────────────────────────────────────────────┤
│  TOOLBAR                                                   │
│  [Search Bar............] [+ Add DLL] [⟳ Refresh]         │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  MODULE GRID                                               │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐       │
│  │ DLL Card     │ │ DLL Card     │ │ DLL Card     │       │
│  │ [Icon]       │ │ [Icon]       │ │ [Icon]       │       │
│  │ Name         │ │ Name         │ │ Name         │       │
│  │ Version      │ │ Version      │ │ Version      │       │
│  │ [Edit][Del]  │ │ [Edit][Del]  │ │ [Edit][Del]  │       │
│  └──────────────┘ └──────────────┘ └──────────────┘       │
│                                                            │
├────────────────────────────────────────────────────────────┤
│  FOOTER                                                    │
│  Total: X modules | Last updated: DateTime                 │
└────────────────────────────────────────────────────────────┘
```

### 3.4 Component Specifications

#### Module Card
- **Size:** 280px x 180px
- **Border:** 1px solid #DEE2E6
- **Border Radius:** 8px
- **Shadow:** 0 2px 4px rgba(0,0,0,0.1)
- **Hover:** Scale(1.02), shadow increase
- **Content:**
  - DLL Icon (64x64)
  - Module Name (truncate at 25 chars)
  - Version badge
  - Status indicator (colored dot)
  - Action buttons (Edit, Delete)

#### Add/Edit Modal
- **Width:** 500px (centered)
- **Background:** White with backdrop blur
- **Fields:**
  - File Upload (drag & drop zone)
  - Module Name (auto-populated from DLL)
  - Version (auto-populated from DLL metadata)
  - Description (optional textarea)
- **Actions:** Cancel, Save

#### Confirmation Dialog
- **Type:** Modal overlay
- **Content:** Icon + Title + Message + Buttons
- **Buttons:** Cancel (secondary) + Confirm (primary danger)

---

## 4. API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/modules | Get all modules |
| GET | /api/modules/{id} | Get single module |
| POST | /api/modules | Add new module |
| PUT | /api/modules/{id} | Update module |
| DELETE | /api/modules/{id} | Delete module |
| GET | /api/modules/search?q={query} | Search modules |
| POST | /api/modules/connect | Test SAP B1 connection |

---

## 5. SAP B1 Integration Design

### 5.1 Connection Configuration

```csharp
public class SapConnectionInfo
{
    public string Server { get; set; }      // Server hostname/IP
    public string CompanyDB { get; set; }   // Database name
    public string UserName { get; set; }    // SAP B1 username
    public string Password { get; set; }    // SAP B1 password
    public DbServerType DbType { get; set; } // SQL Server, HANA, etc.
    public string LicenseServer { get; set; } // Optional
}
```

### 5.2 Module Management Strategy

Since SAP B1 doesn't have a direct API for Corsuite modules, we will:
1. Store module metadata in a local SQLite database
2. Store actual DLL files in a designated folder
3. Use SAP B1 DI API to verify connection and access
4. Maintain a registration table for module tracking

---

## 6. Data Flow

```
User Action (Blazor)
       │
       ▼
HTTP Request (JSON)
       │
       ▼
API Controller
       │
       ▼
Service Layer
       │
       ├──► File System (DLL storage)
       │
       └──► Database (Metadata)
       │
       ▼
Response (JSON)
       │
       ▼
UI Update (Blazor)
```

---

## 7. Security Considerations

1. **Authentication:** Session-based with encrypted cookies
2. **Password Storage:** Hash passwords, never store plain text
3. **File Upload:** Validate file type (.dll only), scan for malware
4. **API Security:** CORS policy, rate limiting
5. **SAP Credentials:** Encrypted at rest, transmitted over HTTPS

---

## 8. Acceptance Criteria

| ID | Criteria | Validation |
|----|----------|------------|
| AC-1 | Home page loads within 2 seconds | Manual test |
| AC-2 | Module list displays all DLLs from storage | Visual check |
| AC-3 | Add module accepts .dll files only | Upload test |
| AC-4 | Edit module updates metadata correctly | Update & verify |
| AC-5 | Delete module removes file and metadata | Delete & verify |
| AC-6 | Search filters modules in real-time | Type & observe |
| AC-7 | Toast notifications appear for all actions | Trigger actions |
| AC-8 | Responsive design works on 1024px+ screens | Resize browser |

---

## 9. Dependencies

```xml
<!-- Server (Api) -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.0.0" />

<!-- Client (Web) -->
<PackageReference Include="Blazorise" Version="2.0.0" />
<PackageReference Include="Blazorise.Bootstrap5" Version="2.0.0" />
```
