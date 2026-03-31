(() => {
  const calendarRoot = document.querySelector("[data-mini-calendar]");
  if (!calendarRoot) {
    return;
  }

  const titleEl = calendarRoot.querySelector("[data-cal-title]");
  const gridEl = calendarRoot.querySelector("[data-cal-grid]");
  const weekdaysEl = calendarRoot.querySelector(".mini-calendar-weekdays");
  const prevBtn = calendarRoot.querySelector('[data-cal-nav="prev"]');
  const nextBtn = calendarRoot.querySelector('[data-cal-nav="next"]');

  if (!titleEl || !gridEl || !weekdaysEl || !prevBtn || !nextBtn) {
    return;
  }

  const locale = "uk-UA";
  const weekdayLabels = ["ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ", "НД"];
  const toIsoDate = (date) => {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, "0");
    const d = String(date.getDate()).padStart(2, "0");
    return `${y}-${m}-${d}`;
  };

  const today = new Date();
  let current = new Date(today.getFullYear(), today.getMonth(), 1);
  let selectedIso = toIsoDate(
    new Date(today.getFullYear(), today.getMonth(), today.getDate()),
  );
  const marks = new Set();
  let currentRange = { from: null, to: null };

  const formatTitle = (date) => {
    const raw = date.toLocaleDateString(locale, {
      month: "long",
      year: "numeric",
    });
    const titleTokens = raw.trim().split(" ");
    const lastToken = titleTokens.at(-1)?.toLowerCase();
    if (lastToken === "р." || lastToken === "р") {
      titleTokens.pop();
    }
    const cleaned = titleTokens.join(" ").trim();
    return cleaned.charAt(0).toUpperCase() + cleaned.slice(1);
  };

  const getMonthMatrix = (date) => {
    const start = new Date(date.getFullYear(), date.getMonth(), 1);
    const end = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    const startDay = (start.getDay() + 6) % 7; // Monday=0
    const totalDays = end.getDate();

    const cells = [];

    // placeholders before month start
    for (let i = 0; i < startDay; i += 1) {
      cells.push({ date: null, inMonth: false });
    }

    // current month days
    for (let i = 1; i <= totalDays; i += 1) {
      const d = new Date(date.getFullYear(), date.getMonth(), i);
      cells.push({ date: d, inMonth: true });
    }

    // pad to complete weeks (up to 6 rows max)
    const remainder = cells.length % 7;
    const trailing = remainder === 0 ? 0 : 7 - remainder;
    for (let i = 0; i < trailing; i += 1) {
      cells.push({ date: null, inMonth: false });
    }

    return cells;
  };

  const emitSelection = (date) => {
    const iso = toIsoDate(date);
    selectedIso = iso;
    globalThis.dashboardSelectedDate = iso;
    globalThis.dispatchEvent(
      new CustomEvent("dashboard:day-selected", {
        detail: { selectedIso: iso, date },
      }),
    );
  };

  const renderWeekdays = () => {
    weekdaysEl.innerHTML = "";
    weekdayLabels.forEach((label) => {
      const cell = document.createElement("div");
      cell.textContent = label;
      weekdaysEl.appendChild(cell);
    });
  };

  const renderGrid = () => {
    gridEl.innerHTML = "";
    titleEl.textContent = formatTitle(current);

    const cells = getMonthMatrix(current);
    cells.forEach(({ date, inMonth }) => {
      const dayBtn = document.createElement("button");
      dayBtn.type = "button";
      dayBtn.className = "mini-cal-day";
      dayBtn.setAttribute("role", "gridcell");

      if (!inMonth || !date) {
        dayBtn.classList.add("is-empty");
        dayBtn.disabled = true;
        gridEl.appendChild(dayBtn);
        return;
      }

      const iso = toIsoDate(date);
      const isToday = iso === toIsoDate(today);
      if (isToday) {
        dayBtn.classList.add("is-today");
      }
      if (iso === selectedIso) {
        dayBtn.classList.add("is-selected");
      }

      const number = document.createElement("span");
      number.className = "day-number";
      number.textContent = String(date.getDate());
      dayBtn.appendChild(number);

      if (marks.has(iso) && iso !== selectedIso) {
        const marker = document.createElement("span");
        marker.className = "day-marker";
        marker.setAttribute("aria-hidden", "true");
        dayBtn.appendChild(marker);
      }

      dayBtn.addEventListener("click", () => {
        emitSelection(date);
        renderGrid();
      });

      gridEl.appendChild(dayBtn);
    });
  };

  const navigate = (dir) => {
    current = new Date(current.getFullYear(), current.getMonth() + dir, 1);
    renderGrid();
  };

  const fetchMarks = async () => {
    try {
      const start = new Date(current.getFullYear(), current.getMonth(), 1);
      const end = new Date(current.getFullYear(), current.getMonth() + 1, 0);
      const startIso = toIsoDate(start);
      const endIso = toIsoDate(end);
      currentRange = { from: startIso, to: endIso };

      const response = await fetch(
        `/api/calendar/marks?from=${startIso}&to=${endIso}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
        },
      );

      if (!response.ok) {
        return;
      }

      const payload = await response.json();
      const markDates = payload?.data;
      if (!Array.isArray(markDates)) {
        return;
      }

      marks.clear();
      markDates.forEach((iso) => marks.add(iso));
      renderGrid();
    } catch (err) {
      console.error("Помилка завантаження позначок календаря", err);
    }
  };

  const addMarkIfInRange = (isoDate) => {
    if (!currentRange.from || !currentRange.to) return;
    if (isoDate >= currentRange.from && isoDate <= currentRange.to) {
      marks.add(isoDate);
      renderGrid();
    }
  };

  globalThis.addEventListener("dashboard:plan-created", (event) => {
    const iso = event?.detail?.scheduledIso;
    if (!iso) return;
    addMarkIfInRange(iso);
  });

  prevBtn.addEventListener("click", () => {
    navigate(-1);
    fetchMarks();
  });

  nextBtn.addEventListener("click", () => {
    navigate(1);
    fetchMarks();
  });

  renderWeekdays();
  emitSelection(new Date(today));
  renderGrid();
  fetchMarks();
})();
