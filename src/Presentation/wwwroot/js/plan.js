(() => {
  const __log = globalThis.AppLogger ?? {
    log: () => {},
    warn: () => {},
    error: () => {},
  };
  const planModal = document.getElementById("planModal");
  const openPlanModalBtn = document.getElementById("openPlanModal");
  const closePlanModalBtn = document.getElementById("closePlanModal");
  const cancelPlanBtn = document.getElementById("cancelPlanBtn");
  const planForm = document.getElementById("planForm");
  const timeGrid = document.querySelector("[data-time-grid]");
  const rangeLabel = document.querySelector("[data-range-label]");
  const planListContainer = document.getElementById("allPlanItems");
  const planCreateAttachmentsInput = document.getElementById(
    "planCreateAttachments",
  );
  const planAttachmentInput = document.getElementById("planAttachmentInput");
  const uploadPlanAttachmentBtn = document.getElementById(
    "uploadPlanAttachmentBtn",
  );
  const planAttachmentsList = document.getElementById("planAttachmentsList");
  const detailSubTasksSummary = document.getElementById(
    "detailSubTasksSummary",
  );
  const editPlanSubTasksList = document.getElementById("editPlanSubTasksList");
  const newPlanSubTaskTitle = document.getElementById("newPlanSubTaskTitle");
  const addPlanSubTaskBtn = document.getElementById("addPlanSubTaskBtn");

  if (
    !planModal ||
    !openPlanModalBtn ||
    !planForm ||
    !timeGrid ||
    !rangeLabel
  ) {
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
    ATTACHMENTS_LOAD_ERROR: "Не вдалося завантажити вкладення",
    ATTACHMENT_UPLOAD_ERROR: "Не вдалося прикріпити файл",
    ATTACHMENT_DELETE_ERROR: "Не вдалося видалити вкладення",
    PLAN_ATTACHMENTS_UPLOAD_ERROR:
      "План створено, але частину файлів не вдалося прикріпити",
    SUBTASK_LOAD_ERROR: "Не вдалося завантажити підзадачі",
    SUBTASK_CREATE_ERROR: "Не вдалося додати підзадачу",
    SUBTASK_UPDATE_ERROR: "Не вдалося оновити підзадачу",
    SUBTASK_DELETE_ERROR: "Не вдалося видалити підзадачу",
    SUBTASK_STATUS_ERROR: "Не вдалося оновити статус підзадачі",
  };

  const PRIORITY_LABELS = {
    1: "Низька",
    2: "Середня",
    3: "Висока",
  };

  let currentWeekStartIso = null;
  let currentWeekEndIso = null;
  let currentPlanSubTasks = [];

  const applyInitialWeekState = () => {
    if (globalThis.dashboardWeekState) {
      currentWeekStartIso = globalThis.dashboardWeekState.weekStartIso;
      currentWeekEndIso = globalThis.dashboardWeekState.weekEndIso;
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
    __log.error(MESSAGES.ERROR_PREFIX + message);
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
    __log.log(MESSAGES.SUCCESS_PREFIX + message);
  };

  const refreshHeroStatus = async () => {
    await globalThis.HeroModule?.refresh?.();
  };

  const updateHeroStatus = async (payload) => {
    if (!payload?.success || !payload?.data) {
      await refreshHeroStatus();
      return;
    }

    const heroData = payload.data;
    const heroModule = globalThis.HeroModule;

    if (heroData.hasLeveledUp) {
      heroModule?.showLevelUp?.(heroData);
      return;
    }

    if (heroModule?.applyStatus) {
      heroModule.applyStatus(heroData);
      return;
    }

    await refreshHeroStatus();
  };

  const processPlanStatusResponse = async (response) => {
    try {
      const payload = await response.json();
      await updateHeroStatus(payload);
    } catch (error) {
      __log.warn("Не вдалося обробити відповідь героя", error);
      await refreshHeroStatus();
    }
  };

  const refreshStatistics = async () => {
    const maxAttempts = 10;
    for (let attempt = 0; attempt < maxAttempts; attempt += 1) {
      const refreshFn = globalThis.StatisticsModule?.refreshStatistics;
      if (typeof refreshFn === "function") {
        await refreshFn();
        return;
      }

      await new Promise((resolve) => {
        setTimeout(resolve, 150);
      });
    }
  };

  const buildDateFromInputs = () => {
    const dateInput = document.getElementById("planDate");
    const timeInput = document.getElementById("planTime");
    if (!dateInput || !timeInput || !dateInput.value || !timeInput.value)
      return null;
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
    const priorityInput = document.querySelector(
      'input[name="planPriority"]:checked',
    );
    const scheduledDate = buildDateFromInputs();

    if (!titleInput || !descriptionInput || !priorityInput) {
      showError(MESSAGES.INVALID_FORMAT);
      return;
    }

    const title = titleInput.value.trim();
    const description = descriptionInput.value.trim();
    const priority = Number.parseInt(priorityInput.value, 10);

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

      const createdPlanId = data.data.id;
      const selectedFiles = Array.from(planCreateAttachmentsInput?.files || []);

      if (createdPlanId && selectedFiles.length > 0) {
        const uploadResult = await uploadAttachmentsForPlan(
          createdPlanId,
          selectedFiles,
        );

        if (uploadResult.failedCount > 0) {
          showError(
            `${MESSAGES.PLAN_ATTACHMENTS_UPLOAD_ERROR}: ${uploadResult.failedCount}`,
          );
        } else {
          showSuccess(`Прикріплено файлів: ${uploadResult.uploadedCount}`);
        }
      }

      showSuccess("План створено");
      globalThis.dispatchEvent(
        new CustomEvent("dashboard:plan-created", {
          detail: { scheduledIso: scheduledDate.toISOString().split("T")[0] },
        }),
      );
      closeModal();
      await loadPlans();

      // Оновлюємо статистику після створення плану
      await refreshStatistics();
    } catch (error) {
      __log.error("Помилка при створенні плану", error);
      showError(MESSAGES.CREATE_ERROR);
    }
  };

  const uploadAttachmentsForPlan = async (planId, files) => {
    let uploadedCount = 0;
    let failedCount = 0;

    for (const file of files) {
      try {
        const formData = new FormData();
        formData.append("file", file);

        const response = await fetch(`/api/plan/${planId}/attachments`, {
          method: "POST",
          body: formData,
        });

        if (!response.ok) {
          failedCount += 1;
          continue;
        }

        const data = await response.json();
        if (data.success) {
          uploadedCount += 1;
        } else {
          failedCount += 1;
        }
      } catch (error) {
        __log.error("Помилка при прикріпленні файлу під час створення", error);
        failedCount += 1;
      }
    }

    return { uploadedCount, failedCount };
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
      __log.error("Помилка при завантаженні планів", error);
      showError(MESSAGES.LOAD_ERROR);
    }
  };

  const renderPlans = (planList) => {
    const plans = planList || {};
    clearPlanMarkers();
    const allPlans = [
      ...(plans.incompletePlans || []),
      ...(plans.completedPlans || []),
    ];
    renderPlanList(plans);
    allPlans.forEach(renderPlanOnGrid);
  };

  const renderPlanList = (planList) => {
    if (!planListContainer) return;
    planListContainer.innerHTML = "";
    const all = [
      ...(planList.incompletePlans || []),
      ...(planList.completedPlans || []),
    ];
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
      checkbox.setAttribute(
        "aria-label",
        plan.isCompleted
          ? "Позначити як невиконаний"
          : "Позначити як виконаний",
      );

      const contentDiv = document.createElement("div");
      contentDiv.className = "todo-item-content";

      const titleSpan = document.createElement("div");
      titleSpan.className = "todo-item-title";
      titleSpan.textContent = plan.title;

      if (plan.hasSubTasks) {
        const subtaskMarker = document.createElement("span");
        subtaskMarker.className = "todo-item-subtask-marker";
        subtaskMarker.textContent = `⭐ ${plan.subTaskCount || 0}`;
        subtaskMarker.title = "Цей план містить підзадачі";
        titleSpan.appendChild(subtaskMarker);
      }

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
      prioritySpan.textContent =
        PRIORITY_LABELS[plan.priority] || plan.priorityLabel || "";
      metaDiv.appendChild(prioritySpan);

      if (plan.scheduledAt) {
        const date = new Date(plan.scheduledAt);
        const timeLabel = document.createElement("span");
        timeLabel.className = "todo-priority";
        timeLabel.textContent = date.toLocaleString("uk-UA", {
          day: "2-digit",
          month: "short",
          hour: "2-digit",
          minute: "2-digit",
        });
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

      checkbox.addEventListener("change", () =>
        handlePlanStatusChange(plan.id, checkbox.checked),
      );
      deleteBtn.addEventListener("click", () => handlePlanDelete(plan.id));

      planListContainer.appendChild(item);
    });
  };

  const handlePlanStatusChange = async (planId, isCompleted) => {
    const endpoint = isCompleted
      ? `/api/plan/complete/${planId}`
      : `/api/plan/incomplete/${planId}`;
    try {
      const response = await fetch(endpoint, { method: "PUT" });
      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || "Помилка оновлення плану");
        return;
      }
      showSuccess(
        isCompleted ? "План позначено виконаним" : "План повернуто до активних",
      );

      await processPlanStatusResponse(response);

      // Завантажуємо плани та оновлюємо статистику в будь-якому випадку
      try {
        // Завантажуємо плани та оновлюємо статистику в будь-якому випадку
        await loadPlans();

        await refreshStatistics();
      } catch (error) {
        __log.warn(
          "Не вдалося оновити пов'язані віджети після зміни плану",
          error,
        );
      }
    } catch (error) {
      __log.error("Помилка при оновленні плану", error);
      showError("Не вдалося оновити план");
    }
  };

  const handlePlanDelete = async (planId) => {
    const confirmDelete = globalThis.confirm(
      "Ви впевнені, що хочете видалити цей план?",
    );
    if (!confirmDelete) return;
    try {
      const response = await fetch(`/api/plan/delete/${planId}`, {
        method: "DELETE",
      });
      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || "Не вдалося видалити план");
        return;
      }
      showSuccess("План видалено");
      await loadPlans();

      // Оновлюємо статистику після видалення плану
      await refreshStatistics();
    } catch (error) {
      __log.error("Помилка при видаленні плану", error);
      showError("Не вдалося видалити план");
    }
  };

  const clearPlanMarkers = () => {
    timeGrid.querySelectorAll(".plan-slot").forEach((el) => el.remove());
  };

  const openPlanDetailsModal = () => {
    const modal = document.getElementById("planDetailsModal");
    if (!modal) return;
    modal.style.display = "flex";
    modal.setAttribute("aria-hidden", "false");
  };

  const closePlanDetailsModal = () => {
    const modal = document.getElementById("planDetailsModal");
    if (!modal) return;
    modal.style.display = "none";
    modal.setAttribute("aria-hidden", "true");
    delete modal.dataset.currentPlanId;
    if (planAttachmentsList) {
      planAttachmentsList.innerHTML = "";
    }
    if (editPlanSubTasksList) {
      editPlanSubTasksList.innerHTML = "";
    }
    if (planAttachmentInput) {
      planAttachmentInput.value = "";
    }
    if (newPlanSubTaskTitle) {
      newPlanSubTaskTitle.value = "";
    }
    currentPlanSubTasks = [];
    updateSubTaskSummary(0);
    // reset view/edit states
    document.getElementById("planDetailsView").style.display = "block";
    document.getElementById("planEditForm").style.display = "none";
  };

  const loadPlanDetails = async (planId) => {
    try {
      const resp = await fetch(`/api/plan/${planId}`, { method: "GET" });
      if (!resp.ok) {
        const e = await resp.json();
        showError(e.message || "Не вдалося завантажити план");
        return null;
      }
      const p = await resp.json();
      if (!p.success || !p.data) {
        showError(p.message || "Невірний формат відповіді");
        return null;
      }
      return p.data;
    } catch (err) {
      console.error(err);
      showError("Не вдалося завантажити план");
      return null;
    }
  };

  const getCurrentPlanIdFromModal = () => {
    const modal = document.getElementById("planDetailsModal");
    const rawId = modal?.dataset?.currentPlanId;
    return rawId ? Number(rawId) : null;
  };

  const renderPlanSubTasks = (subTasks) => {
    if (!editPlanSubTasksList) {
      return;
    }

    editPlanSubTasksList.innerHTML = "";

    if (!Array.isArray(subTasks) || subTasks.length === 0) {
      editPlanSubTasksList.innerHTML =
        '<p class="plan-subtasks-empty">Підзадач поки немає</p>';
      return;
    }

    subTasks.forEach((subTask) => {
      const row = document.createElement("div");
      row.className = `plan-subtask-item ${subTask.isCompleted ? "is-completed" : ""}`;

      const left = document.createElement("div");
      left.className = "plan-subtask-left";

      const checkbox = document.createElement("input");
      checkbox.type = "checkbox";
      checkbox.checked = Boolean(subTask.isCompleted);
      checkbox.className = "plan-subtask-checkbox";
      checkbox.setAttribute("aria-label", "Перемкнути статус підзадачі");
      checkbox.addEventListener("change", async () => {
        await togglePlanSubTaskStatus(subTask.id, checkbox.checked);
      });

      const title = document.createElement("span");
      title.className = "plan-subtask-title";
      title.textContent = subTask.title;

      left.appendChild(checkbox);
      left.appendChild(title);

      const actions = document.createElement("div");
      actions.className = "plan-subtask-actions";

      const editBtn = document.createElement("button");
      editBtn.type = "button";
      editBtn.className = "plan-subtask-action";
      editBtn.textContent = "Редагувати";
      editBtn.addEventListener("click", async () => {
        const nextTitle = prompt("Нова назва підзадачі", subTask.title);
        if (nextTitle === null) {
          return;
        }
        await updatePlanSubTask(subTask.id, nextTitle);
      });

      const deleteBtn = document.createElement("button");
      deleteBtn.type = "button";
      deleteBtn.className = "plan-subtask-action is-danger";
      deleteBtn.textContent = "Видалити";
      deleteBtn.addEventListener("click", async () => {
        await deletePlanSubTask(subTask.id);
      });

      actions.appendChild(editBtn);
      actions.appendChild(deleteBtn);

      row.appendChild(left);
      row.appendChild(actions);
      editPlanSubTasksList.appendChild(row);
    });
  };

  const updateSubTaskSummary = (count) => {
    if (!detailSubTasksSummary) {
      return;
    }

    detailSubTasksSummary.textContent = String(count);
  };

  const loadPlanSubTasks = async (planId) => {
    if (!planId) {
      return;
    }

    try {
      const response = await fetch(`/api/plan/${planId}/subtasks`, {
        method: "GET",
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.SUBTASK_LOAD_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.SUBTASK_LOAD_ERROR);
        return;
      }

      currentPlanSubTasks = data.data || [];
      updateSubTaskSummary(currentPlanSubTasks.length);
      renderPlanSubTasks(currentPlanSubTasks);
    } catch (error) {
      console.error("Помилка завантаження підзадач", error);
      showError(MESSAGES.SUBTASK_LOAD_ERROR);
    }
  };

  const createPlanSubTask = async () => {
    const planId = getCurrentPlanIdFromModal();
    if (!planId || !newPlanSubTaskTitle) {
      return;
    }

    const title = newPlanSubTaskTitle.value.trim();
    if (!title) {
      showError("Назва підзадачі не може бути порожньою");
      return;
    }

    try {
      const response = await fetch(`/api/plan/${planId}/subtasks`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ title }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.SUBTASK_CREATE_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.SUBTASK_CREATE_ERROR);
        return;
      }

      newPlanSubTaskTitle.value = "";
      await loadPlanSubTasks(planId);
      await loadPlans();
    } catch (error) {
      console.error("Помилка створення підзадачі", error);
      showError(MESSAGES.SUBTASK_CREATE_ERROR);
    }
  };

  const updatePlanSubTask = async (subTaskId, nextTitle) => {
    const planId = getCurrentPlanIdFromModal();
    if (!planId) {
      return;
    }

    const title = String(nextTitle || "").trim();
    if (!title) {
      showError("Назва підзадачі не може бути порожньою");
      return;
    }

    try {
      const response = await fetch(
        `/api/plan/${planId}/subtasks/${subTaskId}`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ title }),
        },
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.SUBTASK_UPDATE_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.SUBTASK_UPDATE_ERROR);
        return;
      }

      await loadPlanSubTasks(planId);
      await loadPlans();
    } catch (error) {
      console.error("Помилка оновлення підзадачі", error);
      showError(MESSAGES.SUBTASK_UPDATE_ERROR);
    }
  };

  const togglePlanSubTaskStatus = async (subTaskId, isCompleted) => {
    const planId = getCurrentPlanIdFromModal();
    if (!planId) {
      return;
    }

    try {
      const response = await fetch(
        `/api/plan/${planId}/subtasks/${subTaskId}/status`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ isCompleted }),
        },
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.SUBTASK_STATUS_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.SUBTASK_STATUS_ERROR);
        return;
      }

      await loadPlanSubTasks(planId);
      await loadPlans();
    } catch (error) {
      console.error("Помилка зміни статусу підзадачі", error);
      showError(MESSAGES.SUBTASK_STATUS_ERROR);
    }
  };

  const deletePlanSubTask = async (subTaskId) => {
    const planId = getCurrentPlanIdFromModal();
    if (!planId) {
      return;
    }

    const hasConfirmed = confirm("Видалити підзадачу?");
    if (!hasConfirmed) {
      return;
    }

    try {
      const response = await fetch(
        `/api/plan/${planId}/subtasks/${subTaskId}`,
        {
          method: "DELETE",
        },
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.SUBTASK_DELETE_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.SUBTASK_DELETE_ERROR);
        return;
      }

      await loadPlanSubTasks(planId);
      await loadPlans();
    } catch (error) {
      console.error("Помилка видалення підзадачі", error);
      showError(MESSAGES.SUBTASK_DELETE_ERROR);
    }
  };

  const formatAttachmentDate = (iso) => {
    if (!iso) {
      return "";
    }

    const value = new Date(iso);
    if (Number.isNaN(value.getTime())) {
      return "";
    }

    return value.toLocaleString("uk-UA", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const renderPlanAttachments = (planId, attachments) => {
    if (!planAttachmentsList) {
      return;
    }

    planAttachmentsList.innerHTML = "";

    if (!Array.isArray(attachments) || attachments.length === 0) {
      const empty = document.createElement("p");
      empty.className = "plan-attachment-empty";
      empty.textContent = "Файлів ще немає";
      planAttachmentsList.appendChild(empty);
      return;
    }

    attachments.forEach((attachment) => {
      const row = document.createElement("div");
      row.className = "plan-attachment-item";

      const link = document.createElement("a");
      link.className = "plan-attachment-link";
      link.href = attachment.fileUrl;
      link.target = "_blank";
      link.rel = "noopener noreferrer";
      const uploadedAt = formatAttachmentDate(attachment.createdAt);
      link.textContent = uploadedAt
        ? `${attachment.fileName} (${uploadedAt})`
        : attachment.fileName;

      const deleteBtn = document.createElement("button");
      deleteBtn.type = "button";
      deleteBtn.className = "plan-attachment-delete";
      deleteBtn.textContent = "Видалити";
      deleteBtn.addEventListener("click", async (event) => {
        event.preventDefault();
        event.stopPropagation();
        await deletePlanAttachment(planId, attachment.id);
      });

      row.appendChild(link);
      row.appendChild(deleteBtn);
      planAttachmentsList.appendChild(row);
    });
  };

  const loadPlanAttachments = async (planId) => {
    if (!planId) {
      return;
    }

    try {
      const response = await fetch(`/api/plan/${planId}/attachments`, {
        method: "GET",
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.ATTACHMENTS_LOAD_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.ATTACHMENTS_LOAD_ERROR);
        return;
      }

      renderPlanAttachments(planId, data.data || []);
    } catch (error) {
      console.error("Помилка при завантаженні вкладень", error);
      showError(MESSAGES.ATTACHMENTS_LOAD_ERROR);
    }
  };

  const uploadPlanAttachment = async (planId) => {
    if (!planId || !planAttachmentInput) {
      return;
    }

    const file = planAttachmentInput.files?.[0];
    if (!file) {
      showError("Оберіть файл для завантаження");
      return;
    }

    try {
      const formData = new FormData();
      formData.append("file", file);

      const response = await fetch(`/api/plan/${planId}/attachments`, {
        method: "POST",
        body: formData,
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.ATTACHMENT_UPLOAD_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.ATTACHMENT_UPLOAD_ERROR);
        return;
      }

      showSuccess("Файл успішно прикріплено");
      planAttachmentInput.value = "";
      await loadPlanAttachments(planId);
    } catch (error) {
      console.error("Помилка при прикріпленні файлу", error);
      showError(MESSAGES.ATTACHMENT_UPLOAD_ERROR);
    }
  };

  const deletePlanAttachment = async (planId, attachmentId) => {
    const hasConfirmed = confirm("Видалити це вкладення?");
    if (!hasConfirmed) {
      return;
    }

    try {
      const response = await fetch(
        `/api/plan/${planId}/attachments/${attachmentId}`,
        {
          method: "DELETE",
        },
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.ATTACHMENT_DELETE_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.ATTACHMENT_DELETE_ERROR);
        return;
      }

      showSuccess("Вкладення видалено");
      await loadPlanAttachments(planId);
    } catch (error) {
      console.error("Помилка при видаленні вкладення", error);
      showError(MESSAGES.ATTACHMENT_DELETE_ERROR);
    }
  };

  const fillPlanDetailsView = (plan) => {
    if (!plan) return;
    document.getElementById("detailTitle").textContent = plan.title || "-";
    document.getElementById("detailDescription").textContent =
      plan.description || "-";
    const dt = plan.scheduledAt ? new Date(plan.scheduledAt) : null;
    document.getElementById("detailDateTime").textContent = dt
      ? dt.toLocaleString("uk-UA", {
          day: "2-digit",
          month: "short",
          hour: "2-digit",
          minute: "2-digit",
        })
      : "-";
    const priorityEl = document.getElementById("detailPriority");
    if (priorityEl) {
      priorityEl.textContent = plan.priorityLabel || plan.priority || "-";
    }
    document.getElementById("detailStatus").textContent = plan.isCompleted
      ? "Виконано"
      : "Активний";
    updateSubTaskSummary(plan.subTaskCount || 0);

    // Update priority badge with proper class and label
    const badge = document.getElementById("detailPriorityBadge");
    if (badge) {
      const clsBase = "plan-priority-badge";
      const pr = plan.priority ? String(plan.priority) : "2";
      badge.className = `${clsBase} plan-priority-${pr}`;
      badge.textContent = plan.priorityLabel || getPriorityLabel(pr);
    }

    // status color
    const statusEl = document.getElementById("detailStatus");
    if (statusEl) {
      statusEl.style.color = plan.isCompleted
        ? "var(--dojo-success)"
        : "var(--dojo-muted)";
    }
  };

  const getPriorityLabel = (priority) => {
    if (priority === "1") return "Низька";
    if (priority === "3") return "Висока";
    return "Середня";
  };

  const openPlanDetailsById = async (planId) => {
    if (!planId) return;
    const plan = await loadPlanDetails(planId);
    if (!plan) return;

    fillPlanDetailsView(plan);
    openPlanDetailsModal();

    const detailsModal = document.getElementById("planDetailsModal");
    if (detailsModal) {
      detailsModal.dataset.currentPlanId = String(planId);
    }

    await loadPlanAttachments(planId);
    await loadPlanSubTasks(planId);
  };

  const fillPlanEditForm = (plan) => {
    if (!plan) return;
    document.getElementById("editPlanTitle").value = plan.title || "";
    document.getElementById("editPlanDescription").value =
      plan.description || "";
    if (plan.scheduledAt) {
      const d = new Date(plan.scheduledAt);
      const yyyy = d.getFullYear();
      const mm = String(d.getMonth() + 1).padStart(2, "0");
      const dd = String(d.getDate()).padStart(2, "0");
      const hh = String(d.getHours()).padStart(2, "0");
      const min = String(d.getMinutes()).padStart(2, "0");
      document.getElementById("editPlanDate").value = `${yyyy}-${mm}-${dd}`;
      document.getElementById("editPlanTime").value = `${hh}:${min}`;
    }
    const pr = String(plan.priority || 2);
    const radio = document.querySelector(
      `input[name="editPlanPriority"][value="${pr}"]`,
    );
    if (radio) radio.checked = true;
  };

  const attachDetailHandlers = () => {
    // List items click
    planListContainer.addEventListener("click", async (ev) => {
      if (ev.target.closest("input, button, a")) {
        return;
      }

      const item = ev.target.closest(".todo-item");
      if (!item) return;
      await openPlanDetailsById(item.dataset.planId);
    });

    // Badges on grid - delegate
    timeGrid.addEventListener("click", async (ev) => {
      const badge = ev.target.closest(".plan-slot");
      if (!badge) return;
      await openPlanDetailsById(badge.dataset.planId);
    });

    // modal controls
    document
      .getElementById("closePlanDetailsModal")
      .addEventListener("click", closePlanDetailsModal);
    document
      .getElementById("closePlanDetailsBtn")
      .addEventListener("click", closePlanDetailsModal);

    if (uploadPlanAttachmentBtn && planAttachmentInput) {
      uploadPlanAttachmentBtn.addEventListener("click", () => {
        planAttachmentInput.click();
      });

      planAttachmentInput.addEventListener("change", async () => {
        const planId =
          document.getElementById("planDetailsModal").dataset.currentPlanId;
        if (!planId) {
          return;
        }

        await uploadPlanAttachment(planId);
      });
    }

    if (addPlanSubTaskBtn) {
      addPlanSubTaskBtn.addEventListener("click", async () => {
        await createPlanSubTask();
      });
    }

    if (newPlanSubTaskTitle) {
      newPlanSubTaskTitle.addEventListener("keydown", async (event) => {
        if (event.key !== "Enter") {
          return;
        }

        event.preventDefault();
        await createPlanSubTask();
      });
    }

    document
      .getElementById("editPlanBtn")
      .addEventListener("click", async () => {
        const planId =
          document.getElementById("planDetailsModal").dataset.currentPlanId;
        if (!planId) return;
        const plan = await loadPlanDetails(planId);
        if (!plan) return;
        fillPlanEditForm(plan);
        await loadPlanSubTasks(planId);
        document.getElementById("planDetailsView").style.display = "none";
        document.getElementById("planEditForm").style.display = "block";
      });

    document
      .getElementById("cancelEditPlanBtn")
      .addEventListener("click", (e) => {
        e.preventDefault();
        document.getElementById("planEditForm").style.display = "none";
        document.getElementById("planDetailsView").style.display = "block";
      });

    document
      .getElementById("planEditForm")
      .addEventListener("submit", async (e) => {
        e.preventDefault();
        const planId =
          document.getElementById("planDetailsModal").dataset.currentPlanId;
        if (!planId) return;
        const title = document.getElementById("editPlanTitle").value.trim();
        const description = document
          .getElementById("editPlanDescription")
          .value.trim();
        const date = document.getElementById("editPlanDate").value;
        const time = document.getElementById("editPlanTime").value;
        const pr = document.querySelector(
          'input[name="editPlanPriority"]:checked',
        );
        if (!title || !date || !time || !pr) {
          showError("Заповніть усі обов'язкові поля");
          return;
        }
        const scheduled = new Date(`${date}T${time}`);
        try {
          const resp = await fetch(`/api/plan/${planId}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              title,
              description: description || null,
              scheduledAt: scheduled.toISOString(),
              priority: Number(pr.value),
            }),
          });
          if (!resp.ok) {
            const err = await resp.json();
            showError(err.message || "Не вдалося оновити план");
            return;
          }
          const result = await resp.json();
          if (!result.success) {
            showError(result.message || "Не вдалося оновити план");
            return;
          }
          showSuccess("План оновлено");
          closePlanDetailsModal();
          await loadPlans();

          // Оновлюємо статистику після редагування плану
          await refreshStatistics();
        } catch (err) {
          console.error(err);
          showError("Не вдалося оновити план");
        }
      });
  };

  // update renderPlanOnGrid to set data-plan-id and make it focusable/clickable
  const renderPlanOnGrid = (plan) => {
    if (!plan.scheduledAt || !currentWeekStartIso || !currentWeekEndIso) return;
    const scheduledDate = new Date(plan.scheduledAt);
    const weekStart = new Date(currentWeekStartIso);
    const weekEnd = new Date(currentWeekEndIso);
    weekStart.setHours(0, 0, 0, 0);
    weekEnd.setHours(23, 59, 59, 999);
    if (scheduledDate < weekStart || scheduledDate > weekEnd) return;

    const diffDays = Math.floor(
      (scheduledDate - weekStart) / (1000 * 60 * 60 * 24),
    );
    if (diffDays < 0 || diffDays > 6) return;

    const hour = scheduledDate.getHours();
    const slotRow = timeGrid.querySelectorAll(".dashboard-grid-row")[hour];
    if (!slotRow) return;
    const cell = slotRow.querySelectorAll(".dashboard-slot-cell")[diffDays];
    if (!cell) return;

    const badge = document.createElement("div");
    const priorityClass = plan.priority ? plan.priority : 2;
    badge.className = `plan-slot plan-priority-${priorityClass} ${plan.isCompleted ? "plan-completed" : ""}`;
    badge.textContent = `${plan.title}${plan.hasSubTasks ? " ⭐" : ""}`;
    badge.title = plan.description || "";
    badge.tabIndex = 0;
    badge.setAttribute("role", "button");
    badge.setAttribute("aria-label", `Відкрити план: ${plan.title}`);
    badge.dataset.planId = plan.id;
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

  globalThis.addEventListener("dashboard:week-changed", handleWeekChanged);

  applyInitialWeekState();
  attachDetailHandlers();
  loadPlans();
})();
