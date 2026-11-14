/* RaceDayDB reference schema for a clean SQL Server database.
   Existing databases should be upgraded with a reviewed EF migration rather
   than running this drop-and-create script. */
IF DB_ID(N'RaceDayDB') IS NULL
    CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

DROP TABLE IF EXISTS RESULT;
DROP TABLE IF EXISTS REGISTRATION;
DROP TABLE IF EXISTS WEATHER;
DROP TABLE IF EXISTS CATEGORY;
DROP TABLE IF EXISTS EVENT;
DROP TABLE IF EXISTS PARTICIPANT;
DROP TABLE IF EXISTS EVENT_ORGANISER;
GO

CREATE TABLE EVENT_ORGANISER
(
    OrganiserId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    Password VARCHAR(255) NOT NULL,
    Phone VARCHAR(20) NULL,
    ProfilePictureUrl VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_EVENT_ORGANISER_CreatedAt DEFAULT GETDATE()
);

CREATE TABLE PARTICIPANT
(
    ParticipantId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    Password VARCHAR(255) NOT NULL,
    DateOfBirth DATE NOT NULL,
    Phone VARCHAR(20) NULL,
    ProfilePictureUrl VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_PARTICIPANT_CreatedAt DEFAULT GETDATE()
);

CREATE TABLE EVENT
(
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL,
    EventName VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NULL,
    EventDate DATE NOT NULL,
    Location VARCHAR(150) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    EventType VARCHAR(20) NOT NULL,
    BannerImageUrl VARCHAR(2048) NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_EVENT_Status DEFAULT 'Available',
    CONSTRAINT CK_EVENT_DistanceKm CHECK (DistanceKm > 0),
    CONSTRAINT CK_EVENT_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CONSTRAINT FK_EVENT_EVENT_ORGANISER FOREIGN KEY (OrganiserId)
        REFERENCES EVENT_ORGANISER(OrganiserId)
);

CREATE TABLE CATEGORY
(
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    CategoryName VARCHAR(100) NOT NULL,
    MinimumAge INT NOT NULL,
    MaximumAge INT NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    Price DECIMAL(8,2) NOT NULL CONSTRAINT DF_CATEGORY_Price DEFAULT 0,
    Capacity INT NULL,
    CONSTRAINT CK_CATEGORY_Ages CHECK (MinimumAge >= 0 AND MaximumAge >= MinimumAge),
    CONSTRAINT CK_CATEGORY_Distance CHECK (Distance > 0),
    CONSTRAINT CK_CATEGORY_Price CHECK (Price >= 0),
    CONSTRAINT CK_CATEGORY_Capacity CHECK (Capacity IS NULL OR Capacity > 0),
    CONSTRAINT FK_CATEGORY_EVENT FOREIGN KEY (EventId) REFERENCES EVENT(EventId)
);

CREATE TABLE WEATHER
(
    WeatherId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    Temperature DECIMAL(5,2) NULL,
    WeatherCondition VARCHAR(100) NULL,
    WindSpeed DECIMAL(5,2) NULL,
    WeatherDate DATE NOT NULL,
    CONSTRAINT FK_WEATHER_EVENT FOREIGN KEY (EventId) REFERENCES EVENT(EventId)
);

CREATE TABLE REGISTRATION
(
    RegistrationId INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId INT NOT NULL,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    RegistrationDate DATETIME NOT NULL CONSTRAINT DF_REGISTRATION_RegistrationDate DEFAULT GETDATE(),
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_REGISTRATION_Status DEFAULT 'Pending',
    CONSTRAINT UQ_REGISTRATION_Participant_Event UNIQUE (ParticipantId, EventId),
    CONSTRAINT FK_REGISTRATION_PARTICIPANT FOREIGN KEY (ParticipantId)
        REFERENCES PARTICIPANT(ParticipantId),
    CONSTRAINT FK_REGISTRATION_EVENT FOREIGN KEY (EventId) REFERENCES EVENT(EventId),
    CONSTRAINT FK_REGISTRATION_CATEGORY FOREIGN KEY (CategoryId) REFERENCES CATEGORY(CategoryId)
);

CREATE TABLE RESULT
(
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    RegistrationId INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    Position INT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_RESULT_Status DEFAULT 'Finished',
    CONSTRAINT CK_RESULT_Status CHECK (Status IN ('Finished', 'DNF', 'DQ')),
    CONSTRAINT CK_RESULT_Time CHECK (FinishTime IS NULL OR FinishTime >= '00:00:00'),
    CONSTRAINT CK_RESULT_Position CHECK (Position IS NULL OR Position > 0),
    CONSTRAINT FK_RESULT_REGISTRATION FOREIGN KEY (RegistrationId)
        REFERENCES REGISTRATION(RegistrationId)
);
GO

/* Example rows use random, unusable password hashes. Create real accounts with
   POST /api/auth/register; these sample accounts cannot be logged into. */
INSERT INTO EVENT_ORGANISER
    (FirstName, LastName, Email, Password, Phone)
VALUES
    ('Sarah', 'Mokoena', 'sarah.mokoena@example.test',
     'PBKDF2-SHA256$210000$lJdoyeD9M7cEkDf9iqk7TA==$Ablrww/4YL3xNcWEJVWuDCoLfjiDgjz3tGK8/T58v6o=',
     '0821234567');
DECLARE @OrganiserId INT = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO PARTICIPANT
    (FirstName, LastName, Email, Password, DateOfBirth, Phone)
VALUES
    ('Thabo', 'Nkosi', 'thabo.nkosi@example.test',
     'PBKDF2-SHA256$210000$kaXPSv0jxQVfQTheQ8twHQ==$bg/6/vfH3WHmnBY9/dZ8fgIv6Pu6a2oh9rju5rvoL9o=',
     '1998-05-14', '0837654321');
DECLARE @ParticipantId INT = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO EVENT
    (OrganiserId, EventName, Description, EventDate, Location, DistanceKm, EventType, Status)
VALUES
    (@OrganiserId, 'Cape Town City Run', 'A community road-running event.',
     '2027-10-18', 'Cape Town Stadium', 10.00, 'Run', 'Available');
DECLARE @EventId INT = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO CATEGORY (EventId, CategoryName, MinimumAge, MaximumAge, Distance, Price, Capacity)
VALUES (@EventId, 'Open 10 km', 18, 99, 10.00, 150.00, 500);
DECLARE @CategoryId INT = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO WEATHER (EventId, Temperature, WeatherCondition, WindSpeed, WeatherDate)
VALUES (@EventId, 22.50, 'Sunny', 10.00, '2027-10-18');

INSERT INTO REGISTRATION (ParticipantId, EventId, CategoryId, Status)
VALUES (@ParticipantId, @EventId, @CategoryId, 'Confirmed');
DECLARE @RegistrationId INT = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO RESULT (RegistrationId, FinishTime, Position, Status)
VALUES (@RegistrationId, '01:02:35', 1, 'Finished');
GO

SELECT * FROM EVENT_ORGANISER;
SELECT * FROM PARTICIPANT;
SELECT * FROM EVENT;
SELECT * FROM CATEGORY;
SELECT * FROM WEATHER;
SELECT * FROM REGISTRATION;
SELECT * FROM RESULT;
GO
