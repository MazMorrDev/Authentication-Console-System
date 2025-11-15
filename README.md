# Authentication Console System

```text
  █████╗ ██╗   ██╗████████╗██╗  ██╗███████╗███╗   ██╗████████╗██╗ ██████╗ █████╗ ████████╗██╗ ██████╗ ███╗   ██╗
 ██╔══██╗██║   ██║╚══██╔══╝██║  ██║██╔════╝████╗  ██║╚══██╔══╝██║██╔════╝██╔══██╗╚══██╔══╝██║██╔═══██╗████╗  ██║
 ███████║██║   ██║   ██║   ███████║█████╗  ██╔██╗ ██║   ██║   ██║██║     ███████║   ██║   ██║██║   ██║██╔██╗ ██║
 ██╔══██║██║   ██║   ██║   ██╔══██║██╔══╝  ██║╚██╗██║   ██║   ██║██║     ██╔══██║   ██║   ██║██║   ██║██║╚██╗██║
 ██║  ██║╚██████╔╝   ██║   ██║  ██║███████╗██║ ╚████║   ██║   ██║╚██████╗██║  ██║   ██║   ██║╚██████╔╝██║ ╚████║
 ╚═╝  ╚═╝ ╚═════╝    ╚═╝   ╚═╝  ╚═╝╚══════╝╚═╝  ╚═══╝   ╚═╝   ╚═╝ ╚═════╝╚═╝  ╚═╝   ╚═╝   ╚═╝ ╚═════╝ ╚═╝  ╚═══╝
```

