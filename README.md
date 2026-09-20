# PlayersClubsInfo

PlayersClubsInfo is a RESTful Practice-Only backend API for managing football clubs and players, with user authentication, role-based authorization, JWT access tokens, refresh-token rotation, token revocation, and PostgreSQL persistence.

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
- [Swagger and Scalar](#swagger-and-scalar)
- [Project Structure](#project-structure)
- [Security Notes](#security-notes)

---

## Project Purpose

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

The application-specific tables are:

```text
Clubs
Players
RefreshTokens
RevokedAccessTokens
```

## Identity tables

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

Stores the application's roles.

The application creates these roles automatically:

```text
Root
Manager
User
```

---

## AspNetUserRoles

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

Logout performs two actions when possible:

1. Revokes the supplied refresh token.
2. Stores the current JWT's JTI in `RevokedAccessTokens`.

Therefore the current access token can be rejected immediately instead of remaining valid until its normal expiration.

---

## Logout All

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

### Endpoint

```http
POST /api/auth/register
```

### Authentication

Anonymous.

### Purpose

Creates a new user account.

Every publicly registered account is assigned the `User` role automatically.

### Request

```json
{
  "username": "ali",
  "email": "ali@example.com",
  "password": "Password123!"
}
```

### Example

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

### Endpoint

```http
POST /api/auth/login
```

### Authentication

Anonymous.

### Purpose

Authenticates a user and returns:

- JWT access token.
- Access-token expiration.
- Username.
- Roles.
- Refresh token.

### Request

```json
{
  "username": "root",
  "password": "ChangeMe123!"
}
```

### Example

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "root",
    "password": "ChangeMe123!"
  }'
```

### Response

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

### Endpoint

```http
POST /api/auth/logout
```

### Authentication

Required.

### Purpose

Logs the current session out.

The endpoint can revoke:

- The supplied refresh token.
- The current access token through its JTI.

### Request

```json
{
  "refreshToken": "<REFRESH_TOKEN>"
}
```

### Example

```bash
curl -X POST http://localhost:8080/api/auth/logout \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "<refresh-token>"
  }'
```

### Response

```json
{
  "message": "Logout successful. Refresh token revoked and access token invalidated."
}
```

---

## 4. Logout All

### Endpoint

```http
POST /api/auth/logout-all
```

### Authentication

Required.

### Purpose

Revokes all non-revoked refresh tokens belonging to the current user.

### Example

```bash
curl -X POST http://localhost:8080/api/auth/logout-all \
  -H "Authorization: Bearer <token>"
```

### Response

```json
{
  "message": "All refresh tokens revoked."
}
```

---

## 5. Change Own Password

### Endpoint

```http
PUT /api/auth/change-password
```

### Authentication

Required.

### Purpose

Allows the authenticated user to change their own password.

### Request

```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!"
}
```

### Example

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

```json
{
  "message": "Password changed successfully."
}
```

---

## 6. Refresh Access Token

### Endpoint

```http
POST /api/auth/refresh
```

### Authentication

Anonymous.

The refresh token itself is the credential.

### Purpose

Obtains a new JWT access token without requiring the user to enter their password again.

The old refresh token is revoked and replaced with a new refresh token.

### Request

```json
{
  "refreshToken": "<REFRESH_TOKEN>"
}
```

### Example

```bash
curl -X POST http://localhost:8080/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "<refresh-token>"
  }'
```

### Response

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

### Endpoint

```http
GET /api/clubs
```

### Roles

```text
Root
Manager
User
```

### Purpose

Returns all clubs.

Each club includes:

- ID.
- Name.
- City.
- Player count.
- Players belonging to the club.

### Example

```bash
curl http://localhost:8080/api/clubs \
  -H "Authorization: Bearer <token>"
```

### Response

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

### Endpoint

```http
GET /api/clubs/{id}
```

### Roles

```text
Root
Manager
User
```

### Example

```bash
curl http://localhost:8080/api/clubs/1 \
  -H "Authorization: Bearer <token>"
```

### Response

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

### Endpoint

```http
POST /api/clubs
```

### Roles

```text
Root
Manager
```

### Request

```json
{
  "name": "Basra FC",
  "city": "Basra"
}
```

### Example

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

### Endpoint

```http
PUT /api/clubs/{id}
```

### Roles

```text
Root
Manager
```

### Request

```json
{
  "name": "Basra United",
  "city": "Basra"
}
```

### Example

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

The updated club is returned using the normal club response format.

---

## 11. Delete Club

### Endpoint

```http
DELETE /api/clubs/{id}
```

### Role

```text
Root
```

### Purpose

Deletes a club.

Players belonging to the club are released and become free agents instead of being deleted.

### Example

```bash
curl -X DELETE http://localhost:8080/api/clubs/2 \
  -H "Authorization: Bearer <root-token>"
```

### Response

```http
204 No Content
```

---

# Players

## 12. Get All Players

### Endpoint

```http
GET /api/players
```

### Roles

```text
Root
Manager
User
```

### Example

```bash
curl http://localhost:8080/api/players \
  -H "Authorization: Bearer <token>"
```

### Response

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

### Endpoint

```http
GET /api/players/{id}
```

### Roles

```text
Root
Manager
User
```

### Example

```bash
curl http://localhost:8080/api/players/1 \
  -H "Authorization: Bearer <token>"
