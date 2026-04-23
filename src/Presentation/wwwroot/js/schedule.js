(() => {
  const scheduleModal = document.getElementById("scheduleModal");
  const openScheduleModalBtn = document.getElementById("openScheduleModal");
  const closeScheduleModalBtn = document.getElementById("closeScheduleModal");
  const cancelScheduleBtn = document.getElementById("cancelScheduleBtn");
  const scheduleForm = document.getElementById("scheduleForm");
  const scheduleDetailsModal = document.getElementById("scheduleDetailsModal");
  const closeScheduleDetailsModalBtn = document.getElementById(
    "closeScheduleDetailsModal",
  );
  const closeScheduleDetailsBtn = document.getElementById(
    "closeScheduleDetailsBtn",
  );
  const deleteScheduleOccurrenceBtn = document.getElementById(
    "deleteScheduleOccurrenceBtn",
  );
  const deleteScheduleFutureBtn = document.getElementById(
    "deleteScheduleFutureBtn",
  );
  const recurrenceTypeInput = document.getElementById("scheduleRecurrenceType");
  const weeklyDaysGroup = document.getElementById("scheduleWeeklyDaysGroup");
  const timeGrid = document.querySelector("[data-time-grid]");

  if (
    !scheduleModal ||
    !openScheduleModalBtn ||
    !scheduleForm ||
    !scheduleDetailsModal ||
    !deleteScheduleOccurrenceBtn ||
    !deleteScheduleFutureBtn ||
    !recurrenceTypeInput ||
    !weeklyDaysGroup ||
    !timeGrid
  ) {
    return;
  }

  const MESSAGES = {
    ERROR_PREFIX: "❌ Помилка: ",
    CREATE_ERROR: "Не вдалося створити розклад",
    LOAD_ERROR: "Не вдалося завантажити розклад",
    DELETE_ERROR: "Не вдалося видалити подію розкладу",
    REQUIRED_DATE: "Оберіть дату і час розкладу",
    REQUIRED_TITLE: "Назва розкладу не може бути порожньою",
  };

  const PRIORITY_LABELS = {
    1: "Низька",
    2: "Середня",
    3: "Висока",
  };

  const RECURRENCE_LABELS = {
    none: "Без повторення",
    daily: "Щодня",
    weekly: "Щотижня",
    monthly: "Щомісяця",
  };

  const WEEKDAY_LABELS = {
    0: "Нд",
    1: "Пн",
    2: "Вт",
    3: "Ср",
    4: "Чт",
    5: "Пт",
    6: "Сб",
  };

  let currentWeekStartIso = null;
  let currentWeekEndIso = null;
  let selectedOccurrence = null;

  const showError = (message) => {
    const errorDiv = document.createElement("div");
    errorDiv.className = "alert alert-error";
    errorDiv.setAttribute("role", "alert");
    errorDiv.textContent = MESSAGES.ERROR_PREFIX + message;
    document.body.insertBefore(errorDiv, document.body.firstChild);
    setTimeout(() => errorDiv.remove(), 5000);
  };

  const openModal = () => {
    scheduleModal.style.display = "flex";
    scheduleModal.setAttribute("aria-hidden", "false");
    scheduleForm.reset();
    updateRecurrenceControls();
    document.getElementById("scheduleTitle")?.focus();
  };

  const closeModal = () => {
    scheduleModal.style.display = "none";
    scheduleModal.setAttribute("aria-hidden", "true");
  };

  const openDetailsModal = (schedule) => {
    selectedOccurrence = schedule;

    const titleEl = document.getElementById("scheduleDetailTitle");
    const recurrenceEl = document.getElementById("scheduleDetailRecurrence");
    const priorityBadgeEl = document.getElementById(
      "scheduleDetailPriorityBadge",
    );
    const descriptionEl = document.getElementById("scheduleDetailDescription");
    const dateTimeEl = document.getElementById("scheduleDetailDateTime");
    const endDateEl = document.getElementById("scheduleDetailEndDate");

    if (titleEl) {
      titleEl.textContent = schedule.title || "-";
    }

    if (recurrenceEl) {
      recurrenceEl.textContent = buildRecurrenceText(schedule);
    }

    if (priorityBadgeEl) {
      const priority = Number.parseInt(String(schedule.priority || 2), 10);
      priorityBadgeEl.className = `plan-priority-badge plan-priority-${priority}`;
      priorityBadgeEl.textContent =
        PRIORITY_LABELS[priority] || schedule.priorityLabel || "Середня";
    }

    if (descriptionEl) {
      descriptionEl.textContent = schedule.description || "Опис не вказано";
    }

    if (dateTimeEl) {
      dateTimeEl.textContent = formatDateTime(schedule.occurrenceAt);
    }

    if (endDateEl) {
      endDateEl.textContent = schedule.recurrenceEndDate
        ? formatDate(schedule.recurrenceEndDate)
        : "Без обмежень";
    }

    const isRecurring =
      schedule.recurrenceType && schedule.recurrenceType !== "none";
    deleteScheduleOccurrenceBtn.textContent = isRecurring
      ? "Видалити тільки цей раз"
      : "Видалити подію";
    deleteScheduleFutureBtn.style.display = isRecurring
      ? "inline-flex"
      : "none";

    scheduleDetailsModal.style.display = "flex";
    scheduleDetailsModal.setAttribute("aria-hidden", "false");
  };

  const closeDetailsModal = () => {
    scheduleDetailsModal.style.display = "none";
    scheduleDetailsModal.setAttribute("aria-hidden", "true");
    selectedOccurrence = null;
  };

  const updateRecurrenceControls = () => {
    const isWeekly = recurrenceTypeInput.value === "weekly";
    weeklyDaysGroup.style.display = isWeekly ? "block" : "none";
  };

  const formatDateTime = (value) => {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return "-";
    }

    return date.toLocaleString("uk-UA", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const formatDate = (value) => {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleDateString("uk-UA", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
    });
  };

  const buildRecurrenceText = (schedule) => {
    const type = schedule.recurrenceType || "none";
    if (type === "none") {
      return RECURRENCE_LABELS.none;
    }

    const base = RECURRENCE_LABELS[type] || type;
    const interval = Number.parseInt(
      String(schedule.recurrenceInterval || 1),
      10,
    );
    const intervalText = interval > 1 ? `, інтервал ${interval}` : "";

    if (type !== "weekly" || !Array.isArray(schedule.weeklyDays)) {
      return `${base}${intervalText}`;
    }

    const days = schedule.weeklyDays
      .map((day) => WEEKDAY_LABELS[day])
      .filter(Boolean)
      .join(", ");

    return days ? `${base}${intervalText}: ${days}` : `${base}${intervalText}`;
  };

  const clearScheduleMarkers = () => {
    timeGrid.querySelectorAll(".schedule-slot").forEach((el) => el.remove());
  };

  const getSelectedWeekDays = () => {
    return Array.from(
      weeklyDaysGroup.querySelectorAll('input[type="checkbox"]:checked'),
    ).map((input) => Number.parseInt(input.value, 10));
  };

  const buildScheduleDateTime = () => {
    const dateInput = document.getElementById("scheduleDate");
    const timeInput = document.getElementById("scheduleTime");

    if (!dateInput?.value || !timeInput?.value) {
      return null;
    }

    const [year, month, day] = dateInput.value.split("-").map(Number);
    const [hours, minutes] = timeInput.value.split(":").map(Number);
    return new Date(year, month - 1, day, hours, minutes, 0);
  };

  const createSchedule = async (event) => {
    event.preventDefault();

    const titleInput = document.getElementById("scheduleTitle");
    const descriptionInput = document.getElementById("scheduleDescription");
    const durationInput = document.getElementById("scheduleDuration");
    const recurrenceIntervalInput = document.getElementById(
      "scheduleRecurrenceInterval",
    );
    const recurrenceEndDateInput = document.getElementById(
      "scheduleRecurrenceEndDate",
    );

    const startAt = buildScheduleDateTime();
    const title = titleInput?.value.trim() || "";

    if (!title) {
      showError(MESSAGES.REQUIRED_TITLE);
      return;
    }

    if (!startAt) {
      showError(MESSAGES.REQUIRED_DATE);
      return;
    }

    const durationMinutes = Number.parseInt(durationInput?.value || "60", 10);
    const recurrenceInterval = Number.parseInt(
      recurrenceIntervalInput?.value || "1",
      10,
    );

    const priorityInput = document.querySelector(
      'input[name="schedulePriority"]:checked',
    );
    const priority = priorityInput
      ? Number.parseInt(priorityInput.value, 10)
      : 2;

    const payload = {
      title,
      description: descriptionInput?.value.trim() || null,
      startAt: startAt.toISOString(),
      durationMinutes,
      priority,
      recurrenceType: recurrenceTypeInput.value,
      recurrenceInterval,
      weeklyDays: getSelectedWeekDays(),
      recurrenceEndDate: recurrenceEndDateInput?.value || null,
    };

    try {
      const response = await fetch("/api/schedule/create", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.CREATE_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.CREATE_ERROR);
        return;
      }

      closeModal();
      await loadSchedulesForWeek();
    } catch (error) {
      console.error("Помилка створення розкладу", error);
      showError(MESSAGES.CREATE_ERROR);
    }
  };

  const renderScheduleOnGrid = (schedule) => {
    if (!schedule?.occurrenceAt || !currentWeekStartIso || !currentWeekEndIso) {
      return;
    }

    const occurrenceDate = new Date(schedule.occurrenceAt);
    const weekStart = new Date(currentWeekStartIso);
    const weekEnd = new Date(currentWeekEndIso);
    weekStart.setHours(0, 0, 0, 0);
    weekEnd.setHours(23, 59, 59, 999);

    if (occurrenceDate < weekStart || occurrenceDate > weekEnd) {
      return;
    }

    const diffDays = Math.floor(
      (occurrenceDate - weekStart) / (1000 * 60 * 60 * 24),
    );
    if (diffDays < 0 || diffDays > 6) {
      return;
    }

    const hour = occurrenceDate.getHours();
    const slotRow = timeGrid.querySelectorAll(".dashboard-grid-row")[hour];
    if (!slotRow) {
      return;
    }

    const cell = slotRow.querySelectorAll(".dashboard-slot-cell")[diffDays];
    if (!cell) {
      return;
    }

    const badge = document.createElement("div");
    badge.className = `schedule-slot schedule-priority-${schedule.priority || 2}`;
    badge.textContent = `🔁 ${schedule.title}`;
    const priority =
      PRIORITY_LABELS[schedule.priority] || schedule.priorityLabel || "";
    const descriptionSuffix = schedule.description
      ? ` • ${schedule.description}`
      : "";
    badge.title = `${priority}${descriptionSuffix}`;
    badge.setAttribute("role", "button");
    badge.setAttribute("tabindex", "0");
    badge.addEventListener("click", () => openDetailsModal(schedule));
    badge.addEventListener("keydown", (event) => {
      if (event.key === "Enter" || event.key === " ") {
        event.preventDefault();
        openDetailsModal(schedule);
      }
    });

    cell.appendChild(badge);
  };

  const deleteSelectedOccurrence = async (deleteMode) => {
    if (!selectedOccurrence?.scheduleId || !selectedOccurrence?.occurrenceAt) {
      return;
    }

    const confirmationText =
      deleteMode === "future"
        ? "Видалити цю подію і всі наступні повтори?"
        : "Видалити тільки цю подію?";

    if (!globalThis.confirm(confirmationText)) {
      return;
    }

    try {
      const response = await fetch("/api/schedule/delete", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          scheduleId: selectedOccurrence.scheduleId,
          occurrenceAt: selectedOccurrence.occurrenceAt,
          deleteMode,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.DELETE_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.DELETE_ERROR);
        return;
      }

      closeDetailsModal();
      await loadSchedulesForWeek();
    } catch (error) {
      console.error("Помилка видалення події розкладу", error);
      showError(MESSAGES.DELETE_ERROR);
    }
  };

  const loadSchedulesForWeek = async () => {
    if (!currentWeekStartIso || !currentWeekEndIso) {
      return;
    }

    clearScheduleMarkers();

    const query = new URLSearchParams({
      weekStart: currentWeekStartIso,
      weekEnd: currentWeekEndIso,
    });

    try {
      const response = await fetch(`/api/schedule/list?${query.toString()}`, {
        method: "GET",
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.LOAD_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success || !Array.isArray(data.data)) {
        showError(data.message || MESSAGES.LOAD_ERROR);
        return;
      }

      data.data.forEach(renderScheduleOnGrid);
    } catch (error) {
      console.error("Помилка завантаження розкладу", error);
      showError(MESSAGES.LOAD_ERROR);
    }
  };

  const handleWeekChanged = async (event) => {
    currentWeekStartIso = event.detail?.weekStartIso || null;
    currentWeekEndIso = event.detail?.weekEndIso || null;
    await loadSchedulesForWeek();
  };

  openScheduleModalBtn.addEventListener("click", openModal);
  closeScheduleModalBtn?.addEventListener("click", closeModal);
  cancelScheduleBtn?.addEventListener("click", closeModal);
  closeScheduleDetailsModalBtn?.addEventListener("click", closeDetailsModal);
  closeScheduleDetailsBtn?.addEventListener("click", closeDetailsModal);
  deleteScheduleOccurrenceBtn.addEventListener("click", () =>
    deleteSelectedOccurrence("single"),
  );
  deleteScheduleFutureBtn.addEventListener("click", () =>
    deleteSelectedOccurrence("future"),
  );

  scheduleModal.addEventListener("click", (event) => {
    if (event.target === scheduleModal) {
      closeModal();
    }
  });

  scheduleDetailsModal.addEventListener("click", (event) => {
    if (event.target === scheduleDetailsModal) {
      closeDetailsModal();
    }
  });

  recurrenceTypeInput.addEventListener("change", updateRecurrenceControls);
  scheduleForm.addEventListener("submit", createSchedule);

  globalThis.addEventListener("dashboard:week-changed", handleWeekChanged);

  if (globalThis.dashboardWeekState) {
    currentWeekStartIso = globalThis.dashboardWeekState.weekStartIso;
    currentWeekEndIso = globalThis.dashboardWeekState.weekEndIso;
    loadSchedulesForWeek();
  }
})();
