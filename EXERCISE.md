# Beta Builders - Unity + Backend Exercise

This is an exercise meant to test your proficiency and adaptability in both **Unity** and **backend**, in regards to both development and integration.

You have until **sunday, 03/05/2026, 10:00** to complete the tasks described below.

If you have questions about this exercise, you are free to ask me in my work email: **matan.y@nick.academy**

## Introduction

You will be working with two projects:

1. **MiniGames** -- a Unity educational mini-game platform
2. **Backend** -- a Django REST API that provides authentication and user profiles

Your goal is to connect the Unity game to the provided backend: a player must be able to **log in**, **save their score**, view a **top-10 leaderboard**, **log out**, and play the game in a **WebGL** build.

### Rules

- You may use **any tools** you like to complete the exercise, **including AI agents** (e.g. Cursor, Copilot, ChatGPT).
- If you **do** use AI agents, attach a `PROMPTS.md` file to your answer, containing the prompts you used for **implementation only**. Do not include prompts you used to learn or understand the project.
- You are free to design the UI however you like.

### What we evaluate

Many of the bullets below are deliberately under-specified (when you save the score, where you display it, how you architect networking, where the leaderboard endpoint lives, etc.). Pick whatever you can defend; we evaluate the choice and the reasoning, not adherence to a single right answer. Document any non-obvious decisions briefly in `PROMPTS.md` (or a short `NOTES.md`).

---

## Project Overview -- The Game

MiniGames is a Unity project that runs educational mini-games driven by JSON configuration. **The existing project happens to reside in only one scene** (`Assets/Scenes/SampleScene.unity`).

### Game Flow

1. A **JSON string** is loaded and passed to `GameManager.StartGame(jsonText)` (in `Assets/scripts/GameManager.cs`).
2. The JSON defines a level containing **math quiz questions** (e.g. `_ + 5 = 6`).
3. The player is presented with a math statement and multiple-choice options.
4. On a **correct answer**, a timed mini-game is activated (Hidden Object or Memory Card).
5. The player must complete the mini-game before the timer runs out.
6. **Winning** the mini-game awards XP. **Failing** (timer expires) moves to the next question; if no questions remain, the game is lost.

### JSON Format

The game requires a JSON string to start. The JSON must have a top-level `"levels"` array. Each level contains:

| Field | Type | Description |
|-------|------|-------------|
| `levelId` | int | Unique level identifier |
| `mini_game_id` | string | Maps to a mini-game prefab configured in the Unity Inspector (`"1"` = Memory Card, `"2"` = Hidden Object) |
| `game_time` | float | Seconds the player has to complete the mini-game |
| `levelName` | string | Display name for the level |
| `questions` | array | List of question objects |

Each question contains:

| Field | Type | Description |
|-------|------|-------------|
| `statement` | string | Math statement with `_` as the blank (e.g. `"_+5=6"`) |
| `correctAnswer` | int | The correct numeric answer |
| `options` | int[] | Multiple-choice options |
| `difficulty` | string | Difficulty label (e.g. `"easy"`, `"medium"`) |
| `xp` | int | XP awarded for this question (defaults to 10 if omitted) |

Example JSON files are located in `Assets/jsonsExamples/`.

### Key Scripts

| File | Role |
|------|------|
| `Assets/scripts/GameManager.cs` | Main game orchestrator -- parses JSON, manages levels, quiz, mini-games, timer, win/lose |
| `Assets/scripts/MathStatementQuiz.cs` | Renders math statements and validates answers |
| `Assets/scripts/IMiniGame.cs` | Interface that all mini-games implement |
| `Assets/scripts/MemoryCardGameManager.cs` | Memory card pair-matching mini-game |
| `Assets/scripts/HiddenObjectGameManager.cs` | Hidden object click mini-game |
| `Assets/scripts/MiniJSON.cs` | JSON parser used by GameManager |

### Testing in the Editor

The `GameManager` has a custom editor (`Assets/scripts/Editor/GameManagerEditor.cs`) that lets you paste or load JSON and click "Start Game" while in Play Mode. This is useful for testing without writing any boot code.

---

## Project Overview -- The Backend

The backend is a **Django REST Framework** project with **JWT authentication** already configured.

