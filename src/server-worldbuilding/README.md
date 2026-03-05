Para ejecución: pnpm run dev

# Express - Typescript
--------------------------------------------------------------------------------------------------
| CU-## | Nombre del caso de uso            | Actor principal   | Módulo                | Estado |
|-------|-----------------------------------|------------------ | --------------------- | ------ |
| CU-06	| Gestionar Colaboradores           | Escritor          | Gestión               |   ❌  |
| CU-07	| Configurar Privacidad	            | Escritor          | Gestión               |   ❌  |
| CU-08	| Editar Manuscrito (Texto Rico)    | Editor / Escritor | Escritura (MongoDB)   |   ❌  |
| CU-09	| Gestionar Wiki (Nodos)            | Editor / Escritor	| Worldbuilding (Neo4j) |   ❌  |
| CU-10	| Visualizar Relaciones (Grafo)	    | Lector / Editor   | Worldbuilding (Neo4j) |   ❌  |
| CU-15 | Gestionar Usuarios (Ban/Roles)    | Administrador     | Admin                 |   ❌  |

## CU-06 Gestionar Colaboradores
### Descripción
El Escritor invita a otros usuarios a participar en la obra, asignándoles roles específicos (Editor) para permitir la co-autoría o corrección.
### Precondiciones
El usuario debe ser el Propietario del proyecto (Role Owner).
### Postcondiciones
POS-1: El usuario invitado ahora puede ver y editar el proyecto en su propio Dashboard.
### Flujo normal
1.	El Sistema muestra la lista actual de colaboradores.
2.	El Escritor ingresa el correo electrónico del usuario a invitar.
3.	El Sistema busca al usuario en la base de datos de identidad (SQL Server).
4.	El Sistema valida que el usuario exista (FA-01).
5.	El Escritor selecciona el rol a asignar (ej. "Editor").
6.	El Sistema registra el permiso en la tabla de Roles del proyecto.
7.	El Sistema envía una notificación al usuario invitado.
8.	Termina CU.

