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
git clone https://github.com/oleksin966/RedditAnalyzer.git
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
git clone https://github.com/oleksin966/RedditAnalyzer.git
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

---

## Теоретичне питання

**Які проблеми можуть виникнути при отриманні даних через HTTP + парсинг HTML? Як би ви їх вирішували?**

> Даний проект використовує офіційний Reddit JSON API (`/r/name.json`)
> замість парсингу HTML — це надійніший підхід. Проте навіть при такому
> підході існують типові проблеми:

### 1. Блокування запитів (Rate Limiting)
Reddit обмежує кількість запитів з однієї IP-адреси.
При перевищенні ліміту повертає помилку `429 Too Many Requests`.

**Рішення:**
- У даному проекті використовується анонімний доступ до Reddit JSON API
- Для продакшену рекомендується підключити OAuth2 авторизацію
  через офіційний Reddit API — це дає більший ліміт запитів
- Також можна додати кешування результатів щоб не робити
  повторних запитів до Reddit

---

### 2. Недоступність сайту або неіснуючий subreddit
Reddit може бути тимчасово недоступний, або користувач передав
неіснуючий subreddit — сервер поверне `404` або `503`.

**Рішення:**
- Обробляти `HttpRequestException` і повертати зрозуміле повідомлення
- Встановити `Timeout` щоб запит не висів вічно
- Перевіряти HTTP статус відповіді перед обробкою

---

### 3. Зміна структури HTML або JSON
Reddit може змінити структуру сторінки або формат відповіді —
парсер перестане працювати і повертатиме порожні результати.

**Рішення:**
- Використовувати офіційний API з версіонуванням замість парсингу HTML
- Додати перевірку що відповідь містить очікувані поля

---

### 4. Великий обсяг даних
При великій кількості subreddit-ів або великому ліміті постів
запит може виконуватись дуже довго.

**Рішення:**
- Використовувати `Task.WhenAll` для паралельного завантаження
  (вже реалізовано у проекті)
- Обмежити максимальний ліміт постів

---

### Висновок
Парсинг HTML є крихким підходом — будь-яка зміна дизайну сайту
ламає парсер. Використання офіційного JSON API як у цьому проекті
є більш надійним рішенням, однак проблеми з Rate Limiting,
блокуванням та недоступністю сервісу залишаються актуальними
і вирішуються описаними методами.
