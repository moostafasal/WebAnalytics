# 🚀 Web Analytics Data Aggregator

A robust backend system that ingests web analytics data from multiple sources, processes it through RabbitMQ message broker, aggregates metrics, and exposes reporting APIs protected by JWT authentication.

## 📋 Table of Contents
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Quick Start](#quick-start)
- [API Documentation](#api-documentation)
- [Project Structure](#project-structure)
- [Features](#features)

---

## 🏗️ Architecture

```
┌─────────────┐      ┌──────────────┐      ┌────────────┐
│  Mock JSON  │ ───▶ │   Producer   │ ───▶ │  RabbitMQ  │
│   Files     │      │   (API)      │      │  Exchange  │
└─────────────┘      └──────────────┘      └─────┬──────┘
                                                  │
                                                  ▼
┌─────────────┐      ┌──────────────┐      ┌────────────┐
│  Report API │ ◀─── │   postgresql │ ◀─── │  Consumer  │
│  (JWT Auth) │      │   Database   │      │  Worker    │
└─────────────┘      └──────────────┘      └────────────┘
```

### Data Flow
1. **Ingestion**: API reads Google Analytics + PageSpeed JSON files
2. **Publishing**: Combined records published to RabbitMQ exchange
3. **Processing**: Background consumer reads from queue with retry logic
4. **Aggregation**: Daily statistics calculated and stored in postgresql
5. **Reporting**: Authenticated users query aggregated data via REST APIs

---

## 🛠️ Tech Stack

- **Backend**: .NET 8, ASP.NET Core Web API
- **Database**:postgresql with Entity Framework Core
- **Message Broker**: RabbitMQ
- **Authentication**: JWT Bearer Tokens
- **Resilience**: Polly for retry policies + Dead Letter Queue
- **Documentation**: Swagger/OpenAPI
- **Containerization**: Docker & Docker Compose

---


---

## 🚀 Quick Start

### 1. Clone Repository
```bash
git clone https://github.com/yourusername/web-analytics-aggregator.git
cd web-analytics-aggregator
```

### 2. Start All Services
```bash
docker-compose up -d --build
```

This command starts:
- **postgresql** on port `5432`
- **RabbitMQ** on port `5672` (Management UI on `15672`)
- **API** on port `5000`
- **Consumer** (background service)

### 3. Verify Services

**Check Health:**
```bash
curl http://localhost:5000/health
```

**RabbitMQ Management UI:**
- URL: http://localhost:15672
- Username: `admin`
- Password: `admin123`

**Swagger UI:**
- URL: http://localhost:5000

---

## 📡 API Documentation

### Authentication Endpoints

#### Register User
```bash
POST /api/auth/register
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123"
}
```

#### Login
```bash
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePass123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "john@example.com",
  "name": "John Doe"
}
```

### Protected Endpoints (Require JWT)

#### Trigger Data Ingestion
```bash
POST /api/ingest/trigger
Authorization: Bearer YOUR_JWT_TOKEN
```

#### Get Overview Report
```bash
GET /api/reports/overview
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "totalUsers": 1450,
  "totalSessions": 1700,
  "totalViews": 3850,
  "averagePerformance": 0.91,
  "totalPages": 4,
  "firstDate": "2025-10-20",
  "lastDate": "2025-10-22"
}
```

#### Get Page-Level Reports
```bash
GET /api/reports/pages
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
[
  {
    "page": "/home",
    "totalUsers": 400,
    "totalSessions": 490,
    "totalViews": 1015,
    "averagePerformance": 0.92,
    "averageLCPms": 1850
  }
]
```

---

## 📂 Project Structure

```
WebAnalyticsAggregator/
├── WebAnalytics.API/              # REST API Project
│   ├── Controllers/
│   │   ├── AuthController.cs     # Registration & Login
│   │   ├── ReportsController.cs  # Analytics Reports
│   │   └── IngestController.cs   # Data Ingestion
│   ├── MockData/
│   │   ├── google-analytics.json
│   │   └── pagespeed.json
│   ├── Program.cs
│   └── Dockerfile
│
├── WebAnalytics.Consumer/         # Background Worker
│   ├── Worker.cs                  # RabbitMQ Consumer
│   ├── Program.cs
│   └── Dockerfile
│
├── WebAnalytics.Core/             # Domain Models & DTOs
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── RawData.cs
│   │   └── DailyStats.cs
│   └── DTOs/
│       ├── AnalyticsMessage.cs
│       ├── AuthDTOs.cs
│       └── ReportDTOs.cs
│
├── WebAnalytics.Infrastructure/   # Data Access & Services
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── MessageBroker/
│   │   └── RabbitMQProducer.cs
│   └── Services/
│       ├── AuthService.cs
│       ├── ReportService.cs
│       └── DataIngestionService.cs
│
│
├── docker-compose.yml
└── README.md
```

---

## ✨ Features

### Core Requirements
✅ **Mock Data Adapters**: Reads GA + PSI JSON files  
✅ **RabbitMQ Integration**: Real message broker (no in-memory queues)  
✅ **Background Consumer**: Processes messages with retry logic  
✅ **Daily Aggregation**: Calculates totals and averages per day  
✅ **JWT Authentication**: Secure signup/login with Bearer tokens  
✅ **Reporting APIs**: Overview + page-level analytics  
✅ **Docker Compose**: Single-command deployment  


---




## 🛑 Stopping Services

```bash
docker-compose down        # Stop containers
docker-compose down -v     # Stop and remove volumes (clears data)
```

---


---

---

