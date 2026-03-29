function addBlock() {
    const container = document.getElementById('subreddits');
    const block = document.createElement('div');
    block.className = 'subreddit-block';
    block.innerHTML = `
        <button class="btn btn-remove" onclick="removeBlock(this)">Видалити</button>
        <label>Subreddit:</label>
        <div class="input-prefix-wrapper">
            <span class="input-prefix">r/</span>
            <input type="text" placeholder="cats" class="subreddit-input" />
        </div>
        <label>Ключові слова (через кому):</label>
        <input type="text" placeholder="cat, kitten" class="keywords-input" />
    `;
    container.appendChild(block);
}

function removeBlock(btn) {
    const blocks = document.querySelectorAll('.subreddit-block');
    if (blocks.length > 1) {
        btn.parentElement.remove();
    } else {
        alert('Повинен бути хоча б один subreddit');
    }
}

function buildRequest() {
    const limit = parseInt(document.getElementById('limit').value);
    const blocks = document.querySelectorAll('.subreddit-block');

    const items = Array.from(blocks).map(block => ({
        subreddit: 'r/' + block.querySelector('.subreddit-input').value.trim(),
        keywords: block.querySelector('.keywords-input').value
            .split(',')
            .map(k => k.trim())
            .filter(k => k.length > 0)
    }));

    return { items, limit };
}

async function analyze() {
    const resultDiv = document.getElementById('result');
    resultDiv.innerHTML = '<div class="loading">Завантаження...</div>';

    try {
        const response = await fetch('/api/reddit/analyze', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(buildRequest())
        });

        if (!response.ok) {
            const error = await response.text();
            resultDiv.innerHTML = `<div class="error">Помилка: ${error}</div>`;
            return;
        }

        const data = await response.json();
        renderResult(data, resultDiv);

    } catch (e) {
        resultDiv.innerHTML = `<div class="error">Помилка підключення: ${e.message}</div>`;
    }
}

async function download() {
    const resultDiv = document.getElementById('result');
    resultDiv.innerHTML = '<div class="loading">Завантаження...</div>';

    try {
        const response = await fetch('/api/reddit/analyze/download', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(buildRequest())
        });

        if (!response.ok) {
            const error = await response.text();
            resultDiv.innerHTML = `<div class="error">Помилка: ${error}</div>`;
            return;
        }

        const blob = await response.blob();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `reddit_analysis_${Date.now()}.json`;
        a.click();

        resultDiv.innerHTML = '<div class="loading">Файл завантажено!</div>';

    } catch (e) {
        resultDiv.innerHTML = `<div class="error">Помилка: ${e.message}</div>`;
    }
}

function renderResult(data, container) {
    const keys = Object.keys(data);

    if (keys.length === 0) {
        container.innerHTML = '<div class="no-posts">Нічого не знайдено</div>';
        return;
    }

    let html = '';
    keys.forEach(subreddit => {
        const posts = data[subreddit];
        const subredditUrl = `https://www.reddit.com/${subreddit}`;

        html += `<div class="subreddit-result">`;
        html += `<div class="subreddit-title">
                    <a href="${subredditUrl}" target="_blank">${subreddit}</a>
                    (${posts.length} постів)
                 </div>`;

        if (posts.length === 0) {
            html += `<div class="no-posts">Постів не знайдено</div>`;
        } else {
            posts.forEach(post => {
                const imageLabel = post.hasImage
                    ? '<span class="post-image has-image">є зображення</span>'
                    : '<span class="post-image no-image">без зображення</span>';

                const postUrl = `https://www.reddit.com/${subreddit.replace('/r/', 'r/')}/search/?q=${encodeURIComponent(post.title)}`;

                html += `
                    <div class="post-card">
                        <div class="post-title">
                            <a href="${postUrl}" target="_blank">${post.title}</a>
                        </div>
                        ${imageLabel}
                    </div>`;
            });
        }
        html += `</div>`;
    });

    container.innerHTML = html;
}