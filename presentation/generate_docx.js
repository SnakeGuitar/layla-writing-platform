const fs = require('fs');
const path = require('path');
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, PageOrientation, LevelFormat,
  TabStopType, TabStopPosition, HeadingLevel, BorderStyle, WidthType,
  ShadingType, PageNumber, PageBreak, ExternalHyperlink,
} = require('docx');

// ====== Helpers ======
const border = { style: BorderStyle.SINGLE, size: 4, color: "BFBFBF" };
const borders = { top: border, bottom: border, left: border, right: border };

const ACCENT = "5B6D7A";   // muted slate blue
const TEXT_HEAD = "1F2933";
const TEXT_MUTED = "5F6770";
const TABLE_HEAD_BG = "E8ECF0";
const CODE_BG = "F4F4F2";

// Default font + size set via styles.default.document below
const font = "Arial";

function P(text, opts = {}) {
  return new Paragraph({
    spacing: { after: opts.after ?? 120, before: opts.before ?? 0, line: 320 },
    alignment: opts.align ?? AlignmentType.JUSTIFIED,
    children: Array.isArray(text)
      ? text
      : [new TextRun({ text, font, size: 22, color: TEXT_HEAD, ...opts.run })],
  });
}

function H1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    spacing: { before: 360, after: 200 },
    children: [new TextRun({ text, font, size: 36, bold: true, color: TEXT_HEAD })],
  });
}

function H2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    spacing: { before: 280, after: 160 },
    children: [new TextRun({ text, font, size: 28, bold: true, color: TEXT_HEAD })],
  });
}

function H3(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    spacing: { before: 220, after: 120 },
    children: [new TextRun({ text, font, size: 24, bold: true, color: ACCENT })],
  });
}

function bullet(text) {
  return new Paragraph({
    numbering: { reference: "bullets", level: 0 },
    spacing: { after: 80, line: 300 },
    children: Array.isArray(text)
      ? text
      : [new TextRun({ text, font, size: 22, color: TEXT_HEAD })],
  });
}

function numbered(text, ref = "numbers") {
  return new Paragraph({
    numbering: { reference: ref, level: 0 },
    spacing: { after: 80, line: 300 },
    children: Array.isArray(text)
      ? text
      : [new TextRun({ text, font, size: 22, color: TEXT_HEAD })],
  });
}

function code(text) {
  const lines = text.split('\n');
  return lines.map((line, i) =>
    new Paragraph({
      spacing: { after: i === lines.length - 1 ? 160 : 0, line: 260 },
      shading: { fill: CODE_BG, type: ShadingType.CLEAR },
      indent: { left: 360, right: 360 },
      children: [new TextRun({ text: line || ' ', font: "Consolas", size: 18, color: TEXT_HEAD })],
    })
  );
}

function pageBreak() {
  return new Paragraph({ children: [new PageBreak()] });
}

function mkCell(content, opts = {}) {
  const isHeader = opts.header ?? false;
  const text = Array.isArray(content) ? content : [content];
  const children = text.map(t =>
    typeof t === 'string'
      ? new Paragraph({
          spacing: { after: 0, line: 280 },
          alignment: opts.align ?? AlignmentType.LEFT,
          children: [new TextRun({
            text: t,
            font,
            size: 20,
            bold: isHeader || opts.bold,
            color: isHeader ? TEXT_HEAD : TEXT_HEAD,
          })],
        })
      : t
  );
  return new TableCell({
    borders,
    width: { size: opts.width, type: WidthType.DXA },
    shading: isHeader ? { fill: TABLE_HEAD_BG, type: ShadingType.CLEAR } : undefined,
    margins: { top: 100, bottom: 100, left: 140, right: 140 },
    children,
  });
}

function table(columnWidths, rows) {
  const totalWidth = columnWidths.reduce((a, b) => a + b, 0);
  return new Table({
    width: { size: totalWidth, type: WidthType.DXA },
    columnWidths,
    rows: rows.map((row, i) =>
      new TableRow({
        children: row.map((cell, j) =>
          mkCell(cell, { header: i === 0, width: columnWidths[j] })
        ),
      })
    ),
  });
}

// ====== Document content ======

const titlePage = [
  new Paragraph({
    spacing: { before: 2400, after: 200 },
    alignment: AlignmentType.CENTER,
    children: [new TextRun({ text: "Layla", font, size: 96, bold: true, color: TEXT_HEAD })],
  }),
  new Paragraph({
    spacing: { after: 600 },
    alignment: AlignmentType.CENTER,
    children: [new TextRun({
      text: "Plataforma colaborativa de escritura creativa",
      font, size: 28, italics: true, color: TEXT_MUTED,
    })],
  }),
  new Paragraph({
    spacing: { after: 200 },
    alignment: AlignmentType.CENTER,
    children: [new TextRun({
      text: "Documentación del proyecto y de las tres modalidades de despliegue",
      font, size: 22, color: TEXT_HEAD,
    })],
  }),
  new Paragraph({
    spacing: { before: 1800, after: 100 },
    alignment: AlignmentType.CENTER,
    children: [new TextRun({ text: "Autor", font, size: 18, color: TEXT_MUTED })],
  }),
  new Paragraph({
    spacing: { after: 400 },
    alignment: AlignmentType.CENTER,
    children: [new TextRun({ text: "Luis Donaldo Ortiz García", font, size: 24, bold: true, color: TEXT_HEAD })],
  }),
  new Paragraph({
    spacing: { after: 100 },
    alignment: AlignmentType.CENTER,
    children: [new TextRun({ text: "Materia", font, size: 18, color: TEXT_MUTED })],
  }),
  new Paragraph({
    spacing: { after: 400 },
    alignment: AlignmentType.CENTER,
    children: [new TextRun({ text: "Despliegue de Software", font, size: 22, color: TEXT_HEAD })],
  }),
  new Paragraph({
    spacing: { after: 100 },
    alignment: AlignmentType.CENTER,
    children: [new TextRun({ text: "Fecha", font, size: 18, color: TEXT_MUTED })],
  }),
  new Paragraph({
    spacing: { after: 200 },
    alignment: AlignmentType.CENTER,
    children: [new TextRun({ text: "Mayo de 2026", font, size: 22, color: TEXT_HEAD })],
  }),
  pageBreak(),
];

