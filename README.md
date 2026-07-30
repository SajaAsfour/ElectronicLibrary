# Electronic Library Marketplace API

## Project Overview

Electronic Library Marketplace is an ASP.NET Core Web API that allows users to browse, buy, and sell books through an online marketplace.

The project separates the general information of a book from each user listing. Users with the Seller role can offer the same book with different prices, quantities, formats, and conditions.

---

## Current Phase

This repository currently represents:

**Phase 2: Project Setup and Database Configuration**

The current phase includes:

- Database configuration
- N-tier Architecture
- SOLID Principles
- Identity & Authentication
- JWT Authentication
- Refresh Token
- Generic Repository
- Unit of Work
- Dependency Injection
- Seed Data
- Entity Framework Core Migrations
- English and Arabic localization
- Accept-Language header support

---

## Main Roles

- Admin
- Customer
- Seller role on ApplicationUser

---

## Technologies Used

- ASP.NET Core Web API
- C#
- .NET
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- JWT Authentication
- Refresh Tokens
- N-tier Architecture
- Repository Pattern
- Unit of Work
- Dependency Injection
- Fluent API
- Git & GitHub
- Postman

---

# Project Structure

The project follows **N-tier Architecture**.

```text
ElectronicLibrary.PL
        ↓
ElectronicLibrary.BLL
        ↓
ElectronicLibrary.DAL
```

## ElectronicLibrary.PL

Presentation Layer

Contains:

- Controllers
- Program.cs
- Extensions
- Middleware
- API Endpoints

---

## ElectronicLibrary.BLL

Business Logic Layer

Contains:

- Services
- Interfaces
- Business Logic
- JWT Options

---

## ElectronicLibrary.DAL

Data Access Layer

Contains:

- Models
- DTOs
- Enums
- ApplicationDbContext
- Fluent API Configurations
- Generic Repository
- Unit Of Work
- Seed Data
- Migrations

---

# Main Database Models

- ApplicationUser
- Seller role on ApplicationUser
- Book
- Author
- Publisher
- Category
- BookAuthor
- BookCategory
- BookImage
- Listing
- Cart
- CartItem
- Order
- OrderItem
- Payment
- Coupon
- Review

---

# Database Design

The project separates **Book** from **Listing**.

## Book

Contains:

- Title
- ISBN
- Description
- Language
- Publication Year
- Publisher
- Authors
- Categories
- Images

---

## Listing

Contains:

- Price
- Quantity
- Format
- Condition
- Discount Percentage
- Status
- Seller role on ApplicationUser

This allows multiple users with the Seller role to sell the same book with different prices and conditions.

---

# Main Relationships

- Publisher → Books
- Book → Authors (Many-to-Many)
- Book → Categories (Many-to-Many)
- Book → Images
- Book → Listings
- ApplicationUser (Seller role) → Listings
- Seller profile fields are stored directly on ApplicationUser
- User → Cart
- Cart → CartItems
- Listing → CartItems
- User → Orders
- Order → OrderItems
- Listing → OrderItems
- Order → Payment
- Coupon → Orders
- User → Reviews
- Book → Reviews

---

# SOLID Principles

## Single Responsibility Principle

Every class has one responsibility.

Examples:

- Controllers handle HTTP Requests.
- Services contain Business Logic.
- Repositories access the Database.
- TokenService creates JWT Tokens.
- DatabaseSeeder seeds initial data.

---

## Open / Closed Principle

The project depends on interfaces such as:

- IAuthenticationService
- ITokenService
- IGenericRepository
- IUnitOfWork

Implementations can be changed without modifying the consuming code.

---

## Liskov Substitution Principle

Implementations can replace their interfaces without affecting functionality.

---

## Interface Segregation Principle

Small focused interfaces are used instead of one large interface.

---

## Dependency Inversion Principle

Controllers depend on interfaces rather than concrete implementations.

Dependency Injection is used throughout the project.

---

# Repository Pattern

The Generic Repository supports:

- Query()
- GetOneAsync()
- GetAllAsync()
- ExistsAsync()
- AddAsync()
- Update()
- Delete()

Database changes are committed through Unit Of Work.

---

# Unit Of Work

The Unit Of Work provides:

- Repository access
- SaveChangesAsync()

This allows multiple operations to be committed together.

---

# Identity

The project uses **ASP.NET Core Identity**.

ApplicationUser extends IdentityUser and contains:

- FullName
- City
- Address
- RefreshTokenHash
- RefreshTokenExpiryTime

Identity manages:

- Password hashing
- Users
- Roles
- Authentication

---

# Authentication Endpoints

The project currently provides the following authentication endpoints:

```text
POST /api/account/register
POST /api/account/login
POST /api/account/refresh-token
POST /api/account/logout
```

## Register

Creates a new user account and assigns the default role:

```text
Customer
```

## Login

Validates the user's email and password, then returns:

- Access Token
- Refresh Token
- Token expiration times
- User information
- User roles

