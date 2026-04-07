# PRD - Corsuite Administration Web Tool

## 1. Project Overview

**Project Name:** Corsuite Administration Web (CAW)

**Project Type:** Web Application (Blazor + .NET 10)

**Core Functionality:** A web-based replacement for the Corsuite Administration Desktop tool that allows users to manage .dll modules in SAP Business One. Provides CRUD operations (Create, Read, Update, Delete) for DLL modules with version tracking.

**Target Users:** SAP B1 Administrators, IT Managers, System Administrators who manage Corsuite modules and SAP B1 add-ons.

---

## 2. Problem Statement

The current Corsuite Administration tool is a desktop application. Users need a modern web-based interface to:
- View all currently loaded DLL modules in Corsuite with their names and versions
- Add new DLL modules to Corsuite
- Update existing DLL modules
- Delete DLL modules from Corsuite
- Access this functionality from any device without installing desktop software

---

## 3. User Stories

| ID | User Story | Priority |
|----|------------|----------|
| US-01 | As an admin, I want to view a list of all DLL modules in Corsuite with name and version so I can see what's currently installed | High |
| US-02 | As an admin, I want to add a new DLL module to Corsuite by uploading a file so I can extend functionality | High |
| US-03 | As an admin, I want to update an existing DLL module by uploading a new version so I can patch or upgrade modules | High |
| US-04 | As an admin, I want to delete a DLL module from Corsuite so I can remove unwanted or obsolete modules | High |
| US-05 | As an admin, I want to view detailed information about a DLL module so I can verify its properties | High |
| US-06 | As an admin, I want to search/filter the DLL list so I can quickly find specific modules | Medium |
| US-07 | As an admin, I want to see operation confirmations and errors so I know if actions succeeded | Medium |
| US-08 | As an admin, I want a responsive UI that works on desktop and tablet | Low |

---

## 4. Functional Requirements

### 4.1 Module Management

| Req ID | Requirement | Description |
|--------|-------------|-------------|
| FR-01 | List Modules | Display all DLL modules in Corsuite with Name, Version, Status, Date Added |
| FR-02 | Add Module | Upload and register a new DLL module to Corsuite |
| FR-03 | Update Module | Replace an existing DLL with a new version |
| FR-04 | Delete Module | Unregister and remove a DLL from Corsuite |
| FR-05 | View Details | Show detailed module information (Name, Version, Size, Path, Dependencies) |
| FR-06 | Search/Filter | Filter modules by name or version |

### 4.2 SAP B1 Integration

| Req ID | Requirement | Description |
|--------|-------------|-------------|
| FR-07 | Connect to SAP B1 | Establish connection to SAP Business One company database |
| FR-08 | Authentication | Support SAP B1 credentials for connection |
| FR-09 | Get Module List | Retrieve all registered Corsuite DLL modules |
| FR-10 | Register Module | Add new module to SAP B1 registration |
| FR-11 | Unregister Module | Remove module from SAP B1 registration |

### 4.3 User Interface

| Req ID | Requirement | Description |
|--------|-------------|-------------|
| FR-12 | Dashboard | Home page showing module list |
| FR-13 | Module Cards | Visual cards displaying module info |
| FR-14 | Upload Form | Form for uploading new DLL files |
| FR-15 | Confirmation Dialogs | Confirm before delete/update operations |
| FR-16 | Toast Notifications | Show success/error messages |
| FR-17 | Loading States | Show loading indicators during operations |

### 4.4 Data Model

**Module Entity:**
```
- Id: Guid (Primary Key)
- Name: string (required, max 100 chars)
- Version: string (required, format: x.x.x.x)
- FilePath: string (required)
- FileSize: long (bytes)
- Description: string (optional, max 500 chars)
- Status: enum (Active, Inactive, Error)
- AddedDate: DateTime
- ModifiedDate: DateTime
- AddedBy: string
```

---

## 5. Non-Functional Requirements

| Category | Requirement |
|----------|-------------|
| Performance | Page load under 2 seconds |
| Usability | Intuitive navigation, clear feedback |
| Security | Encrypted connection to SAP B1, secure credential storage |
| Compatibility | Modern browsers (Chrome, Firefox, Edge, Safari) |
| Responsiveness | Mobile-friendly layout |

---

## 6. Technical Stack

- **Frontend:** Blazor Web App (.NET 10)
- **Backend:** ASP.NET Core Web API
- **Database:** SQLite (for local cache) + SAP B1 DI API
- **Authentication:** SAP B1 Credentials + Session-based auth
- **UI Framework:** Bootstrap 5 + Blazorise

---

## 7. Out of Scope (v1.0)

- User management/role-based access
- Module dependency management
- Batch operations
- Audit logging
- Backup/Restore functionality

---

## 8. Success Criteria

1. User can view all DLL modules currently in Corsuite
2. User can add a new DLL module successfully
3. User can update an existing DLL module
4. User can delete a DLL module
5. All operations show appropriate feedback
6. Application connects to SAP B1 and retrieves module data