const resumen = [
  H1("Resumen ejecutivo"),
  P("Este documento describe el proyecto Layla, una plataforma colaborativa de escritura creativa con arquitectura distribuida, y detalla las tres modalidades de despliegue implementadas como parte del proyecto final de la materia Despliegue de Software."),
  P("La aplicación se compone de ocho servicios coordinados (dos backends, un API gateway, tres bases de datos, un broker de mensajería y tres clientes) y se despliega sobre tres máquinas virtuales con separación lógica por capas: datos, lógica de aplicación y borde."),
  P("Se demuestran tres aproximaciones distintas al despliegue del mismo sistema: instalación manual sobre las máquinas virtuales, despliegue mediante contenedores Docker y despliegue automatizado con Vagrant y Puppet como infraestructura como código. El documento concluye con una comparativa cuantitativa y cualitativa de las tres alternativas."),
  pageBreak(),
];

const intro = [
  H1("1. Introducción"),
  H2("1.1 Contexto"),
  P("El desarrollo moderno de aplicaciones distribuidas plantea un desafío que va más allá de la codificación: el despliegue. Una aplicación funcional en el entorno de desarrollo no aporta valor hasta que se ejecuta de forma confiable en infraestructura productiva. Las decisiones de despliegue impactan en la velocidad de iteración, la reproducibilidad del entorno entre miembros del equipo y la resiliencia ante fallos."),
  P("Este trabajo aborda el despliegue de un sistema real (Layla) de tres maneras distintas, con el objetivo didáctico de evidenciar la progresión desde procedimientos artesanales hacia técnicas modernas de infraestructura como código."),
  H2("1.2 Objetivos"),
  bullet("Documentar la arquitectura del proyecto Layla y sus componentes."),
  bullet("Implementar tres modalidades de despliegue completas sobre un mínimo de tres máquinas virtuales."),
  bullet("Comparar las modalidades en términos de esfuerzo, reproducibilidad y mantenimiento."),
  bullet("Demostrar el impacto de la automatización en la calidad del código de producción."),
  H2("1.3 Alcance"),
  P("El trabajo cubre la totalidad de los servicios del sistema Layla (backends, gateway, clientes web y bases de datos) y las tres modalidades de despliegue. Quedan fuera del alcance la implementación de pipelines de integración continua y el aprovisionamiento sobre proveedores de nube pública."),
  pageBreak(),
];

