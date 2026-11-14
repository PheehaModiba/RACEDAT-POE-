# ASP APPLICATION
Part 1 Deliverables (/docs folder)
File	Description
RaceDay_ERD.png	Entity Relationship Diagram covering all 6 entities (Users, Sessions, Events, Categories, Enrolments, Results), with primary keys, foreign keys, and cardinality.
RaceDay_API_Endpoint_Plan.md	Full endpoint plan covering Authentication, User Profile, Events, Categories, Event Enrolments, and Results.
RaceDay_Database.sql	SQL Server script that creates the full schema matching the ERD, with constraints and seed data.
Running the SQL Script
Open SQL Server Management Studio (SSMS) and connect to a local or clean SQL Server instance.
Open docs/RaceDay_Database.sql.
Execute the script (F5). It will drop any existing RaceDayDB database, recreate it, create all tables with their constraints, and seed it with sample data (2 Organisers, 2 Participants, 3 Events, 5 Categories, 3 Enrolments, 1 Result).
Verify the tables and seed data under Databases → RaceDayDB → Tables.
CI/CD

A GitHub Actions workflow (.github/workflows/ci.yml) runs on every push and validates that the /docs folder exists and contains the ERD, API endpoint plan, and SQL script, and that a README is present.
<img width="1319" height="577" alt="image" src="https://github.com/user-attachments/assets/02050fc0-4771-4039-ae73-4452574030d9" />
youtube link: https://youtu.be/etloDhRNm70

/
├── .github/
│   └── workflows/
│       └── ci.yml
├── docs/
│   ├── RaceDay_ERD.png
│   ├── RaceDay_API_Endpoint_Plan.md
│   ├── RaceDay_Database.sql
│   └── ci-success-screenshot.png
└── README.md

##  Part 2 Deliverables (RESTful API Development)

### Technical Specifications Architecture
* Framework:ASP.NET Core Web API with C# (.NET 8.0 / 9.0 Core Engine).
* ORM Engine: Entity Framework Core utilizing a structured **Code-First Approach**.
* Database Driver: Microsoft SQL Server Database Provider.
* Authentication State: Secure Server-Side Session Management matching custom structural action filters.
* **Data Security:** Strict cryptographic security hashing performed using the verified `BCrypt.Net-Next` package.

### Core API Functionality Implemented
* **Authentication Infrastructure:** Secure signup and login methods mapping active session tokens (`UserId` & `UserRole`) to cross-check user roles before processing logic.
* **Profile Layer Handling:** Dynamic retrieval and editing actions targeting authenticated profile parameters for both active user tracks.
* **Fixture Management Routing:** Complete CRUD handling endpoints restricted to `Organiser` accounts to create, modify, or eliminate South African sports fixtures.
* **Dynamic Event Categorization:** Logic for organisers to inject division thresholds (*e.g., U20, Senior, 10km, 21km*) and permit users to browse availability strings.
* **Participant Enrolments Container:** Registration interface tracking links connecting participant accounts, parent events, and target category slots.
* **Results Processing Engine:** Secure data routes giving organisers post-race input privileges for times and leaderboard positions while letting participants fetch verified personal histories.

### Local Initialization & Setup Instructions
1. Open your workspace terminal window inside the root folder of the `RaceDayApi` folder.
2. Confirm your target local engine path points correctly inside the **`appsettings.json`** file:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```
3. Install the missing package dependencies and trigger project compilation:
   ```bash
   dotnet restore
   dotnet build
   ```
4. Fire up the backend engine runtime locally:
   ```bash
   dotnet run
   ```

### Executing the xUnit Automated Test Suite
The codebase includes comprehensive unit tests checking structural validation paths, authentication checks, data storage handling, and role verification controls:
```bash
# Execute the testing package directly inside your console shell
dotnet test --verbosity normal
```


