---

## Usage

The service accepts a list of subreddits with keywords,
fetches posts from Reddit and returns filtered results.

Two parsing modes are available:
- **Default** — lightweight API-based fetching
- **HTML Parser** — full browser rendering via Playwright/Chromium (`useHtmlParser: true`)

Use `useHtmlParser: true` when Reddit blocks API requests or when you need
to fetch post body text for keyword filtering.

### Features

- Fetch first N posts from each subreddit
- Filter posts by title and post body text
- Two parsing modes: API and HTML (Playwright)
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
  "limit": 20,
  "useHtmlParser": false
}
```

### Parameters

| Parameter | Type | Description |
|---|---|---|
| `items` | array | List of subreddits with keywords |
| `subreddit` | string | Subreddit name (e.g. `r/nature`) |
| `keywords` | array | Keywords to filter posts by title and body |
| `limit` | integer | Number of posts to fetch per subreddit |
| `useHtmlParser` | boolean | Use Playwright HTML parser instead of default. Default: `false` |

---

## Response Example
```json
{
  "/r/nature": [
    {
      "title": "Beautiful forest in autumn",
      "hasMedia": "video"
    },
    {
      "title": "River in the mountains",
      "hasMedia": "gallery"
    }
  ],
  "/r/cats": [
    {
      "title": "My cat learned to open doors",
      "hasMedia": "image"
    }
  ]
}
```

### Response Fields

| Field | Type | Description |
|---|---|---|
| `title` | string | Post title |
| `hasMedia` | string | Type of media in the post (`image`, `video`, `gallery`, or `false` if none) |

---

## Logging


All events are logged to `out.log` file:
```
2026-10-04 12:00:00 [INF] Fetching posts from r/nature
2026-10-04 12:00:01 [INF] Subreddit r/nature: found 3 posts
2026-10-04 12:00:02 [ERR] Failed to connect to Reddit
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