const proyecto = [
  H1("2. Descripción del proyecto Layla"),
  H2("2.1 Visión general"),
  P("Layla es una plataforma colaborativa orientada a autores de ficción que escriben novelas en conjunto. Permite a múltiples usuarios trabajar simultáneamente sobre un manuscrito, gestionar una wiki interna del universo narrativo, visualizar las relaciones entre personajes y lugares en forma de grafo, y comunicarse mediante una sesión de voz en tiempo real."),
  P("El sistema reemplaza el uso fragmentado de herramientas genéricas (procesadores de texto en la nube, mensajería, hojas de cálculo, gestores de notas) por un espacio único integrado, con permisos finos por proyecto y persistencia consistente entre sus distintos subsistemas."),
  H2("2.2 Casos de uso"),
  P("El sistema implementa quince casos de uso agrupados por actor:"),
  table([1500, 4200, 2200, 1460], [
    ["ID", "Caso de uso", "Actor", "Estado"],
    ["CU-01", "Explorar catálogo público", "Anónimo", "Implementado"],
    ["CU-02", "Previsualizar sinopsis", "Anónimo", "Implementado"],
    ["CU-03", "Iniciar sesión / Registrarse", "Usuario", "Implementado"],
    ["CU-04", "Gestionar perfil", "Usuario", "Implementado"],
    ["CU-05", "Crear proyecto", "Escritor", "Implementado"],
    ["CU-06", "Gestionar colaboradores", "Escritor (Owner)", "Implementado"],
    ["CU-07", "Configurar privacidad", "Escritor (Owner)", "Implementado"],
    ["CU-08", "Editar manuscrito", "Editor / Escritor", "Implementado"],
    ["CU-09", "Gestionar wiki", "Editor / Escritor", "Implementado"],
    ["CU-10", "Visualizar grafo narrativo", "Lector / Editor", "Implementado"],
    ["CU-11", "Sesión de voz (hablar)", "Escritor", "Implementado"],
    ["CU-12", "Unirse como oyente", "Lector", "Implementado"],
    ["CU-13", "Leer historia completa", "Lector", "No iniciado"],
    ["CU-14", "Reportes del sistema", "Administrador", "No iniciado"],
    ["CU-15", "Gestionar usuarios", "Administrador", "Implementado"],
  ]),
  P("De los quince casos, doce están implementados de extremo a extremo. Los tres pendientes corresponden a funcionalidades de lectura final y administración avanzada que no afectan al núcleo colaborativo del producto.", { run: { italics: true, color: TEXT_MUTED } }),
  H2("2.3 Arquitectura general"),
  P("La arquitectura sigue un modelo de tres capas con separación clara de responsabilidades:"),
  H3("Capa de clientes"),
  P("Tres aplicaciones cliente, cada una orientada a un escenario de uso distinto:"),
  table([2400, 2800, 4160], [
    ["Cliente", "Tecnología", "Rol"],
    ["Desktop", "WPF · .NET 9", "Espacio principal de escritura. Editor, wiki, grafo y voz."],
    ["Web", "Blazor Server · .NET 9", "Catálogo público y panel de administración."],
    ["Android", "Kotlin + Jetpack Compose", "Compañero móvil. Voz push-to-talk y consulta de wiki."],
  ]),
  H3("Capa de aplicación"),
  P("Dos servicios backend con división por dominio, no por funcionalidad:"),
  bullet([
    new TextRun({ text: "server-core", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: " (ASP.NET Core 10): autenticación, gestión de usuarios, proyectos, roles y SignalR para la sesión de voz. Persiste en SQL Server.", font, size: 22, color: TEXT_HEAD }),
  ]),
  bullet([
    new TextRun({ text: "server-worldbuilding", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: " (Node.js 22 + Express 5 + TypeScript): manuscritos, capítulos, entradas de wiki y grafo narrativo. Persiste en MongoDB (documentos) y Neo4j (grafo).", font, size: 22, color: TEXT_HEAD }),
  ]),
  bullet([
    new TextRun({ text: "api-gateway", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: " (YARP sobre ASP.NET Core 10): único punto de entrada. Enruta por path hacia el backend correspondiente y reenvía WebSockets para los hubs de SignalR.", font, size: 22, color: TEXT_HEAD }),
  ]),
  H3("Capa de datos y mensajería"),
  table([2400, 2400, 4560], [
    ["Componente", "Tecnología", "Responsabilidad"],
    ["Base relacional", "SQL Server 2022", "Identidad (Identity Framework), proyectos, roles, asignaciones."],
    ["Base documental", "MongoDB", "Manuscritos, capítulos en formato RTF, entradas de wiki."],
    ["Base de grafo", "Neo4j 5", "Grafo narrativo: nodos, relaciones, recorridos."],
    ["Broker de eventos", "RabbitMQ 3", "Comunicación asíncrona entre los dos backends."],
  ]),
  H2("2.4 Patrón de mensajería: outbox after commit"),
  P("Los dos backends se comunican mediante eventos publicados en RabbitMQ sobre el exchange topic worldbuilding.events. El patrón aplicado es outbox after commit: server-core publica el evento project.created únicamente después de que la transacción SQL ha sido confirmada. Esto evita situaciones inconsistentes en las que el consumidor (server-worldbuilding) intentaría inicializar documentos para un proyecto que aún no existe en la base relacional."),
  P("El consumidor utiliza un identificador único por evento y persiste el cursor de procesamiento, lo que permite reanudar el consumo sin duplicaciones tras una caída."),
  H2("2.5 Convenciones de diseño en server-core"),
  P("El backend principal sigue una arquitectura limpia (Clean Architecture) en tres proyectos: Layla.Api (controladores, hubs, middleware, configuración), Layla.Core (entidades, interfaces, servicios de dominio, DTO, enumeraciones) y Layla.Infrastructure (repositorios EF Core, integraciones con RabbitMQ y servicios externos)."),
  P("Convenciones destacables:"),
  bullet([
    new TextRun({ text: "Result<T>:", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: " todos los servicios retornan un tipo Result con propiedades IsSuccess, Data y Error. Las excepciones no se propagan a la capa de presentación.", font, size: 22, color: TEXT_HEAD }),
  ]),
  bullet([
    new TextRun({ text: "ErrorCode:", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: " enumeración tipada que sustituye a las cadenas de error mágicas. Los controladores mapean ErrorCode a códigos HTTP de forma centralizada mediante ApiControllerBase.RespondWithError.", font, size: 22, color: TEXT_HEAD }),
  ]),
  bullet([
    new TextRun({ text: "TokenVersion:", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: " los JWT incluyen una versión que se valida contra la base de datos en cada petición. Esto permite invalidar sesiones de forma instantánea (cierre de sesión global, baneo).", font, size: 22, color: TEXT_HEAD }),
  ]),
  bullet([
    new TextRun({ text: "Bootstrap modular:", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: " Program.cs delega la configuración a cuatro módulos (Secrets, Builder, Services, Secure) con responsabilidades separadas y validación fail-fast de la configuración crítica.", font, size: 22, color: TEXT_HEAD }),
  ]),
  pageBreak(),
];

const despliegue = [
  H1("3. Topología de despliegue"),
  P("Las tres modalidades despliegan la misma topología sobre tres máquinas virtuales Ubuntu Server 22.04 LTS, conectadas mediante una red privada de VirtualBox (192.168.56.0/24):"),
  table([2400, 2200, 4760], [
    ["Máquina virtual", "Dirección IP", "Servicios alojados"],
    ["layla-data", "192.168.56.10", "SQL Server, MongoDB, Neo4j, RabbitMQ"],
    ["layla-apps", "192.168.56.11", "server-core, server-worldbuilding, client-web"],
    ["layla-edge", "192.168.56.12", "API Gateway (único expuesto al host)"],
  ]),
  P("Cada máquina virtual cuenta con dos adaptadores de red: NAT para acceso a Internet (descarga de paquetes e imágenes) y host-only para la comunicación interna entre las VM y con el host."),
  P("Las especificaciones de hardware virtual asignadas son:"),
  table([2400, 1800, 1600, 3560], [
    ["VM", "RAM", "CPU", "Disco virtual"],
    ["layla-data", "3 GB", "2 núcleos", "20 GB VDI dinámico"],
    ["layla-apps", "3 GB", "2 núcleos", "20 GB VDI dinámico"],
    ["layla-edge", "1 GB", "1 núcleo", "20 GB VDI dinámico"],
  ]),
  pageBreak(),
];

