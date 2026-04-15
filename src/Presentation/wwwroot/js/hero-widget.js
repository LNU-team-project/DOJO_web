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
      levelEl.textContent = `Рівень ${data.level}`;
      const percent = Math.max(0, Math.min(100, Number(data.progressPercent || 0)));
      expInner.style.width = `${percent}%`;
      expText.textContent = `Потрібно ${Math.max(0, Number(data.expToNextLevel || 0) - Number(data.expPoints || 0))} XP до наступного рівня`;
    } catch (err) {
      console.error("Помилка при отриманні статусу героя:", err);
    }
  }

  // Глобальна експозиція
  window.HeroModule = {
    refresh,
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

