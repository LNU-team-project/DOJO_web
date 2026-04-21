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
  const notificationsModal = document.getElementById("notificationsModal");
  const notificationsModalOverlay = document.getElementById("notificationsModalOverlay");
  const openNotificationsModalBtn = document.getElementById("openNotificationsModal");
  const closeNotificationsModalBtn = document.getElementById("closeNotificationsModal");
  const closeNotificationsModalFooterBtn = document.getElementById("closeNotificationsModalFooter");
  const openNotificationSettingsBtn = document.getElementById("openNotificationSettingsBtn");
  const notificationsSettingsModal = document.getElementById("notificationsSettingsModal");
  const notificationsSettingsModalOverlay = document.getElementById("notificationsSettingsModalOverlay");
  const closeNotificationsSettingsModalBtn = document.getElementById("closeNotificationsSettingsModal");
  const closeNotificationsSettingsModalFooterBtn = document.getElementById("closeNotificationsSettingsModalFooter");

  if (!rangeLabel || !daysHeader || !timeGrid || !prevButton || !nextButton || !board) {
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

  const toggleNotificationsModal = (isOpen) => {
    if (!notificationsModal) {
      return;
    }

    notificationsModal.classList.toggle("show", isOpen);
    notificationsModal.setAttribute("aria-hidden", String(!isOpen));
  };

  const openNotificationsModal = () => toggleNotificationsModal(true);

  const closeNotificationsModal = () => toggleNotificationsModal(false);

  const toggleNotificationsSettingsModal = (isOpen) => {
    if (!notificationsSettingsModal) {
      return;
    }

    notificationsSettingsModal.classList.toggle("show", isOpen);
    notificationsSettingsModal.setAttribute("aria-hidden", String(!isOpen));
  };

  const openNotificationsSettingsModal = () => toggleNotificationsSettingsModal(true);

  const closeNotificationsSettingsModal = () => toggleNotificationsSettingsModal(false);

  const bindNotificationModalControls = () => {
    if (!notificationsModal || !openNotificationsModalBtn) {
      return;
    }

    openNotificationsModalBtn.addEventListener("click", openNotificationsModal);

    if (notificationsModalOverlay) {
      notificationsModalOverlay.addEventListener("click", closeNotificationsModal);
    }

    if (closeNotificationsModalBtn) {
      closeNotificationsModalBtn.addEventListener("click", closeNotificationsModal);
    }

    if (closeNotificationsModalFooterBtn) {
      closeNotificationsModalFooterBtn.addEventListener("click", closeNotificationsModal);
    }

    if (openNotificationSettingsBtn) {
      openNotificationSettingsBtn.addEventListener("click", openNotificationsSettingsModal);
    }

    if (notificationsSettingsModalOverlay) {
      notificationsSettingsModalOverlay.addEventListener("click", closeNotificationsSettingsModal);
    }

    if (closeNotificationsSettingsModalBtn) {
      closeNotificationsSettingsModalBtn.addEventListener("click", closeNotificationsSettingsModal);
    }

    if (closeNotificationsSettingsModalFooterBtn) {
      closeNotificationsSettingsModalFooterBtn.addEventListener("click", closeNotificationsSettingsModal);
    }

    globalThis.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && notificationsSettingsModal?.classList.contains("show")) {
        closeNotificationsSettingsModal();
        return;
      }

      if (event.key === "Escape" && notificationsModal.classList.contains("show")) {
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
    const detail = {
      dates,
      weekStartIso: dates[0].toISOString(),
      weekEndIso: dates.at(-1).toISOString(),
    };
    globalThis.dashboardWeekState = detail;
    globalThis.dispatchEvent(new CustomEvent("dashboard:week-changed", { detail }));
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
    if (parts.length !== 3 || Number.isNaN(parts[0]) || Number.isNaN(parts[1]) || Number.isNaN(parts[2])) {
      return;
    }
    const target = new Date(parts[0], parts[1] - 1, parts[2]);
    setWeekByDate(target);
  });

  bindNotificationModalControls();

  render();
})();
