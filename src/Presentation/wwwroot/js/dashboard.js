(() => {
  const root = document.querySelector("[data-dashboard-root]");
  if (!root) {
    return;
  }

  const rangeLabel = root.querySelector("[data-range-label]");
  const daysHeader = root.querySelector("[data-days-header]");
  const timeGrid = root.querySelector("[data-time-grid]");
  const board = root.querySelector(".dashboard-board");
  const prevButton = root.querySelector("[data-range-dir='prev']");
  const nextButton = root.querySelector("[data-range-dir='next']");
  const notificationsUrl = root.dataset.notificationsUrl;
  const notificationsModal = document.getElementById("notificationsModal");
  const notificationsModalOverlay = document.getElementById(
    "notificationsModalOverlay",
  );
  const openNotificationsModalBtn = document.getElementById(
    "openNotificationsModal",
  );
  const closeNotificationsModalBtn = document.getElementById(
    "closeNotificationsModal",
  );
  const closeNotificationsModalFooterBtn = document.getElementById(
    "closeNotificationsModalFooter",
  );
  let notificationsList =
    root.querySelector("[data-notifications-list]") ||
    root.querySelector(".notifications-modal-list");

  if (
    !rangeLabel ||
    !daysHeader ||
    !timeGrid ||
    !prevButton ||
    !nextButton ||
    !board
  ) {
    return;
  }

  const locale = "uk-UA";
  const dayNames = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд"];
  const timeSlots = Array.from(
    { length: 24 },
    (_, index) => `${String(index).padStart(2, "0")}:00`,
  );

  let weekOffset = 0;

  const getWeekStartForDate = (date) => {
    const base = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const day = base.getDay();
    const offsetToMonday = day === 0 ? -6 : 1 - day;
    base.setDate(base.getDate() + offsetToMonday);
    return base;
  };

  const getWeekStart = (date) => {
    const start = getWeekStartForDate(date);
    start.setDate(start.getDate() + weekOffset * 7);
    return start;
  };

  const getWeekDates = () => {
    const start = getWeekStart(new Date());
    return Array.from({ length: 7 }, (_, index) => {
      const dayDate = new Date(start);
      dayDate.setDate(start.getDate() + index);
      return dayDate;
    });
  };

  const formatDate = (date) =>
    date.toLocaleDateString(locale, {
      day: "2-digit",
      month: "short",
      year: "numeric",
    });

  const renderRange = (dates) => {
    const start = dates[0];
    const end = dates[dates.length - 1];
    rangeLabel.textContent = `${formatDate(start)} - ${formatDate(end)}`;
  };

  const renderDays = (dates) => {
    daysHeader.innerHTML = "";

    const spacer = document.createElement("div");
    spacer.className = "dashboard-day-spacer";
    spacer.setAttribute("aria-hidden", "true");
    daysHeader.appendChild(spacer);

    dates.forEach((date, index) => {
      const dayCell = document.createElement("div");
      dayCell.className = "dashboard-day-cell";

      const title = document.createElement("span");
      title.className = "dashboard-day-name";
      title.textContent = dayNames[index];

      const number = document.createElement("span");
      number.className = "dashboard-day-number";
      number.textContent = String(date.getDate()).padStart(2, "0");

      dayCell.append(title, number);
      daysHeader.appendChild(dayCell);
    });
  };

  const renderTimeGrid = () => {
    timeGrid.innerHTML = "";

    timeSlots.forEach((slot) => {
      const row = document.createElement("div");
      row.className = "dashboard-grid-row";

      const time = document.createElement("div");
      time.className = "dashboard-time-label";
      time.textContent = slot;

      const cells = document.createElement("div");
      cells.className = "dashboard-row-cells";

      for (let day = 0; day < 7; day += 1) {
        const cell = document.createElement("div");
        cell.className = "dashboard-slot-cell";
        cells.appendChild(cell);
      }

      row.append(time, cells);
      timeGrid.appendChild(row);
    });
  };

  const syncHeaderWithGrid = () => {
    const scrollbarWidth = timeGrid.offsetWidth - timeGrid.clientWidth;
    root.style.setProperty(
      "--dashboard-scrollbar-offset",
      `${Math.max(scrollbarWidth, 0)}px`,
    );
  };

  const syncGridViewport = () => {
    const availableHeight = board.clientHeight - daysHeader.offsetHeight;
    if (availableHeight > 0) {
      timeGrid.style.maxHeight = `${availableHeight}px`;
    }
  };

  const getSeverityClass = (notification) => {
    return Number(notification?.severity) === 2
      ? "notification-item-warning"
      : "notification-item-info";
  };

  const getNotificationsList = () => {
    if (notificationsList) {
      return notificationsList;
    }

    notificationsList =
      root.querySelector("[data-notifications-list]") ||
      document.querySelector(".notifications-modal-list");
    return notificationsList;
  };

  const renderNotificationsState = (badge, title, description) => {
    const list = getNotificationsList();
    if (!list) {
      return;
    }

    list.innerHTML = `
      <li class="notification-item notification-item-info">
        <div class="notification-item-badge">${badge}</div>
        <div class="notification-item-content">
          <h3 class="notification-item-title">${title}</h3>
          <p class="notification-item-description">${description}</p>
        </div>
      </li>`;
  };

  const renderNotifications = (notifications) => {
    const list = getNotificationsList();
    if (!list) {
      return;
    }

    if (!Array.isArray(notifications) || notifications.length === 0) {
      renderNotificationsState(
        "Інфо",
        "Немає нових сповіщень",
        "Зараз у вас немає важливих оновлень.",
      );
      return;
    }

    list.innerHTML = notifications
      .map((notification) => {
        const actionsHtml =
          Array.isArray(notification.actions) && notification.actions.length > 0
            ? `
            <div class="notification-item-actions">
              ${notification.actions
                .map(
                  (action) => `
                  <button type="button"
                          class="notification-item-action"
                          data-notification-action="${action.action ?? ""}"
                          data-request-id="${action.requestId ?? ""}">
                    ${action.label ?? "Дія"}
                  </button>`,
                )
                .join("")}
            </div>`
            : "";

        return `
        <li class="notification-item ${getSeverityClass(notification)}">
          <div class="notification-item-badge">${notification.badge ?? "Інфо"}</div>
          <div class="notification-item-content">
            <h3 class="notification-item-title">${notification.title ?? "Сповіщення"}</h3>
            <p class="notification-item-description">${notification.description ?? ""}</p>
            ${actionsHtml}
          </div>
        </li>`;
      })
      .join("");
  };

  const handleNotificationActionClick = async (event) => {
    const button = event.target.closest("[data-notification-action]");
    if (!button || !notificationsModal?.classList.contains("show")) {
      return;
    }

    const requestId = Number.parseInt(button.dataset.requestId ?? "", 10);
    const action = button.dataset.notificationAction;
    if (!Number.isFinite(requestId) || !action) {
      return;
    }

    let endpoint = null;
    if (action === "accept") {
      endpoint = `/api/friends/requests/${requestId}/accept`;
    } else if (action === "decline") {
      endpoint = `/api/friends/requests/${requestId}/decline`;
    }

    if (!endpoint) {
      return;
    }

    button.disabled = true;
    try {
      const response = await fetch(endpoint, {
        method: "POST",
        credentials: "include",
      });

      if (!response.ok) {
        button.disabled = false;
        return;
      }

      const payload = await response.json().catch(() => null);
      if (!payload?.success) {
        button.disabled = false;
        return;
      }

      void loadNotifications();
    } catch {
      button.disabled = false;
    }
  };

  const loadNotifications = async () => {
    if (!notificationsUrl) {
      renderNotifications([]);
      return;
    }

    renderNotificationsState(
      "Завантаження",
      "Отримуємо актуальні повідомлення...",
      "Зачекайте кілька секунд...",
    );

    try {
      const response = await fetch(notificationsUrl, {
        credentials: "include",
      });
      if (!response.ok) {
        renderNotifications([]);
        return;
      }

      const payload = await response.json();
      if (!payload?.success) {
        renderNotifications([]);
        return;
      }

      renderNotifications(payload.data ?? []);
    } catch {
      renderNotifications([]);
    }
  };

  const toggleNotificationsModal = (isOpen) => {
    if (!notificationsModal) {
      return;
    }

    notificationsModal.classList.toggle("show", isOpen);
    notificationsModal.setAttribute("aria-hidden", String(!isOpen));
  };

  const openNotificationsModal = () => toggleNotificationsModal(true);

  const closeNotificationsModal = () => toggleNotificationsModal(false);

  const bindNotificationModalControls = () => {
    if (!notificationsModal || !openNotificationsModalBtn) {
      return;
    }

    openNotificationsModalBtn.addEventListener("click", () => {
      openNotificationsModal();
      void loadNotifications();
    });

    if (notificationsModalOverlay) {
      notificationsModalOverlay.addEventListener(
        "click",
        closeNotificationsModal,
      );
    }

    if (closeNotificationsModalBtn) {
      closeNotificationsModalBtn.addEventListener(
        "click",
        closeNotificationsModal,
      );
    }

    if (closeNotificationsModalFooterBtn) {
      closeNotificationsModalFooterBtn.addEventListener(
        "click",
        closeNotificationsModal,
      );
    }

    if (notificationsList) {
      notificationsList.addEventListener("click", (event) => {
        void handleNotificationActionClick(event);
      });
    }

    globalThis.addEventListener("keydown", (event) => {
      if (
        event.key === "Escape" &&
        notificationsModal.classList.contains("show")
      ) {
        closeNotificationsModal();
      }
    });
  };

  const render = () => {
    const dates = getWeekDates();
    renderRange(dates);
    renderDays(dates);
    renderTimeGrid();
    syncGridViewport();
    syncHeaderWithGrid();

    const weekStartBoundary = new Date(dates[0]);
    weekStartBoundary.setHours(0, 0, 0, 0);
    const weekEndBoundary = new Date(dates.at(-1));
    weekEndBoundary.setHours(23, 59, 59, 999);

    const detail = {
      dates,
      weekStartIso: weekStartBoundary.toISOString(),
      weekEndIso: weekEndBoundary.toISOString(),
    };
    globalThis.dashboardWeekState = detail;
    globalThis.dispatchEvent(
      new CustomEvent("dashboard:week-changed", { detail }),
    );
  };

  const setWeekByDate = (date) => {
    const nowWeekStart = getWeekStartForDate(new Date());
    const targetWeekStart = getWeekStartForDate(date);
    const diffMs = targetWeekStart.getTime() - nowWeekStart.getTime();
    weekOffset = Math.round(diffMs / (7 * 24 * 60 * 60 * 1000));
    render();
  };

  prevButton.addEventListener("click", () => {
    weekOffset -= 1;
    render();
  });

  nextButton.addEventListener("click", () => {
    weekOffset += 1;
    render();
  });

  globalThis.addEventListener("resize", () => {
    syncGridViewport();
    syncHeaderWithGrid();
  });

  globalThis.addEventListener("dashboard:day-selected", (event) => {
    const iso = event?.detail?.selectedIso;
    if (!iso) {
      return;
    }
    const parts = iso.split("-").map((x) => Number.parseInt(x, 10));
    if (
      parts.length !== 3 ||
      Number.isNaN(parts[0]) ||
      Number.isNaN(parts[1]) ||
      Number.isNaN(parts[2])
    ) {
      return;
    }
    const target = new Date(parts[0], parts[1] - 1, parts[2]);
    setWeekByDate(target);
  });

  bindNotificationModalControls();
  void loadNotifications();

  render();
})();
