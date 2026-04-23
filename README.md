UserManagementAPI
ASP.NET Core Web API for managing users (CRUD), with validation and middleware (error handling, token auth, request/response logging).

Run
cd d:\.net\UserManagementAPI
dotnet run
Default dev token is set in appsettings.Development.json:

Auth:ApiToken = dev-token-123
Authentication
/users endpoints require:

Authorization: Bearer <token>

Endpoints
GET /users?skip=0&take=100
GET /users/{id}
POST /users
PUT /users/{id}
DELETE /users/{id}
Example requests (PowerShell)
$h = @{ Authorization = "Bearer dev-token-123" }

irm http://localhost:5277/users -Headers $h

irm http://localhost:5277/users -Method Post -Headers $h -ContentType "application/json" -Body '{
  "firstName":"Mia",
  "lastName":"Singh",
  "email":"mia.singh@techhive.com",
  "department":"HR"
}'
