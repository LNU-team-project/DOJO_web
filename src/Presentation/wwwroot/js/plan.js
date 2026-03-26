(() => {
  const planModal = document.getElementById("planModal");
  const openPlanModalBtn = document.getElementById("openPlanModal");
  const closePlanModalBtn = document.getElementById("closePlanModal");
  const cancelPlanBtn = document.getElementById("cancelPlanBtn");
  const planForm = document.getElementById("planForm");
  const timeGrid = document.querySelector("[data-time-grid]");
  const rangeLabel = document.querySelector("[data-range-label]");
  const planListContainer = document.getElementById("allPlanItems");

  if (!planModal || !openPlanModalBtn || !planForm || !timeGrid || !rangeLabel) {
    return;
  }

  const MESSAGES = {
    ERROR_PREFIX: "❌ Помилка: ",
    SUCCESS_PREFIX: "✅ ",
    INVALID_TITLE: "Назва плану не може бути порожньою",
    INVALID_DATE: "Оберіть дату та час для плану",
    CREATE_ERROR: "Не вдалося створити план",
    LOAD_ERROR: "Не вдалося завантажити плани",
    INVALID_FORMAT: "Невірний формат відповіді",
  };

  const PRIORITY_LABELS = {
    1: "Низька",
    2: "Середня",
    3: "Висока",
  };

  let currentWeekStartIso = null;
  let currentWeekEndIso = null;

  const applyInitialWeekState = () => {
    if (window.dashboardWeekState) {
      currentWeekStartIso = window.dashboardWeekState.weekStartIso;
      currentWeekEndIso = window.dashboardWeekState.weekEndIso;
    }
  };

  const openModal = () => {
    planModal.style.display = "flex";
    planModal.setAttribute("aria-hidden", "false");
    planForm.reset();
    const titleInput = document.getElementById("planTitle");
    if (titleInput) titleInput.focus();
  };

  const closeModal = () => {
    planModal.style.display = "none";
    planModal.setAttribute("aria-hidden", "true");
    planForm.reset();
  };

  const showError = (message) => {
    console.error(MESSAGES.ERROR_PREFIX + message);
    const errorDiv = document.createElement("div");
    errorDiv.className = "alert alert-error";
    errorDiv.setAttribute("role", "alert");
    errorDiv.textContent = MESSAGES.ERROR_PREFIX + message;
    const container = document.querySelector("body");
    if (container) {
      container.insertBefore(errorDiv, container.firstChild);
      setTimeout(() => errorDiv.remove(), 5000);
    }
  };

  const showSuccess = (message) => {
    console.log(MESSAGES.SUCCESS_PREFIX + message);
    const successDiv = document.createElement("div");
    successDiv.className = "alert alert-success";
    successDiv.setAttribute("role", "status");
    successDiv.textContent = MESSAGES.SUCCESS_PREFIX + message;
    const container = document.querySelector("body");
    if (container) {
      container.insertBefore(successDiv, container.firstChild);
      setTimeout(() => successDiv.remove(), 3000);
    }
  };

  const buildDateFromInputs = () => {
    const dateInput = document.getElementById("planDate");
    const timeInput = document.getElementById("planTime");
    if (!dateInput || !timeInput || !dateInput.value || !timeInput.value) return null;
    const [hours, minutes] = timeInput.value.split(":").map(Number);
    const dateParts = dateInput.value.split("-").map(Number);
    if (dateParts.length !== 3) return null;
    const [year, month, day] = dateParts;
    return new Date(year, month - 1, day, hours, minutes, 0);
  };

  const handleFormSubmit = async (event) => {
    event.preventDefault();
    const titleInput = document.getElementById("planTitle");
    const descriptionInput = document.getElementById("planDescription");
    const priorityInput = document.querySelector('input[name="planPriority"]:checked');
    const scheduledDate = buildDateFromInputs();

    if (!titleInput || !descriptionInput || !priorityInput) {
      showError(MESSAGES.INVALID_FORMAT);
      return;
    }

    const title = titleInput.value.trim();
    const description = descriptionInput.value.trim();
    const priority = parseInt(priorityInput.value, 10);

    if (!title) {
      showError(MESSAGES.INVALID_TITLE);
      return;
    }

    if (!scheduledDate) {
      showError(MESSAGES.INVALID_DATE);
      return;
    }

    try {
      const response = await fetch("/api/plan/create", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          title,
          description: description || null,
          scheduledAt: scheduledDate.toISOString(),
          priority,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || MESSAGES.CREATE_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success || !data.data) {
        showError(data.message || MESSAGES.CREATE_ERROR);
        return;
      }

      showSuccess("План створено");
      closeModal();
      await loadPlans();
    } catch (error) {
      console.error("Помилка при створенні плану", error);
      showError(MESSAGES.CREATE_ERROR);
    }
  };

  const loadPlans = async () => {
    try {
      const response = await fetch("/api/plan/list", { method: "GET" });
      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || MESSAGES.LOAD_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success || !data.data) {
        showError(MESSAGES.INVALID_FORMAT);
        return;
      }

      renderPlans(data.data);
    } catch (error) {
      console.error("Помилка при завантаженні планів", error);
      showError(MESSAGES.LOAD_ERROR);
    }
  };

  const renderPlans = (planList) => {
    const plans = planList || {};
    clearPlanMarkers();
    const allPlans = [...(plans.incompletePlans || []), ...(plans.completedPlans || [])];
    renderPlanList(plans);
    allPlans.forEach(renderPlanOnGrid);
  };

  const renderPlanList = (planList) => {
    if (!planListContainer) return;
    planListContainer.innerHTML = "";
    const all = [...(planList.incompletePlans || []), ...(planList.completedPlans || [])];
    if (all.length === 0) {
      planListContainer.innerHTML = `<p class="todo-empty-message">Немає планів</p>`;
      return;
    }

    all.forEach((plan) => {
      const item = document.createElement("div");
      item.className = `todo-item ${plan.isCompleted ? "completed" : ""}`;
      item.dataset.planId = plan.id;

      const checkbox = document.createElement("input");
      checkbox.type = "checkbox";
      checkbox.className = "todo-item-checkbox";
      checkbox.checked = plan.isCompleted;
      checkbox.setAttribute("aria-label", plan.isCompleted ? "Позначити як невиконаний" : "Позначити як виконаний");

      const contentDiv = document.createElement("div");
      contentDiv.className = "todo-item-content";

      const titleSpan = document.createElement("div");
      titleSpan.className = "todo-item-title";
      titleSpan.textContent = plan.title;
      contentDiv.appendChild(titleSpan);

      if (plan.description) {
        const descriptionSpan = document.createElement("div");
        descriptionSpan.className = "todo-item-description";
        descriptionSpan.textContent = plan.description;
        contentDiv.appendChild(descriptionSpan);
      }

      const metaDiv = document.createElement("div");
      metaDiv.className = "todo-item-meta";

      const prioritySpan = document.createElement("span");
      prioritySpan.className = `todo-priority priority-${plan.priority}`;
      prioritySpan.textContent = PRIORITY_LABELS[plan.priority] || plan.priorityLabel || "";
      metaDiv.appendChild(prioritySpan);

      if (plan.scheduledAt) {
        const date = new Date(plan.scheduledAt);
        const timeLabel = document.createElement("span");
        timeLabel.className = "todo-priority";
        timeLabel.textContent = date.toLocaleString("uk-UA", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" });
        metaDiv.appendChild(timeLabel);
      }

      contentDiv.appendChild(metaDiv);

      const deleteBtn = document.createElement("button");
      deleteBtn.type = "button";
      deleteBtn.className = "todo-item-delete";
      deleteBtn.setAttribute("aria-label", "Видалити план");
      deleteBtn.innerHTML = "🗑️";

      item.appendChild(checkbox);
      item.appendChild(contentDiv);
      item.appendChild(deleteBtn);

      checkbox.addEventListener("change", () => handlePlanStatusChange(plan.id, checkbox.checked));
      deleteBtn.addEventListener("click", () => handlePlanDelete(plan.id));

      planListContainer.appendChild(item);
    });
  };

  const handlePlanStatusChange = async (planId, isCompleted) => {
    const endpoint = isCompleted ? `/api/plan/complete/${planId}` : `/api/plan/incomplete/${planId}`;
    try {
      const response = await fetch(endpoint, { method: "PUT" });
      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || "Помилка оновлення плану");
        return;
      }
      showSuccess(isCompleted ? "План позначено виконаним" : "План повернуто до активних");
      await loadPlans();
    } catch (error) {
      console.error("Помилка при оновленні плану", error);
      showError("Не вдалося оновити план");
    }
  };

  const handlePlanDelete = async (planId) => {
    const confirmDelete = confirm("Ви впевнені, що хочете видалити цей план?");
    if (!confirmDelete) return;
    try {
      const response = await fetch(`/api/plan/delete/${planId}`, { method: "DELETE" });
      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || "Не вдалося видалити план");
        return;
      }
      showSuccess("План видалено");
      await loadPlans();
    } catch (error) {
      console.error("Помилка при видаленні плану", error);
      showError("Не вдалося видалити план");
    }
  };

  const clearPlanMarkers = () => {
    timeGrid.querySelectorAll(".plan-slot").forEach((el) => el.remove());
  };

  const renderPlanOnGrid = (plan) => {
    if (!plan.scheduledAt || !currentWeekStartIso || !currentWeekEndIso) return;
    const scheduledDate = new Date(plan.scheduledAt);
    const weekStart = new Date(currentWeekStartIso);
    const weekEnd = new Date(currentWeekEndIso);
    weekStart.setHours(0, 0, 0, 0);
    weekEnd.setHours(23, 59, 59, 999);
    if (scheduledDate < weekStart || scheduledDate > weekEnd) return;

    const diffDays = Math.floor((scheduledDate - weekStart) / (1000 * 60 * 60 * 24));
    if (diffDays < 0 || diffDays > 6) return;

    const hour = scheduledDate.getHours();
    const slotRow = timeGrid.querySelectorAll(".dashboard-grid-row")[hour];
    if (!slotRow) return;
    const cell = slotRow.querySelectorAll(".dashboard-slot-cell")[diffDays];
    if (!cell) return;

    const badge = document.createElement("div");
    const priorityClass = plan.priority ? plan.priority : 2;
    badge.className = `plan-slot plan-priority-${priorityClass} ${plan.isCompleted ? "plan-completed" : ""}`;
    badge.textContent = plan.title;
    badge.title = plan.description || "";
    if (plan.isCompleted) {
      badge.style.opacity = "0.6";
      badge.style.textDecoration = "line-through";
    }
    cell.appendChild(badge);
  };

  const handleWeekChanged = (event) => {
    currentWeekStartIso = event.detail?.weekStartIso || null;
    currentWeekEndIso = event.detail?.weekEndIso || null;
    clearPlanMarkers();
    loadPlans();
  };

  openPlanModalBtn.addEventListener("click", openModal);
  closePlanModalBtn.addEventListener("click", closeModal);
  cancelPlanBtn.addEventListener("click", closeModal);
  planModal.addEventListener("click", (event) => {
    if (event.target === planModal) closeModal();
  });
  planForm.addEventListener("submit", handleFormSubmit);

  window.addEventListener("dashboard:week-changed", handleWeekChanged);

  applyInitialWeekState();
  loadPlans();
})();
