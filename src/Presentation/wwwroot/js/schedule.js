(() => {
  const scheduleModal = document.getElementById("scheduleModal");
  const openScheduleModalBtn = document.getElementById("openScheduleModal");
  const closeScheduleModalBtn = document.getElementById("closeScheduleModal");
  const cancelScheduleBtn = document.getElementById("cancelScheduleBtn");
  const scheduleForm = document.getElementById("scheduleForm");
  const recurrenceTypeInput = document.getElementById("scheduleRecurrenceType");
  const weeklyDaysGroup = document.getElementById("scheduleWeeklyDaysGroup");
  const timeGrid = document.querySelector("[data-time-grid]");

  if (
    !scheduleModal ||
    !openScheduleModalBtn ||
    !scheduleForm ||
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
    REQUIRED_DATE: "Оберіть дату і час розкладу",
    REQUIRED_TITLE: "Назва розкладу не може бути порожньою",
  };

  const PRIORITY_LABELS = {
    1: "Низька",
    2: "Середня",
    3: "Висока",
  };

  let currentWeekStartIso = null;
  let currentWeekEndIso = null;

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

  const updateRecurrenceControls = () => {
    const isWeekly = recurrenceTypeInput.value === "weekly";
    weeklyDaysGroup.style.display = isWeekly ? "block" : "none";
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
      'input[name="planPriority"]:checked',
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
    badge.title = `${priority}${schedule.description ? ` • ${schedule.description}` : ""}`;

    cell.appendChild(badge);
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

  scheduleModal.addEventListener("click", (event) => {
    if (event.target === scheduleModal) {
      closeModal();
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
