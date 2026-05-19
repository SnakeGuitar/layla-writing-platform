# File Tree: src

```
├── 📁 client-android
│   ├── 📁 app
│   │   ├── 📁 src
│   │   │   └── 📁 main
│   │   │       ├── 📁 java
│   │   │       │   └── 📁 com
│   │   │       │       └── 📁 layla
│   │   │       │           └── 📁 android
│   │   │       │               ├── 📁 data
│   │   │       │               │   ├── 📁 api
│   │   │       │               │   ├── 📁 audio
│   │   │       │               │   ├── 📁 local
│   │   │       │               │   ├── 📁 model
│   │   │       │               │   └── 📁 repository
│   │   │       │               ├── 📁 ui
│   │   │       │               │   ├── 📁 auth
│   │   │       │               │   ├── 📁 feed
│   │   │       │               │   ├── 📁 projects
│   │   │       │               │   ├── 📁 reader
│   │   │       │               │   ├── 📁 theme
│   │   │       │               │   ├── 📁 voice
│   │   │       │               │   └── 📁 workspace
│   │   │       │               └── ☕ MainActivity.kt
│   │   │       └── 📁 res
│   │   │           ├── 📁 mipmap-hdpi
│   │   │           │   ├── 🖼️ ic_launcher.webp
│   │   │           │   └── 🖼️ ic_launcher_round.webp
│   │   │           ├── 📁 mipmap-mdpi
│   │   │           │   ├── 🖼️ ic_launcher.webp
│   │   │           │   └── 🖼️ ic_launcher_round.webp
│   │   │           ├── 📁 mipmap-xhdpi
│   │   │           │   ├── 🖼️ ic_launcher.webp
│   │   │           │   └── 🖼️ ic_launcher_round.webp
│   │   │           ├── 📁 mipmap-xxhdpi
│   │   │           │   ├── 🖼️ ic_launcher.webp
│   │   │           │   └── 🖼️ ic_launcher_round.webp
│   │   │           └── 📁 mipmap-xxxhdpi
│   │   │               ├── 🖼️ ic_launcher.webp
│   │   │               └── 🖼️ ic_launcher_round.webp
│   │   ├── ⚙️ .gitignore
│   │   └── 📄 proguard-rules.pro
│   ├── 📁 gradle
│   │   ├── 📁 wrapper
│   │   │   ├── 📄 gradle-wrapper.jar
│   │   │   └── 📄 gradle-wrapper.properties
│   │   ├── 📄 gradle-daemon-jvm.properties
│   │   └── ⚙️ libs.versions.toml
│   ├── ⚙️ .gitignore
│   ├── 📝 FileTree.md
│   ├── 📄 gradle.properties
│   ├── 📄 gradlew
│   ├── 📄 gradlew.bat
│   └── 📄 settings.gradle.kts
├── 📁 client-desktop
│   ├── 📁 Data
│   │   └── 📄 LocalCacheManager.cs
│   ├── 📁 Fonts
│   │   ├── 📄 Inter-Bold.ttf
│   │   ├── 📄 Inter-Italic.ttf
│   │   ├── 📄 Inter-Regular.ttf
│   │   └── 📄 Inter-SemiBold.ttf
│   ├── 📁 Models
│   │   ├── 📁 Graphs
│   │   │   └── 📄 NarrativeGraphModels.cs
│   │   ├── 📁 Manuscripts
│   │   │   ├── 📄 Chapter.cs
│   │   │   └── 📄 Manuscript.cs
│   │   ├── 📁 Projects
│   │   │   ├── 📄 Collaborator.cs
│   │   │   ├── 📄 CreateProjectRequest.cs
│   │   │   ├── 📄 ParticipantPresence.cs
│   │   │   ├── 📄 Project.cs
│   │   │   ├── 📄 UpdateProjectRequest.cs
│   │   │   └── 📄 VoiceParticipant.cs
│   │   ├── 📁 User
│   │   │   ├── 📁 Authentication
│   │   │   │   ├── 📄 AuthResponse.cs
│   │   │   │   ├── 📄 AuthResult.cs
│   │   │   │   ├── 📄 LoginRequest.cs
│   │   │   │   ├── 📄 RegisterRequest.cs
│   │   │   │   └── 📄 VerifyEmailRequest.cs
│   │   │   └── 📁 Validation
│   │   │       └── 📄 ValidationResult.cs
│   │   └── 📁 Wikis
│   │       ├── 📄 AppearanceRecord.cs
│   │       ├── 📄 Mention.cs
│   │       └── 📄 WikiEntry.cs
│   ├── 📁 Properties
│   │   └── 📁 PublishProfiles
│   ├── 📁 Services
│   │   ├── 📁 Graphs
│   │   │   ├── 📄 GraphApiService.cs
│   │   │   └── 📄 IGraphApiService.cs
│   │   ├── 📁 Logger
│   │   │   └── 📄 Log.cs
│   │   ├── 📁 Manuscripts
│   │   │   ├── 📄 IManuscriptApiService.cs
│   │   │   └── 📄 ManuscriptApiService.cs
│   │   ├── 📁 Projetcs
│   │   │   ├── 📄 IProjectApiService.cs
│   │   │   ├── 📄 ProjectApiService.cs
│   │   │   ├── 📄 VoiceConnection.cs
│   │   │   └── 📄 WorkspaceMediator.cs
│   │   ├── 📁 User
│   │   │   ├── 📁 Authentication
│   │   │   │   ├── 📄 AuthService.cs
│   │   │   │   └── 📄 IAuthService.cs
│   │   │   └── 📁 Validation
│   │   │       └── 📄 ValidationService.cs
│   │   ├── 📁 Wikis
│   │   │   ├── 📄 IWikiApiService.cs
│   │   │   └── 📄 WikiApiService.cs
│   │   ├── 📄 ConfigurationService.cs
│   │   ├── 📄 LocalCacheManager.cs
│   │   ├── 📄 ServiceLocator.cs
│   │   └── 📄 SessionManager.cs
│   ├── 📁 Themes
│   │   ├── 📄 DarkTheme.xaml
│   │   ├── 📄 LightTheme.xaml
│   │   └── 📄 SpaceTheme.xaml
│   ├── 📁 ViewModels
│   │   ├── 📁 Manuscripts
│   │   │   └── 📄 ManuscriptEditorViewModel.cs
│   │   ├── 📁 Projects
│   │   │   ├── 📄 ProjectListViewModel.cs
│   │   │   ├── 📄 PublicProjectsViewModel.cs
│   │   │   ├── 📄 ReaderWorkspaceViewModel.cs
│   │   │   ├── 📄 VoicePanelViewModel.cs
│   │   │   ├── 📄 VoiceParticipantViewModel.cs
│   │   │   └── 📄 WorkspaceViewModel.cs
│   │   ├── 📁 User
│   │   │   ├── 📄 LoginViewModel.cs
│   │   │   ├── 📄 SettingsViewModel.cs
│   │   │   └── 📄 SignUpViewModel.cs
│   │   └── 📁 Wikis
│   │       ├── 📄 NarrativeGraphViewModel.cs
│   │       └── 📄 WikiEntityEditorViewModel.cs
│   ├── 📁 Views
│   │   ├── 📁 Manuscripts
│   │   │   ├── 📄 ImageResizerAdorner.cs
│   │   │   ├── 📄 ManuscriptEditorView.xaml
│   │   │   └── 📄 ManuscriptEditorView.xaml.cs
│   │   ├── 📁 Projects
│   │   │   ├── 📄 ProjectListView.xaml
│   │   │   ├── 📄 ProjectListView.xaml.cs
│   │   │   ├── 📄 PublicProjectsView.xaml
│   │   │   ├── 📄 PublicProjectsView.xaml.cs
│   │   │   ├── 📄 ReaderWorkspaceView.xaml
│   │   │   ├── 📄 ReaderWorkspaceView.xaml.cs
│   │   │   ├── 📄 VoicePanelView.xaml
│   │   │   ├── 📄 VoicePanelView.xaml.cs
│   │   │   ├── 📄 WorkspaceView.xaml
│   │   │   └── 📄 WorkspaceView.xaml.cs
│   │   ├── 📁 User
│   │   │   ├── 📄 LoginView.xaml
│   │   │   ├── 📄 LoginView.xaml.cs
│   │   │   ├── 📄 SettingsView.xaml
│   │   │   ├── 📄 SettingsView.xaml.cs
│   │   │   ├── 📄 SignUpView.xaml
│   │   │   └── 📄 SignUpView.xaml.cs
│   │   └── 📁 Wikis
│   │       ├── 📄 NarrativeGraphView.xaml
│   │       ├── 📄 NarrativeGraphView.xaml.cs
│   │       ├── 📄 WikiEntityEditorView.xaml
│   │       └── 📄 WikiEntityEditorView.xaml.cs
│   ├── ⚙️ .gitignore
│   ├── 📄 App.xaml
│   ├── 📄 App.xaml.cs
│   ├── 📄 AssemblyInfo.cs
│   ├── 📝 FileTree.md
│   ├── 📄 Layla.Desktop.csproj
│   ├── 📄 Layla.Desktop.csproj.lscache
│   ├── 📄 Layla.Desktop.sln
│   └── 📝 README.md
├── 📁 client-web
│   ├── 📁 Application
│   │   ├── 📁 Config
│   │   │   ├── 📁 Http
│   │   │   │   ├── 📄 ApiClient.cs
│   │   │   │   ├── 📄 ApiException.cs
│   │   │   │   ├── 📄 ApiRequest.cs
│   │   │   │   └── 📄 ApiResponse.cs
│   │   │   └── 📁 SignalR
│   │   │       ├── 📄 ISignalRClient.cs
│   │   │       └── 📄 SignalRClient.cs
│   │   ├── 📁 Schemas
│   │   │   ├── 📁 Auth
│   │   │   │   ├── 📄 LoginRequest.cs
│   │   │   │   ├── 📄 LoginResponse.cs
│   │   │   │   ├── 📄 RegisterRequest.cs
│   │   │   │   └── 📄 VerifyEmailRequest.cs
│   │   │   ├── 📁 Manuscripts
│   │   │   │   ├── 📄 CreateManuscript.cs
│   │   │   │   └── 📄 UpdateManuscript.cs
│   │   │   ├── 📁 Projects
│   │   │   │   ├── 📄 CreateProject.cs
│   │   │   │   └── 📄 UpdateProject.cs
│   │   │   └── 📁 Wikis
│   │   │       ├── 📄 CreateWiki.cs
│   │   │       ├── 📄 CreateWikiPage.cs
│   │   │       ├── 📄 UpdateWiki.cs
│   │   │       └── 📄 UpdateWikiPage.cs
│   │   └── 📁 Services
│   │       ├── 📁 ActiveStatusAuthor
│   │       │   ├── 📄 IConnectionService.cs
│   │       │   ├── 📄 IPresenceService.cs
│   │       │   ├── 📄 IStatusService.cs
│   │       │   └── 📄 PresenceService.cs
│   │       ├── 📁 Auth
│   │       │   ├── 📄 AuthService.cs
│   │       │   ├── 📄 IAuthService.cs
│   │       │   ├── 📄 LaylaAuthenticationStateProvider.cs
│   │       │   └── 📄 NoopAuthenticationHandler.cs
│   │       ├── 📁 Projects
│   │       │   ├── 📄 IProjectService.cs
│   │       │   └── 📄 ProjectService.cs
│   │       ├── 📁 Session
│   │       │   ├── 📄 ISessionManager.cs
│   │       │   └── 📄 SessionManager.cs
│   │       └── 📁 Voice
│   │           ├── 📄 IAudioService.cs
│   │           ├── 📄 IConnectionService.cs
│   │           ├── 📄 IRoomService.cs
│   │           ├── 📄 IVoiceService.cs
│   │           └── 📄 VoiceService.cs
│   ├── 📁 Config
│   │   ├── 📄 HttpClientConfig.cs
│   │   ├── 📄 Secrets.cs
│   │   └── 📄 Services.cs
│   ├── 📁 Helpers
│   │   ├── 📁 Validation
│   │   │   ├── 📄 ValidationResult.cs
│   │   │   └── 📄 ValidationService.cs
│   │   ├── 📄 EncryptData.cs
│   │   └── 📄 FormatDate.cs
│   ├── 📁 Models
│   │   ├── 📁 Authentication
│   │   │   └── 📄 AuthResult.cs
│   │   ├── 📄 AppUser.cs
│   │   ├── 📄 CreateProjectRequest.cs
│   │   ├── 📄 Project.cs
│   │   ├── 📄 ProjectRole.cs
│   │   └── 📄 UpdateProjectRequest.cs
│   ├── 📁 Properties
│   │   └── ⚙️ launchSettings.json
│   ├── 📁 UI
│   │   ├── 📁 Components
│   │   │   ├── 📄 ProjectCard.razor
│   │   │   ├── 📄 ProjectCard2.razor
│   │   │   └── 📄 RedirectToLogin.razor
│   │   ├── 📁 Layout
│   │   │   ├── 📄 LayoutEmpty.razor
│   │   │   ├── 📄 MainLayout.razor
│   │   │   ├── 📄 NavMenu.razor
│   │   │   └── 🎨 NavMenu.razor.css
│   │   ├── 📁 Pages
│   │   │   ├── 📁 Admin
│   │   │   │   ├── 📄 Dashboard.razor
│   │   │   │   ├── 🎨 Dashboard.razor.css
│   │   │   │   └── 📄 ManageUser.razor
│   │   │   ├── 📁 Auth
│   │   │   │   ├── 📄 Login.razor
│   │   │   │   └── 📄 Register.razor
│   │   │   ├── 📁 Errors
│   │   │   │   └── 📄 Error.razor
│   │   │   ├── 📁 Projects
│   │   │   │   └── 📄 MyProjects.razor
│   │   │   ├── 📁 Voice
│   │   │   │   └── 📄 VoiceRoom.razor
│   │   │   ├── 📄 About.razor
│   │   │   ├── 📄 Home.razor
│   │   │   └── 📄 Nothing.razor
│   │   ├── 📁 Styles
│   │   │   └── 🎨 Styles.css
│   │   ├── 📄 App.razor
│   │   ├── 📄 Routes.razor
│   │   └── 📄 _Imports.razor
│   ├── 📁 wwwroot
│   │   ├── 📁 js
│   │   │   ├── 📄 chartInterop.js
│   │   │   ├── 📄 chartInterop.ts
│   │   │   └── 📄 voiceAudio.js
│   │   ├── 📁 styles
│   │   │   └── 🎨 styles.css
│   │   └── 🖼️ favicon.png
│   ├── ⚙️ .dockerignore
│   ├── ⚙️ .gitignore
│   ├── 🐳 Dockerfile
│   ├── 📝 FileTree.md
│   ├── 📄 Program.cs
│   ├── 📝 README.md
│   ├── ⚙️ appsettings.Development.json
│   ├── 📄 client-web.csproj
│   ├── 📄 client-web.csproj.lscache
│   ├── 📄 client-web.sln
│   ├── ⚙️ package.json
│   └── ⚙️ tsconfig.json
├── 📁 infraestructure-api_gateway
│   ├── 📁 Middlewares
│   │   └── 📄 CorrelationIdTransform.cs
│   ├── 📁 Policies
│   │   ├── 📄 MinReplicasActivePolicy .cs
│   │   └── 📄 MinReplicasPassivePolicy.cs
│   ├── 📁 Properties
│   │   └── ⚙️ launchSettings.json
│   ├── ⚙️ .dockerignore
│   ├── ⚙️ .gitignore
│   ├── 🐳 Dockerfile
│   ├── 📄 Program.cs
│   ├── 📝 Readme.md
│   ├── 📄 api-gateway.csproj
│   ├── 📄 api-gateway.csproj.lscache
│   ├── 📄 api-gateway.http
│   ├── 📄 api-gateway.sln
│   └── ⚙️ appsettings.Development.json
├── 📁 server-core
│   ├── 📁 Layla.Api
│   │   ├── 📁 Certs
│   │   │   └── 📄 aspnetapp.pfx
│   │   ├── 📁 Config
│   │   │   ├── 📄 Secrets.cs
│   │   │   ├── 📄 Secure.cs
│   │   │   └── 📄 Services.cs
│   │   ├── 📁 Controllers
│   │   │   ├── 📄 ApiControllerBase.cs
│   │   │   ├── 📄 ProjectsController.cs
│   │   │   ├── 📄 TokensController.cs
│   │   │   └── 📄 UsersController.cs
│   │   ├── 📁 Extensions
│   │   │   └── 📄 ClaimsPrincipalExtensions.cs
│   │   ├── 📁 Filters
│   │   │   └── 📄 RequireUserIdFilter.cs
│   │   ├── 📁 Hubs
│   │   │   ├── 📄 PresenceHub.cs
│   │   │   └── 📄 VoiceHub.cs
│   │   ├── 📁 Middleware
│   │   │   ├── 📄 GlobalExceptionMiddleware.cs
│   │   │   └── 📄 TokenVersionValidator.cs
│   │   ├── 📁 Properties
│   │   │   └── ⚙️ launchSettings.json
│   │   ├── 🐳 Dockerfile
│   │   ├── 📝 FileTree.md
│   │   ├── 📄 Layla.Api.csproj
│   │   ├── 📄 Layla.Api.csproj.lscache
│   │   ├── 📄 Layla.Api.http
│   │   ├── 📄 Program.cs
│   │   └── ⚙️ appsettings.Development.json
│   ├── 📁 Layla.Core
│   │   ├── 📁 Common
│   │   │   ├── 📄 ErrorCode.cs
│   │   │   └── 📄 Result.cs
│   │   ├── 📁 Configuration
│   │   │   ├── 📄 EmailSettings.cs
│   │   │   └── 📄 JwtSettings.cs
│   │   ├── 📁 Constants
│   │   │   ├── 📄 AppRoles.cs
│   │   │   ├── 📄 ClaimNames.cs
│   │   │   ├── 📄 HttpContextConstants.cs
│   │   │   ├── 📄 HubConstants.cs
│   │   │   ├── 📄 MessagingConstants.cs
│   │   │   └── 📄 ProjectRoles.cs
│   │   ├── 📁 Contracts
│   │   │   ├── 📁 AppUser
│   │   │   │   ├── 📄 UpdateAppUserRequestDto.cs
│   │   │   │   └── 📄 UserResponseDto.cs
│   │   │   ├── 📁 Auth
│   │   │   │   ├── 📄 AuthResponseDto.cs
│   │   │   │   ├── 📄 LoginRequestDto.cs
│   │   │   │   ├── 📄 RegisterRequestDto.cs
│   │   │   │   └── 📄 VerifyEmailRequestDto.cs
│   │   │   ├── 📁 Manuscript
│   │   │   │   └── 📄 ManuscriptDtos.cs
│   │   │   ├── 📁 Project
│   │   │   │   ├── 📄 CollaboratorResponseDto.cs
│   │   │   │   ├── 📄 CreateProjectRequestDto.cs
│   │   │   │   ├── 📄 InviteCollaboratorRequestDto.cs
│   │   │   │   ├── 📄 ProjectResponseDto.cs
│   │   │   │   └── 📄 UpdateProjectRequestDto.cs
│   │   │   ├── 📁 Voice
│   │   │   │   └── 📄 VoiceDtos.cs
│   │   │   ├── 📁 Wiki
│   │   │   │   └── 📄 WikiDtos.cs
│   │   │   └── 📄 ParticipantPresenceDto.cs
│   │   ├── 📁 Entities
│   │   │   ├── 📄 AppUser.cs
│   │   │   ├── 📄 Project.cs
│   │   │   └── 📄 ProjectRole.cs
│   │   ├── 📁 Events
│   │   │   └── 📄 ProjectCreatedEvent.cs
│   │   ├── 📁 Extensions
│   │   │   ├── 📄 IdentityErrorFormatter.cs
│   │   │   └── 📄 ServiceCollectionExtensions.cs
│   │   ├── 📁 IntegrationEvents
│   │   │   └── 📄 ProjectCreatedEvent.cs
│   │   ├── 📁 Interfaces
│   │   │   ├── 📁 Data
│   │   │   │   ├── 📄 IAppUserRepository.cs
│   │   │   │   ├── 📄 IProjectRepository.cs
│   │   │   │   └── 📄 ITransactionalRepository.cs
│   │   │   ├── 📁 Queue
│   │   │   │   ├── 📄 IEventBus.cs
│   │   │   │   ├── 📄 IEventPublisher.cs
│   │   │   │   └── 📄 IPublisher.cs
│   │   │   ├── 📁 Services
│   │   │   │   ├── 📄 IAppUserService.cs
│   │   │   │   ├── 📄 IAuthService.cs
│   │   │   │   ├── 📄 IEmailService.cs
│   │   │   │   ├── 📄 IProjectService.cs
│   │   │   │   └── 📄 ITokenService.cs
│   │   │   ├── 📄 IPresenceTracker.cs
│   │   │   └── 📄 IVoiceRoomManager.cs
│   │   ├── 📁 Services
│   │   │   ├── 📄 AppUserService.cs
│   │   │   ├── 📄 BaseService.cs
│   │   │   ├── 📄 ProjectService.cs
│   │   │   └── 📄 TokenService.cs
│   │   ├── 📝 FileTree.md
│   │   ├── 📄 Layla.Core.csproj
│   │   └── 📄 Layla.Core.csproj.lscache
│   ├── 📁 Layla.Infrastructure
│   │   ├── 📁 Data
│   │   │   ├── 📁 Repositories
│   │   │   │   ├── 📄 AppUserRepository.cs
│   │   │   │   ├── 📄 ProjectRepository.cs
│   │   │   │   └── 📄 TransactionalRepository.cs
│   │   │   └── 📄 ApplicationDbContext.cs
│   │   ├── 📁 Extensions
│   │   │   └── 📄 ServiceCollectionExtensions.cs
│   │   ├── 📁 Migrations
│   │   │   ├── 📄 20260224061649_InitialCreate.Designer.cs
│   │   │   ├── 📄 20260224061649_InitialCreate.cs
│   │   │   ├── 📄 20260225180803_AddProjectEntities.Designer.cs
│   │   │   ├── 📄 20260225180803_AddProjectEntities.cs
│   │   │   ├── 📄 20260226050300_UpdateProjectConfiguration.Designer.cs
│   │   │   ├── 📄 20260226050300_UpdateProjectConfiguration.cs
│   │   │   ├── 📄 20260226225648_AddTokenVersionToUsers.Designer.cs
│   │   │   ├── 📄 20260226225648_AddTokenVersionToUsers.cs
│   │   │   ├── 📄 20260313220544_PendingModelChanges.Designer.cs
│   │   │   ├── 📄 20260313220544_PendingModelChanges.cs
│   │   │   ├── 📄 20260315234106_UpdateProjectModel_20260315174059.Designer.cs
│   │   │   ├── 📄 20260315234106_UpdateProjectModel_20260315174059.cs
│   │   │   ├── 📄 20260324031747_AddPerformanceIndexes.Designer.cs
│   │   │   ├── 📄 20260324031747_AddPerformanceIndexes.cs
│   │   │   ├── 📄 20260326220508_AddProjectAndRoles.Designer.cs
│   │   │   ├── 📄 20260326220508_AddProjectAndRoles.cs
│   │   │   └── 📄 ApplicationDbContextModelSnapshot.cs
│   │   ├── 📁 Queue
│   │   │   ├── 📄 Connection.cs
│   │   │   ├── 📄 Consumer.cs
│   │   │   ├── 📄 EventBusAdapter.cs
│   │   │   └── 📄 Publisher.cs
│   │   ├── 📁 Services
│   │   │   ├── 📄 AuthService.cs
│   │   │   ├── 📄 EmailService.cs
│   │   │   ├── 📄 PresenceTracker.cs
│   │   │   └── 📄 VoiceRoomManager.cs
│   │   ├── 📝 FileTree.md
│   │   ├── 📄 Layla.Infrastructure.csproj
│   │   └── 📄 Layla.Infrastructure.csproj.lscache
│   ├── ⚙️ .gitignore
│   ├── 📝 FileTree.md
│   ├── 📄 Layla.Core.slnx
│   ├── 📝 README.md
│   └── 📄 migration.sql
├── 📁 server-worldbuilding
│   ├── 📁 src
│   │   ├── 📁 config
│   │   │   └── 📄 env.ts
│   │   ├── 📁 consumers
│   │   │   └── 📄 projectCreated.consumer.ts
│   │   ├── 📁 controllers
│   │   │   ├── 📄 Graph.controller.ts
│   │   │   ├── 📄 Manuscripts.controller.ts
│   │   │   └── 📄 Wiki.controller.ts
│   │   ├── 📁 db
│   │   │   ├── 📄 mongoose.ts
│   │   │   └── 📄 neo4j.ts
│   │   ├── 📁 docs
│   │   │   └── 📄 swagger.ts
│   │   ├── 📁 interfaces
│   │   │   ├── 📁 auth
│   │   │   │   ├── 📄 AuthRequest.ts
│   │   │   │   ├── 📄 JwtPayloadCustom.ts
│   │   │   │   └── 📄 TokenPair.ts
│   │   │   ├── 📁 graph
│   │   │   │   └── 📄 IGraphResult.ts
│   │   │   ├── 📁 manuscript
│   │   │   │   └── 📄 IManuscript.ts
│   │   │   ├── 📁 repositories
│   │   │   │   ├── 📄 IGraphRepository.ts
│   │   │   │   ├── 📄 IManuscriptRepository.ts
│   │   │   │   └── 📄 IWikiEntryRepository.ts
│   │   │   └── 📁 wiki
│   │   │       └── 📄 IWikiEntry.ts
│   │   ├── 📁 middlewares
│   │   │   ├── 📄 Auth.ts
│   │   │   ├── 📄 ProjectGuard.ts
│   │   │   └── 📄 RateLimiter.ts
│   │   ├── 📁 models
│   │   │   ├── 📄 Manuscript.model.ts
│   │   │   └── 📄 WikiEntry.model.ts
│   │   ├── 📁 repositories
│   │   │   ├── 📄 MongooseManuscriptRepository.ts
│   │   │   ├── 📄 MongooseWikiEntryRepository.ts
│   │   │   └── 📄 Neo4jGraphRepository.ts
│   │   ├── 📁 routes
│   │   │   ├── 📄 Graph.ts
│   │   │   ├── 📄 Manuscripts.ts
│   │   │   └── 📄 Wiki.ts
│   │   ├── 📁 services
│   │   │   ├── 📄 Graph.service.ts
│   │   │   ├── 📄 Manuscript.service.ts
│   │   │   ├── 📄 Mention.service.ts
│   │   │   ├── 📄 WikiEntry.service.ts
│   │   │   └── 📄 container.ts
│   │   ├── 📁 utils
│   │   │   ├── 📄 ManageJWT.ts
│   │   │   └── 📄 asyncHandler.ts
│   │   ├── 📁 validation
│   │   │   └── 📄 index.ts
│   │   ├── 📁 workers
│   │   │   └── 📄 neo4jSyncWorker.ts
│   │   └── 📄 index.ts
│   ├── ⚙️ .dockerignore
│   ├── ⚙️ .gitignore
│   ├── 🐳 Dockerfile
│   ├── 📝 FileTree.md
│   ├── 📝 README.md
│   ├── 📄 eslint.config.mts
│   ├── 📄 example.http
│   ├── ⚙️ package.json
│   └── ⚙️ tsconfig.json
├── ⚙️ .gitignore
├── 📝 FileTree.md
├── 📝 README.md
└── ⚙️ docker-compose.yml
```

---
*Generated by FileTree Pro Extension*