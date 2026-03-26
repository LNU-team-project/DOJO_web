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

  const getWeekStart = (date) => {
    const base = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const day = base.getDay();
    const offsetToMonday = day === 0 ? -6 : 1 - day;
    base.setDate(base.getDate() + offsetToMonday + weekOffset * 7);
    return base;
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

  render();
})();
