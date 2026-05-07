/**
 * Модуль управління статистикою користувача
 * Відповідає за отримання та відображення статистики
 */

(function () {
  "use strict";
  const __log = globalThis.AppLogger ?? {
    log: () => {},
    warn: () => {},
    error: () => {},
  };

  const STATS_CONFIG = {
    apiEndpoint: "/api/statistics",
    selectors: {
      widget: "[data-statistics-root]",
      modal: "#statisticsModal",
      modalBody: "#statisticsModalBody",
      modalClose: "#closeStatisticsModal",
      openButton: "#openStatisticsModal",
      completedTodos: "[data-stat-completed-todos]",
      completedPlans: "[data-stat-completed-plans]",
      pomodoroSessions: "[data-stat-pomodoro-sessions]",
      pomodoroMinutes: "[data-stat-pomodoro-minutes]",
    },
  };

  /**
   * Отримує статистику за день з сервера
   */
  async function fetchTodayStatistics() {
    try {
      const response = await fetch(`${STATS_CONFIG.apiEndpoint}/today`);
      if (!response.ok) {
        throw new Error("Помилка при отриманні статистики");
      }
      const result = await response.json();
      if (!result.success || !result.data) {
        throw new Error(result.message || "Невірний формат відповіді");
      }
      return result.data;
    } catch (error) {
      console.error("Помилка завантаження статистики:", error);
      return null;
    }
  }

  /**
   * Отримує детальну статистику з сервера
   */
  async function fetchDetailedStatistics(startDate = null) {
    try {
      const url = new URL(
        `${STATS_CONFIG.apiEndpoint}/detailed`,
        globalThis.location?.origin ?? location.origin,
      );
      if (startDate) {
        url.searchParams.append("startDate", startDate);
      }
      const response = await fetch(url.toString());
      if (!response.ok) {
        throw new Error("Помилка при отриманні детальної статистики");
      }
      const result = await response.json();
      if (!result.success || !result.data) {
        throw new Error(result.message || "Невірний формат відповіді");
      }
      return result.data;
    } catch (error) {
      __log.error("Помилка завантаження детальної статистики:", error);
      return null;
    }
  }

  /**
   * Отримує статистику за тиждень з сервера
   */
  async function fetchWeeklyProgress(dateInWeek = null) {
    try {
      const url = new URL(
        `${STATS_CONFIG.apiEndpoint}/weekly`,
        globalThis.location?.origin ?? location.origin,
      );
      if (dateInWeek) {
        url.searchParams.append("dateInWeek", dateInWeek);
      }
      const response = await fetch(url.toString());
      if (!response.ok) {
        throw new Error("Помилка при отриманні статистики за тиждень");
      }
      const result = await response.json();
      if (!result.success || !result.data) {
        throw new Error(result.message || "Невірний формат відповіді");
      }
      return result.data;
    } catch (error) {
      __log.error("Помилка завантаження статистики за тиждень:", error);
      return null;
    }
  }

  /**
   * Оновлює віджет статистики на головній сторінці
   */
  function updateWidgetDisplay(stats) {
    if (!stats) return;

    const els = {
      todos: document.querySelector(STATS_CONFIG.selectors.completedTodos),
      plans: document.querySelector(STATS_CONFIG.selectors.completedPlans),
      sessions: document.querySelector(STATS_CONFIG.selectors.pomodoroSessions),
      minutes: document.querySelector(STATS_CONFIG.selectors.pomodoroMinutes),
    };

    if (els.todos) els.todos.textContent = stats.completedTodos || 0;
    if (els.plans) els.plans.textContent = stats.completedPlans || 0;
    if (els.sessions)
      els.sessions.textContent = stats.completedPomodoroSessions || 0;
    if (els.minutes) els.minutes.textContent = stats.totalPomodoroMinutes || 0;
  }

  /**
   * Форматує дату для відображення
   */
  function formatDate(dateString) {
    if (!dateString) return "-";
    const date = new Date(dateString);
    return date.toLocaleDateString("uk-UA", {
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  }

  /**
   * Форматує дату для відображення у графіку
   */
  function formatDateShort(dateString) {
    if (!dateString) return "-";
    const date = new Date(dateString);
    return date.toLocaleDateString("uk-UA", {
      month: "short",
      day: "numeric",
    });
  }

  /**
   * Створює HTML для детальної статистики
   */
  function createDetailedStatsHTML(stats) {
    if (!stats) {
      return '<div class="statistics-empty">Не вдалося завантажити статистику</div>';
    }

    const todoRate = (stats.todoCompletionRate || 0).toFixed(1);
    const planRate = (stats.planCompletionRate || 0).toFixed(1);

    return `
      <div class="statistics-section">
        <h3 class="statistics-section-title">Завдання</h3>
        <div class="statistics-metric">
          <span class="statistics-metric-label">Виконано завдань</span>
          <div>
            <span class="statistics-metric-value">${stats.completedTodos || 0}</span>
            <span class="statistics-metric-unit">із ${stats.totalTodos || 0}</span>
          </div>
        </div>
        <div class="statistics-progress">
          <div class="statistics-progress-label">
            <span>Відсоток виконання</span>
            <span>${todoRate}%</span>
          </div>
          <div class="statistics-progress-bar">
            <div class="statistics-progress-fill" style="width: ${todoRate}%"></div>
          </div>
        </div>
        ${
          stats.lastCompletedTodo
            ? `
          <div class="statistics-metric" style="margin-top: 8px;">
            <span class="statistics-metric-label">Останнє завдання</span>
            <span class="statistics-metric-value">${formatDate(stats.lastCompletedTodo)}</span>
          </div>
        `
            : ""
        }
      </div>

      <div class="statistics-section">
        <h3 class="statistics-section-title">Плани</h3>
        <div class="statistics-metric">
          <span class="statistics-metric-label">Виконано планів</span>
          <div>
            <span class="statistics-metric-value">${stats.completedPlans || 0}</span>
            <span class="statistics-metric-unit">із ${stats.totalPlans || 0}</span>
          </div>
        </div>
        <div class="statistics-progress">
          <div class="statistics-progress-label">
            <span>Відсоток виконання</span>
            <span>${planRate}%</span>
          </div>
          <div class="statistics-progress-bar">
            <div class="statistics-progress-fill" style="width: ${planRate}%"></div>
          </div>
        </div>
        ${
          stats.lastCompletedPlan
            ? `
          <div class="statistics-metric" style="margin-top: 8px;">
            <span class="statistics-metric-label">Останній план</span>
            <span class="statistics-metric-value">${formatDate(stats.lastCompletedPlan)}</span>
          </div>
        `
            : ""
        }
      </div>

      <div class="statistics-section">
        <h3 class="statistics-section-title">Помодоро</h3>
        <div class="statistics-metric">
          <span class="statistics-metric-label">Сесій фокусу</span>
          <span class="statistics-metric-value accent">${stats.completedPomodoroSessions || 0}</span>
        </div>
        <div class="statistics-metric">
          <span class="statistics-metric-label">Хвилин фокусу</span>
          <div>
            <span class="statistics-metric-value">${stats.totalPomodoroMinutes || 0}</span>
            <span class="statistics-metric-unit">хвилин</span>
          </div>
        </div>
        <div class="statistics-metric">
          <span class="statistics-metric-label">Всього сесій</span>
          <span class="statistics-metric-value">${stats.totalPomodoroSessions || 0}</span>
        </div>
      </div>
    `;
  }

  function createWeeklyChartHTML(weeklyData) {
    if (!weeklyData || !weeklyData.dailyStats) {
      return '<div class="statistics-empty">Не вдалося завантажити статистику за тиждень</div>';
    }

    const maxValue =
      Math.max(
        ...weeklyData.dailyStats.map((d) =>
          Math.max(d.completedTodos, d.completedPlans, d.pomodoroSessions),
        ),
      ) || 1;

    const chartHTML = weeklyData.dailyStats
      .map((day) => {
        const todosHeight = (day.completedTodos / maxValue) * 100;
        const plansHeight = (day.completedPlans / maxValue) * 100;
        const pomodoroHeight = (day.pomodoroSessions / maxValue) * 100;

        return `
        <div class="weekly-chart-bar-group">
          <div class="weekly-chart-bars">
            <div class="weekly-chart-bar-item">
              <div class="weekly-chart-bar" style="height: ${todosHeight}%" title="${day.completedTodos} завдань" aria-label="${day.dayName}: ${day.completedTodos} завдань">
                <span class="weekly-chart-bar-label">${day.completedTodos}</span>
              </div>
              <span class="weekly-chart-bar-legend" style="--legend-color: var(--dojo-primary);">Завдання</span>
            </div>
            <div class="weekly-chart-bar-item">
              <div class="weekly-chart-bar weekly-chart-bar-plans" style="height: ${plansHeight}%" title="${day.completedPlans} планів" aria-label="${day.dayName}: ${day.completedPlans} планів">
                <span class="weekly-chart-bar-label">${day.completedPlans}</span>
              </div>
              <span class="weekly-chart-bar-legend" style="--legend-color: var(--dojo-accent);">Плани</span>
            </div>
            <div class="weekly-chart-bar-item">
              <div class="weekly-chart-bar weekly-chart-bar-pomodoro" style="height: ${pomodoroHeight}%" title="${day.pomodoroSessions} сесій" aria-label="${day.dayName}: ${day.pomodoroSessions} сесій">
                <span class="weekly-chart-bar-label">${day.pomodoroSessions}</span>
              </div>
              <span class="weekly-chart-bar-legend" style="--legend-color: var(--dojo-success);">Помодоро</span>
            </div>
          </div>
          <div class="weekly-chart-day-label">${day.dayName}</div>
          <div class="weekly-chart-date">${formatDateShort(day.date)}</div>
        </div>
      `;
      })
      .join("");

    const weekStart = new Date(weeklyData.weekStartDate).toLocaleDateString(
      "uk-UA",
      { day: "numeric", month: "short" },
    );
    const weekEnd = new Date(weeklyData.weekEndDate).toLocaleDateString(
      "uk-UA",
      { day: "numeric", month: "short" },
    );

    return `
      <div class="statistics-section">
        <div class="statistics-weekly-header">
          <h3 class="statistics-section-title">Тижневий прогрес (${weekStart} - ${weekEnd})</h3>
        </div>
        
        <div class="weekly-chart-container">
          ${chartHTML}
        </div>

        <div class="statistics-grid" style="margin-top: 24px;">
          <div class="statistics-item">
            <span class="statistics-label">Завдань за тиждень</span>
            <span class="statistics-value">${weeklyData.totalCompletedTodos}</span>
            <span class="statistics-metric-unit">середньо ${weeklyData.averageTodosPerDay} в день</span>
          </div>
          <div class="statistics-item">
            <span class="statistics-label">Планів за тиждень</span>
            <span class="statistics-value">${weeklyData.totalCompletedPlans}</span>
            <span class="statistics-metric-unit">середньо ${weeklyData.averagePlansPerDay} в день</span>
          </div>
          <div class="statistics-item">
            <span class="statistics-label">Помодоро сесій</span>
            <span class="statistics-value accent">${weeklyData.totalPomodoroSessions}</span>
            <span class="statistics-metric-unit">середньо ${weeklyData.averagePomodoroSessionsPerDay} в день</span>
          </div>
          <div class="statistics-item">
            <span class="statistics-label">Хвилин фокусу</span>
            <span class="statistics-value">${weeklyData.totalPomodoroMinutes}</span>
            <span class="statistics-metric-unit">хвилин</span>
          </div>
        </div>
      </div>
    `;
  }

  /**
   * Відкриває модальне вікно з детальною статистикою
   */
  async function openDetailedStatistics() {
    const modal = document.querySelector(STATS_CONFIG.selectors.modal);
    const modalBody = document.querySelector(STATS_CONFIG.selectors.modalBody);

    if (!modal || !modalBody) return;

    // Показати модаль
    modal.classList.add("show");
    modal.setAttribute("aria-hidden", "false");

    // Показати спіннер
    modalBody.innerHTML =
      '<div class="statistics-loading"><div class="statistics-spinner"></div></div>';

    // Завантажити дані
    const stats = await fetchDetailedStatistics();

    // Оновити вміст модалі
    modalBody.innerHTML = createDetailedStatsHTML(stats);
  }

  /**
   * Отримує детальну статистику з кнопкою для перегляду тижневої
   */
  async function openDetailedStatisticsWithTabs() {
    const modal = document.querySelector(STATS_CONFIG.selectors.modal);
    const modalBody = document.querySelector(STATS_CONFIG.selectors.modalBody);

    if (!modal || !modalBody) return;

    // Показати модаль
    modal.classList.add("show");
    modal.setAttribute("aria-hidden", "false");

    // Показати спіннер
    modalBody.innerHTML =
      '<div class="statistics-loading"><div class="statistics-spinner"></div></div>';

    // Завантажити дані
    const stats = await fetchDetailedStatistics();

    // Оновити вміст модалі з кнопкою для тижневої статистики
    const detailedHTML = createDetailedStatsHTML(stats);
    const weeklyButtonHTML = `
      <div class="statistics-action-buttons">
        <button type="button" class="statistics-tab-button" id="viewWeeklyBtn">
          📊 Переглянути статистику за 7 днів
        </button>
      </div>
    `;

    modalBody.innerHTML = weeklyButtonHTML + detailedHTML;

    // Додати слухач на кнопку тижневої статистики
    const weeklyBtn = document.getElementById("viewWeeklyBtn");
    if (weeklyBtn) {
      weeklyBtn.addEventListener("click", openWeeklyProgress);
    }
  }

  /**
   * Відкриває вікно з тижневою статистикою
   */
  async function openWeeklyProgress() {
    const modal = document.querySelector(STATS_CONFIG.selectors.modal);
    const modalBody = document.querySelector(STATS_CONFIG.selectors.modalBody);

    if (!modal || !modalBody) return;

    // Показати спіннер
    modalBody.innerHTML =
      '<div class="statistics-loading"><div class="statistics-spinner"></div></div>';

    // Завантажити дані
    const weeklyData = await fetchWeeklyProgress();

    // Оновити вміст модалі
    const weeklyHTML = createWeeklyChartHTML(weeklyData);
    const backButtonHTML = `
      <div class="statistics-action-buttons">
        <button type="button" class="statistics-tab-button" id="backToDetailedBtn">
          ← Повернутись до детальної статистики
        </button>
      </div>
    `;

    modalBody.innerHTML = backButtonHTML + weeklyHTML;

    // Додати слухач на кнопку повернення
    const backBtn = document.getElementById("backToDetailedBtn");
    if (backBtn) {
      backBtn.addEventListener("click", openDetailedStatisticsWithTabs);
    }
  }

  /**
   * Закриває модальне вікно
   */
  function closeDetailedStatistics() {
    const modal = document.querySelector(STATS_CONFIG.selectors.modal);
    if (!modal) return;

    modal.classList.remove("show");
    modal.setAttribute("aria-hidden", "true");
  }

  /**
   * Оновлює статистику (публічний метод для інших модулів)
   */
  async function refreshStatistics() {
    const stats = await fetchTodayStatistics();
    if (stats) {
      updateWidgetDisplay(stats);
    }
  }

  /**
   * Ініціалізує модуль статистики
   */
  function init() {
    // Оновити віджет при завантаженні
    fetchTodayStatistics().then((stats) => {
      if (stats) {
        updateWidgetDisplay(stats);
      }
    });

    // Слухачі подій
    const openBtn = document.querySelector(STATS_CONFIG.selectors.openButton);
    const closeBtn = document.querySelector(STATS_CONFIG.selectors.modalClose);
    const modal = document.querySelector(STATS_CONFIG.selectors.modal);

    if (openBtn) {
      openBtn.addEventListener("click", openDetailedStatisticsWithTabs);
    }

    if (closeBtn) {
      closeBtn.addEventListener("click", closeDetailedStatistics);
    }

    // Закрити модаль при кліку поза нею
    if (modal) {
      modal.addEventListener("click", (e) => {
        if (e.target === modal) {
          closeDetailedStatistics();
        }
      });
    }

    // Оновлювати статистику щохвилини
    setInterval(() => {
      fetchTodayStatistics().then((stats) => {
        if (stats) {
          updateWidgetDisplay(stats);
        }
      });
    }, 60000);
  }

  // Ініціалізувати при завантаженні DOM
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }

  // Експортувати функції для глобального використання
  globalThis.StatisticsModule = {
    fetchTodayStatistics,
    fetchDetailedStatistics,
    updateWidgetDisplay,
    openDetailedStatistics,
    closeDetailedStatistics,
    refreshStatistics,
  };
})();
