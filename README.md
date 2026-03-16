# 🤝 WorkConnect - Professional Services Marketplace

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?style=for-the-badge&logo=mysql&logoColor=white)
![C#](https://img.shields.io/badge/C%23-11.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![JavaScript](https://img.shields.io/badge/JavaScript-ES6-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black)

A full-stack web application connecting service providers with customers, featuring real-time messaging, booking management, rating systems, and comprehensive analytics dashboards.

---

## 📋 Table of Contents
- [Overview](#overview)
- [Key Features](#key-features)
- [Videos walkthrough](#Video-walk-through)
- [Technology Stack](#technology-stack)
- [Architecture & Design Patterns](#architecture--design-patterns)
- [Database Schema](#database-schema)
- [Installation & Setup](#installation--setup)
- [API Endpoints](#api-endpoints)
- [Security Features](#security-features)
- [Performance Optimizations](#performance-optimizations)
- [Skills Demonstrated](#skills-demonstrated)
- [Future Enhancements](#future-enhancements)


---

## 🎯 Overview

**WorkConnect** is a modern service marketplace platform that bridges the gap between service providers and customers. Built with ASP.NET Core MVC and MySQL, it provides a seamless experience for discovering services, managing bookings, real-time communication, and business analytics.

### **User Roles:**
- **👥 Customers:** Browse services, book providers, negotiate pricing, submit ratings
- **💼 Service Providers:** Post services, manage bookings, track earnings, view analytics
- **🛡️ Administrators:** Monitor platform activity, moderate content, manage users

---

## ✨ Key Features

### **🔐 Authentication & Authorization**
- Session-based authentication with secure password hashing
- Role-based access control (Customer, Provider, Admin)
- Protected routes with middleware validation
- Persistent login sessions with auto-expiration

### **📊 Service Management**
- CRUD operations for service listings with image uploads
- Advanced search and filtering by category, location, price
- Dynamic pricing with customer-provider negotiation
- Service rating and review system with aggregate calculations
- "Top Rated" badges for high-performing providers

### **📅 Booking System**
- Multi-step booking workflow with date selection
- Price negotiation between customers and providers
- Status tracking: Pending → Accepted → Completed
- Email/SMS notifications for booking updates
- Booking history with detailed transaction logs

### **💬 Real-Time Messaging**
- One-on-one chat between customers and providers
- Unread message counter with live updates
- Message persistence with MySQL storage
- Search functionality to find providers
- Admin chat monitoring dashboard

### **⭐ Rating & Review System**
- 5-star rating scale with half-star precision
- Written reviews with character limits
- Automatic calculation of provider average ratings
- Rating breakdown visualization (1-5 stars distribution)
- Pending ratings notification system for customers

### **📈 Analytics Dashboard (Provider)**
- Real-time statistics: Pending, Accepted, Completed jobs
- Total earnings calculation from completed bookings
- Interactive Chart.js visualization (7-day trends)
- Rating analytics with star distribution breakdown
- Recent activity feed with status indicators
- Quick action shortcuts to key workflows

### **🎨 Responsive UI/UX**
- Mobile-first responsive design
- Custom orange theme with gradient accents
- Font Awesome icon integration
- Smooth animations and transitions
- Accessible forms with client-side validation


---


## 📸 Video walk through

### **Provider flow of events 1**
(https://github.com/user-attachments/assets/ce347ab7-f014-4d03-b59b-848cb890efa0)
-Logged in provider can post a job. 
Open the chat tab and chat with a potential client. 
View their orders and choose to accept, reject or negotiate the deal.
Provider can see the different stages the service is in. 
Can view dashoard with service and revenue data.

### **Provider flow of events 2**
(https://github.com/user-attachments/assets/16b6cc17-78ba-4b2a-b7ab-13d6a514ca18)
Provider can view their published services and choose to deactivate a service or remove it from the customers view. 
Can edit service information. 
Providers and Customers can edit their profile information.

### **Customer flow of events 1**
(https://github.com/user-attachments/assets/d9531458-9a70-4985-a1b9-a12ee2733e1d)
Logged in customer browses through services
Searches for a specific service provider
Books a service.

### **Customer flow of events 2**
(https://github.com/user-attachments/assets/6214a6aa-82a3-4093-9a21-58c9df66d7bb)
Customer looks through their bookings.
Searches for a specific provider and opens the chat feature.
Customer checks their bookings to see the progress of their order request in the top left corner of the card (Status changes from Pending to Accepted to Complete.)
Once complete the customer gets a notification to rate the service.*

---
## 🛠️ Technology Stack

### **Backend**
| Technology | Purpose |
|-----------|---------|
| **ASP.NET Core 8.0 MVC** | Web application framework |
| **C# 11.0** | Primary programming language |
| **Entity Framework Core** | ORM (planned migration) |
| **ADO.NET** | Direct database access with MySqlConnector |
| **Session Management** | User state persistence |

### **Frontend**
| Technology | Purpose |
|-----------|---------|
| **Razor Pages** | Server-side rendering |
| **HTML5/CSS3** | Semantic markup and styling |
| **JavaScript (ES6+)** | Client-side interactivity |
| **Chart.js** | Data visualization |
| **Font Awesome 6** | Icon library |
| **Google Fonts (Poppins)** | Typography |

### **Database**
| Technology | Purpose |
|-----------|---------|
| **MySQL 8.0** | Relational database |
| **MySQL Workbench** | Database administration |
| **Stored Procedures** | Complex query optimization (planned) |

### **Development Tools**
- **Visual Studio 2022** - IDE
- **Git/GitHub** - Version control
- **Postman** - API testing
- **Chrome DevTools** - Frontend debugging

---

## 🏗️ Architecture & Design Patterns

### **MVC Pattern**
```
Models (Data Layer)
    ↓
Controllers (Business Logic)
    ↓
Views (Presentation Layer)
```

### **Design Patterns Implemented:**
- **Repository Pattern:** Planned abstraction for data access
- **Dependency Injection:** IConfiguration injection for database connections
- **View Components:** Reusable UI components (e.g., PendingRatingsNotification)
- **Anti-Forgery Tokens:** CSRF protection on all forms
- **Separation of Concerns:** Clear division between UI, logic, and data layers

### **Project Structure:**
```
WorkConnect/
├── Controllers/          # MVC Controllers
│   ├── AccountController.cs
│   ├── BookingsController.cs
│   ├── ProviderController.cs
│   ├── ServicesController.cs
│   └── ChatController.cs
├── Models/              # Data models and ViewModels
│   ├── Booking.cs
│   ├── Service.cs
│   ├── User.cs
│   └── ProviderDashboardViewModel.cs
├── Views/               # Razor views
│   ├── Shared/
│   ├── Bookings/
│   ├── Provider/
│   └── Services/
├── ViewComponents/      # Reusable components
│   └── PendingRatingsNotificationViewComponent.cs
├── wwwroot/            # Static assets
│   ├── css/
│   ├── js/
│   └── images/
└── appsettings.json    # Configuration
```

---

## 🗄️ Database Schema

### **Core Tables:**

#### **`h_users`** (Users)
```sql
- Id (INT, PK, AUTO_INCREMENT)
- FirstName (VARCHAR)
- LastName (VARCHAR)
- Email (VARCHAR, UNIQUE)
- Password (VARCHAR, HASHED)
- UserType (ENUM: 'customer', 'provider', 'admin')
- Phone (VARCHAR)
- WhatsApp (VARCHAR)
- Address (TEXT)
- ProviderImage (VARCHAR)
- IsActive (BOOLEAN)
- EmailConfirmed (BOOLEAN)
- CreatedAt (DATETIME)
- UpdatedAt (DATETIME)
- LastLogin (DATETIME)
```

#### **`service`** (Service Listings)
```sql
- Id (INT, PK, AUTO_INCREMENT)
- Name (VARCHAR)
- Description (TEXT)
- location (VARCHAR)
- duration (VARCHAR)
- availability (VARCHAR)
- rating (DECIMAL(3,2))
- reviewcount (INT)
- price (DECIMAL(10,2))
- serviceImages (VARCHAR)
- ProviderImages (VARCHAR)
- ProviderId (INT, FK → h_users.Id)
- IsActive (BOOLEAN)
- created_at (DATETIME)
- updated_at (DATETIME)
```

#### **`bookings`** (Booking Transactions)
```sql
- Id (INT, PK, AUTO_INCREMENT)
- ServiceId (INT, FK → service.Id)
- CustomerId (INT, FK → h_users.Id)
- ProviderId (INT, FK → h_users.Id)
- BookingDate (DATETIME)
- CustomerNotes (TEXT)
- ProposedPrice (DECIMAL(10,2))
- AgreedPrice (DECIMAL(10,2), NULLABLE)
- Status (ENUM: 'pending', 'accepted', 'rejected', 'completed')
- Rating (DECIMAL(3,2), NULLABLE)
- ReviewText (TEXT, NULLABLE)
- RatedAt (DATETIME, NULLABLE)
- CreatedAt (DATETIME)
- UpdatedAt (DATETIME)
```

#### **`messages`** (Chat Messages)
```sql
- Id (INT, PK, AUTO_INCREMENT)
- SenderId (INT, FK → h_users.Id)
- ReceiverId (INT, FK → h_users.Id)
- MessageText (TEXT)
- IsRead (BOOLEAN)
- SentAt (DATETIME)
```

### **Relationships:**
- One-to-Many: Provider → Services
- One-to-Many: Service → Bookings
- Many-to-Many: Users ↔ Users (via Messages)

### **Indexes for Performance:**
```sql
CREATE INDEX idx_bookings_status ON bookings(Status);
CREATE INDEX idx_bookings_provider_status ON bookings(ProviderId, Status);
CREATE INDEX idx_bookings_customer_status ON bookings(CustomerId, Status);
CREATE INDEX idx_messages_receiver ON messages(ReceiverId, IsRead);
```

---





---

## 🚀 Installation & Setup

### **Prerequisites:**
- .NET 8.0 SDK
- MySQL 8.0+
- Visual Studio 2022 (or VS Code)
- Git

### **Step 1: Clone Repository**
```bash
git clone https://github.com/DakarayiG/phpMVC.git
cd workconnect
```

### **Step 2: Database Setup**
```sql
-- Create database
CREATE DATABASE workconnect_db;

-- Run migration scripts (in order)
SOURCE database/01_create_users_table.sql;
SOURCE database/02_create_services_table.sql;
SOURCE database/03_create_bookings_table.sql;
SOURCE database/04_create_messages_table.sql;
SOURCE database/05_add_rating_columns.sql;
```

### **Step 3: Configure Connection String**
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=workconnect_db;User=root;Password=yourpassword;"
  }
}
```

### **Step 4: Restore Dependencies**
```bash
dotnet restore
```

### **Step 5: Run Application**
```bash
dotnet run
```

Navigate to: `https://localhost:5001`

### **Default Test Accounts:**
```
Provider:
Email: provider@workconnect.com
Password: Provider123!

Customer:
Email: customer@workconnect.com
Password: Customer123!
```

---

## 🔌 API Endpoints

### **Authentication**
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/Account/Register` | User registration |
| POST | `/Account/Login` | User login |
| POST | `/Account/Logout` | User logout |

### **Services**
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Services/Index` | List all services |
| GET | `/Services/Details/{id}` | Service details |
| POST | `/PostJobs/Create` | Create service (Provider) |
| PUT | `/PostJobs/Edit/{id}` | Update service |
| DELETE | `/PostJobs/Delete/{id}` | Delete service |

### **Bookings**
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/Bookings/Book` | Create booking |
| GET | `/Bookings/MyBookings` | Customer bookings |
| GET | `/Bookings/Orders` | Provider pending orders |
| POST | `/Bookings/AcceptBooking` | Accept booking |
| POST | `/Bookings/RejectBooking` | Reject booking |
| POST | `/Bookings/CompleteBooking` | Mark as completed |

### **Ratings**
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Bookings/PendingRatings` | Pending ratings list |
| POST | `/Bookings/SubmitRating` | Submit rating |

### **Chat**
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Chat/Index` | Chat inbox |
| GET | `/Chat/GetMessages/{userId}` | Get conversation |
| POST | `/Chat/SendMessage` | Send message |
| GET | `/Chat/UnreadCount` | Unread message count |

### **Dashboard**
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Provider/Dashboard` | Provider analytics |

---

## 🔒 Security Features

### **Authentication & Authorization**
- ✅ Secure password hashing (BCrypt recommended)
- ✅ Session-based authentication with HTTPS
- ✅ Role-based access control (RBAC)
- ✅ Anti-forgery tokens on all forms (CSRF protection)
- ✅ Input validation and sanitization

### **SQL Injection Prevention**
- ✅ Parameterized queries throughout application
- ✅ No dynamic SQL concatenation
- ✅ Prepared statements for all database operations

### **XSS Protection**
- ✅ Razor automatic HTML encoding
- ✅ Content Security Policy headers
- ✅ Input sanitization on user-generated content

### **Data Protection**
- ✅ Session encryption
- ✅ HTTPS enforcement in production
- ✅ Secure cookie settings
- ✅ Connection string encryption

---

## ⚡ Performance Optimizations

### **Database:**
- Indexed columns for frequent queries (Status, ProviderId, CustomerId)
- Connection pooling with MySqlConnector
- Async/await for database operations
- Query optimization with EXPLAIN analysis

### **Frontend:**
- Minified CSS/JS bundles
- Image optimization and lazy loading
- CDN usage for third-party libraries
- Browser caching strategies

### **Application:**
- Efficient session management
- ViewModels to reduce over-fetching
- Pagination for large datasets
- Debounced search inputs

---

## 💼 Skills Demonstrated

### **Backend Development:**
- ✅ ASP.NET Core MVC architecture
- ✅ C# object-oriented programming
- ✅ RESTful API design principles
- ✅ Database design and normalization
- ✅ SQL query optimization
- ✅ Session management and state handling
- ✅ Dependency injection
- ✅ Asynchronous programming (async/await)

### **Frontend Development:**
- ✅ Responsive web design (Mobile-first)
- ✅ Modern CSS (Flexbox, Grid, Animations)
- ✅ Vanilla JavaScript (ES6+)
- ✅ AJAX/Fetch API for asynchronous requests
- ✅ DOM manipulation
- ✅ Chart.js data visualization
- ✅ Form validation (client & server)

### **Database Management:**
- ✅ MySQL database design
- ✅ Complex SQL queries (JOINs, Aggregations, Subqueries)
- ✅ Database indexing strategies
- ✅ Transaction management
- ✅ Data integrity constraints
- ✅ Migration scripting

### **Software Engineering:**
- ✅ MVC design pattern
- ✅ Repository pattern (planned)
- ✅ SOLID principles
- ✅ Code organization and modularity
- ✅ Error handling and logging
- ✅ Version control (Git/GitHub)

### **Security:**
- ✅ Authentication and authorization
- ✅ SQL injection prevention
- ✅ XSS protection
- ✅ CSRF token implementation
- ✅ Secure password handling

### **UI/UX Design:**
- ✅ User-centered design principles
- ✅ Consistent design system
- ✅ Accessibility best practices
- ✅ Intuitive navigation flows
- ✅ Visual feedback and micro-interactions

---

## 🔮 Future Enhancements

### **Phase 1: Core Improvements**
- [ ] Email verification system
- [ ] Password reset functionality
- [ ] Advanced search filters (availability, distance)
- [ ] Service categories and tags
- [ ] Provider profile pages with portfolio

### **Phase 2: Enhanced Features**
- [ ] Payment gateway integration (Stripe/PayPal)
- [ ] Automated booking reminders (SMS/Email)
- [ ] Calendar integration for providers
- [ ] Multi-image upload for services
- [ ] Provider verification badges

### **Phase 3: Advanced Functionality**
- [ ] Real-time notifications (SignalR)
- [ ] Video chat integration
- [ ] AI-powered service recommendations
- [ ] Mobile app (React Native/Flutter)
- [ ] Advanced analytics with data export

### **Phase 4: Enterprise Features**
- [ ] Multi-language support (i18n)
- [ ] Admin dashboard with metrics
- [ ] Automated testing (Unit, Integration)
- [ ] CI/CD pipeline setup
- [ ] Microservices architecture migration

---

## 📊 Project Metrics

- **Lines of Code:** ~15,000+
- **Development Time:** 6-8 weeks
- **Database Tables:** 4 core tables
- **Controllers:** 7
- **Views:** 25+
- **API Endpoints:** 20+

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👨‍💻 Author

Full Name : Dakarayi Gezana
- GitHub: [@DakarayiG](hhttps://github.com/DakarayiG/phpMVC.git)
- LinkedIn: [Dakarayi Gezana](https://www.linkedin.com/in/dakarayi-gezana-18a450318?utm_source=share&utm_campaign=share_via&utm_content=profile&utm_medium=ios_app)
- Email: gezanadakarayi@gmail.com

---

## 🙏 Acknowledgments

- ASP.NET Core Documentation
- MySQL Documentation
- Chart.js Community
- Font Awesome
- Stack Overflow Community

---

## 📞 Support

For support, email gezanadakarayi@gmail.com or open an issue in the GitHub repository.

---

<div align="center">

**⭐ Star this repository if you found it helpful!**

Made using ASP.NET Core & MySQL

</div>
