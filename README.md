# 🏠 BevoBnB: Your Home Away from Home in Austin 🤘

Welcome to BevoBnB, the cozy corner of Austin where every stay feels like home! This project was developed for MIS 333K at The University of Texas at Austin. 🎓

## 🌟 Features

### 👥 For Guests
- Browse and search properties with flexible filters
- Make reservations for your perfect stay
- Share your experiences through reviews
- Track your booking history
- Secure account management

### 🏡 For Hosts
- List and manage your properties
- Set custom pricing and discounts
- Monitor bookings and revenue
- Engage with guest reviews
- View detailed performance reports

### 👨‍💼 For Administrators
- Oversee user management
- Monitor platform activity
- Handle dispute resolution
- Generate system reports
- Maintain property categories

## 🛠 Tech Stack

- **Backend**: ASP.NET Core MVC
- **Database**: SQL Server
- **Cloud**: Microsoft Azure
- **Authentication**: Microsoft Identity
- **Frontend**: HTML, CSS, JavaScript
- **Version Control**: Git

## 🚀 Getting Started

1. Clone the repository
```bash
git clone https://github.com/mis333k-fall2024/Group-25.git
```

2. Install dependencies
```bash
dotnet restore
```

3. Update database connection string in `appsettings.json`

4. Run database migrations
```bash
dotnet ef database update
```

5. Start the application
```bash
dotnet run
```

## 📊 Database Schema

The application uses a relational database with the following key entities:
- Users (Customers, Hosts, Administrators)
- Properties
- Reservations
- Reviews
- Categories

## 🔐 Authentication

We implement role-based access control using Microsoft Identity with three main roles:
- Customer
- Host
- Administrator

## 💰 Business Rules

- Hosts earn 90% of stay revenue (10% platform fee)
- Hosts receive 100% of cleaning fees
- Properties require admin approval before listing
- Flexible cancellation policies for both hosts and guests
- Custom pricing for weekday/weekend stays

## 👥 Contributing

This project is part of MIS 333K coursework. Please ensure you follow the provided coding standards and guidelines.

## 📝 License

This project is for educational purposes only. All rights reserved.

## 🙏 Acknowledgments

- Professor and TAs of MIS 333K
- The University of Texas at Austin
- McCombs School of Business

---
Made with 🧡 by Team 25 | Fall 2024
