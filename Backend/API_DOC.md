# API Documentation

Base URL: `http://127.0.0.1:8000`

---

## Authentication

This API uses **JWT (JSON Web Token)** authentication. All endpoints require a valid access token unless marked as **Public**.

Include the token in the `Authorization` header:

```
Authorization: Bearer <access_token>
```

| Setting          | Value      |
|------------------|------------|
| Access token TTL | 30 minutes |
| Refresh token TTL| 1 day      |

---

## Endpoints

### Auth

#### `POST /api/token/` — Obtain Token Pair

**Auth:** Public

Get an access and refresh token by providing user credentials.

**Request body:**

```json
{
    "username": "nicki",
    "password": "nicktest123"
}
```

**Response `200 OK`:**

```json
{
    "access": "eyJ0eXAiOiJKV1QiLCJhbGci...",
    "refresh": "eyJ0eXAiOiJKV1QiLCJhbGci..."
}
```

**Error `401 Unauthorized`:**

```json
{
    "detail": "No active account found with the given credentials"
}
```

---

#### `POST /api/token/refresh/` — Refresh Access Token

**Auth:** Public

Exchange a valid refresh token for a new access token.

**Request body:**

```json
{
    "refresh": "eyJ0eXAiOiJKV1QiLCJhbGci..."
}
```

**Response `200 OK`:**

```json
{
    "access": "eyJ0eXAiOiJKV1QiLCJhbGci..."
}
```

**Error `401 Unauthorized`:**

```json
{
    "detail": "Token is invalid or expired",
    "code": "token_not_valid"
}
```

---

### Health

#### `GET /api/health/` — Health Check

**Auth:** Public

Returns the server status.

**Response `200 OK`:**

```json
{
    "status": "ok"
}
```

---

### Profiles

All profile endpoints require JWT authentication.

#### `GET /api/users/profiles/` — List All Profiles

**Auth:** Required

Returns a list of all user profiles.

**Response `200 OK`:**

```json
[
    {
        "id": 1,
        "username": "nicki",
        "display_name": "",
        "score": 0
    },
    {
        "id": 2,
        "username": "nikolas",
        "display_name": "",
        "score": 0
    }
]
```

---

#### `GET /api/users/profiles/{id}/` — Get Profile

**Auth:** Required

Returns a single profile by ID.

**Response `200 OK`:**

```json
{
    "id": 1,
    "username": "nicki",
    "display_name": "",
    "score": 0
}
```

**Error `404 Not Found`:**

```json
{
    "detail": "Not found."
}
```

---

#### `GET /api/users/profiles/me/` — Get My Profile

**Auth:** Required

Returns the authenticated caller's own profile. The profile id does not need to be known by the client.

**Response `200 OK`:**

```json
{
    "id": 1,
    "username": "nicki",
    "display_name": "",
    "score": 0
}
```

---

#### `PATCH /api/users/profiles/me/` — Update My Profile

**Auth:** Required

Update one or more fields on the authenticated caller's own profile. The `score` field is **replaced** by the value sent — clients that want a running total must compute the new total locally and send it.

**Request body (example — update score only):**

```json
{
    "score": 50
}
```

**Response `200 OK`:**

```json
{
    "id": 1,
    "username": "nicki",
    "display_name": "Nicki",
    "score": 50
}
```

**Error `400 Bad Request`:**

```json
{
    "score": ["A valid integer is required."]
}
```

---

## Error Responses

All error responses follow this format:

| Status | Meaning                              |
|--------|--------------------------------------|
| 400    | Bad request / validation error       |
| 401    | Missing or invalid JWT token         |
| 404    | Resource not found                   |
| 405    | HTTP method not allowed              |

Unauthenticated requests to protected endpoints return:

```json
{
    "detail": "Authentication credentials were not provided."
}
```

---

## Data Models

### Profile

| Field        | Type    | Description                        | Read-only |
|--------------|---------|------------------------------------|-----------|
| id           | integer | Auto-generated primary key         | yes       |
| username     | string  | Username from the linked User      | yes       |
| display_name | string  | User's display name (max 150 char) | no        |
| score        | integer | User's score (default: 0)          | no        |