// =================== MODO 1 — MANUAL ===================
const modo1 = [
  H1("4. Modalidad 1: Despliegue manual"),
  H2("4.1 Concepto"),
  P("La modalidad manual constituye la línea base contra la cual se justifican las modalidades automatizadas. Consiste en instalar, configurar y operar cada componente del sistema interactivamente sobre cada máquina virtual, sin asistencia de herramientas declarativas."),
  P("Esta modalidad permite comprender la complejidad real del despliegue distribuido y dimensionar el ahorro operativo que aportan las herramientas posteriores."),
  H2("4.2 Procedimiento ejecutado"),
  H3("4.2.1 Creación de las máquinas virtuales"),
  P("Se utilizó VirtualBox 7.0.20 como hipervisor. Para cada máquina virtual se siguió el procedimiento siguiente:"),
  numbered("Creación de una nueva máquina virtual con la imagen ISO de Ubuntu Server 22.04.5 LTS adjuntada como medio óptico."),
  numbered("Asignación de recursos según la tabla de especificaciones (RAM, CPU, disco)."),
  numbered("Configuración del adaptador 1 como NAT y del adaptador 2 como adaptador solo-anfitrión, sobre la red VirtualBox Host-Only Ethernet Adapter."),
  numbered("Instalación interactiva de Ubuntu Server: selección de idioma, configuración de teclado, particionado completo del disco sin LVM, creación del usuario layla con contraseña, definición del nombre de host coincidente con el nombre de la VM, e instalación obligatoria del servidor OpenSSH."),
  numbered("Tras el primer arranque, fijación de la dirección IP estática mediante edición del archivo /etc/netplan/50-cloud-init.yaml y aplicación con netplan apply."),
  numbered("Deshabilitación de la regeneración automática del netplan por parte de cloud-init mediante la creación del archivo /etc/cloud/cloud.cfg.d/99-disable-network-config.cfg con la directiva network: {config: disabled}."),
  P("Para optimizar el proceso, se creó una primera máquina virtual completa (layla-data) y se generaron las dos restantes mediante clonación con regeneración de direcciones MAC. Tras la clonación se ajustaron individualmente el nombre de host, la dirección IP y los recursos asignados."),
  P("Una vez completada la red privada, se verificó la conectividad entre las tres máquinas y desde el host hacia cada una de ellas mediante el cliente SSH Bitvise."),
  H3("4.2.2 Provisión de la VM de datos"),
  P("Sobre layla-data se instalaron los cuatro servicios de persistencia y mensajería de forma manual, cada uno con sus repositorios oficiales:"),
  bullet("SQL Server 2022: registro de la clave pública de Microsoft, alta del repositorio mssql-server-2022, instalación del paquete mssql-server y configuración interactiva con mssql-conf setup (selección de la edición Developer, aceptación de los términos de licencia, definición de la contraseña del usuario SA)."),
  bullet("MongoDB: registro de la clave pública de MongoDB Inc., alta del repositorio correspondiente a Jammy, instalación del paquete mongodb-org, edición de /etc/mongod.conf para permitir bind en 0.0.0.0 y habilitación de la autenticación. Creación del usuario administrador mediante el shell mongo."),
  bullet("Neo4j 5: registro de la clave pública de Neo Technology, alta del repositorio stable 5, instalación del paquete neo4j, edición de /etc/neo4j/neo4j.conf para habilitar la escucha en todas las interfaces, y cambio de la contraseña por defecto mediante cypher-shell."),
  bullet("RabbitMQ 3: instalación desde el repositorio oficial de Ubuntu, habilitación del plugin de gestión, y creación del usuario administrativo con permisos sobre el virtual host por defecto."),
  P("Cada servicio fue habilitado como unidad de systemd con arranque automático en el inicio. Las reglas de UFW se ajustaron para permitir tráfico entrante únicamente desde la subred 192.168.56.0/24 hacia los puertos de cada servicio (1433, 27017, 7687, 5672), manteniendo abierto el puerto 22 para SSH."),
  H3("4.2.3 Provisión de la VM de aplicación"),
  P("Sobre layla-apps se instalaron los tiempos de ejecución y se desplegaron los tres servicios de aplicación:"),
  numbered("Instalación del SDK .NET 10 desde el repositorio oficial packages.microsoft.com.", "numbers2"),
  numbered("Instalación de Node.js 22 mediante NodeSource y del gestor de paquetes pnpm.", "numbers2"),
  numbered("Publicación local de los tres servicios (dotnet publish para server-core y client-web, pnpm build para server-worldbuilding) y transferencia de los artefactos compilados a la máquina virtual mediante SFTP a /opt/layla/.", "numbers2"),
  numbered("Creación de archivos de variables de entorno en /etc/layla/ para cada servicio, conteniendo las cadenas de conexión apuntando a layla-data, las credenciales JWT, los parámetros de RabbitMQ y la configuración SMTP.", "numbers2"),
  numbered("Creación de unidades systemd (layla-core.service, layla-worldbuilding.service, layla-web.service) con directivas Restart=always, EnvironmentFile apuntando a los archivos correspondientes y dependencia After=network.target.", "numbers2"),
  numbered("Habilitación de las unidades con systemctl enable --now y verificación mediante journalctl -u <servicio> -f.", "numbers2"),
  H3("4.2.4 Provisión de la VM de borde"),
  P("Sobre layla-edge se instaló únicamente el tiempo de ejecución de .NET 10 y se desplegó el API Gateway. La configuración del gateway se ajustó mediante variables de entorno para que sus clústers apuntaran a las direcciones IP reales de los servicios de aplicación en layla-apps, en lugar de los nombres lógicos utilizados en el archivo de configuración por defecto."),
  P("Las reglas de firewall en esta máquina mantienen abierto únicamente el puerto 5000 (gateway) hacia el exterior, además del puerto 22 para administración."),
  H3("4.2.5 Verificación end-to-end"),
  P("Una vez completada la provisión de las tres máquinas, se verificó el sistema realizando peticiones desde el host hacia el gateway:"),
  ...code("curl http://192.168.56.12:5000/health\ncurl http://192.168.56.12:5000/api/projects/public"),
  P("La primera petición retornó la cadena Healthy, indicando que el gateway estaba operativo. La segunda atravesó el gateway, alcanzó server-core en layla-apps, consultó SQL Server en layla-data y retornó una lista JSON vacía (representación correcta de un sistema sin proyectos públicos registrados)."),
  H2("4.3 Evaluación de la modalidad"),
  P("El despliegue manual completo requirió aproximadamente seis horas de trabajo continuo en su primera ejecución, distribuidas entre la creación e instalación de las máquinas virtuales (dos horas), la provisión de la VM de datos (dos horas) y la provisión de las VM de aplicación y borde (dos horas)."),
  P("El número total de comandos ejecutados manualmente superó los sesenta, sin contar las decisiones interactivas en los instaladores. La modalidad es funcional pero presenta debilidades significativas:"),
  bullet("Reproducibilidad nula. Cualquier error tipográfico o paso omitido obliga a reiniciar el procedimiento desde el punto de fallo, frecuentemente sin trazabilidad clara de la causa."),
  bullet("Versiones inconsistentes. Los paquetes obtenidos vía apt-get reflejan el estado actual del repositorio, que puede cambiar entre ejecuciones."),
  bullet("Onboarding costoso. Cualquier nuevo miembro del equipo debe repetir el procedimiento completo para obtener un entorno funcional."),
  bullet("Imposibilidad de auditoría. La configuración resultante vive únicamente en la memoria del operador y en los archivos locales de cada VM."),
  P("Estas limitaciones motivan la adopción de las modalidades posteriores."),
  pageBreak(),
];

