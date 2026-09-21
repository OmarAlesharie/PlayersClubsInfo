# PlayersClubsInfo

PlayersClubsInfo is a Practice RESTful backend API Project for managing football clubs and players, with user authentication, role-based authorization, JWT access tokens, refresh-token rotation, token revocation, and PostgreSQL persistence.

The project is implemented as an ASP.NET Core Web API and is designed as a practical backend project demonstrating CRUD operations, Entity Framework Core, ASP.NET Core Identity, JWT authentication, role-based authorization, PostgreSQL, and Docker-based development.

## Table of Contents

- [Project Purpose](#project-purpose)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Database](#database)
- [Database Schema](#database-schema)
- [Relationships](#relationships)
- [Authentication and Authorization](#authentication-and-authorization)
- [User Roles and Permissions](#user-roles-and-permissions)
- [API Authentication](#api-authentication)
- [API Reference](#api-reference)
  - [Authentication](#authentication)
  - [Clubs](#clubs)
  - [Players](#players)
  - [Users](#users)
- [HTTP Status Codes](#http-status-codes)
- [Example API Workflow](#example-api-workflow)
- [Entity Framework Core Migrations](#entity-framework-core-migrations)
- [Docker Development Environment](#docker-development-environment)
  - [Fedora / SELinux: Docker Secret Permission Denied](#fedora--selinux-docker-secret-permission-denied)
- [Swagger and Scalar](#swagger-and-scalar)
- [Project Structure](#project-structure)
- [Security Notes](#security-notes)

---

## Project Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


The purpose of PlayersClubsInfo is to provide a backend service for maintaining football club and player information.

The API provides:

- Football club management.
- Football player management.
- Club/player relationships.
- Free-agent players.
- Player transfers between clubs.
- Player releases from clubs.
- User registration and authentication.
- Role-based authorization.
- Administrative user management.
- Short-lived JWT access tokens.
- Database-backed refresh tokens.
- Refresh-token rotation.
- Access-token revocation on logout.
- Logout from all sessions.
- Password changes.
- PostgreSQL persistence through Entity Framework Core.

The project is API-only. A separate frontend or client can consume the REST endpoints using JSON over HTTP.

---

## Technology Stack

[⬆️ Back to Table of Contents](#table-of-contents)


| Technology | Purpose |
|---|---|
| **C#** | Primary programming language |
| **.NET 10** | Application runtime and framework |
| **ASP.NET Core Web API** | REST API framework |
| **Entity Framework Core 10** | ORM and database access |
| **Npgsql EF Core Provider** | PostgreSQL integration |
| **PostgreSQL 17** | Relational database |
| **ASP.NET Core Identity** | User and role management |
| **JWT Bearer Authentication** | Access-token authentication |
| **Refresh Tokens** | Long-lived authentication continuation |
| **Swagger / Swashbuckle** | OpenAPI documentation |
| **Scalar** | Interactive API documentation |
| **Docker** | Application containerization |
| **Docker Compose** | Local multi-container development |
| **Docker Secrets** | Development secret injection |

The project targets `net10.0`. Its main authentication, Identity, EF Core, OpenAPI, PostgreSQL, Scalar, and Swagger dependencies are defined in `PlayersClubsInfo.csproj`.

---

## Architecture

[⬆️ Back to Table of Contents](#table-of-contents)


The application follows a conventional ASP.NET Core Web API structure:

```text
Client
  |
  | HTTP / JSON
  v
ASP.NET Core Web API
  |
  +-- Controllers
  |     +-- AuthController
  |     +-- ClubsController
  |     +-- PlayersController
  |     +-- UsersController
  |
  +-- Services
  |     +-- ClubService
  |     +-- PlayerService
  |     +-- TokenCleanupService
  |
  +-- Entity Framework Core
  |
  v
PostgreSQL
```

Authentication is handled by ASP.NET Core Identity and JWT Bearer authentication.

Authorization is performed using Identity roles embedded into JWT claims.

---

# Database

The application uses PostgreSQL as its relational database.

The main application database is:

```text
PlayersClubsInfo
```

The Docker development configuration uses:

```text
Host: postgres
Port: 5432
Database: PlayersClubsInfo
Username: football
```

The PostgreSQL password is supplied through a Docker Secret rather than being stored directly in the application configuration.

---

# Database Schema

The database contains two groups of tables:

1. Application/domain tables.
2. ASP.NET Core Identity tables.

## Application tables

[⬆️ Back to Table of Contents](#table-of-contents)


The application-specific tables are:

```text
Clubs
Players
RefreshTokens
RevokedAccessTokens
```

## Identity tables

[⬆️ Back to Table of Contents](#table-of-contents)


ASP.NET Core Identity creates:

```text
AspNetUsers
AspNetRoles
AspNetUserRoles
AspNetUserClaims
AspNetRoleClaims
AspNetUserLogins
AspNetUserTokens
```

EF Core also maintains:

```text
__EFMigrationsHistory
```

which records the migrations that have been applied to the database.

---

# Entity Schema

## Clubs

[⬆️ Back to Table of Contents](#table-of-contents)


The `Clubs` table stores football club information.

| Column | Type | Description |
|---|---|---|
| `Id` | integer | Primary key |
| `Name` | text | Club name |
| `City` | text | Club city |

Example:

```json
{
  "id": 1,
  "name": "Baghdad FC",
  "city": "Baghdad"
}
```

---

## Players

[⬆️ Back to Table of Contents](#table-of-contents)


The `Players` table stores football player information.

| Column | Type | Description |
|---|---|---|
| `Id` | integer | Primary key |
| `Name` | text | Player name |
| `Age` | integer | Player age |
| `Position` | text | Player position |
| `ClubId` | integer, nullable | Foreign key to `Clubs.Id` |

A player can have a `NULL` `ClubId`.

A player without a club is therefore a free agent.

Example:

```json
{
  "id": 10,
  "name": "Ali Hassan",
  "age": 24,
  "position": "Forward",
  "clubId": null,
  "clubName": null
}
```

---

## RefreshTokens

[⬆️ Back to Table of Contents](#table-of-contents)


`RefreshTokens` stores hashed refresh tokens used to obtain new JWT access tokens.

| Column | Type | Description |
|---|---|---|
| `Id` | integer | Primary key |
| `UserId` | text | FK to `AspNetUsers.Id` |
| `TokenHash` | varchar(128) | SHA-256 hash of refresh token |
| `Created` | timestamp | Creation time |
| `RevokedAt` | timestamp, nullable | Revocation time |
| `Revoked` | boolean | Whether the token has been revoked |
| `Expires` | timestamp | Expiration time |
| `ReplacedByTokenHash` | varchar(128), nullable | Hash of replacement token after rotation |

The actual refresh token is not stored directly. The API stores its SHA-256 hash.

A unique index exists on `TokenHash`.

---

## RevokedAccessTokens

[⬆️ Back to Table of Contents](#table-of-contents)


`RevokedAccessTokens` stores the JTI of invalidated JWT access tokens.

| Column | Type | Description |
|---|---|---|
| `Id` | integer | Primary key |
| `Jti` | varchar(128) | JWT ID |
| `ExpiresAt` | timestamp | Original token expiration time |

When a request contains a JWT, the JWT authentication process checks whether its JTI exists in this table.

This allows an otherwise-valid JWT to be invalidated before its normal expiration time.

---

# ASP.NET Core Identity Schema

Identity manages users and roles through the standard Identity tables.

## AspNetUsers

[⬆️ Back to Table of Contents](#table-of-contents)


The project uses `ApplicationUser`, which currently derives directly from `IdentityUser`.

Important fields include:

```text
Id
UserName
Email
EmailConfirmed
PasswordHash
SecurityStamp
ConcurrencyStamp
PhoneNumber
PhoneNumberConfirmed
TwoFactorEnabled
LockoutEnd
LockoutEnabled
AccessFailedCount
```

Passwords are handled by ASP.NET Core Identity and are stored as password hashes rather than plain text.

---

## AspNetRoles

[⬆️ Back to Table of Contents](#table-of-contents)


Stores the application's roles.

The application creates these roles automatically:

```text
Root
Manager
User
```

---

## AspNetUserRoles

[⬆️ Back to Table of Contents](#table-of-contents)


This table links users to roles.

Conceptually:

```text
AspNetUsers
    |
    | UserId
    v
AspNetUserRoles
    |
    | RoleId
    v
AspNetRoles
```

---

# Relationships

## Club -> Players

[⬆️ Back to Table of Contents](#table-of-contents)


The relationship is:

```text
Club 1 -------- * Player
```

A club can have many players.

A player belongs to zero or one club.

```text
Club
 |
 +-- Player
 +-- Player
 +-- Player
```

The foreign key is:

```text
Players.ClubId -> Clubs.Id
```

The foreign key uses:

```text
ON DELETE SET NULL
```

Therefore, deleting a club does not delete its players.

Instead, the players become free agents.

The application also explicitly releases the club's players before deleting the club.

---

## User -> RefreshTokens

[⬆️ Back to Table of Contents](#table-of-contents)


The relationship is:

```text
User 1 -------- * RefreshToken
```

A user can have multiple refresh tokens, for example when the same account is logged in from multiple devices.

Deleting a user cascades to their refresh tokens.

---

# Authentication and Authorization

The application uses two token types.

## Access Token

[⬆️ Back to Table of Contents](#table-of-contents)


The access token is a JWT.

It contains claims including:

```text
NameIdentifier
Name
Jti
Role
```

The JWT is signed using HMAC SHA-256.

The configured development lifetime is:

```text
15 minutes
```

Clients send the access token using:

```http
Authorization: Bearer <JWT>
```

---

## Refresh Token

[⬆️ Back to Table of Contents](#table-of-contents)


Refresh tokens have a longer lifetime:

```text
7 days
```

The refresh token is randomly generated and its SHA-256 hash is stored in PostgreSQL.

When the client calls:

```http
POST /api/auth/refresh
```

the current refresh token is revoked and a new refresh token is generated.

This is refresh-token rotation.

---

## Logout

[⬆️ Back to Table of Contents](#table-of-contents)


Logout performs two actions when possible:

1. Revokes the supplied refresh token.
2. Stores the current JWT's JTI in `RevokedAccessTokens`.

Therefore the current access token can be rejected immediately instead of remaining valid until its normal expiration.

---

## Logout All

[⬆️ Back to Table of Contents](#table-of-contents)


`logout-all` revokes all active refresh tokens belonging to the authenticated user.

It does not create a database record for every existing JWT access token.

Existing access tokens therefore remain subject to their normal JWT validation lifetime unless they are individually revoked through the logout operation.

---

# User Roles and Permissions

The application has three roles.

| Operation | Root | Manager | User |
|---|:---:|:---:|:---:|
| Register account | Anonymous | Anonymous | Anonymous |
| Login | Yes | Yes | Yes |
| Logout | Yes | Yes | Yes |
| Logout all sessions | Yes | Yes | Yes |
| Change own password | Yes | Yes | Yes |
| Refresh token | Yes | Yes | Yes |
| View clubs | Yes | Yes | Yes |
| View a club | Yes | Yes | Yes |
| Create club | Yes | Yes | No |
| Update club | Yes | Yes | No |
| Delete club | Yes | No | No |
| View players | Yes | Yes | Yes |
| View a player | Yes | Yes | Yes |
| Create player | Yes | Yes | No |
| Update player | Yes | Yes | No |
| Delete player | Yes | No | No |
| Transfer player | Yes | Yes | No |
| Release player | Yes | Yes | No |
| List users | Yes | No | No |
| View user | Yes | No | No |
| Create user | Yes | No | No |
| Update user | Yes | No | No |
| Delete user | Yes | No | No |
| Reset another user's password | Yes | No | No |

## Root

[⬆️ Back to Table of Contents](#table-of-contents)


`Root` is the highest-privileged application role.

Root can:

- Manage clubs.
- Delete clubs.
- Manage players.
- Delete players.
- Transfer players.
- Release players.
- Manage users.
- Change another user's password.
- Use all authenticated user operations.

The default Root account is created by the application's identity seeding process.

---

## Manager

[⬆️ Back to Table of Contents](#table-of-contents)


`Manager` can manage football data but cannot manage users.

Managers can:

- View clubs.
- Create clubs.
- Update clubs.
- View players.
- Create players.
- Update players.
- Transfer players.
- Release players.

Managers cannot:

- Delete clubs.
- Delete players.
- List users.
- Create users.
- Modify user roles.
- Delete users.
- Reset another user's password.

---

## User

[⬆️ Back to Table of Contents](#table-of-contents)


`User` has read access to football data.

Users can:

- View clubs.
- View individual clubs.
- View players.
- View individual players.
- Manage their own authentication session.
- Change their own password.

Users cannot create, update, transfer, release, or delete football data.

---

# API Authentication

For protected endpoints, send:

```http
Authorization: Bearer <access-token>
```

Example:

```http
GET /api/clubs
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

The JWT contains the user's roles, and the API uses those roles for authorization.

---

# API Reference

Base URL for the Docker development environment:

```text
http://localhost:8080
```

The examples below use JSON.

Replace:

```text
<token>
```

with the JWT returned by the login endpoint.

Replace:

```text
<refresh-token>
```

with the refresh token returned by login or refresh.

---

# Authentication

## 1. Register

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/auth/register
```

### Authentication

[⬆️ Back to Table of Contents](#table-of-contents)


Anonymous.

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Creates a new user account.

Every publicly registered account is assigned the `User` role automatically.

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "username": "ali",
  "email": "ali@example.com",
  "password": "Password123!"
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "ali",
    "email": "ali@example.com",
    "password": "Password123!"
  }'
```

### Success

[⬆️ Back to Table of Contents](#table-of-contents)


```http
201 Created
```

```json
{
  "message": "User registered successfully"
}
```

Username must be between 3 and 50 characters.

Password must be at least 8 characters.

---

## 2. Login

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/auth/login
```

### Authentication

[⬆️ Back to Table of Contents](#table-of-contents)


Anonymous.

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Authenticates a user and returns:

- JWT access token.
- Access-token expiration.
- Username.
- Roles.
- Refresh token.

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "username": "root",
  "password": "ChangeMe123!"
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "root",
    "password": "ChangeMe123!"
  }'
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "token": "<JWT>",
  "expiresAt": "2026-09-20T20:30:00Z",
  "username": "root",
  "roles": [
    "Root"
  ],
  "refreshToken": "<REFRESH_TOKEN>"
}
```

---

## 3. Logout

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/auth/logout
```

### Authentication

[⬆️ Back to Table of Contents](#table-of-contents)


Required.

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Logs the current session out.

The endpoint can revoke:

- The supplied refresh token.
- The current access token through its JTI.

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "refreshToken": "<REFRESH_TOKEN>"
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X POST http://localhost:8080/api/auth/logout \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "<refresh-token>"
  }'
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "message": "Logout successful. Refresh token revoked and access token invalidated."
}
```

---

## 4. Logout All

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/auth/logout-all
```

### Authentication

[⬆️ Back to Table of Contents](#table-of-contents)


Required.

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Revokes all non-revoked refresh tokens belonging to the current user.

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X POST http://localhost:8080/api/auth/logout-all \
  -H "Authorization: Bearer <token>"
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "message": "All refresh tokens revoked."
}
```

---

## 5. Change Own Password

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
PUT /api/auth/change-password
```

### Authentication

[⬆️ Back to Table of Contents](#table-of-contents)


Required.

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Allows the authenticated user to change their own password.

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!"
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X PUT http://localhost:8080/api/auth/change-password \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "OldPassword123!",
    "newPassword": "NewPassword123!"
  }'
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "message": "Password changed successfully."
}
```

---

## 6. Refresh Access Token

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/auth/refresh
```

### Authentication

[⬆️ Back to Table of Contents](#table-of-contents)


Anonymous.

The refresh token itself is the credential.

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Obtains a new JWT access token without requiring the user to enter their password again.

The old refresh token is revoked and replaced with a new refresh token.

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "refreshToken": "<REFRESH_TOKEN>"
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X POST http://localhost:8080/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "<refresh-token>"
  }'
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "token": "<NEW_JWT>",
  "expiresAt": "2026-09-20T21:00:00Z",
  "username": "root",
  "roles": [
    "Root"
  ],
  "refreshToken": "<NEW_REFRESH_TOKEN>"
}
```

---

# Clubs

## 7. Get All Clubs

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
GET /api/clubs
```

### Roles

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
Manager
User
```

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Returns all clubs.

Each club includes:

- ID.
- Name.
- City.
- Player count.
- Players belonging to the club.

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl http://localhost:8080/api/clubs \
  -H "Authorization: Bearer <token>"
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
[
  {
    "id": 1,
    "name": "Baghdad FC",
    "city": "Baghdad",
    "playerCount": 2,
    "players": [
      {
        "id": 1,
        "name": "Ali Hassan",
        "age": 24,
        "position": "Forward"
      },
      {
        "id": 2,
        "name": "Omar Ahmed",
        "age": 27,
        "position": "Midfielder"
      }
    ]
  }
]
```

---

## 8. Get Club by ID

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
GET /api/clubs/{id}
```

### Roles

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
Manager
User
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl http://localhost:8080/api/clubs/1 \
  -H "Authorization: Bearer <token>"
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "id": 1,
  "name": "Baghdad FC",
  "city": "Baghdad",
  "playerCount": 2,
  "players": [
    {
      "id": 1,
      "name": "Ali Hassan",
      "age": 24,
      "position": "Forward"
    }
  ]
}
```

Returns `404 Not Found` when the club does not exist.

---

## 9. Create Club

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/clubs
```

### Roles

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
Manager
```

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "name": "Basra FC",
  "city": "Basra"
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X POST http://localhost:8080/api/clubs \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Basra FC",
    "city": "Basra"
  }'
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```http
201 Created
```

```json
{
  "id": 2,
  "name": "Basra FC",
  "city": "Basra",
  "playerCount": 0,
  "players": []
}
```

---

## 10. Update Club

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
PUT /api/clubs/{id}
```

### Roles

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
Manager
```

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "name": "Basra United",
  "city": "Basra"
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X PUT http://localhost:8080/api/clubs/2 \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Basra United",
    "city": "Basra"
  }'
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


The updated club is returned using the normal club response format.

---

## 11. Delete Club

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
DELETE /api/clubs/{id}
```

### Role

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
```

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Deletes a club.

Players belonging to the club are released and become free agents instead of being deleted.

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X DELETE http://localhost:8080/api/clubs/2 \
  -H "Authorization: Bearer <root-token>"
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```http
204 No Content
```

---

# Players

## 12. Get All Players

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
GET /api/players
```

### Roles

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
Manager
User
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl http://localhost:8080/api/players \
  -H "Authorization: Bearer <token>"
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
[
  {
    "id": 1,
    "name": "Ali Hassan",
    "age": 24,
    "position": "Forward",
    "clubId": 1,
    "clubName": "Baghdad FC"
  },
  {
    "id": 2,
    "name": "Omar Ahmed",
    "age": 27,
    "position": "Midfielder",
    "clubId": null,
    "clubName": null
  }
]
```

---

## 13. Get Player by ID

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
GET /api/players/{id}
```

### Roles

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
Manager
User
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl http://localhost:8080/api/players/1 \
  -H "Authorization: Bearer <token>"
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "id": 1,
  "name": "Ali Hassan",
  "age": 24,
  "position": "Forward",
  "clubId": 1,
  "clubName": "Baghdad FC"
}
```

---

## 14. Create Player

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/players
```

### Roles

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
Manager
```

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Creates a player.

A player can optionally be assigned to a club immediately.

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "name": "Hassan Ali",
  "age": 22,
  "position": "Defender",
  "clubId": 1
}
```

To create a free agent:

```json
{
  "name": "Hassan Ali",
  "age": 22,
  "position": "Defender",
  "clubId": null
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X POST http://localhost:8080/api/players \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Hassan Ali",
    "age": 22,
    "position": "Defender",
    "clubId": 1
  }'
```

The specified club must exist.

---

## 15. Update Player

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
PUT /api/players/{id}
```

### Roles

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
Manager
```

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "name": "Hassan Ali",
  "age": 23,
  "position": "Centre Back"
}
```

The DTO currently also contains a nullable `clubId` property, but the controller does not use that property when updating the player.

Club assignment is handled explicitly by the transfer and release endpoints.

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X PUT http://localhost:8080/api/players/1 \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Hassan Ali",
    "age": 23,
    "position": "Centre Back"
  }'
```

---

## 16. Delete Player

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
DELETE /api/players/{id}
```

### Role

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X DELETE http://localhost:8080/api/players/1 \
  -H "Authorization: Bearer <root-token>"
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```http
204 No Content
```

---

## 17. Transfer Player

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/players/{id}/transfer
```

### Roles

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
Manager
```

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Moves a player to another club.

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "clubId": 2
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X POST http://localhost:8080/api/players/1/transfer \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "clubId": 2
  }'
```

The destination club must exist.

The request fails if the player already belongs to the destination club.

---

## 18. Release Player

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/players/{id}/release
```

### Roles

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
Manager
```

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Removes the player's club assignment and makes the player a free agent.

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X POST http://localhost:8080/api/players/1/release \
  -H "Authorization: Bearer <token>"
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


The updated player is returned with:

```json
{
  "id": 1,
  "name": "Ali Hassan",
  "age": 24,
  "position": "Forward",
  "clubId": null,
  "clubName": null
}
```

If the player is already a free agent, the API returns a bad-request response.

---

# Users

All `/api/users` endpoints require the `Root` role.

Managers and normal Users cannot access these endpoints.

## 19. Get All Users

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
GET /api/users
```

### Role

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl http://localhost:8080/api/users \
  -H "Authorization: Bearer <root-token>"
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
[
  {
    "id": "user-id",
    "username": "ali",
    "email": "ali@example.com",
    "roles": [
      "User"
    ],
    "emailConfirmed": false,
    "lockoutEnabled": false
  }
]
```

---

## 20. Get User by ID

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
GET /api/users/{id}
```

### Role

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl http://localhost:8080/api/users/<user-id> \
  -H "Authorization: Bearer <root-token>"
```

---

## 21. Create User

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/users
```

### Role

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
```

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Creates a user and assigns one of the existing roles.

Valid roles are:

```text
Root
Manager
User
```

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "username": "manager1",
  "email": "manager@example.com",
  "password": "Password123!",
  "role": "Manager"
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X POST http://localhost:8080/api/users \
  -H "Authorization: Bearer <root-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "manager1",
    "email": "manager@example.com",
    "password": "Password123!",
    "role": "Manager"
  }'
```

---

## 22. Update User

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
PUT /api/users/{id}
```

### Role

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
```

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Updates:

- Email.
- Role.
- Lockout setting.

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "email": "manager-new@example.com",
  "role": "Manager",
  "lockoutEnabled": true
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X PUT http://localhost:8080/api/users/<user-id> \
  -H "Authorization: Bearer <root-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "manager-new@example.com",
    "role": "Manager",
    "lockoutEnabled": true
  }'
```

The endpoint replaces the user's existing roles with the supplied role.

---

## 23. Delete User

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
DELETE /api/users/{id}
```

### Role

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X DELETE http://localhost:8080/api/users/<user-id> \
  -H "Authorization: Bearer <root-token>"
```

A Root user cannot delete their own account.

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```http
204 No Content
```

---

## 24. Reset Another User's Password

[⬆️ Back to Table of Contents](#table-of-contents)


### Endpoint

[⬆️ Back to Table of Contents](#table-of-contents)


```http
PUT /api/users/{id}/password
```

### Role

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Root
```

### Purpose

[⬆️ Back to Table of Contents](#table-of-contents)


Allows Root to set a new password for another user.

### Request

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "newPassword": "NewPassword123!"
}
```

### Example

[⬆️ Back to Table of Contents](#table-of-contents)


```bash
curl -X PUT http://localhost:8080/api/users/<user-id>/password \
  -H "Authorization: Bearer <root-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "newPassword": "NewPassword123!"
  }'
```

### Response

[⬆️ Back to Table of Contents](#table-of-contents)


```json
{
  "message": "Password changed successfully."
}
```

---

# API Permission Summary

## Authentication endpoints

[⬆️ Back to Table of Contents](#table-of-contents)


| Endpoint | Anonymous | Authenticated | Root | Manager | User |
|---|:---:|:---:|:---:|:---:|:---:|
| `POST /api/auth/register` | Yes | Yes | Yes | Yes | Yes |
| `POST /api/auth/login` | Yes | Yes | Yes | Yes | Yes |
| `POST /api/auth/logout` | No | Yes | Yes | Yes | Yes |
| `POST /api/auth/logout-all` | No | Yes | Yes | Yes | Yes |
| `PUT /api/auth/change-password` | No | Yes | Yes | Yes | Yes |
| `POST /api/auth/refresh` | Yes | Yes | Yes | Yes | Yes |

## Club endpoints

[⬆️ Back to Table of Contents](#table-of-contents)


| Endpoint | Root | Manager | User |
|---|:---:|:---:|:---:|
| `GET /api/clubs` | Yes | Yes | Yes |
| `GET /api/clubs/{id}` | Yes | Yes | Yes |
| `POST /api/clubs` | Yes | Yes | No |
| `PUT /api/clubs/{id}` | Yes | Yes | No |
| `DELETE /api/clubs/{id}` | Yes | No | No |

## Player endpoints

[⬆️ Back to Table of Contents](#table-of-contents)


| Endpoint | Root | Manager | User |
|---|:---:|:---:|:---:|
| `GET /api/players` | Yes | Yes | Yes |
| `GET /api/players/{id}` | Yes | Yes | Yes |
| `POST /api/players` | Yes | Yes | No |
| `PUT /api/players/{id}` | Yes | Yes | No |
| `DELETE /api/players/{id}` | Yes | No | No |
| `POST /api/players/{id}/transfer` | Yes | Yes | No |
| `POST /api/players/{id}/release` | Yes | Yes | No |

## User-management endpoints

[⬆️ Back to Table of Contents](#table-of-contents)


| Endpoint | Root | Manager | User |
|---|:---:|:---:|:---:|
| `GET /api/users` | Yes | No | No |
| `GET /api/users/{id}` | Yes | No | No |
| `POST /api/users` | Yes | No | No |
| `PUT /api/users/{id}` | Yes | No | No |
| `DELETE /api/users/{id}` | Yes | No | No |
| `PUT /api/users/{id}/password` | Yes | No | No |

---

# HTTP Status Codes

Common responses include:

| Status | Meaning |
|---|---|
| `200 OK` | Request completed successfully |
| `201 Created` | Resource created successfully |
| `204 No Content` | Resource deleted successfully |
| `400 Bad Request` | Invalid request or business rule violation |
| `401 Unauthorized` | Missing, invalid, expired, or revoked authentication |
| `403 Forbidden` | Authenticated user does not have the required role |
| `404 Not Found` | Requested resource does not exist |
| `409 Conflict` | Resource conflicts with an existing username/email |

---

# Example API Workflow

A normal client workflow can look like this.

## Step 1: Register

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/auth/register
```

```json
{
  "username": "manager1",
  "email": "manager@example.com",
  "password": "Password123!"
}
```

The newly registered account receives the `User` role.

## Step 2: Root changes the user's role

[⬆️ Back to Table of Contents](#table-of-contents)


Root creates or updates the user:

```http
PUT /api/users/{id}
```

```json
{
  "email": "manager@example.com",
  "role": "Manager",
  "lockoutEnabled": false
}
```

## Step 3: Manager logs in

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/auth/login
```

The API returns:

```text
Access Token
Refresh Token
Expiration
Username
Roles
```

## Step 4: Manager creates a club

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/clubs
Authorization: Bearer <token>
```

```json
{
  "name": "Baghdad FC",
  "city": "Baghdad"
}
```

## Step 5: Manager creates a player

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/players
Authorization: Bearer <token>
```

```json
{
  "name": "Ali Hassan",
  "age": 24,
  "position": "Forward",
  "clubId": 1
}
```

## Step 6: Manager transfers the player

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/players/1/transfer
Authorization: Bearer <token>
```

```json
{
  "clubId": 2
}
```

## Step 7: Access token expires

[⬆️ Back to Table of Contents](#table-of-contents)


The client uses the refresh token:

```http
POST /api/auth/refresh
```

```json
{
  "refreshToken": "<refresh-token>"
}
```

The server returns a new access token and a new refresh token.

## Step 8: Logout

[⬆️ Back to Table of Contents](#table-of-contents)


```http
POST /api/auth/logout
Authorization: Bearer <token>
```

```json
{
  "refreshToken": "<refresh-token>"
}
```

The refresh token is revoked and the current access-token JTI is recorded as revoked.

---

# Entity Framework Core Migrations

The project uses EF Core migrations to maintain the PostgreSQL schema.

Current migrations include:

```text
20260913170434_InitialCreate
20260913203531_AddIdentity
20260915222440_ConfigurePlayerClubRelationship
20260916224044_AddRevokedAccessTokens
```

The migrations create:

1. Clubs and Players.
2. ASP.NET Core Identity tables.
3. The `Player -> Club` `SET NULL` relationship.
4. Refresh-token and revoked-access-token tables.

In the Docker development configuration, migrations are applied automatically when the API starts.

The startup sequence is:

```text
Start PostgreSQL
      |
      v
PostgreSQL health check passes
      |
      v
Start API
      |
      v
EF Core MigrateAsync()
      |
      v
Seed Identity roles and Root user
      |
      v
API becomes available
```

This makes a new development database usable without manually running `Update-Database`.

---

# Docker Development Environment

The development environment contains two containers:

```text
playersclubsinfo-api
playersclubsinfo-postgres
```

The API is built from the project's Dockerfile.

PostgreSQL uses the official PostgreSQL image.

The database is persisted through the Docker volume:

```text
postgres-data
```

ASP.NET Core Data Protection keys are persisted through:

```text
aspnet-data-protection
```

Docker Secrets are used for:

```text
postgres-password
jwt-key
admin-password
```

The expected development secret files are:

```text
secrets/
├── postgres-password.txt
├── jwt-key.txt
└── admin-password.txt
```

These files should not be committed to Git.

## Fedora / SELinux: Docker Secret Permission Denied

[⬆️ Back to Table of Contents](#table-of-contents)


When running Docker Compose on Fedora with SELinux enforcing, the PostgreSQL container may fail to start even when the secret files have normal Unix permissions such as `644`. A typical error is:

```text
/run/secrets/postgres-password: Permission denied
```

Check the SELinux state and labels:

```bash
getenforce
ls -Zd secrets/
ls -Z secrets/
```

If SELinux is `Enforcing` and the `secrets/` directory and its files are labeled `user_home_t`, the container may be denied access to the host-side secret files. Do not solve this by using `chmod 777` or by disabling SELinux.

### Persistent SELinux solution

[⬆️ Back to Table of Contents](#table-of-contents)


Install the Fedora SELinux management utilities if necessary:

```bash
sudo dnf install policycoreutils-python-utils
```

From the project root, create a persistent SELinux file-context rule for the project's `secrets/` directory:

```bash
sudo semanage fcontext -a -t container_file_t "$(pwd)/secrets(/.*)?"
```

Apply the context:

```bash
sudo restorecon -Rv secrets/
```

Verify the result:

```bash
ls -Zd secrets/
ls -Z secrets/
```

The files should now have an SELinux type of `container_file_t` instead of `user_home_t`. For example:

```text
unconfined_u:object_r:container_file_t:s0 secrets/
```

After applying the persistent context, restart the Compose environment:

```bash
sudo docker compose down -v
sudo docker compose up --build
```

`semanage fcontext` is preferred over using `chcon` as the permanent solution because it records the desired file context in the SELinux policy configuration. `restorecon` then applies that policy to the files.

This solution is specific to Fedora/SELinux environments. Other Linux distributions may not require this additional SELinux configuration.

Start the development environment with:

```bash
docker compose up --build
```

Stop it with:

```bash
docker compose down
```

To remove the containers and their Compose-managed volumes and start with a completely new database:

```bash
docker compose down -v
docker compose up --build
```

---

# Swagger and Scalar

When the application runs in the Development environment, both Swagger UI and Scalar are enabled.

The OpenAPI document is exposed through Swagger.

The project configures a Bearer authentication scheme so clients can provide:

```text
Authorization: Bearer <JWT>
```

Scalar uses the same OpenAPI document.

Typical development URLs are:

```text
http://localhost:8080/swagger
http://localhost:8080/scalar/v1
```

The exact Scalar path can depend on the Scalar package/configuration version.

---

# Project Structure

```text
PlayersClubsInfo/
│
├── PlayersClubsInfo.slnx
│
├── PlayersClubsInfo/
│   │
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── ClubsController.cs
│   │   ├── PlayersController.cs
│   │   └── UsersController.cs
│   │
│   ├── Data/
│   │   ├── IdentitySeeder.cs
│   │   └── PlayersClubsInfoContext.cs
│   │
│   ├── DTOs/
│   │   ├── AuthResponseDto.cs
│   │   ├── ChangeOwnPasswordDto.cs
│   │   ├── ChangePasswordDto.cs
│   │   ├── CreateUserDto.cs
│   │   ├── LoginDto.cs
│   │   ├── RefreshRequestDto.cs
│   │   ├── RegisterDto.cs
│   │   ├── RevokeRequestDto.cs
│   │   ├── UpdateUserDto.cs
│   │   ├── UserResponseDto.cs
│   │   │
│   │   ├── Club/
│   │   │   ├── ClubResponseDto.cs
│   │   │   ├── CreateClubDto.cs
│   │   │   └── UpdateClubDto.cs
│   │   │
│   │   └── Player/
│   │       ├── CreatePlayerDto.cs
│   │       ├── PlayerResponseDto.cs
│   │       ├── TransferPlayerDto.cs
│   │       └── UpdatePlayerDto.cs
│   │
│   ├── Migrations/
│   │   ├── InitialCreate
│   │   ├── AddIdentity
│   │   ├── ConfigurePlayerClubRelationship
│   │   └── AddRevokedAccessTokens
│   │
│   ├── Models/
│   │   ├── ApplicationUser.cs
│   │   ├── Club.cs
│   │   ├── Player.cs
│   │   ├── RefreshToken.cs
│   │   └── RevokedAccessToken.cs
│   │
│   ├── Services/
│   │   ├── ClubService.cs
│   │   ├── PlayerService.cs
│   │   └── TokenCleanupService.cs
│   │
│   ├── Program.cs
│   ├── PlayersClubsInfo.csproj
│   └── appsettings.json
│
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
└── README.md
```

---

# Security Notes

## Passwords

[⬆️ Back to Table of Contents](#table-of-contents)


Passwords are managed by ASP.NET Core Identity and are not stored as plain text.

## JWT Signing Key

[⬆️ Back to Table of Contents](#table-of-contents)


The JWT signing key should be kept outside source control.

For Docker development, use the Docker Secret:

```text
jwt-key
```

## PostgreSQL Password

[⬆️ Back to Table of Contents](#table-of-contents)


The PostgreSQL password should be supplied through:

```text
postgres-password
```

rather than being committed to `appsettings.json`.

## Root Password

[⬆️ Back to Table of Contents](#table-of-contents)


The default development Root password is supplied through:

```text
admin-password
```

Change development credentials before using the application in any real environment.


## Detailed Authentication & Token Management (JWT + Refresh Tokens + JTI Revocation)

[⬆️ Back to Table of Contents](#table-of-contents)


This section documents the authentication architecture implemented by the project. It covers JWT access tokens, refresh tokens, JTI-based access-token revocation, refresh-token rotation, logout, logout-all, and background token cleanup.

### Architecture overview

[⬆️ Back to Table of Contents](#table-of-contents)


The project uses a two-token model:

```text
Client
  │
  ├── Login
  ▼
ASP.NET Core API
  │
  ├───────────────┬────────────────
  ▼               ▼
JWT Access      Refresh Token
Token            (random secret)
(short-lived)       │
  │                 │ SHA-256
  │                 ▼
  │           RefreshTokens table
  │           (hash only)
  │
  └── JTI
       │
       ▼
RevokedAccessTokens
```

- **Access token:** a short-lived, signed JWT used on normal API requests.
- **Refresh token:** a long-lived cryptographically random value used to obtain a new access token.
- **JTI:** a unique identifier contained in each access token. It allows the server to invalidate a JWT before its normal expiration time.
- Refresh tokens are stored server-side only as SHA-256 hashes.
- Refresh tokens are rotated when used.
- Revoked access-token JTIs are checked by the JWT bearer validation pipeline.

### Access token (JWT)

[⬆️ Back to Table of Contents](#table-of-contents)


`AuthController.GenerateJwtToken(...)` creates the access token.

The JWT contains:

- `ClaimTypes.NameIdentifier` — user ID.
- `ClaimTypes.Name` — username.
- `ClaimTypes.Role` — user roles.
- `JwtRegisteredClaimNames.Jti` — unique GUID for this token.

The token is signed with HMAC-SHA256 using `Jwt:Key` and expires according to `Jwt:ExpirationMinutes`.

```csharp
private (string Token, DateTime ExpiresAt) GenerateJwtToken(
    ApplicationUser user,
    IList<string> roles)
{
    var jwtKey = _configuration["Jwt:Key"]!;
    var issuer = _configuration["Jwt:Issuer"]!;
    var audience = _configuration["Jwt:Audience"]!;
    var expirationMinutes =
        _configuration.GetValue<int>("Jwt:ExpirationMinutes");

    var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Name, user.UserName!),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    foreach (var role in roles)
        claims.Add(new Claim(ClaimTypes.Role, role));

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwtKey));

    var credentials = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: expiresAt,
        signingCredentials: credentials);

    return (
        new JwtSecurityTokenHandler().WriteToken(token),
        expiresAt);
}
```

### JWT validation and JTI revocation

[⬆️ Back to Table of Contents](#table-of-contents)


Normal JWT validation checks the issuer, audience, lifetime, and signing key. The project adds a database-backed revocation check after successful JWT validation.

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,

    ValidIssuer = builder.Configuration["Jwt:Issuer"],
    ValidAudience = builder.Configuration["Jwt:Audience"],

    IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:Key"]!)),

    ClockSkew = TimeSpan.FromSeconds(30)
};

options.Events = new JwtBearerEvents
{
    OnTokenValidated = async context =>
    {
        var jti = context.Principal?
            .FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        if (string.IsNullOrEmpty(jti))
            return;

        var db = context.HttpContext.RequestServices
            .GetRequiredService<PlayersClubsInfoContext>();

        var revoked = await db.RevokedAccessTokens
            .AnyAsync(r => r.Jti == jti);

        if (revoked)
            context.Fail("Token has been revoked.");
    }
};
```

Therefore, a JWT can be cryptographically valid but still rejected because its JTI has been revoked.

---

### Refresh tokens

[⬆️ Back to Table of Contents](#table-of-contents)


Refresh tokens are different from JWT access tokens:

- 64 cryptographically random bytes are generated.
- The value is Base64 encoded.
- The raw token is returned to the client.
- Only its SHA-256 hash is stored in PostgreSQL.
- The example lifetime is 7 days.
- The token can be revoked and rotated independently of the access token.

#### RefreshToken model

[⬆️ Back to Table of Contents](#table-of-contents)


```csharp
public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public DateTime Created { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool Revoked { get; set; }
    public DateTime Expires { get; set; }
    public string? ReplacedByTokenHash { get; set; }
}
```

#### Generating and hashing refresh tokens

[⬆️ Back to Table of Contents](#table-of-contents)


```csharp
private static string GenerateSecureRefreshToken()
{
    var bytes = new byte[64];

    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(bytes);

    return Convert.ToBase64String(bytes);
}

private static string HashToken(string token)
{
    using var sha = SHA256.Create();

    var bytes = Encoding.UTF8.GetBytes(token);
    var hash = sha.ComputeHash(bytes);

    return Convert.ToBase64String(hash);
}
```

The database therefore contains the hash rather than the usable refresh-token secret.

---

### Login: issue access + refresh tokens

[⬆️ Back to Table of Contents](#table-of-contents)


After successful password validation:

```csharp
var refreshToken = GenerateSecureRefreshToken();

var refreshTokenRecord = new RefreshToken
{
    UserId = user.Id,
    TokenHash = HashToken(refreshToken),
    Created = DateTime.UtcNow,
    Expires = DateTime.UtcNow.AddDays(7),
    Revoked = false
};

await _db.RefreshTokens.AddAsync(refreshTokenRecord);
await _db.SaveChangesAsync();

var roles = await _userManager.GetRolesAsync(user);
var token = GenerateJwtToken(user, roles);

return Ok(new AuthResponseDto
{
    Token = token.Token,
    ExpiresAt = token.ExpiresAt,
    Username = user.UserName!,
    Roles = roles,
    RefreshToken = refreshToken
});
```

The client receives a short-lived access token and a long-lived refresh token.

---

### Refresh-token rotation

[⬆️ Back to Table of Contents](#table-of-contents)


The client sends the refresh token to:

```text
POST /api/auth/refresh
```

The server hashes the supplied value and verifies that the corresponding record exists, is not revoked, and has not expired.

On success, the existing refresh token is revoked and replaced by a newly generated refresh token.

```csharp
[HttpPost("refresh")]
[AllowAnonymous]
public async Task<IActionResult> Refresh(
    [FromBody] RefreshRequestDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        return BadRequest(new
        {
            message = "Refresh token is required."
        });

    var incomingHash = HashToken(dto.RefreshToken);

    var existing = await _db.RefreshTokens
        .FirstOrDefaultAsync(t => t.TokenHash == incomingHash);

    if (existing == null ||
        existing.Revoked ||
        existing.Expires <= DateTime.UtcNow)
    {
        return Unauthorized(new
        {
            message = "Invalid or expired refresh token."
        });
    }

    existing.Revoked = true;
    existing.RevokedAt = DateTime.UtcNow;

    var newRefreshToken = GenerateSecureRefreshToken();

    var newRecord = new RefreshToken
    {
        UserId = existing.UserId,
        TokenHash = HashToken(newRefreshToken),
        Created = DateTime.UtcNow,
        Expires = DateTime.UtcNow.AddDays(7),
        Revoked = false
    };

    existing.ReplacedByTokenHash = newRecord.TokenHash;

    _db.RefreshTokens.Add(newRecord);
    await _db.SaveChangesAsync();

    var user = await _userManager.FindByIdAsync(existing.UserId);
    var roles = await _userManager.GetRolesAsync(user!);
    var newJwt = GenerateJwtToken(user!, roles);

    return Ok(new AuthResponseDto
    {
        Token = newJwt.Token,
        ExpiresAt = newJwt.ExpiresAt,
        Username = user!.UserName!,
        Roles = roles,
        RefreshToken = newRefreshToken
    });
}
```

The resulting token chain is:

```text
Refresh Token A
      │
      ├── used
      ▼
   REVOKED
      │
      └── replaced by → Refresh Token B
                              │
                              ├── used
                              ▼
                           REVOKED
                              │
                              └── replaced by → Refresh Token C
```

`ReplacedByTokenHash` preserves the relationship and can support auditing and future refresh-token reuse detection.

---

### Logout and immediate access-token invalidation

[⬆️ Back to Table of Contents](#table-of-contents)


The logout endpoint is:

```text
POST /api/auth/logout
```

and requires:

```csharp
[Authorize]
```

Logout handles both token types:

1. If a refresh token is supplied, its database record is revoked.
2. The current access token's JTI is stored in `RevokedAccessTokens`.
3. The JTI record contains the token's expiration time.
4. Subsequent requests using that JWT fail the `OnTokenValidated` revocation check.

```csharp
[HttpPost("logout")]
[Authorize]
public async Task<IActionResult> Logout(
    [FromBody] RevokeRequestDto dto)
{
    var userId =
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (userId == null)
        return Unauthorized();

    if (!string.IsNullOrWhiteSpace(dto?.RefreshToken))
    {
        var hash = HashToken(dto.RefreshToken);

        var rtoken = await _db.RefreshTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == hash &&
                     t.UserId == userId);

        if (rtoken != null && !rtoken.Revoked)
        {
            rtoken.Revoked = true;
            rtoken.RevokedAt = DateTime.UtcNow;
        }
    }

    var jti =
        User.FindFirstValue(JwtRegisteredClaimNames.Jti)
        ?? User.FindFirstValue("jti");

    if (!string.IsNullOrEmpty(jti))
    {
        DateTime expiresAt;

        var expClaim =
            User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (!string.IsNullOrEmpty(expClaim) &&
            long.TryParse(expClaim, out var seconds))
        {
            expiresAt =
                DateTimeOffset
                    .FromUnixTimeSeconds(seconds)
                    .UtcDateTime;
        }
        else
        {
            expiresAt = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int>(
                    "Jwt:ExpirationMinutes"));
        }

        _db.RevokedAccessTokens.Add(
            new RevokedAccessToken
            {
                Jti = jti,
                ExpiresAt = expiresAt
            });
    }

    await _db.SaveChangesAsync();

    return Ok(new
    {
        message =
            "Logout successful. Refresh token revoked " +
            "and access token invalidated."
    });
}
```

### Why JTI is needed for logout

[⬆️ Back to Table of Contents](#table-of-contents)


JWT authentication is normally stateless. If a token is correctly signed and has not expired, it can normally be accepted without a server-side session lookup.

That creates this situation:

```text
JWT issued
    │
    ├── valid for 15 minutes
    │
    └── user logs out after 2 minutes
              │
              ▼
       JWT would normally
       remain valid for 13 minutes
```

The project's JTI revocation mechanism changes this:

```text
JWT
 └── JTI = unique-token-id
             │
             ▼
     RevokedAccessTokens
             │
             └── JTI exists
                    │
                    ▼
             context.Fail(...)
                    │
                    ▼
                REJECT
```

This makes the current access token invalid immediately for subsequent requests.

---

### RevokedAccessToken model

[⬆️ Back to Table of Contents](#table-of-contents)


```csharp
public class RevokedAccessToken
{
    public int Id { get; set; }
    public string Jti { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
```

The EF Core context exposes:

```csharp
public DbSet<RefreshToken> RefreshTokens =>
    Set<RefreshToken>();

public DbSet<RevokedAccessToken> RevokedAccessTokens =>
    Set<RevokedAccessToken>();
```

`RefreshToken.TokenHash` has a unique index, while revoked access tokens store the JTI and its expiration time.

---

### Logout-all

[⬆️ Back to Table of Contents](#table-of-contents)


The project also provides:

```text
POST /api/auth/logout-all
```

It revokes all currently active refresh tokens belonging to the authenticated user:

```csharp
[HttpPost("logout-all")]
[Authorize]
public async Task<IActionResult> LogoutAll()
{
    var userId =
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (userId == null)
        return Unauthorized();

    var tokens = _db.RefreshTokens
        .Where(t =>
            t.UserId == userId &&
            !t.Revoked);

    await tokens.ForEachAsync(t =>
    {
        t.Revoked = true;
        t.RevokedAt = DateTime.UtcNow;
    });

    await _db.SaveChangesAsync();

    return Ok(new
    {
        message = "All refresh tokens revoked."
    });
}
```

This invalidates the user's refresh-token sessions. Existing access tokens are handled independently by their JWT lifetime and JTI revocation mechanism.

---

### Token cleanup background service

[⬆️ Back to Table of Contents](#table-of-contents)


`TokenCleanupService` periodically removes authentication records that no longer need to exist.

It removes:

- expired refresh tokens;
- old revoked refresh tokens according to the retention policy;
- expired revoked access-token JTIs.

```csharp
var expiredRefreshTokens =
    await db.RefreshTokens
        .Where(t => t.Expires <= DateTime.UtcNow)
        .ToListAsync(ct);

db.RefreshTokens.RemoveRange(expiredRefreshTokens);

var oldRevoked =
    await db.RefreshTokens
        .Where(t =>
            t.Revoked &&
            t.RevokedAt != null &&
            t.RevokedAt <=
                DateTime.UtcNow.AddDays(-retentionDays))
        .ToListAsync(ct);

db.RefreshTokens.RemoveRange(oldRevoked);

var expiredRevokedJtis =
    await db.RevokedAccessTokens
        .Where(r => r.ExpiresAt <= DateTime.UtcNow)
        .ToListAsync(ct);

db.RevokedAccessTokens.RemoveRange(expiredRevokedJtis);

await db.SaveChangesAsync(ct);
```

`ExpiresAt` is important because once a JWT would have expired naturally, its JTI no longer needs to remain in the revocation table.

---

### Complete authentication lifecycle

[⬆️ Back to Table of Contents](#table-of-contents)


```text
1. LOGIN
   │
   ├── username/password
   ▼
   Server
   │
   ├── Access JWT
   │     └── short lifetime + unique JTI
   │
   └── Refresh token
         └── raw value → client
               hash → database

2. NORMAL API REQUEST
   │
   └── Authorization: Bearer <access-token>
          │
          ▼
       JwtBearer
          │
          ├── signature valid?
          ├── issuer valid?
          ├── audience valid?
          ├── lifetime valid?
          └── JTI revoked?
                 │
                 ├── yes → REJECT
                 └── no  → ACCEPT

3. ACCESS TOKEN EXPIRES
   │
   ▼
   POST /api/auth/refresh
   │
   ├── hash refresh token
   ├── find database record
   ├── check not revoked
   ├── check not expired
   ├── revoke old refresh token
   ├── create new refresh token
   └── issue new JWT

4. LOGOUT
   │
   ▼
   POST /api/auth/logout
   │
   ├── revoke refresh token
   └── store current access-token JTI
          │
          ▼
      Future requests
          │
          └── JTI found → REJECT

5. CLEANUP
   │
   └── TokenCleanupService
          ├── remove expired refresh tokens
          └── remove expired revoked JTIs
```

### Access token vs. refresh token

[⬆️ Back to Table of Contents](#table-of-contents)


| Property | Access Token | Refresh Token |
|---|---|---|
| Format | JWT | Random secret |
| Purpose | Authenticate API requests | Obtain a new access token |
| Lifetime | Short | Long |
| Stored server-side | JTI only when revoked | SHA-256 hash |
| Contains claims | Yes | No |
| Signed | Yes | No; cryptographically random |
| Rotated | New JWT on refresh | Yes |
| Revoked on logout | Current JTI is recorded | Supplied token is revoked |
| Used on normal API requests | Yes | No |

### Expiration vs. revocation

[⬆️ Back to Table of Contents](#table-of-contents)


```text
Expiration:
JWT reaches its `exp` time
        │
        └── JwtBearer rejects it naturally

Revocation:
User logs out before `exp`
        │
        └── JTI stored in RevokedAccessTokens
                │
                └── JwtBearer rejects it immediately
```

Refresh tokens have their own independent expiration and revocation state.

### Security considerations

[⬆️ Back to Table of Contents](#table-of-contents)


- Use HTTPS in production.
- Keep access-token lifetime short and use refresh tokens for session continuation.
- Store only hashed refresh tokens server-side.
- Rotate refresh tokens when they are used.
- `ReplacedByTokenHash` can support refresh-token reuse detection and auditing.
- Consider device/session records when explicit per-device session management is required.
- Rate-limit refresh endpoints.
- Protect JWT signing keys and authentication secrets with appropriate secret management.
- For larger deployments, consider asymmetric signing such as RS256 for key distribution and verification.


---

# License

This project is licensed under the MIT License.

