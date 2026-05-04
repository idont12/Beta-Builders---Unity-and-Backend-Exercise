# Backend - Django REST API

A Django REST Framework project with a health-check endpoint.

## Prerequisites

- Python 3.9+

## Setup

### 1. Activate the virtual environment

```powershell
.\venv\Scripts\Activate
```

### 2. Install dependencies

```powershell
pip install -r requirements.txt
```

### 3. Run database migrations

```powershell
python manage.py migrate
```

### 4. Create a superuser

A default superuser is pre-configured:

- **Username:** nick_admin
- **Password:** Nick@123

To create it, run:

```powershell
python manage.py createsuperuser
```

### Pre-configured regular users

| Username | Password    |
|----------|-------------|
| nicki    | nicktest123 |
| nikolas  | nicktest123 |
| nikol    | nicktest123 |

### 5. Start the development server

```powershell
python manage.py runserver
```

The server will start at **http://127.0.0.1:8000**.

## Authentication (JWT)

Most API endpoints require a JWT token. The health-check endpoint is public.

### Login (obtain tokens)

```
POST http://127.0.0.1:8000/api/token/
Content-Type: application/json

{
    "username": "nicki",
    "password": "nicktest123"
}
```

Response:

```json
{
    "access": "<access_token>",
    "refresh": "<refresh_token>"
}
```

### Refresh an expired access token

```
POST http://127.0.0.1:8000/api/token/refresh/
Content-Type: application/json

{
    "refresh": "<refresh_token>"
}
```

### Use the token

Include the access token in the `Authorization` header:

```
Authorization: Bearer <access_token>
```

Token lifetimes: access = 30 minutes, refresh = 1 day.

## Verify it works

Once the server is running:

- **Health check (public):** http://127.0.0.1:8000/api/health/ — should return `{"status": "ok"}`
- **Admin panel:** http://127.0.0.1:8000/admin/ — log in with the superuser credentials
- **Profiles (requires JWT):** `GET http://127.0.0.1:8000/api/users/profiles/`

## Project structure

```
Backend/
├── config/          # Django project settings
│   ├── settings.py
│   ├── urls.py
│   ├── wsgi.py
│   └── asgi.py
├── api/             # API application
│   ├── views.py     # API views (health check)
│   ├── urls.py      # API URL routing
│   └── models.py    # Database models
├── manage.py
├── requirements.txt
└── README.md
```