```

### Response

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

### Endpoint

```http
POST /api/players
```

### Roles

```text
Root
Manager
```

### Purpose

Creates a player.

A player can optionally be assigned to a club immediately.

### Request

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

### Endpoint

```http
PUT /api/players/{id}
```

### Roles

```text
Root
Manager
```

### Request

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

### Endpoint

```http
DELETE /api/players/{id}
```

### Role

```text
Root
```

### Example

```bash
curl -X DELETE http://localhost:8080/api/players/1 \
  -H "Authorization: Bearer <root-token>"
```

### Response

```http
204 No Content
```

---

## 17. Transfer Player

### Endpoint

```http
POST /api/players/{id}/transfer
```

### Roles

```text
Root
Manager
```

### Purpose

Moves a player to another club.

### Request

```json
{
  "clubId": 2
}
```

### Example

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

### Endpoint

```http
POST /api/players/{id}/release
```

### Roles

```text
Root
Manager
```

### Purpose

Removes the player's club assignment and makes the player a free agent.

### Example

```bash
curl -X POST http://localhost:8080/api/players/1/release \
  -H "Authorization: Bearer <token>"
```

### Response

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

### Endpoint

```http
GET /api/users
```

### Role

```text
Root
```

### Example

```bash
curl http://localhost:8080/api/users \
  -H "Authorization: Bearer <root-token>"
```

### Response

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

### Endpoint

```http
GET /api/users/{id}
```

### Role

```text
Root
```

### Example

```bash
curl http://localhost:8080/api/users/<user-id> \
  -H "Authorization: Bearer <root-token>"
```

---

## 21. Create User

### Endpoint

```http
POST /api/users
```

### Role

```text
Root
```

### Purpose

Creates a user and assigns one of the existing roles.

Valid roles are:

```text
Root
Manager
User
```

### Request

```json
{
  "username": "manager1",
  "email": "manager@example.com",
  "password": "Password123!",
  "role": "Manager"
}
```

### Example

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

### Endpoint

```http
PUT /api/users/{id}
```

### Role

```text
Root
```

### Purpose

Updates:

- Email.
- Role.
- Lockout setting.

### Request

```json
{
  "email": "manager-new@example.com",
  "role": "Manager",
  "lockoutEnabled": true
}
```

### Example

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

### Endpoint

```http
DELETE /api/users/{id}
```

### Role

```text
Root
```

### Example

```bash
curl -X DELETE http://localhost:8080/api/users/<user-id> \
  -H "Authorization: Bearer <root-token>"
```

A Root user cannot delete their own account.

### Response

```http
204 No Content
```

---

## 24. Reset Another User's Password

### Endpoint

```http
PUT /api/users/{id}/password
```

### Role

```text
Root
```

### Purpose

Allows Root to set a new password for another user.

### Request

```json
{
  "newPassword": "NewPassword123!"
}
```

### Example

```bash
curl -X PUT http://localhost:8080/api/users/<user-id>/password \
  -H "Authorization: Bearer <root-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "newPassword": "NewPassword123!"
  }'
```

### Response

```json
{
  "message": "Password changed successfully."
}
```

---

# API Permission Summary

## Authentication endpoints

| Endpoint | Anonymous | Authenticated | Root | Manager | User |
|---|:---:|:---:|:---:|:---:|:---:|
| `POST /api/auth/register` | Yes | Yes | Yes | Yes | Yes |
| `POST /api/auth/login` | Yes | Yes | Yes | Yes | Yes |
| `POST /api/auth/logout` | No | Yes | Yes | Yes | Yes |
| `POST /api/auth/logout-all` | No | Yes | Yes | Yes | Yes |
| `PUT /api/auth/change-password` | No | Yes | Yes | Yes | Yes |
| `POST /api/auth/refresh` | Yes | Yes | Yes | Yes | Yes |

## Club endpoints

| Endpoint | Root | Manager | User |
|---|:---:|:---:|:---:|
| `GET /api/clubs` | Yes | Yes | Yes |
| `GET /api/clubs/{id}` | Yes | Yes | Yes |
| `POST /api/clubs` | Yes | Yes | No |
| `PUT /api/clubs/{id}` | Yes | Yes | No |
| `DELETE /api/clubs/{id}` | Yes | No | No |

## Player endpoints

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

Passwords are managed by ASP.NET Core Identity and are not stored as plain text.

## JWT Signing Key

The JWT signing key should be kept outside source control.

For Docker development, use the Docker Secret:

```text
jwt-key
```

## PostgreSQL Password

The PostgreSQL password should be supplied through:

```text
postgres-password
```

rather than being committed to `appsettings.json`.

## Root Password

The default development Root password is supplied through:

```text
admin-password
```

Change development credentials before using the application in any real environment.

## Refresh Tokens

Only hashes of refresh tokens are stored in the database.

## Access Token Revocation

The JTI of a logged-out access token can be stored in `RevokedAccessTokens`.

Expired revoked-token records can be removed by the application's token cleanup service.

## Development Configuration

The Docker Compose configuration is intended for development. Production deployment should additionally address:

- HTTPS/TLS termination.
- Production secret management.
- Database backups.
- Database network isolation.
- Production logging and monitoring.
- Key encryption/persistence strategy.
- Secure cookie/client-side token handling where applicable.
- Rate limiting and abuse protection.
- Appropriate CORS policy.
- Production PostgreSQL configuration.

---

# License

This project is licensed under the MIT License.

