(() => {
  const levelEl = document.getElementById("heroLevel");
  const expInner = document.getElementById("heroExpInner");
  const expText = document.getElementById("heroExpText");

  if (!levelEl || !expInner || !expText) {
    // Якщо віджет відсутній на сторінці — створюємо заглушку модулю
    window.HeroModule = {
      async refresh() {
        return;
      },
    };
    return;
  }

  const MESSAGES = {
    FETCH_ERROR: "Не вдалося завантажити статус героя",
  };

  function applyStatus(data) {
    if (!data) return;
    levelEl.textContent = `Рівень ${data.level}`;
    const percent = Math.max(0, Math.min(100, Number(data.progressPercent || 0)));
    expInner.style.width = `${percent}%`;
    // Використовуємо спеціальне поле для того, щоб точно показати скільки лишилось
    const remaining = Number(data.expToLevelRemaining ?? (Number(data.expToNextLevel || 0) - Number(data.expPoints || 0)));
    expText.textContent = `Потрібно ${Math.max(0, remaining)} XP до наступного рівня`;
  }

  async function refresh() {
    try {
      const res = await fetch("/api/hero/status", { method: "GET" });
      if (!res.ok) {
        console.warn(MESSAGES.FETCH_ERROR);
        return;
      }

      const payload = await res.json();
      if (!payload || !payload.success || !payload.data) {
        console.warn(MESSAGES.FETCH_ERROR);
        return;
      }

      const data = payload.data;
      applyStatus(data);
    } catch (err) {
      console.error("Помилка при отриманні статусу героя:", err);
    }
  }

  // Показати анімацію підвищення рівня з тимчасовою заміною картинки
  async function showLevelUp(data) {
    try {
      applyStatus(data);

      // знаходимо img елемент свинки у віджеті
      const img = document.querySelector('.hero-img img');
      if (!img) return;

      const originalSrc = img.getAttribute('src');
      const newSrc = '/images/piggy_new.svg';

      // замінюємо іконку
      img.setAttribute('src', newSrc);

      // тимчасово зберігаємо оригінальний текст level badge
      const originalBadgeText = levelEl.textContent;
      const originalBadgeStyle = levelEl.getAttribute('data-original-style') || '';

      // замінюємо текст бейджу на 'Новий рівень!'
      levelEl.textContent = 'Новий рівень!';
      // трохи змінюємо стиль, щоб було помітно
      levelEl.style.background = 'linear-gradient(90deg,#ff9ab8,#ff6f99)';
      levelEl.style.color = '#fff';
      levelEl.style.fontWeight = '700';

      // через 3 секунди повертаємо назад і оновимо статус
      setTimeout(async () => {
        levelEl.textContent = originalBadgeText;
        levelEl.removeAttribute('style');
        img.setAttribute('src', originalSrc);
        await refresh();
      }, 3000);
    } catch (err) {
      console.error('Помилка при показі LevelUp анімації', err);
    }
  }

  // Глобальна експозиція
  window.HeroModule = {
    refresh,
    showLevelUp,
    applyStatus,
  };

  // Слухаємо глобальну подію на оновлення
  globalThis.addEventListener("hero:refresh", () => {
    refresh();
  });

  // Автоматичне оновлення при завантаженні сторінки
  globalThis.addEventListener("DOMContentLoaded", () => {
    // декілька мілісекунд пауза — щоб auth cookie могли встановитись
    setTimeout(refresh, 50);
  });
})();
