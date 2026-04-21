(function() {
    const openBtn = document.getElementById('openLeaderboardBtn');
    const root = document.getElementById('leaderboardRoot');
    const closeBtn = document.getElementById('leaderboardCloseBtn');
    const overlay = document.getElementById('leaderboardOverlay');
    const leaderboardList = document.getElementById('leaderboardList');
    const sortButtons = document.querySelectorAll('.leaderboard-sort-btn');
    const searchFilter = document.getElementById('leaderboardSearch');
    
    let isLoaded = false;
    let allItems = [];
    let currentSort = 'xp';

    if (!openBtn || !root) {
        console.error('Leaderboard elements not found');
        return;
    }

    // Завантажити дані лідерборду
    async function loadLeaderboardData() {
        try {
            console.log('Fetching leaderboard data...');
            const response = await fetch('/Home/GetLeaderboard?limit=50');
            if (response.ok) {
                const html = await response.text();
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, 'text/html');
                const newList = doc.querySelector('.leaderboard-list');
                if (newList && leaderboardList) {
                    console.log('Leaderboard data loaded successfully');
                    leaderboardList.innerHTML = newList.innerHTML;
                    // Зберігаємо всі елементи для фільтрування
                    allItems = Array.from(leaderboardList.querySelectorAll('.leaderboard-item'));
                }
                isLoaded = true;
            } else {
                console.error('Failed to fetch leaderboard:', response.status);
            }
        } catch (err) {
            console.error('Помилка при завантаженні лідерборду:', err);
        }
    }

    // Функція для сортування та фільтрування лідерборду
    function sortAndFilterLeaderboard() {
        const searchTerm = searchFilter.value.toLowerCase().trim();
        
        console.log(`Sorting by: ${currentSort}, search: ${searchTerm}`);
        
        // Фільтруємо за пошуком
        const filtered = allItems.filter(item => {
            const username = item.querySelector('.leaderboard-name')?.textContent.toLowerCase() || '';
            return !searchTerm || username.includes(searchTerm);
        });

        // Сортуємо відповідно до вибору
        const sorted = filtered.sort((a, b) => {
            if (currentSort === 'xp') {
                const xpA = parseInt(a.querySelector('.leaderboard-score')?.getAttribute('data-xp') || 0);
                const xpB = parseInt(b.querySelector('.leaderboard-score')?.getAttribute('data-xp') || 0);
                return xpB - xpA; // За спаданням
            } else if (currentSort === 'pomodoro') {
                const pomA = parseInt(a.querySelector('.leaderboard-pomodoro')?.getAttribute('data-pomodoro') || 0);
                const pomB = parseInt(b.querySelector('.leaderboard-pomodoro')?.getAttribute('data-pomodoro') || 0);
                return pomB - pomA; // За спаданням
            } else if (currentSort === 'level') {
                const levelA = parseInt(a.querySelector('.leaderboard-level')?.getAttribute('data-level') || 0);
                const levelB = parseInt(b.querySelector('.leaderboard-level')?.getAttribute('data-level') || 0);
                return levelB - levelA; // За спаданням
            }
            return 0;
        });

        // Очищаємо список та додаємо відсортовані елементи
        leaderboardList.innerHTML = '';
        
        if (sorted.length === 0 && allItems.length > 0) {
            const emptyItem = document.createElement('li');
            emptyItem.className = 'leaderboard-empty';
            emptyItem.textContent = 'Користувачів не знайдено';
            leaderboardList.appendChild(emptyItem);
        } else {
            // Додаємо відсортовані елементи з оновленим рангом
            sorted.forEach((item, index) => {
                const clonedItem = item.cloneNode(true);
                // Оновлюємо ранг (номер позиції) на основі нового порядку
                const rankElement = clonedItem.querySelector('.leaderboard-rank');
                if (rankElement) {
                    rankElement.textContent = index + 1;
                }
                leaderboardList.appendChild(clonedItem);
            });
        }
    }

    // Обробники подій для кнопок сортування
    sortButtons.forEach(btn => {
        btn.addEventListener('click', function() {
            const newSort = this.getAttribute('data-sort');
            
            // Видаляємо активний клас з усіх кнопок
            sortButtons.forEach(b => {
                b.classList.remove('leaderboard-sort-btn-active');
                b.setAttribute('aria-pressed', 'false');
            });
            
            // Додаємо активний клас до натиснутої кнопки
            this.classList.add('leaderboard-sort-btn-active');
            this.setAttribute('aria-pressed', 'true');
            
            // Оновлюємо поточне сортування
            currentSort = newSort;
            
            // Сортуємо список
            sortAndFilterLeaderboard();
            
            console.log(`Sorting changed to: ${newSort}`);
        });
    });

    // Обробник для пошуку
    if (searchFilter) {
        searchFilter.addEventListener('input', sortAndFilterLeaderboard);
    }

    // Відкрити лідерборд при натисканні кнопки
    openBtn.addEventListener('click', async function() {
        console.log('Leaderboard button clicked');
        root.classList.add('show');
        
        // Завантажити дані лідерборду якщо ще не завантажено
        if (!isLoaded) {
            await loadLeaderboardData();
        }
    });

    // Закрити при натисканні кнопки закриття
    if (closeBtn) {
        closeBtn.addEventListener('click', function() {
            console.log('Leaderboard close button clicked');
            root.classList.remove('show');
        });
    }

    // Закрити коли натиснути на оверлей (темний фон)
    if (overlay) {
        overlay.addEventListener('click', function() {
            console.log('Leaderboard overlay clicked');
            root.classList.remove('show');
        });
    }

    console.log('Leaderboard script initialized');
})();