## Refresh Token

Validates the expired Access Token and Refresh Token, then generates new tokens.

The old Refresh Token is replaced with a new one.

## Logout

Removes the stored Refresh Token information from the user account.

---

# JWT Authentication

The project uses JWT Bearer Authentication.

The Access Token contains claims such as:

- User ID
- User Name
- Email
- Full Name
- Roles
- Token ID

The Access Token has a short expiration period.

The Refresh Token has a longer expiration period and is used to generate a new Access Token.

The project stores a hash of the Refresh Token instead of storing the raw Refresh Token.

---

# Roles

The application contains the following roles:

- Admin
- Customer
- Seller role on ApplicationUser

The roles are automatically created when the application starts.

A default Admin user is also created using configuration values stored in .NET User Secrets.

---

# Seed Data

The project automatically seeds:

- Admin Role
- Customer Role
- Seller Role
- Default Admin User
- Initial Categories
- Initial Authors
- Initial Publishers
- Sample Book

The seed logic checks whether data already exists before inserting it.

This prevents duplicate records every time the application runs.

---

# Global Exception Handling

The project uses a Global Exception Handling Middleware.

It converts exceptions into suitable HTTP responses.

Examples:

- `400 Bad Request`
- `401 Unauthorized`
- `404 Not Found`
- `500 Internal Server Error`

Example error response:

```json
{
  "statusCode": 400,
  "message": "A user with this email already exists."
}
```

---

# Required Configuration

Sensitive values are stored using .NET User Secrets.

The required configuration values are:

```text
ConnectionStrings:DefaultConnection
Jwt:SecretKey
SeedAdmin:Email
SeedAdmin:Password
SeedAdmin:FullName
```

Example User Secrets file:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING"
  },
  "Jwt": {
    "SecretKey": "YOUR_SECRET_KEY_WITH_AT_LEAST_32_BYTES"
  },
  "SeedAdmin": {
    "Email": "YOUR_ADMIN_EMAIL",
    "Password": "YOUR_ADMIN_PASSWORD",
    "FullName": "System Administrator"
  }
}
```

---

# appsettings.json

The non-sensitive settings can remain inside `appsettings.json`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Issuer": "ElectronicLibraryApi",
    "Audience": "ElectronicLibraryClient",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

# Prerequisites

Install the following tools before running the project:

- .NET SDK
- SQL Server
- Visual Studio or Visual Studio Code
- Entity Framework Core CLI
- Postman

To install Entity Framework Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

---

# How to Run the Project

## 1. Clone the Repository

```bash
git clone https://github.com/SajaAsfour/ElectronicLibrary
cd ElectronicLibrary
```

## 2. Restore Packages

```bash
dotnet restore
```

## 3. Build the Solution

```bash
dotnet build
```

## 4. Configure User Secrets

Open the User Secrets file for `ElectronicLibrary.PL`.

Add:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=.;Initial Catalog=ElectronicLibraryDb;Integrated Security=True;Encrypt=True;Trust Server Certificate=True;"
  },
  "Jwt": {
    "SecretKey": "YOUR_SECRET_KEY_WITH_AT_LEAST_32_BYTES"
  },
  "SeedAdmin": {
    "Email": "YOUR_ADMIN_EMAIL",
    "Password": "YOUR_ADMIN_PASSWORD",
    "FullName": "System Administrator"
  }
}
```

## 5. Apply Database Migrations
```bash
add-migration inital
update-database
```
## 6. Run the API

The API URL will appear in the terminal.

---

# Testing the API

The API can be tested using **Postman**.

## Register

**POST**

```text
/api/account/register
```

Example Request:

```json
{
  "fullName": "Test Customer",
  "email": "customer@example.com",
  "password": "Customer@123",
  "confirmPassword": "Customer@123",
  "city": "Ramallah",
  "address": "Main Street"
}
```

---

## Login

**POST**

```text
/api/account/login
```

Example Request:

```json
{
  "email": "customer@example.com",
  "password": "Customer@123"
}
```

---

## Refresh Token

**POST**

```text
/api/account/refresh-token
```

Example Request:

```json
{
  "accessToken": "ACCESS_TOKEN",
  "refreshToken": "REFRESH_TOKEN"
}
```

---

## Logout

**POST**

```text
/api/account/logout
```

Authorization:

```text
Bearer ACCESS_TOKEN
```

---

# Future Work

The following features will be implemented in later phases:

- Book Management
- Author Management
- Publisher Management
- Category Management
- Seller Listing Management
- Shopping Cart
- Checkout
- Order Processing
- Payment Integration
- Coupons and Promotions
- Reviews and Ratings
- Searching
- Filtering
- Pagination
- Soft Delete
- Audit Fields
- Image Upload
- Email Notifications

---

# Author

**Project Name**

Electronic Library Marketplace API

Developed as the final ASP.NET Core Web API course project by SajaAsfour.

Current implementation covers **Phase 2: Project Setup and Database Configuration**.