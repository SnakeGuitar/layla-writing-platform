```bash
dotnet run          // Para ejecutar
dotner watch        // Monitorear cambios
dotnet build        // Compilación


// Instalar paquetes via pnpm
pnpm install
// Actualizar archivo estatico de estilos CSS 
npx @tailwindcss/cli -i ./UI/Styles/Styles.css -o ./wwwroot/styles/styles.css
// Precompilar Typescript a Javascript
npx tsc wwwroot/js/chartInterop.ts --target ES6 --outDir wwwroot/js
npx tsc wwwroot/js/chartInterop.ts --target ES6 --module none --outDir wwwroot/js
```

# Blazor - C#, Tailwind
------------------------------------------------------------------------------------------------------
|  IU#  | Nombre de IU                      | Casos de uso involucrados | Tipos de usuario  | Estado |
|-------|-----------------------------------|-------------------------- | ----------------- | ------ |
| IU-01	| Public feed home                  |                           |                   |   ❌  |
| IU-02	| Story preview                     |                           |                   |   ❌  |
| IU-03	| Immersive reader                  |                           |                   |   ❌  |
| IU-04	| Web login                         |                           |                   |   ❌  |
| IU-05	| Admin dashboard                   |                           |                   |   ❌  |
| IU-06 | User management                   |                           |                   |   ❌  |
-----------------------------------------------------------------------------------------------------