// =================== MODO 2 — DOCKER ===================
const modo2 = [
  H1("5. Modalidad 2: Despliegue con contenedores"),
  H2("5.1 Concepto"),
  P("La segunda modalidad mantiene la topología de tres máquinas virtuales pero reemplaza la instalación nativa de cada servicio por la ejecución de contenedores Docker. Cada servicio se distribuye como una imagen versionada, lo que elimina la instalación interactiva y fija las versiones de runtime de forma declarativa."),
  H2("5.2 Estructura de archivos"),
  P("El despliegue se organiza en tres archivos Compose independientes, uno por máquina virtual, y un archivo de variables de entorno compartido:"),
  ...code("deploy/files/\n├── compose/\n│   ├── compose.data.yml   # SQL Server, MongoDB, Neo4j, RabbitMQ\n│   ├── compose.apps.yml   # server-core, server-worldbuilding, client-web\n│   └── compose.edge.yml   # api-gateway\n└── env/\n    └── .env.shared        # Variables comunes a las tres VM"),
  P("Esta separación permite arrancar cada capa de forma independiente y refleja la asignación de servicios a máquinas virtuales en el plano lógico."),
  H2("5.3 Procedimiento de despliegue"),
  P("Sobre cada máquina virtual previamente creada (con Ubuntu Server 22.04 y conectividad SSH establecida) se ejecuta la siguiente secuencia:"),
  numbered("Instalación de Docker Engine mediante el script oficial de Docker: curl -fsSL https://get.docker.com | sudo sh. Adición del usuario administrativo al grupo docker para permitir su uso sin privilegios elevados."),
  numbered("Transferencia mediante SFTP del archivo compose correspondiente y del archivo .env.shared al directorio home del usuario."),
  numbered("Para layla-apps y layla-edge, transferencia adicional del código fuente del repositorio, ya que sus servicios se construyen localmente a partir de los Dockerfile."),
  numbered("Ejecución del comando docker compose -f compose.<vm>.yml --env-file .env.shared up -d para levantar el stack correspondiente."),
  H2("5.4 Ejemplo: VM de datos"),
  P("El archivo compose.data.yml define los cuatro servicios de persistencia y mensajería mediante imágenes oficiales, sin necesidad de procesos de construcción:"),
  ...code("services:\n  sqlserver:\n    image: mcr.microsoft.com/mssql/server:2022-latest\n    environment:\n      - ACCEPT_EULA=Y\n      - SA_PASSWORD=${SQL_PASSWORD}\n    ports: ['1433:1433']\n    volumes: [sql_data:/var/opt/mssql]\n    healthcheck: { ... }\n    restart: unless-stopped\n\n  mongodb:\n    image: mongo:4.4\n    # ...\n\n  neo4j:\n    image: neo4j:5\n    # ...\n\n  rabbitmq:\n    image: rabbitmq:3-management\n    # ..."),
  P("Aspectos relevantes de esta definición:"),
  bullet("La directiva image fija la versión exacta del servicio. La imagen mongo:4.4 será siempre la misma, independientemente de la fecha de despliegue."),
  bullet("La directiva restart: unless-stopped proporciona auto-reinicio declarativo en caso de fallo del proceso, sin necesidad de configurar systemd."),
  bullet("Los healthchecks declarativos permiten que docker compose detecte el estado real de cada contenedor y bloquee dependencias hasta que un servicio esté saludable."),
  bullet("Los volúmenes nombrados (sql_data, mongo_data, neo4j_data, rabbit_data) desacoplan los datos persistentes del ciclo de vida del contenedor."),
  H2("5.5 Decisión técnica: MongoDB 4.4"),
  P("Se eligió la versión 4.4 de MongoDB en lugar de la última versión disponible (8.x). Las versiones 5 y posteriores requieren la presencia de instrucciones AVX en el procesador, que VirtualBox no expone al sistema huésped por defecto. En la práctica, intentar arrancar mongo:7 sobre una VM de VirtualBox produce un fallo inmediato con código de salida 132 (SIGILL, instrucción ilegal). La versión 4.4 conserva todas las funcionalidades requeridas por el dominio de la aplicación y opera correctamente sin AVX."),
  H2("5.6 Evaluación de la modalidad"),
  P("El despliegue con contenedores reduce el tiempo de provisión por máquina virtual desde aproximadamente dos horas (modalidad manual) a unos cinco minutos. El número de comandos por VM cae de veinte a tres (instalar Docker, transferir archivos, ejecutar docker compose up)."),
  P("La modalidad ofrece ventajas claras: reproducibilidad de los servicios, fijación de versiones, healthchecks declarativos y auto-reinicio sin systemd. Sin embargo, no resuelve la creación y configuración inicial de las máquinas virtuales, que sigue siendo un procedimiento manual idéntico al de la modalidad anterior."),
  pageBreak(),
];

