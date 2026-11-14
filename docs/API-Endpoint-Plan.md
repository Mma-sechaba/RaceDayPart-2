# RaceDay API endpoint plan

The API is implemented with ASP.NET Core controllers. All event dates use ISO
8601 date values. Protected routes use the API session cookie and role checks.
Image upload routes accept multipart form data and only accept JPG, PNG, WEBP,
or GIF images up to 5 MB.

## Events

| Method | Route | Access | Purpose and behavior |
|---|---|---|---|
| GET | `/api/events` | Public | Browse events. Supports `search`, `type`, `location`, `from`, `to`, and `upcomingOnly` filters. |
| GET | `/api/events/{id}` | Public | Event details, categories, and related weather information. |
| GET | `/api/events/mine` | Organiser | Dashboard data for events owned by the signed-in organiser. |
| POST | `/api/events` | Organiser | Create an event. Requires a non-empty name and location, a positive distance, and a type of `Run`, `Walk`, or `Cycle`. Ownership comes from the authenticated user. |
| PUT | `/api/events/{id}` | Owning organiser | Update event details subject to the same validation rules. |
| POST | `/api/events/{id}/banner` | Owning organiser | Validate and upload an event banner; returns and saves its public URL. |
| DELETE | `/api/events/{id}` | Owning organiser | Delete an event only when it has no enrolments; otherwise returns `409 Conflict`. |
| GET | `/api/events/{eventId}/enrolments` | Owning organiser | List enrolments and participant/category details for an event. |

## Categories

| Method | Route | Access | Purpose and behavior |
|---|---|---|---|
| GET | `/api/categories` | Public | List categories, including their enrolled-participant counts. |
| GET | `/api/categories/{id}` | Public | Return one category and its enrolled-participant count. |
| POST | `/api/categories` | Organiser | Create a category with a valid name, age range, distance, and price. |
| PUT | `/api/categories/{id}` | Owning organiser | Update category details after validation. |
| DELETE | `/api/categories/{id}` | Owning organiser | Delete a category only when it has no enrolments; otherwise returns `409 Conflict`. |

## Enrolments

The legacy `/api/registrations` routes remain available; the `/api/enrolments`
routes are the user-facing aliases.

| Method | Route | Access | Purpose and behavior |
|---|---|---|---|
| GET | `/api/registrations` | Organiser | List enrolments for events owned by the organiser. |
| GET | `/api/registrations/me` | Participant | List the signed-in participant's enrolments. |
| GET | `/api/registrations/{id}` | Owning organiser or enrollee | Return event, category, participant, status, and result details. |
| POST | `/api/registrations` or `/api/enrolments` | Participant | Enrol in a future event and valid category. Past events are rejected, cancelled entries do not consume capacity, and duplicate enrolments are rejected. |
| PUT | `/api/registrations/{id}` | Owning organiser or enrollee | Update permitted enrolment details. Organiser status changes use the status endpoint. |
| PUT | `/api/enrolments/{id}/status` | Owning organiser | Change an enrolment status using an allowed status value. |
| DELETE | `/api/registrations/{id}` or `/api/enrolments/{id}` | Owning organiser or enrollee | Cancel an enrolment. A participant cannot cancel after a result exists. |

Enrolment responses include event, category, participant, and result details.

## Results

| Method | Route | Access | Purpose and behavior |
|---|---|---|---|
| GET | `/api/results` | Public | Return the richer event leaderboard ordered by finishing position/time. |
| GET | `/api/results/{id}` | Owning organiser or participant | Return result and associated participant/event details. |
| GET | `/api/results/me` | Participant | Return the signed-in participant's results, including event name/date, category, and position of the total finishers. |
| POST | `/api/results` | Owning organiser | Record a result with status `Finished`, `DNF`, or `DQ`; validate time and position. |
| PUT | `/api/results/{id}` | Owning organiser | Update a result after validating its time, position, and status. |
| DELETE | `/api/results/{id}` | Owning organiser | Remove an incorrect result. |

## Accounts and profiles

| Method | Route | Access | Purpose and behavior |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register an organiser or participant. Requires a name, valid email, and password of at least eight characters; duplicate email returns `409 Conflict`. |
| POST | `/api/auth/login` | Public | Authenticate and issue the session cookie. The response includes the profile picture URL. |
| POST | `/api/auth/logout` | Authenticated | End the current session. |
| GET | `/api/profile` | Authenticated | Return the current user's profile, including picture URL. |
| PUT | `/api/profile` | Authenticated | Update profile details. An email already used by another account returns `409 Conflict`. |
| PUT | `/api/profile/password` | Authenticated | Change the current user's password. |
| POST | `/api/users/me/profile-picture` | Authenticated | Validate and upload a profile picture; returns and saves its public URL. |

## Organisers, participants, and weather

Existing organiser, participant, and weather endpoints remain available as
documented in their controllers. Organiser and participant account mutations
are restricted to the appropriate authenticated user or role. The API reads
saved weather data; live third-party weather retrieval requires a separately
configured provider.

## Common responses

- `200 OK`: successful reads and updates.
- `201 Created`: successful resource creation.
- `204 No Content`: successful deletion or logout.
- `400 Bad Request`: invalid input, unsupported image, or business-rule failure.
- `401 Unauthorized`: authentication is required.
- `403 Forbidden`: the caller lacks the required role or ownership.
- `404 Not Found`: the requested resource does not exist.
- `409 Conflict`: duplicate email or an event/category delete blocked by existing enrolments.
- `503 Service Unavailable`: an external dependency such as Azure Blob Storage is unavailable.
