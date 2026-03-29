# Reddit Analyzer API

A web service for analyzing Reddit posts by keywords.

---

## Tech Stack

- .NET 10.0
- ASP.NET Core Web API
- Serilog (logging)
- Swagger UI
- Docker

---

## Getting Started

### Run with Docker (Recommended)

1. Clone the repository
```bash
git clone https://github.com/YourName/RedditAnalyzer.git
cd RedditAnalyzer
```

2. Start the application
```bash
docker-compose up --build
```

3. Open in browser
```
http://localhost:8080        ← UI
http://localhost:8080/swagger ← Swagger UI
```

---

### Run without Docker

1. Install .NET 10.0 SDK
   https://dotnet.microsoft.com/download

2. Clone the repository
```bash
git clone https://github.com/YourName/RedditAnalyzer.git
cd RedditAnalyzer
```

3. Run the project
```bash
dotnet run
```

4. Open in browser
```
http://localhost:5000         ← UI
http://localhost:5000/swagger ← Swagger UI
```

---

## Usage

The service accepts a list of subreddits with keywords,
fetches posts from Reddit and returns filtered results.

### Features

- Fetch first N posts from each subreddit
- Filter posts by title and post body
- Detect whether a post contains an image
- Export results as a JSON file
- Logging to out.log file
- Parallel fetching of all subreddits simultaneously

---

## API Endpoints

| Method | URL | Description |
|---|---|---|
| POST | `/api/reddit/analyze` | Returns filtered results as JSON |
| POST | `/api/reddit/analyze/download` | Downloads results as a JSON file |

---

## Request Example

**POST** `/api/reddit/analyze`
```json
{
  "items": [
    {
      "subreddit": "r/nature",
      "keywords": ["forest", "river", "tree"]
    },
    {
      "subreddit": "r/cats",
      "keywords": ["cat", "kitten"]
    }
  ],
  "limit": 25
}
```

### Parameters

| Parameter | Type | Description |
|---|---|---|
| `items` | array | List of subreddits with keywords |
| `subreddit` | string | Subreddit name (e.g. `r/nature`) |
| `keywords` | array | Keywords to filter posts by |
| `limit` | integer | Number of posts to fetch per subreddit |

---

## Response Example
```json
{
  "/r/nature": [
    {
      "title": "Beautiful forest in autumn",
      "hasImage": true
    },
    {
      "title": "River in the mountains",
      "hasImage": false
    }
  ],
  "/r/cats": [
    {
      "title": "My cat learned to open doors",
      "hasImage": true
    }
  ]
}
```

### Response Fields

| Field | Type | Description |
|---|---|---|
| `title` | string | Post title |
| `hasImage` | boolean | Whether the post contains an image |

---

## Logging

All events are logged to `out.log` file:
```
2024-01-01 12:00:00 [INF] Fetching posts from r/nature
2024-01-01 12:00:01 [INF] Subreddit r/nature: found 3 posts
2024-01-01 12:00:02 [ERR] Failed to connect to Reddit
```