// =================== MODO 3 — VAGRANT + PUPPET ===================
const modo3 = [
  H1("6. Modalidad 3: Despliegue automatizado con Vagrant y Puppet"),
  H2("6.1 Concepto"),
  P("La tercera modalidad aplica el principio de infraestructura como código en su forma plena: la totalidad del entorno (creación de máquinas virtuales, configuración de red, instalación de Docker, despliegue de contenedores) queda expresada en archivos versionados en el repositorio del proyecto. La ejecución completa se reduce a un único comando."),
  P("Se utilizan dos herramientas complementarias:"),
  bullet([
    new TextRun({ text: "Vagrant", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: " (HashiCorp): orquesta el ciclo de vida de las máquinas virtuales sobre VirtualBox. Define recursos, red, puertos forwardeados y carpetas compartidas con el host.", font, size: 22, color: TEXT_HEAD }),
  ]),
  bullet([
    new TextRun({ text: "Puppet", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: " (Puppet Inc.): herramienta de gestión de configuración declarativa. Se ejecuta dentro de cada máquina virtual y aplica los manifiestos que describen el estado deseado del sistema.", font, size: 22, color: TEXT_HEAD }),
  ]),
  H2("6.2 Estructura del directorio deploy"),
  ...code("deploy/\n├── Vagrantfile                       # Define las 3 VM\n├── puppet/\n│   ├── bootstrap.sh                  # Instala Puppet agent en cada VM\n│   ├── manifests/\n│   │   ├── data.pp                   # Manifiesto de la VM de datos\n│   │   ├── apps.pp                   # Manifiesto de la VM de aplicación\n│   │   └── edge.pp                   # Manifiesto de la VM de borde\n│   └── modules/\n│       └── laylacommon/manifests/\n│           ├── init.pp\n│           ├── docker_install.pp     # Clase reusable: instala Docker\n│           └── stack.pp              # Definición reusable: levanta compose\n└── files/\n    ├── compose/                      # Los 3 archivos compose\n    └── env/\n        └── .env.shared"),
  H2("6.3 El Vagrantfile"),
  P("Define las tres máquinas virtuales sobre la imagen base bento/ubuntu-22.04, una imagen mantenida oficialmente por la comunidad Vagrant para Ubuntu Server 22.04. Cada VM declara su nombre de host, su dirección IP en la red privada, los recursos asignados y el manifiesto Puppet que debe aplicarse:"),
  ...code('config.vm.box = "bento/ubuntu-22.04"\nconfig.vm.synced_folder "..", "/vagrant"\n\nconfig.vm.define "data" do |m|\n  m.vm.hostname = "layla-data"\n  m.vm.network "private_network", ip: "192.168.56.10"\n  m.vm.provider("virtualbox") { |vb|\n    vb.memory = 3072; vb.cpus = 2\n  }\n  m.vm.provision "shell", path: "puppet/bootstrap.sh"\n  m.vm.provision "puppet" do |p|\n    p.manifests_path = "puppet/manifests"\n    p.manifest_file  = "data.pp"\n    p.module_path    = "puppet/modules"\n  end\nend\n# ... apps y edge siguen el mismo patrón'),
  P("La directiva synced_folder monta el repositorio completo en el directorio /vagrant de cada máquina virtual, permitiendo que los manifiestos Puppet accedan a los archivos compose y al código fuente para construcción de imágenes."),
  H2("6.4 Los manifiestos Puppet"),
  P("Cada uno de los tres manifiestos por VM es deliberadamente minimalista: incluye la clase reusable de instalación de Docker y declara una instancia del tipo definido que copia el compose correspondiente y ejecuta docker compose up:"),
  ...code("# data.pp — manifiesto completo de la VM de datos\ninclude laylacommon::docker_install\n\nlaylacommon::stack { 'data':\n  compose_source => '/vagrant/deploy/files/compose/compose.data.yml',\n  env_source     => '/vagrant/deploy/files/env/.env.shared',\n}"),
  P("La lógica reusable vive en el módulo laylacommon, dentro de puppet/modules. La clase docker_install instala Docker Engine y el plugin Compose mediante recursos declarativos:"),
  ...code("package { 'docker-ce':\n  ensure  => installed,\n}\n\nservice { 'docker':\n  ensure  => running,\n  enable  => true,\n  require => Package['docker-ce'],\n}"),
  P("La diferencia frente a un script imperativo es sustancial: en lugar de indicar la secuencia de comandos a ejecutar, se describe el estado final esperado. Puppet determina las operaciones necesarias para alcanzarlo y, si el estado ya está garantizado, no realiza ninguna acción. Esto hace que la aplicación del manifiesto sea idempotente: ejecutarlo dos veces produce el mismo resultado que ejecutarlo una vez."),
  H2("6.5 Comando único de despliegue"),
  P("Tras situarse en el directorio deploy, el despliegue completo se ejecuta mediante:"),
  ...code("vagrant up"),
  P("Internamente, Vagrant realiza la siguiente secuencia para cada una de las tres máquinas virtuales:"),
  numbered("Clonación de la imagen base bento/ubuntu-22.04 en modo linked clone, lo que minimiza el uso de disco al compartir el disco base con todas las VM.", "numbers3"),
  numbered("Configuración de la red privada con la dirección IP fija declarada.", "numbers3"),
  numbered("Montaje del repositorio en /vagrant mediante el mecanismo de carpetas compartidas de VirtualBox.", "numbers3"),
  numbered("Configuración del port forwarding para la VM de borde: localhost:5000 del host se mapea al puerto 5000 de la VM edge.", "numbers3"),
  numbered("Ejecución del script bootstrap.sh que instala el agente Puppet.", "numbers3"),
  numbered("Aplicación del manifiesto Puppet correspondiente, que instala Docker, copia los archivos compose y arranca los contenedores.", "numbers3"),
  P("El tiempo total de ejecución es de aproximadamente diez minutos en la primera invocación (incluyendo la descarga de las imágenes Docker) y de dos minutos en invocaciones posteriores que aprovechan la cache de imágenes."),
  H2("6.6 Verificación"),
  P("Tras la finalización del comando vagrant up, el sistema completo está operativo. La verificación se realiza desde el host:"),
  ...code('PS> curl http://localhost:5000/api/projects/public\n\nStatusCode        : 200\nStatusDescription : OK\nContent           : []\nServer            : Kestrel'),
  P("La respuesta HTTP 200 con cuerpo JSON vacío confirma que la petición atravesó la cadena completa: cliente PowerShell del host, port-forwarding de Vagrant hacia la VM de borde, API Gateway YARP, server-core en la VM de aplicación, SQL Server en la VM de datos."),
  H2("6.7 Evaluación de la modalidad"),
  P("La modalidad automatizada reduce el tiempo total de despliegue a un orden de magnitud por debajo de la modalidad manual y elimina por completo la intervención humana durante el procedimiento. La reproducibilidad es total: cualquier desarrollador del equipo, en cualquier máquina con VirtualBox y Vagrant instalados, obtiene un entorno funcionalmente idéntico ejecutando vagrant up."),
  P("Adicionalmente, el repositorio del proyecto pasa a contener no solo el código de la aplicación sino también la totalidad de la infraestructura sobre la que se ejecuta. Esto permite revisar cambios de infraestructura en pull requests, versionar la evolución del entorno y reproducir entornos históricos a partir de commits anteriores."),
  pageBreak(),
];

