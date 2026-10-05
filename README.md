# 🦙 LamaWMS

**LamaWMS** is a lightweight Warehouse Management System designed for fabric and roll inventory management in manufacturing environments.

## ✨ Features

- 🧵 Fabric catalog management
- 🧻 Individual roll tracking
- 📍 Warehouse cell management
- 🔒 Fabric reservations for production requests
- 🏭 Product material requirements
- 🔄 Receipt, consumption, return, write-off and adjustment operations
- 📜 Complete transaction history
- 🔎 Search across warehouse directories
- 📊 Excel export for reservations and transactions
- 📱 Tablet-friendly web interface

## 🛠️ Technology Stack

- **C# / .NET 10**
- **ASP.NET Core MVC**
- **Entity Framework Core**
- **PostgreSQL**
- **Npgsql**
- **Bootstrap**
- **ClosedXML**

## 🏗️ Architecture

```text
LamaWMS
├── LamaWMS.Domain
├── LamaWMS.Infrastructure
└── LamaWMS.WebUI
```

## 🏭 Production Environment

LamaWMS is intended primarily for deployment inside a trusted company network.

A typical installation uses:

- 🪟 Windows Server
- 🌐 IIS
- ⚙️ .NET 10 Hosting Bundle
- 🐘 PostgreSQL
- 🔗 Internal DNS
- 💾 Automated PostgreSQL backups

Direct PostgreSQL access from client computers is not required. Only the LamaWMS web application needs database access.

## 🦙 About

LamaWMS was developed as a lightweight manufacturing-oriented warehouse management system with a focus on practical fabric inventory control.

The project is designed around a simple principle:

> 📦 Keep warehouse operations transparent, traceable, and easy to use.
