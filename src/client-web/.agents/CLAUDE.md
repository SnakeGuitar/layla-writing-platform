# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# .NET commands
dotnet run                    # Run the application
dotnet watch                  # Hot reload on changes
dotnet build                  # Build

# Frontend assets
pnpm install                  # Install Node.js dependencies
npx @tailwindcss/cli -i ./UI/Styles/Styles.css -o ./wwwroot/styles/styles.css  # Compile Tailwind CSS
npx tsc wwwroot/js/chartInterop.ts --target ES6 --module none --outDir wwwroot/js  # Compile TypeScript
```

## Architecture Overview

**Stack:** Blazor Web App (.NET 9) + Razor Components (Interactive Server render mode) + Tailwind CSS v4 + TypeScript

**Project Structure:**
- `UI/` - Razor pages, components (Blazor UI) and styles
- `Services/` - Business logic and HTTP clients
  - `ApiClient.cs` - Centralized HTTP client with automatic JSON serialization and error handling
  - `AuthService.cs`, `ProjectService.cs`, `VoiceService.cs`, `PresenceService.cs` - Domain services
- `wwwroot/` - Static assets (JS interop via TypeScript, compiled CSS)
- `Models/`, `Schemas/`, `Helpers/` - Shared types and utilities

**API Configuration:**
- Requires `ApiUrls:Acceso` and `ApiUrls:Core` in appsettings or user secrets
- Uses SignalR for real-time communication (voice rooms, presence)

**Key Patterns:**
- All HTTP requests go through `ApiClient` with bearer token auth support
- TypeScript interop exposed via `window.createBarChart` for Chart.js visualizations
- Services registered as scoped dependencies in DI container
