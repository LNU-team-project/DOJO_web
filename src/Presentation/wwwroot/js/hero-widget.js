(() => {
  const levelEl = document.getElementById("heroLevel");
  const expInner = document.getElementById("heroExpInner");
  const expText = document.getElementById("heroExpText");

  if (!levelEl || !expInner || !expText) {
    // Якщо віджет відсутній на сторінці — створюємо заглушку модулю
    globalThis.HeroModule = {
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
    const percent = Math.max(
      0,
      Math.min(100, Number(data.progressPercent || 0)),
    );
    expInner.style.width = `${percent}%`;
    // Використовуємо спеціальне поле для того, щоб точно показати скільки лишилось
    const remaining = Number(
      data.expToLevelRemaining ??
        Number(data.expToNextLevel || 0) - Number(data.expPoints || 0),
    );
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
      if (!payload?.success || !payload?.data) {
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
      const img = document.querySelector(".hero-img img");
      if (!img) return;

      const originalSrc = img.getAttribute("src");
      const newSrc = "/images/piggy_new.svg";

      // замінюємо іконку
      img.setAttribute("src", newSrc);

      // тимчасово зберігаємо оригінальний текст level badge
      const originalBadgeText = levelEl.textContent;

      // замінюємо текст бейджу та додаємо клас для анімації
      levelEl.textContent = "Новий рівень!";
      levelEl.classList.add("level-up");

      // запуск конфетті ефекту
      spawnConfetti(20);

      // через 3 секунди повертаємо назад і оновимо статус
      setTimeout(async () => {
        levelEl.textContent = originalBadgeText;
        levelEl.classList.remove("level-up");
        img.setAttribute("src", originalSrc);
        await refresh();
      }, 3000);
    } catch (err) {
      console.error("Помилка при показі LevelUp анімації", err);
    }
  }

  function spawnConfetti(count) {
    const container = document.createElement("div");
    container.className = "confetti-container";
    const widget = document.querySelector(".hero-widget");
    if (!widget) return;
    widget.appendChild(container);

    const colors = ["#FF6B6B", "#FFD93D", "#6BCB77", "#4D96FF", "#B983FF"];

    for (let i = 0; i < count; i++) {
      const piece = document.createElement("div");
      piece.className = "confetti-piece";
      piece.style.background =
        colors[Math.floor(Math.random() * colors.length)];
      // random start position near top-center of widget
      const startX = 40 + Math.random() * 60; // percent
      piece.style.left = `${startX}%`;
      piece.style.top = `${10 + Math.random() * 20}px`;
      const delay = Math.random() * 0.6;
      piece.style.animation = `confetti-fall 1.6s cubic-bezier(.2,.8,.2,1) ${delay}s forwards`;
      // random rotate
      piece.style.transform = `rotate(${Math.random() * 360}deg)`;
      container.appendChild(piece);
    }

    // remove container after animation
    setTimeout(() => {
      container?.remove();
    }, 2200);
  }

  // Глобальна експозиція
  globalThis.HeroModule = {
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