// =================== BUGS ===================
const bugs = [
  H1("7. Hallazgos durante la automatización"),
  P("Un efecto secundario relevante del despliegue automatizado fue la exposición de cinco defectos en el código fuente del proyecto que la modalidad manual habría enmascarado mediante intervención humana. El despliegue declarativo, al carecer de un operador que ajuste configuraciones sobre la marcha, expuso estos problemas como fallos inmediatos del arranque."),
  H2("7.1 Vinculación de URL hardcodeada"),
  P("Los archivos Builder.cs de server-core y client-web invocaban builder.WebHost.UseUrls con el host literal localhost, sobrescribiendo cualquier configuración recibida por variables de entorno. En el entorno de desarrollo este comportamiento es transparente, ya que el host es localhost. En un contenedor, sin embargo, vincular a localhost hace que el servicio solo sea accesible desde dentro del propio contenedor, no desde otras máquinas o contenedores en la red."),
  P("La corrección consistió en seleccionar el host de binding según el entorno: el símbolo + (escucha en todas las interfaces) en producción y localhost en desarrollo."),
  H2("7.2 Redirección HTTPS en producción"),
  P("Program.cs de server-core invocaba app.UseHttpsRedirection en entornos distintos de desarrollo. Esto provocaba que las peticiones HTTP recibidas desde el API Gateway fueran respondidas con un código 307 redirigiendo a HTTPS, lo que YARP no sigue por defecto en una configuración de proxy de aplicación. La consecuencia era un fallo continuo del enrutamiento entre el gateway y los servicios internos."),
  P("Se eliminó la directiva en producción. El razonamiento arquitectónico es que el API Gateway actúa como terminador TLS de cara al exterior, mientras que el tráfico interno entre el gateway y los servicios de aplicación es HTTP sobre la red privada, lo cual es aceptable porque dicha red está aislada del exterior."),
  H2("7.3 Variable de entorno de puerto incorrecta"),
  P("El servicio server-worldbuilding leía el puerto de escucha desde process.env.PORT, mientras que el archivo Compose definía las variables PORT_HTTP y PORT_HTTPS. Como resultado, el servicio se iniciaba en un puerto aleatorio asignado por el sistema operativo, con el mensaje de log Server running on http://localhost:undefined."),
  P("La corrección consistió en añadir la variable PORT (con el valor de PORT_HTTP) al bloque de variables de entorno del servicio en el archivo Compose."),
  H2("7.4 Middleware sin servicios registrados"),
  P("El proyecto api-gateway invocaba app.UseAuthentication y app.UseAuthorization en su pipeline de middleware, pero el bloque correspondiente AddAuthentication estaba comentado en la configuración de servicios. ASP.NET Core arroja una excepción al construir la aplicación cuando se intenta usar middleware cuyos servicios no han sido registrados."),
  P("Se eliminaron las llamadas a UseAuthentication y UseAuthorization del pipeline, dejando claros los TODO en el código para reintroducirlas cuando se implemente la autenticación en el gateway."),
  H2("7.5 Incompatibilidad de MongoDB con CPU virtualizado"),
  P("MongoDB 5 y versiones posteriores requieren la instrucción AVX en el procesador. VirtualBox no expone esta instrucción por defecto al sistema huésped. El contenedor mongo:7 entraba en un bucle de reinicio con código de salida 132 (SIGILL)."),
  P("Se sustituyó la imagen por mongo:4.4, la última versión compatible con procesadores sin AVX, manteniendo funcionalidad equivalente para el dominio del proyecto."),
  H2("7.6 Reflexión"),
  P("La automatización no oculta los defectos del código de producción: los expone. En el despliegue manual, un operador habría notado el primer fallo, habría ajustado la configuración o el código, habría continuado, y habría dejado el sistema funcionando sin documentar los ajustes realizados. En el despliegue automatizado, cada uno de estos defectos se manifiesta como un error reproducible que debe corregirse en el código fuente, no en la configuración del entorno. El resultado es código de producción de mayor calidad."),
  pageBreak(),
];

// =================== COMPARATIVA ===================
const comparativa = [
  H1("8. Análisis comparativo"),
  H2("8.1 Tabla comparativa global"),
  table([2400, 2200, 2200, 2560], [
    ["Aspecto", "Manual", "Docker", "Vagrant + Puppet"],
    ["Creación de las VM", "Manual", "Manual", "Automático"],
    ["Instalación de runtimes", "apt-get por servicio", "Docker en cada VM", "Puppet"],
    ["Configuración de servicios", "Archivos manuales", "Compose declarativo", "Compose vía Puppet"],
    ["Arranque de servicios", "Unidades systemd", "docker compose up", "Puppet + Docker"],
    ["Reproducibilidad", "Nula", "Parcial", "Total"],
    ["Tiempo total (primera vez)", "~6 horas", "~30 minutos", "~10 minutos"],
    ["Tiempo total (siguientes)", "~6 horas", "~5 minutos", "~2 minutos"],
    ["Comandos manuales", "Más de 60", "Aproximadamente 5", "Uno"],
    ["Versionado del entorno", "No", "Parcial", "Total"],
  ]),
  H2("8.2 Cuándo aplica cada modalidad"),
  P("Las tres modalidades no son alternativas excluyentes en términos pedagógicos, sino estadios evolutivos. En términos prácticos, sin embargo, cada una tiene un nicho de aplicación:"),
  bullet([
    new TextRun({ text: "Manual: ", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: "útil en escenarios de aprendizaje inicial y en sistemas legados muy específicos donde la automatización es desproporcionada respecto al volumen de cambios.", font, size: 22, color: TEXT_HEAD }),
  ]),
  bullet([
    new TextRun({ text: "Docker: ", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: "adecuado para entornos donde la infraestructura subyacente ya está provisionada (por ejemplo, un proveedor de nube que entrega servidores pre-configurados) y solo falta orquestar la capa de aplicación.", font, size: 22, color: TEXT_HEAD }),
  ]),
  bullet([
    new TextRun({ text: "Vagrant + Puppet: ", font, size: 22, bold: true, color: TEXT_HEAD }),
    new TextRun({ text: "ideal para entornos de desarrollo local reproducibles y para describir la infraestructura completa de un proyecto en su repositorio.", font, size: 22, color: TEXT_HEAD }),
  ]),
  P("Cabe mencionar que en entornos productivos reales sobre nube, las herramientas dominantes hoy son Terraform (provisión de infraestructura) y Ansible o Kubernetes (configuración y orquestación). Vagrant y Puppet siguen las mismas ideas fundamentales y son adecuados para el escenario académico de máquinas virtuales locales."),
  pageBreak(),
];

