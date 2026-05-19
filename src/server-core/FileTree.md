# File Tree: server-core

```
├── 📁 Layla.Api
│   ├── 📁 Certs
│   │   └── 📄 aspnetapp.pfx
│   ├── 📁 Config
│   │   ├── 📄 Builder.cs
│   │   ├── 📄 Secrets.cs
│   │   ├── 📄 Secure.cs
│   │   └── 📄 Services.cs
│   ├── 📁 Controllers
│   │   ├── 📄 ApiControllerBase.cs
│   │   ├── 📄 ProjectsController.cs
│   │   ├── 📄 TokensController.cs
│   │   └── 📄 UsersController.cs
│   ├── 📁 Extensions
│   │   └── 📄 ClaimsPrincipalExtensions.cs
│   ├── 📁 Filters
│   │   └── 📄 RequireUserIdFilter.cs
│   ├── 📁 Hubs
│   │   ├── 📄 PresenceHub.cs
│   │   └── 📄 VoiceHub.cs
│   ├── 📁 Middleware
│   │   ├── 📄 GlobalExceptionMiddleware.cs
│   │   └── 📄 TokenVersionValidator.cs
│   ├── 📁 Properties
│   │   └── ⚙️ launchSettings.json
│   ├── 🐳 Dockerfile
│   ├── 📝 FileTree.md
│   ├── 📄 Layla.Api.csproj
│   ├── 📄 Layla.Api.csproj.lscache
│   ├── 📄 Layla.Api.http
│   ├── 📄 Program.cs
│   └── ⚙️ appsettings.Development.json
├── 📁 Layla.Core
│   ├── 📁 Common
│   │   ├── 📄 ErrorCode.cs
│   │   └── 📄 Result.cs
│   ├── 📁 Configuration
│   │   ├── 📄 EmailSettings.cs
│   │   └── 📄 JwtSettings.cs
│   ├── 📁 Constants
│   │   ├── 📄 AppRoles.cs
│   │   ├── 📄 ClaimNames.cs
│   │   ├── 📄 HttpContextConstants.cs
│   │   ├── 📄 HubConstants.cs
│   │   ├── 📄 MessagingConstants.cs
│   │   └── 📄 ProjectRoles.cs
│   ├── 📁 Contracts
│   │   ├── 📁 AppUser
│   │   │   ├── 📄 UpdateAppUserRequestDto.cs
│   │   │   └── 📄 UserResponseDto.cs
│   │   ├── 📁 Auth
│   │   │   ├── 📄 AuthResponseDto.cs
│   │   │   ├── 📄 LoginRequestDto.cs
│   │   │   ├── 📄 RegisterRequestDto.cs
│   │   │   └── 📄 VerifyEmailRequestDto.cs
│   │   ├── 📁 Manuscript
│   │   │   └── 📄 ManuscriptDtos.cs
│   │   ├── 📁 Project
│   │   │   ├── 📄 CollaboratorResponseDto.cs
│   │   │   ├── 📄 CreateProjectRequestDto.cs
│   │   │   ├── 📄 InviteCollaboratorRequestDto.cs
│   │   │   ├── 📄 ProjectResponseDto.cs
│   │   │   └── 📄 UpdateProjectRequestDto.cs
│   │   ├── 📁 Voice
│   │   │   └── 📄 VoiceDtos.cs
│   │   ├── 📁 Wiki
│   │   │   └── 📄 WikiDtos.cs
│   │   └── 📄 ParticipantPresenceDto.cs
│   ├── 📁 Entities
│   │   ├── 📄 AppUser.cs
│   │   ├── 📄 Project.cs
│   │   └── 📄 ProjectRole.cs
│   ├── 📁 Events
│   │   └── 📄 ProjectCreatedEvent.cs
│   ├── 📁 Extensions
│   │   ├── 📄 IdentityErrorFormatter.cs
│   │   └── 📄 ServiceCollectionExtensions.cs
│   ├── 📁 IntegrationEvents
│   │   └── 📄 ProjectCreatedEvent.cs
│   ├── 📁 Interfaces
│   │   ├── 📁 Data
│   │   │   ├── 📄 IAppUserRepository.cs
│   │   │   ├── 📄 IProjectRepository.cs
│   │   │   └── 📄 ITransactionalRepository.cs
│   │   ├── 📁 Queue
│   │   │   ├── 📄 IEventBus.cs
│   │   │   ├── 📄 IEventPublisher.cs
│   │   │   └── 📄 IPublisher.cs
│   │   ├── 📁 Services
│   │   │   ├── 📄 IAppUserService.cs
│   │   │   ├── 📄 IAuthService.cs
│   │   │   ├── 📄 IEmailService.cs
│   │   │   ├── 📄 IProjectService.cs
│   │   │   └── 📄 ITokenService.cs
│   │   ├── 📄 IPresenceTracker.cs
│   │   └── 📄 IVoiceRoomManager.cs
│   ├── 📁 Services
│   │   ├── 📄 AppUserService.cs
│   │   ├── 📄 BaseService.cs
│   │   ├── 📄 ProjectService.cs
│   │   └── 📄 TokenService.cs
│   ├── 📝 FileTree.md
│   ├── 📄 Layla.Core.csproj
│   └── 📄 Layla.Core.csproj.lscache
├── 📁 Layla.Infrastructure
│   ├── 📁 Data
│   │   ├── 📁 Repositories
│   │   │   ├── 📄 AppUserRepository.cs
│   │   │   ├── 📄 ProjectRepository.cs
│   │   │   └── 📄 TransactionalRepository.cs
│   │   └── 📄 ApplicationDbContext.cs
│   ├── 📁 Extensions
│   │   └── 📄 ServiceCollectionExtensions.cs
│   ├── 📁 Migrations
│   │   ├── 📄 20260224061649_InitialCreate.Designer.cs
│   │   ├── 📄 20260224061649_InitialCreate.cs
│   │   ├── 📄 20260225180803_AddProjectEntities.Designer.cs
│   │   ├── 📄 20260225180803_AddProjectEntities.cs
│   │   ├── 📄 20260226050300_UpdateProjectConfiguration.Designer.cs
│   │   ├── 📄 20260226050300_UpdateProjectConfiguration.cs
│   │   ├── 📄 20260226225648_AddTokenVersionToUsers.Designer.cs
│   │   ├── 📄 20260226225648_AddTokenVersionToUsers.cs
│   │   ├── 📄 20260313220544_PendingModelChanges.Designer.cs
│   │   ├── 📄 20260313220544_PendingModelChanges.cs
│   │   ├── 📄 20260315234106_UpdateProjectModel_20260315174059.Designer.cs
│   │   ├── 📄 20260315234106_UpdateProjectModel_20260315174059.cs
│   │   ├── 📄 20260324031747_AddPerformanceIndexes.Designer.cs
│   │   ├── 📄 20260324031747_AddPerformanceIndexes.cs
│   │   ├── 📄 20260326220508_AddProjectAndRoles.Designer.cs
│   │   ├── 📄 20260326220508_AddProjectAndRoles.cs
│   │   └── 📄 ApplicationDbContextModelSnapshot.cs
│   ├── 📁 Queue
│   │   ├── 📄 Connection.cs
│   │   ├── 📄 Consumer.cs
│   │   ├── 📄 EventBusAdapter.cs
│   │   └── 📄 Publisher.cs
│   ├── 📁 Services
│   │   ├── 📄 AuthService.cs
│   │   ├── 📄 EmailService.cs
│   │   ├── 📄 PresenceTracker.cs
│   │   └── 📄 VoiceRoomManager.cs
│   ├── 📝 FileTree.md
│   ├── 📄 Layla.Infrastructure.csproj
│   └── 📄 Layla.Infrastructure.csproj.lscache
├── ⚙️ .gitignore
├── 📄 Layla.Core.slnx
├── 📝 README.md
└── 📄 migration.sql
```

---
*Generated by FileTree Pro Extension*