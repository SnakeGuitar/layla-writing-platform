# Arquitectura: Colaboración en Tiempo Real y Wiki Dinámica

Este documento detalla la implementación del sistema colaborativo, justificando las extensiones a la base de datos actual para cumplir con los nuevos requisitos (Control de Versiones y Tokenización Dinámica).

## 1. Justificación de Extensiones a la Base de Datos

Para cumplir con el prompt original sin romper el ecosistema actual, extendemos los modelos existentes:

### A. Extensiones en MongoDB (`server-worldbuilding`)
1. **Colección Nueva: `ChapterVersions`**: 
   * **Por qué**: El esquema actual `Manuscript.model.ts` almacena el `content` del capítulo usando *Last-Write-Wins*. El nuevo requisito exige un **Historial y Control de Versiones** con previsualización de diferencias tipo Git. Para esto, necesitamos una colección inmutable `ChapterVersion` que almacene snapshots (hitos) y deltas (autoguardados) sin sobrecargar el documento principal del manuscrito.
2. **Modificación a `WikiEntry.model.ts`**:
   * **Por qué**: Se añade el campo `aliases: [{ type: String }]`. El algoritmo Aho-Corasick necesita saber todas las formas en las que un personaje puede ser mencionado en el texto (ej. "Menudo", "El Menudo", "Jefe"). El campo actual `tags` tiene un propósito semántico, por lo que `aliases` es específico para el motor de tokenización.

### B. Extensiones en SQL Server (`server-core`)
1. **Tabla Nueva: `OutboxMessages`**:
   * **Por qué**: Para el sistema de **Hot Eviction**. Cuando a un usuario se le revoca el rol en la tabla `ProjectRoles`, necesitamos garantizar que el servicio SignalR lo desconecte. Usaremos el patrón Outbox en EF Core para guardar el evento en la misma transacción SQL y que un worker lo envíe a RabbitMQ de forma segura.

---

## 2. Modelos de Base de Datos Actualizados

### MongoDB (Mongoose)

```typescript
// src/server-worldbuilding/src/models/WikiEntry.model.ts (Actualizado)
import { Schema, model } from "mongoose";
import type { IWikiEntry } from "@/interfaces/wiki/IWikiEntry";

const WikiEntrySchema = new Schema<IWikiEntry>({
    projectId: { type: String, required: true, index: true },
    entityId: { type: String, required: true, unique: true, index: true },
    name: { type: String, required: true, maxlength: 200 },
    aliases: [{ type: String }], // NUEVO: Para el tokenizador Aho-Corasick
    entityType: { type: String, required: true },
    description: { type: String, default: "" },
    tags: [{ type: String }],
    neo4jSynced: { type: Boolean, default: false }
}, { timestamps: true });

export const WikiEntryModel = model<IWikiEntry>("WikiEntry", WikiEntrySchema);

// src/server-worldbuilding/src/models/ChapterVersion.model.ts (NUEVO)
import { Schema, model } from "mongoose";

const ChapterVersionSchema = new Schema({
    chapterId: { type: String, required: true, index: true },
    projectId: { type: String, required: true },
    content: { type: String, required: true }, // Texto en Delta o RTF
    isMilestone: { type: Boolean, default: false }, // true si es un Snapshot completo
    createdBy: { type: String, required: true }, // AppUserId de quien generó el guardado
}, { timestamps: true });

export const ChapterVersionModel = model("ChapterVersion", ChapterVersionSchema);
```

### SQL Server (EF Core)

```csharp
// Layla.Infrastructure/Data/ApplicationDbContext.cs (Extensión)
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty; // ej: "ClientEvicted"
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; } = false;
}

// Se añade DbSet<OutboxMessage> a ApplicationDbContext.
```

---

## 3. Lógica de Controladores y SignalR

### API Express (`server-worldbuilding`)