// =================== CONCLUSIONES ===================
const conclusiones = [
  H1("9. Conclusiones"),
  P("El presente trabajo cubrió el despliegue completo de un sistema distribuido real (Layla, plataforma colaborativa de escritura creativa con ocho servicios coordinados) mediante tres modalidades de complejidad creciente sobre tres máquinas virtuales."),
  P("La modalidad manual demostró el costo real de operar sin automatización: aproximadamente seis horas de trabajo continuo, más de sesenta comandos y la imposibilidad de reproducir el entorno sin volver a realizar el procedimiento desde el principio."),
  P("La modalidad con contenedores Docker redujo el tiempo de provisión a treinta minutos y fijó las versiones del software de forma declarativa, eliminando la instalación interactiva. Sin embargo, mantuvo el costo de creación manual de las máquinas virtuales."),
  P("La modalidad con Vagrant y Puppet llevó el procedimiento al límite: un único comando produjo el entorno completo en diez minutos, con reproducibilidad total y versionado en el repositorio del proyecto. Como efecto secundario, expuso cinco defectos en el código fuente que las modalidades anteriores habrían enmascarado."),
  P("La conclusión central es que el despliegue debe entenderse como código: una pieza más del repositorio del proyecto, sujeta a revisión, versionado y prueba, en igualdad de condiciones con el código de aplicación. La inversión en automatización no es opcional en sistemas distribuidos con más de un puñado de componentes; es la condición que hace viable el trabajo en equipo, la operación a largo plazo y la calidad del software producido."),
  H1("10. Referencias"),
  bullet("HashiCorp. Vagrant Documentation. https://developer.hashicorp.com/vagrant"),
  bullet("Puppet Inc. Puppet Language Reference. https://www.puppet.com/docs/puppet"),
  bullet("Docker Inc. Compose Specification. https://docs.docker.com/compose"),
  bullet("Microsoft. ASP.NET Core Documentation. https://learn.microsoft.com/aspnet/core"),
  bullet("MongoDB Inc. Installation on Ubuntu. https://www.mongodb.com/docs/manual/tutorial/install-mongodb-on-ubuntu"),
  bullet("Neo4j. Operations Manual. https://neo4j.com/docs/operations-manual"),
  bullet("RabbitMQ. Installation Guide. https://www.rabbitmq.com/docs/install-debian"),
];

// ====== Build the document ======
const doc = new Document({
  creator: "Luis Donaldo Ortiz García",
  title: "Layla — Documentación del proyecto y despliegue",
  styles: {
    default: { document: { run: { font, size: 22, color: TEXT_HEAD } } },
    paragraphStyles: [
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 36, bold: true, font, color: TEXT_HEAD },
        paragraph: { spacing: { before: 360, after: 200 }, outlineLevel: 0 } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 28, bold: true, font, color: TEXT_HEAD },
        paragraph: { spacing: { before: 280, after: 160 }, outlineLevel: 1 } },
      { id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 24, bold: true, font, color: ACCENT },
        paragraph: { spacing: { before: 220, after: 120 }, outlineLevel: 2 } },
    ],
  },
  numbering: {
    config: [
      { reference: "bullets",
        levels: [{ level: 0, format: LevelFormat.BULLET, text: "•", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
      { reference: "numbers",
        levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
      { reference: "numbers2",
        levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
      { reference: "numbers3",
        levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
    ]
  },
  sections: [{
    properties: {
      page: {
        size: { width: 12240, height: 15840 },
        margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 }
      }
    },
    headers: {
      default: new Header({ children: [new Paragraph({
        alignment: AlignmentType.RIGHT,
        children: [new TextRun({ text: "Layla — Despliegue de Software", font, size: 18, color: TEXT_MUTED, italics: true })]
      })] })
    },
    footers: {
      default: new Footer({ children: [new Paragraph({
        alignment: AlignmentType.CENTER,
        children: [
          new TextRun({ text: "Página ", font, size: 18, color: TEXT_MUTED }),
          new TextRun({ children: [PageNumber.CURRENT], font, size: 18, color: TEXT_MUTED }),
          new TextRun({ text: " de ", font, size: 18, color: TEXT_MUTED }),
          new TextRun({ children: [PageNumber.TOTAL_PAGES], font, size: 18, color: TEXT_MUTED }),
        ]
      })] })
    },
    children: [
      ...titlePage,
      ...resumen,
      ...intro,
      ...proyecto,
      ...despliegue,
      ...modo1,
      ...modo2,
      ...modo3,
      ...bugs,
      ...comparativa,
      ...conclusiones,
    ]
  }]
});

const outputPath = path.resolve("C:\\Users\\snake\\Desktop\\Layla\\presentation\\Documento_Despliegue_Layla.docx");
Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync(outputPath, buffer);
  console.log("OK:", outputPath, "(" + buffer.length + " bytes)");
});
