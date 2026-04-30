(function () {
  const __log = globalThis.AppLogger ?? {
    log: () => {},
    warn: () => {},
    error: () => {},
  };
  const openBtn = document.getElementById("openLeaderboardBtn");
  const root = document.getElementById("leaderboardRoot");
  const closeBtn = document.getElementById("leaderboardCloseBtn");
  const overlay = document.getElementById("leaderboardOverlay");
  const leaderboardList = document.getElementById("leaderboardList");
  const sortButtons = document.querySelectorAll(".leaderboard-sort-btn");
  const searchFilter = document.getElementById("leaderboardSearch");

  let isLoaded = false;
  let currentSort = "xp";
  const LIMIT = 50;

  if (!openBtn || !root) {
    __log.error("Leaderboard elements not found");
    return;
  }

  // Завантажити дані лідерборду
  async function loadLeaderboardData() {
    try {
      __log.log("Fetching leaderboard data...");
      const response = await fetch(
        `/Leaderboard/GetLeaderboard?limit=${LIMIT}`,
      );
      if (response.ok) {
        const html = await response.text();
        if (leaderboardList) {
          leaderboardList.innerHTML = html;
        }
        isLoaded = true;
      } else {
        __log.error("Failed to fetch leaderboard:", response.status);
      }
    } catch (err) {
      __log.error("Помилка при завантаженні лідерbordu:", err);
    }
  }

  // Функція для запиту лідерборду з фільтруванням та сортуванням
  async function fetchLeaderboardData(sortBy = "xp", searchTerm = "") {
    try {
      __log.log(`Fetching leaderboard: sort=${sortBy}, search=${searchTerm}`);

      let url = "/Leaderboard/GetFilteredAndSorted?limit=" + LIMIT;
      if (searchTerm) {
        url += "&searchTerm=" + encodeURIComponent(searchTerm);
      }
      if (sortBy) {
        url += "&sortBy=" + encodeURIComponent(sortBy);
      }

      const response = await fetch(url);
      if (response.ok) {
        const html = await response.text();
        if (leaderboardList) {
          leaderboardList.innerHTML = html;
        }
      } else {
        __log.error("Failed to fetch leaderboard:", response.status);
      }
    } catch (err) {
      __log.error("Помилка при отриманні даних лідерборду:", err);
    }
  }

  // Обробники подій для кнопок сортування
  sortButtons.forEach((btn) => {
    btn.addEventListener("click", async function () {
      const newSort = this.getAttribute("data-sort");

      // Видаляємо активний клас з усіх кнопок
      sortButtons.forEach((b) => {
        b.classList.remove("leaderboard-sort-btn-active");
        b.setAttribute("aria-pressed", "false");
      });

      // Додаємо активний клас до натиснутої кнопки
      this.classList.add("leaderboard-sort-btn-active");
      this.setAttribute("aria-pressed", "true");

      // Оновлюємо поточне сортування
      currentSort = newSort;

      // Отримуємо дані з сервера з новим сортуванням
      const searchTerm = searchFilter ? searchFilter.value : "";
      await fetchLeaderboardData(currentSort, searchTerm);

      __log.log(`Sorting changed to: ${newSort}`);
    });
  });

  // Обробник для пошуку
  if (searchFilter) {
    searchFilter.addEventListener("input", async function () {
      const searchTerm = this.value;
      await fetchLeaderboardData(currentSort, searchTerm);
    });
  }

  // Відкрити лідерборд при натисканні кнопки
  openBtn.addEventListener("click", async function () {
    __log.log("Leaderboard button clicked");
    root.classList.add("show");

    // Завантажити дані лідерборду якщо ще не завантажено
    if (!isLoaded) {
      await loadLeaderboardData();
    }
  });

  // Закрити при натисканні кнопки закриття
  if (closeBtn) {
    closeBtn.addEventListener("click", function () {
      __log.log("Leaderboard close button clicked");
      root.classList.remove("show");
    });
  }

  // Закрити коли натиснути на оверлей (темний фон)
  if (overlay) {
    overlay.addEventListener("click", function () {
      __log.log("Leaderboard overlay clicked");
      root.classList.remove("show");
    });
  }
})();