### Existing Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/token/` | Public | Obtain access + refresh JWT tokens (login) |
| `POST` | `/api/token/refresh/` | Public | Refresh an expired access token |
| `GET` | `/api/health/` | Public | Health check -- returns `{"status": "ok"}` |
| `GET` | `/api/users/profiles/` | Required | List all user profiles (used by the leaderboard) |
| `GET` | `/api/users/profiles/{id}/` | Required | Get a single profile (read-only) |
| `GET` | `/api/users/profiles/me/` | Required | Get the authenticated caller's own profile |
| `PATCH` | `/api/users/profiles/me/` | Required | Update the authenticated caller's own profile (e.g. update score only) |

### Authentication

Send a `POST` to `/api/token/` with `username` and `password` to receive an access token and a refresh token. Include the access token in subsequent requests:

```
Authorization: Bearer <access_token>
```

- Access token lifetime: **30 minutes**
- Refresh token lifetime: **1 day**

### Profile Model

Each user has a `Profile` with the following fields:

| Field | Type | Read-only | Description |
|-------|------|-----------|-------------|
| `id` | integer | yes | Auto-generated primary key |
| `username` | string | yes | Username from the linked User |
| `display_name` | string | no | Display name (max 150 characters) |
| `score` | integer | no | User's score (default: 0) |

For full API details, see `API_DOC.md` and `README.md` in the Backend project.

---

## Requirements

You must implement the following:

### 1. Login Screen (Unity)

- Add a **login UI** to the Unity game. The design is entirely up to you.
- The login must authenticate against the backend API by sending a `POST` request to `/api/token/` with `username` and `password`.
- On successful login, store the JWT access token and use it for subsequent API calls.
- On failure, display an appropriate error message to the player.
- Take into account the possibility of **long play sessions** (30+ minutes).

### 2. Logout Button + Popup (Unity)

- Add a **logout** button accessible to the player while logged in.
- When pressing the button, a **popup** should appear, asking the player if they're sure they want to log out.
- If the player cancels the logout sequence, the popup should disappear.
- Logging out should clear the stored JWT tokens and return the player to the login screen.

### 3. Save Score (Unity)

- When the player **wins** a mini-game (completes the mini-game before the timer runs out), update and display their score in the client (Unity) side.
- Afterwards, send the updated score to the backend.
- Use the profiles API (`PATCH /api/users/profiles/me/`) to update the user's score.
- Take into account that the `score` field on the backend is **replaced** by whatever you send.
- If the score update request fails, display an appropriate error message to the player.

### 4. Leaderboard (Unity + Backend)

- Add a panel ingame that shows the top 10 players who have accumulated the most score.
- Assume we don't want to use `/api/users/profiles/` to get all profiles, and only care for the top 10 leading players.
- The leaderboard should also be **accessible from the login screen**.
- The leaderboard should only show each player's `username` and `score`.

### 5. WebGL Build (Unity)

- The final game must be built for the **WebGL** platform.
- When hosting the WebGL build locally, the communication with the server (which also runs locally) should function correctly.

### Stretch goal: Registration Screen (Unity + Backend)

- Add an additional screen alongside the login screen, where the players can register to the backend via the client (Unity).
- The player can input their username and password into this screen.
- On success, continue normally (akin to logging in).
- On failure (username already exists, connection error etc.) show an appropriate error message.
- The backend has **no registration endpoint** yet. You'll need to add one as part of this stretch goal. The new endpoint should be **public** (unauthenticated, since registering users don't have a token).

---

## Setup Instructions

### Backend

1. Open a terminal in the `Backend` folder.

2. Activate the virtual environment:

```powershell
.\venv\Scripts\Activate
```

3. Install dependencies:

```powershell
pip install -r requirements.txt
```

4. Run database migrations:

```powershell
python manage.py migrate
```

5. Start the development server:

```powershell
python manage.py runserver
```

The server should now be accessible through port `8000` in your local IP or at `http://localhost:8000`.

### Pre-configured Test Users

| Username | Password |
|----------|----------|
| nicki | nicktest123 |
| nikolas | nicktest123 |
| nikol | nicktest123 |

Admin user: `nick_admin` / `Nick@123`

### Unity Project

1. Open Unity Hub and add the `MiniGames` project.
2. Open the project (Unity 6000.3.10f1).
3. Open the scene `Assets/Scenes/SampleScene.unity`.

---

## Deliverables

When you are done, submit the following:

1. **WebGL Build** -- the compiled WebGL build output folder.
2. **Unity Assets** -- compress only the `Assets/` folder into a `.zip` file (not the full Unity project).
3. **Backend** -- compress the full backend project into a `.zip` file.
4. **`PROMPTS.md`** -- a markdown file containing **only** the prompts you used for implementation. Do not include prompts used for learning or understanding the project.
