(() => {
  // Константи для повідомлень
  const MESSAGES = {
    ERROR_PREFIX: "❌ Помилка: ",
    SUCCESS_PREFIX: "✅ ",
    LOAD_ERROR: "Не вдалося завантажити завдання",
    UPDATE_ERROR: "Не вдалося оновити завдання",
    DELETE_ERROR: "Не вдалося видалити завдання",
    CREATE_ERROR: "Не вдалося створити завдання",
    INVALID_FORMAT: "Невірний формат відповіді",
    INVALID_TITLE: "Назва завдання не може бути порожною",
    SELECT_PRIORITY: "Будь ласка, виберіть рівень складності",
    CONFIRM_DELETE: "Ви впевнені, що хочете видалити це завдання?",
    TASK_COMPLETED: "Завдання позначено як виконане",
    TASK_INCOMPLETE: "Завдання повернено до активних",
    TASK_DELETED: "Завдання видалено",
    TASK_CREATED: "Завдання успішно створено!",
    EMPTY_LIST: "Немає завдань",
    FETCH_ERROR: "Помилка при запиті до сервера",
  };

  const PRIORITY_LABELS = {
    1: "Низька",
    2: "Середня",
    3: "Висока",
  };

  const todoModal = document.getElementById("todoModal");
  const openTodoModalBtn = document.getElementById("openTodoModal");
  const closeTodoModalBtn = document.getElementById("closeTodoModal");
  const cancelTodoBtn = document.getElementById("cancelTodoBtn");
  const todoForm = document.getElementById("todoForm");
  const allTodoItems = document.getElementById("allTodoItems");
  const todoDetailsModal = document.getElementById("todoDetailsModal");
  const closeTodoDetailsModalBtn = document.getElementById("closeTodoDetailsModal");
  const closeTodoDetailsBtn = document.getElementById("closeTodoDetailsBtn");
  const editTodoBtn = document.getElementById("editTodoBtn");
  const cancelEditTodoBtn = document.getElementById("cancelEditTodoBtn");
  const todoDetailsView = document.getElementById("todoDetailsView");
  const todoEditForm = document.getElementById("todoEditForm");
  const todoDetailTitle = document.getElementById("todoDetailTitle");
  const todoDetailStatus = document.getElementById("todoDetailStatus");
  const todoDetailPriorityBadge = document.getElementById("todoDetailPriorityBadge");
  const todoDetailDescription = document.getElementById("todoDetailDescription");
  const todoDetailDueDate = document.getElementById("todoDetailDueDate");
  const todoDetailCreatedAt = document.getElementById("todoDetailCreatedAt");

  if (
    !todoModal ||
    !openTodoModalBtn ||
    !todoForm ||
    !allTodoItems ||
    !todoDetailsModal ||
    !todoEditForm
  ) {
    return;
  }

  let currentTodoDetails = null;

  const openModal = (isEditMode = false) => {
    closeTodoDetailsModal();
    todoModal.style.display = "flex";
    todoModal.setAttribute("aria-hidden", "false");
    if (!isEditMode) {
      todoForm.reset();
      // Скидаємо режим редагування при відкритті для нового завдання
      delete todoForm.dataset.editingTodoId;
      const submitBtn = todoForm.querySelector('button[type="submit"]');
      if (submitBtn) {
        submitBtn.textContent = "Додати завдання";
      }
      document.getElementById("todoModalTitle").textContent = "Додати ціль";
    }
    const titleInput = document.getElementById("todoTitle");
    if (titleInput) {
      titleInput.focus();
    }
  };

  /**
   * Закриває модальне вікно
   */
  const closeModal = () => {
    todoModal.style.display = "none";
    todoModal.setAttribute("aria-hidden", "true");
    todoForm.reset();
  };

  /**
   * Закриває модальне вікно при кліку поза ним
   */
  const handleModalBackdropClick = (event) => {
    if (event.target === todoModal) {
      closeModal();
    }
  };

  /**
   * Показує повідомлення про помилку користувачу
   */
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

  /**
   * Показує повідомлення про успіх
   */
  const showSuccess = (message) => {
    console.log(MESSAGES.SUCCESS_PREFIX + message);
  };

  /**
   * Отримує пріоритет як текст
   */
  const getPriorityLabel = (priority) => {
    return PRIORITY_LABELS[priority] || "Невідома";
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

  const processTodoStatusResponse = async (response) => {
    try {
      const payload = await response.json();
      await updateHeroStatus(payload);
    } catch (error) {
      console.warn("Не вдалося обробити відповідь героя", error);
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

  const createTodoElement = (todo) => {
    const div = document.createElement("div");
    div.className = `todo-item ${todo.isCompleted ? "completed" : ""}`;
    div.dataset.todoId = todo.id;

    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.className = "todo-item-checkbox";
    checkbox.checked = todo.isCompleted;
    checkbox.setAttribute(
      "aria-label",
      `Позначити як ${todo.isCompleted ? "невиконане" : "виконане"}`,
    );

    const contentDiv = document.createElement("div");
    contentDiv.className = "todo-item-content";

    const titleSpan = document.createElement("div");
    titleSpan.className = "todo-item-title";
    titleSpan.textContent = todo.title;
    contentDiv.appendChild(titleSpan);

    const descriptionSpan = document.createElement("div");
    descriptionSpan.className = "todo-item-description";
    descriptionSpan.textContent = todo.description || "";
    contentDiv.appendChild(descriptionSpan);

    const metaDiv = document.createElement("div");
    metaDiv.className = "todo-item-meta";

    const prioritySpan = document.createElement("span");
    prioritySpan.className = `todo-priority priority-${todo.priority}`;
    prioritySpan.textContent = getPriorityLabel(todo.priority);
    metaDiv.appendChild(prioritySpan);

    contentDiv.appendChild(metaDiv);

    const hasDescription = Boolean(todo.description && todo.description.trim() !== "");
    if (!hasDescription) {
      descriptionSpan.textContent = "Опис відсутній";
    }

    const deleteBtn = document.createElement("button");
    deleteBtn.type = "button";
    deleteBtn.className = "todo-item-delete";
    deleteBtn.setAttribute("aria-label", "Видалити завдання");
    deleteBtn.innerHTML = "🗑️";

    div.appendChild(checkbox);
    div.appendChild(contentDiv);
    div.appendChild(deleteBtn);

    // Обробники подій
    checkbox.addEventListener("change", () =>
      handleTodoStatusChange(todo.id, checkbox.checked),
    );
    deleteBtn.addEventListener("click", () => handleTodoDelete(todo.id));
    div.addEventListener("click", (event) => {
      if (event.target.closest("input, button, a, label")) {
        return;
      }

      openTodoDetails(todo);
    });

    return div;
  };

  const loadTodos = async () => {
    try {
      const response = await fetch("/api/todo/list", {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      });

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

      renderTodos(data.data);
    } catch (error) {
      console.error("Помилка при завантаженні TODO:", error);
      showError(MESSAGES.LOAD_ERROR);
    }
  };

  const renderTodos = (todoList) => {
    if (!allTodoItems || !todoList) {
      return;
    }

    allTodoItems.innerHTML = "";

    // Об'єднуємо активні та виконані завдання
    const incompleteTodos = todoList.incompleteTodos || [];
    const completedTodos = todoList.completedTodos || [];
    const allTodos = [...incompleteTodos, ...completedTodos];

    if (allTodos.length === 0) {
      allTodoItems.innerHTML = `<p class="todo-empty-message">${MESSAGES.EMPTY_LIST}</p>`;
    } else {
      allTodos.forEach((todo) => {
        allTodoItems.appendChild(createTodoElement(todo));
      });
    }
  };

  const handleTodoStatusChange = async (todoId, isCompleted) => {
    try {
      const endpoint = isCompleted
        ? `complete/${todoId}`
        : `incomplete/${todoId}`;
      const response = await fetch(`/api/todo/${endpoint}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || MESSAGES.UPDATE_ERROR);
        await loadTodos();
        return;
      }

      const message = isCompleted
        ? MESSAGES.TASK_COMPLETED
        : MESSAGES.TASK_INCOMPLETE;
      showSuccess(message);

      await loadTodos();
      await processTodoStatusResponse(response);
      await refreshStatistics();
    } catch (error) {
      console.error("Помилка при оновленні статусу TODO:", error);
      showError(MESSAGES.UPDATE_ERROR);
      await loadTodos();
    }
  };

  const handleTodoDelete = async (todoId) => {
    const confirmDelete = confirm(MESSAGES.CONFIRM_DELETE);
    if (!confirmDelete) {
      return;
    }

    try {
      const response = await fetch(`/api/todo/delete/${todoId}`, {
        method: "DELETE",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || MESSAGES.DELETE_ERROR);
        return;
      }

      showSuccess(MESSAGES.TASK_DELETED);
      await loadTodos();

      // Оновлюємо статистику після видалення завдання
      await refreshStatistics();
    } catch (error) {
      console.error("Помилка при видаленні TODO:", error);
      showError(MESSAGES.DELETE_ERROR);
    }
  };

  const handleTodoEdit = (todo) => {
    const nextTodo = todo || currentTodoDetails;
    if (!nextTodo) {
      return;
    }

    currentTodoDetails = nextTodo;
    fillTodoEditForm(nextTodo);
    if (todoDetailsView) {
      todoDetailsView.style.display = "none";
    }
    if (todoEditForm) {
      todoEditForm.style.display = "block";
    }
    const titleInput = document.getElementById("editTodoTitle");
    titleInput?.focus();
  };

  const formatTodoDate = (dateValue) => {
    if (!dateValue) {
      return "Без дедлайну";
    }

    const date = new Date(`${dateValue}T00:00:00`);
    if (Number.isNaN(date.getTime())) {
      return String(dateValue);
    }

    return new Intl.DateTimeFormat("uk-UA", {
      day: "numeric",
      month: "long",
      year: "numeric",
    }).format(date);
  };

  const formatTodoCreatedAt = (value) => {
    if (!value) {
      return "-";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return String(value);
    }

    return new Intl.DateTimeFormat("uk-UA", {
      day: "numeric",
      month: "long",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    }).format(date);
  };

  const setTodoDetailsBadge = (todo) => {
    if (!todoDetailPriorityBadge) {
      return;
    }

    todoDetailPriorityBadge.className = `plan-priority-badge plan-priority-${todo.priority || 2}`;
    todoDetailPriorityBadge.textContent = getPriorityLabel(todo.priority);
  };

  const fillTodoDetailsView = (todo) => {
    if (!todo) {
      return;
    }

    if (todoDetailTitle) {
      todoDetailTitle.textContent = todo.title || "-";
      todoDetailTitle.style.textDecoration = todo.isCompleted ? "line-through" : "none";
      todoDetailTitle.style.color = todo.isCompleted ? "var(--dojo-muted)" : "var(--dojo-ink)";
    }

    if (todoDetailStatus) {
      todoDetailStatus.textContent = todo.isCompleted ? "Виконане" : "Активне";
      todoDetailStatus.style.color = todo.isCompleted
        ? "var(--dojo-success)"
        : "var(--dojo-muted)";
    }

    setTodoDetailsBadge(todo);

    if (todoDetailDescription) {
      todoDetailDescription.textContent = todo.description?.trim() || "Опис відсутній";
    }

    if (todoDetailDueDate) {
      todoDetailDueDate.textContent = formatTodoDate(todo.dueDate);
    }

    if (todoDetailCreatedAt) {
      todoDetailCreatedAt.textContent = formatTodoCreatedAt(todo.createdAt);
    }
  };

  const fillTodoEditForm = (todo) => {
    if (!todo) {
      return;
    }

    const titleInput = document.getElementById("editTodoTitle");
    const descriptionInput = document.getElementById("editTodoDescription");
    const dueDateInput = document.getElementById("editTodoDueDate");
    const priorityInput = document.querySelector(
      `input[name="editTodoPriority"][value="${todo.priority}"]`,
    );

    if (titleInput) titleInput.value = todo.title || "";
    if (descriptionInput) descriptionInput.value = todo.description || "";
    if (dueDateInput) dueDateInput.value = todo.dueDate || "";
    if (priorityInput) priorityInput.checked = true;
  };

  const openTodoDetails = (todo) => {
    if (!todo) {
      return;
    }

    currentTodoDetails = todo;
    fillTodoDetailsView(todo);

    if (todoDetailsView) {
      todoDetailsView.style.display = "flex";
    }
    if (todoEditForm) {
      todoEditForm.style.display = "none";
    }

    todoDetailsModal.style.display = "flex";
    todoDetailsModal.setAttribute("aria-hidden", "false");
    todoDetailsModal.dataset.currentTodoId = String(todo.id);
  };

  const closeTodoDetailsModal = () => {
    if (!todoDetailsModal) {
      return;
    }

    todoDetailsModal.style.display = "none";
    todoDetailsModal.setAttribute("aria-hidden", "true");
    delete todoDetailsModal.dataset.currentTodoId;
    currentTodoDetails = null;
    if (todoDetailsView) {
      todoDetailsView.style.display = "flex";
    }
    if (todoEditForm) {
      todoEditForm.style.display = "none";
      todoEditForm.reset();
    }
  };

  const handleTodoDetailsBackdropClick = (event) => {
    if (event.target === todoDetailsModal) {
      closeTodoDetailsModal();
    }
  };

  const handleTodoEditFormSubmit = async (event) => {
    event.preventDefault();

    const todoId = todoDetailsModal?.dataset?.currentTodoId;
    const titleInput = document.getElementById("editTodoTitle");
    const descriptionInput = document.getElementById("editTodoDescription");
    const dueDateInput = document.getElementById("editTodoDueDate");
    const priorityInput = document.querySelector(
      'input[name="editTodoPriority"]:checked',
    );

    if (!todoId || !titleInput || !descriptionInput || !priorityInput) {
      showError(MESSAGES.INVALID_FORMAT);
      return;
    }

    const title = titleInput.value.trim();
    const description = descriptionInput.value.trim();
    const dueDate = dueDateInput?.value || "";

    if (!title) {
      showError(MESSAGES.INVALID_TITLE);
      return;
    }

    try {
      const response = await fetch(`/api/todo/update/${todoId}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          title,
          description: description || null,
          priority: Number.parseInt(priorityInput.value, 10),
          dueDate: dueDate || null,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || MESSAGES.UPDATE_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success || !data.data) {
        showError(data.message || MESSAGES.UPDATE_ERROR);
        return;
      }

      showSuccess("Завдання успішно оновлено!");
      currentTodoDetails = data.data;
      fillTodoDetailsView(data.data);
      await loadTodos();
      await refreshStatistics();
      if (todoEditForm) {
        todoEditForm.style.display = "none";
      }
      if (todoDetailsView) {
        todoDetailsView.style.display = "flex";
      }
    } catch (error) {
      console.error("Помилка при оновленні TODO:", error);
      showError(MESSAGES.UPDATE_ERROR);
    }
  };

  const handleFormSubmit = async (event) => {
    event.preventDefault();

    const titleInput = document.getElementById("todoTitle");
    const descriptionInput = document.getElementById("todoDescription");
    const priorityInput = document.querySelector(
      'input[name="priority"]:checked',
    );

    if (!titleInput || !descriptionInput) {
      showError(MESSAGES.INVALID_FORMAT);
      return;
    }

    const title = titleInput.value.trim();
    const description = descriptionInput.value.trim();

    if (!title) {
      showError(MESSAGES.INVALID_TITLE);
      return;
    }

    if (!priorityInput) {
      showError(MESSAGES.SELECT_PRIORITY);
      return;
    }

    const priority = priorityInput.value;
    const editingTodoId = todoForm.dataset.editingTodoId;

    try {
      let response;
      let successMessage;

      if (editingTodoId) {
        // Редагування існуючого завдання
        response = await fetch(`/api/todo/update/${editingTodoId}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            title,
            description: description || null,
            priority: Number.parseInt(priority, 10),
          }),
        });
        successMessage = "Завдання успішно оновлено!";
      } else {
        // Створення нового завдання
        response = await fetch("/api/todo/create", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            title,
            description: description || null,
            priority: Number.parseInt(priority, 10),
          }),
        });
        successMessage = MESSAGES.TASK_CREATED;
      }

      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || MESSAGES.CREATE_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.CREATE_ERROR);
        return;
      }

      showSuccess(successMessage);
      closeModal();

      // Скидаємо режим редагування
      delete todoForm.dataset.editingTodoId;
      const submitBtn = todoForm.querySelector('button[type="submit"]');
      if (submitBtn) {
        submitBtn.textContent = "Додати завдання";
      }
      document.getElementById("todoModalTitle").textContent = "Додати ціль";

      await loadTodos();

      // Оновлюємо статистику після створення/оновлення завдання
      await refreshStatistics();
    } catch (error) {
      console.error("Помилка при створенні/оновленні TODO:", error);
      showError(MESSAGES.CREATE_ERROR);
    }
  };

  // Обробники подій
  openTodoModalBtn.addEventListener("click", openModal);
  closeTodoModalBtn.addEventListener("click", closeModal);
  cancelTodoBtn.addEventListener("click", closeModal);
  todoModal.addEventListener("click", handleModalBackdropClick);
  todoForm.addEventListener("submit", handleFormSubmit);
  closeTodoDetailsModalBtn?.addEventListener("click", closeTodoDetailsModal);
  closeTodoDetailsBtn?.addEventListener("click", closeTodoDetailsModal);
  todoDetailsModal.addEventListener("click", handleTodoDetailsBackdropClick);
  cancelEditTodoBtn?.addEventListener("click", (event) => {
    event.preventDefault();
    if (todoEditForm) {
      todoEditForm.style.display = "none";
    }
    if (todoDetailsView) {
      todoDetailsView.style.display = "flex";
    }
  });
  editTodoBtn?.addEventListener("click", () => handleTodoEdit());
  todoEditForm.addEventListener("submit", handleTodoEditFormSubmit);

  loadTodos();
})();