```typescript
// routes/wiki.routes.ts
import { Router } from "express";
import { WikiEntryModel } from "@/models/WikiEntry.model";

const router = Router();

// Caché en caliente para el Tokenizador del Cliente
router.get("/:projectId/detectable", async (req, res) => {
    const entries = await WikiEntryModel.find(
        { projectId: req.params.projectId },
        "entityId name aliases entityType"
    ).lean();
    
    // Formato optimizado para Aho-Corasick en cliente
    const detectable = entries.map(e => ({
        id: e.entityId,
        mainToken: e.name,
        aliases: e.aliases || [],
        type: e.entityType
    }));
    
    res.json(detectable);
});

// Autoguardado con Debounce e Historial
router.put("/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/autosave", async (req, res) => {
    const { content, mentions, userId } = req.body;
    
    // 1. Actualiza el documento principal (Last-Write-Wins para lectura rápida)
    await ManuscriptModel.updateOne(
        { "chapters.chapterId": req.params.chapterId },
        { 
            $set: { 
                "chapters.$.content": content,
                "chapters.$.mentions": mentions 
            } 
        }
    );

    // 2. Guarda el delta en el historial de versiones
    await ChapterVersionModel.create({
        chapterId: req.params.chapterId,
        projectId: req.params.projectId,
        content: content,
        isMilestone: false,
        createdBy: userId
    });

    res.status(200).send();
});
```

### SignalR Hub (`server-core`)

```csharp
// Layla.Api/Hubs/ManuscriptHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class ManuscriptHub : Hub
{
    // Colaboración en tiempo real (cursores efímeros)
    public async Task JoinChapterGroup(string chapterId)
    {
        // Se asume validación de roles previa...
        await Groups.AddToGroupAsync(Context.ConnectionId, chapterId);
    }

    public async Task SendCursorMoved(string chapterId, int positionOffset)
    {
        await Clients.OthersInGroup(chapterId).SendAsync("OnCursorMoved", Context.UserIdentifier, positionOffset);
    }
}
```

---

## 4. Arquitectura de Clientes (.NET 9 Parity)

Para garantizar la **paridad absoluta** entre WPF y Blazor Server, crearemos una librería de clases compartida.

### `Layla.Client.Shared` (.NET 9 Class Library)

1. **`WikiTokenizer` (Aho-Corasick)**: Motor de búsqueda en C# puro. Se inicializa con la respuesta de `/api/wiki/:projectId/detectable`.
2. **`ManuscriptHubClient`**: Wrapper sobre `HubConnection` de SignalR que gestiona las reconexiones y expone eventos C# puros (`CursorMoved`, `ClientEvicted`, `WikiEntitiesChanged`).
3. **Servicios HTTP**: `ManuscriptApiService` inyectable por DI tanto en el contenedor de WPF (`ServiceLocator`) como en el de Blazor.

### Implementación en WPF (Escritorio)
- **Tokenización visual**: Se usará el evento asíncrono sobre el `RichTextBox` que, al escribir la palabra clave completa + espacio (para evitar re-renderizados costosos), ejecutará `WikiTokenizer.FindMentions()` en segundo plano y envolverá el texto en un elemento `Hyperlink` interactivo.
- **CRUD UI**: Uso de diálogos nativos del sistema (o ventanas personalizadas con FluentWPF) para la gestión crítica (ej. expulsar usuario).
- **Control de Versiones**: Un panel lateral (`SidebarVersionHistory`) dibujará un *Timeline* leyendo de `/api/chapters/:chapterId/versions` y usará una librería de Diff de C# para mostrar rojo/verde en las diferencias del `RichTextBox`.

### Implementación en Blazor Server (Web)
- **Tokenización visual**: Se usará un JSInterop que inyecte una extensión personalizada en Quill o TipTap. Al recibir el evento `WikiEntitiesChanged` de SignalR, Blazor invocará una función JS que forzará al editor a re-escanear el DOM actual basándose en el arreglo actualizado de aliases.
- **CRUD UI**: Uso de componentes de Modal de Blazor compartiendo la misma lógica de ViewModel subyacente que WPF.