A secure and robust authentication system built with C# and PostgreSQL, featuring a console-based interface with comprehensive user management capabilities. This project is just for practicing theorical authentication above ADO .NET.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Database Setup](#database-setup)
- [Usage](#usage)
- [Commands](#commands)
- [Project Structure](#project-structure)
- [Technical Details](#technical-details)
- [Security Features](#security-features)
- [Troubleshooting](#troubleshooting)

## Features

- **Secure Authentication** - Password hashing using BCrypt
- **User Management** - Complete CRUD operations for users
- **Database Migrations** - Automated schema management
- **Beautiful Console UI** - Colored output and formatted displays
- **ADO.NET Performance** - Direct database access for optimal performance
- **Role-Based System** - Extensible role management (Admin/User)
- **SQL Logging** - Debugging and monitoring capabilities
- **Transaction Support** - Safe database operations

## Architecture

```text
AuthenticationConsoleSystem/
+-- Services/          # Business logic layer
+-- Models/            # Data models
+-- Database/          # Database configuration & migrations
+-- Utilities/         # Helper classes
+-- Console/           # User interface layer
```

## Prerequisites

- **.NET 10.0+** SDK
- **PostgreSQL 16+** database server
- **PostgreSQL credentials** with database creation privileges

## Installation

1. **Clone or download the project**
2. **Configure database connection** in `DatabaseConfig.cs`:

    ```csharp
    public static string ConnectionString { get; } =
        "Host=localhost;Port=5432;Database=authentication_console_system;Username=your_username;Password=your_password;";
    ```

3. **Build the project**:

    ```bash
    dotnet build
    ```

4. **Run the application**:

    ```bash
    dotnet run
    ```

## Database Setup

The system includes an automated migration system:

1. **First run**: The application will prompt to run migrations
2. **Manual migration**: Use the `migrate` command anytime
3. **Check status**: Use `db-status` to verify database health

### Database Schema

```text
    +----------+
    |  Users   |
    +----------+
    | Id PK    |
    | UserName |
    | HashPass |
    | IsLogged |
    | CreatedAt|
    | UpdatedAt|
    +----------+
```

## Usage

When you start the application, you'll see the welcome screen:

```text
+-----------------------------------------------------------------------------+
|                  Welcome to Authentication Console System!                  |
|             Type 'help' for available commands or 'exit' to quit            |
+-----------------------------------------------------------------------------+
```

## Commands

```text
+==============================================================+
|           AUTHENTICATION CONSOLE SYSTEM                      |
+==============================================================+
| Available commands:                                          |
|                                                              |
| list          - Show all user accounts                       |
| register      - Create a new user account                    |
| login         - Login with existing account                  |
| logout <id>   - Logout user with specified ID                |
| info <id>     - Show user information by ID                  |
| migrate       - Run database migrations                      |
| db-status     - Check database status                        |
| exit          - Shutdown the application                     |
|                                                              |
+==============================================================+
```

### Command Examples

**Register a new user:**

```text
> register
Please enter your username:
john_doe
Please enter your password:
secret123
[SUCCESS] User 'john_doe' registered successfully!
```

**Login:**

```text
> login
Please enter your username:
john_doe
Please enter your password:
secret123
[SUCCESS] Welcome back, john_doe!
```

**List all users:**

```text
> list
Found 3 user(s):
ID: 1 | User: john_doe | ONLINE
ID: 2 | User: jane_smith | OFFLINE
ID: 3 | User: admin | ONLINE
```

**Get user information:**

```text
> info 1
+================================+
|         USER INFORMATION       |
+================================+
| ID: 1                          |
| Username: john_doe             |
| Status: LOGGED IN              |
|                                |
| Password Hash:                 |
| $2a$11$XrDnXqB2PZkHvCJjJ8yY... |
+================================+
```

## Project Structure

### Core Files

| File                  | Purpose                                     |
| --------------------- | ------------------------------------------- |
| `Program.cs`          | Application entry point and main loop       |
| `CommandProcesser.cs` | Command parsing and execution               |
| `UserService.cs`      | User business logic and database operations |
| `MigrationService.cs` | Database schema management                  |
| `User.cs`             | User data model                             |
| `DatabaseConfig.cs`   | Database connection configuration           |

### Key Classes

- **UserService**: Handles all user-related operations (CRUD, authentication)
- **MigrationService**: Manages database schema versions and migrations
- **CommandProcesser**: Processes user commands from console input
- **ConsolePersonalizer**: Provides colored console output utilities
- **Logs**: SQL query logging for debugging

## Technical Details

### Database Access

- Uses **ADO.NET** with **Npgsql** for PostgreSQL
- Connection factory pattern for dependency injection
- Transaction support for data integrity
- Parameterized queries to prevent SQL injection

### Password Security

- **BCrypt** hashing algorithm for password storage
- Salted hashes with automatic salt generation
- Secure verification without storing plain text

### Migration System

- Version-controlled database schema changes
- Track applied migrations in `__Migrations` table
- Rollback support for failed migrations
- Automatic creation of indexes and constraints

## Security Features

- **Password Hashing**: All passwords are hashed using BCrypt
- **SQL Injection Protection**: Parameterized queries throughout
- **Transaction Safety**: ACID compliance for critical operations
- **Input Validation**: Comprehensive input sanitization
- **Session Management**: Track user login states securely

## Troubleshooting

### Common Issues

1. **Database Connection Failed**

   - Verify PostgreSQL is running
   - Check connection string in `DatabaseConfig.cs`
   - Ensure database exists and user has permissions

2. **Migration Errors**

   - Run `db-status` to check current state
   - Use `migrate` to apply missing migrations
   - Check PostgreSQL logs for detailed errors

3. **Authentication Failures**
   - Verify username/password combination
   - Check if user exists with `list` command
   - Ensure proper password hashing is working

### Debug Mode

Enable SQL logging by default in `Logs.cs` to see all database queries for debugging purposes.

### Database Recovery

If migrations fail, you can:

1. Manually check the `__Migrations` table
2. Run specific migration SQL manually
3. Reset the database and run full migration

## Development

### Adding New Features

1. **New Commands**: Add to `CommandProcesser.cs`
2. **Database Changes**: Create new migration in `MigrationService.cs`
3. **Business Logic**: Extend appropriate service classes
4. **Models**: Add new properties to `User.cs` or create new models

### Extending the System

The architecture supports:

- Additional user fields
- New authentication methods
- Enhanced role permissions
- Audit logging
- Email integration

---

Authentication Console System - Secure user management made simple
