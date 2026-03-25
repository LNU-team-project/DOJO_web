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

  if (!todoModal || !openTodoModalBtn || !todoForm || !allTodoItems) {
    return;
  }

  
  const openModal = () => {
    todoModal.style.display = "flex";
    todoModal.setAttribute("aria-hidden", "false");
    todoForm.reset();
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

  /**
   * Отримує пріоритет як текст
   */
  const getPriorityLabel = (priority) => {
    return PRIORITY_LABELS[priority] || "Невідома";
  };

  
  const createTodoElement = (todo) => {
    const div = document.createElement("div");
    div.className = `todo-item ${todo.isCompleted ? "completed" : ""}`;
    div.dataset.todoId = todo.id;

    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.className = "todo-item-checkbox";
    checkbox.checked = todo.isCompleted;
    checkbox.setAttribute("aria-label", `Позначити як ${todo.isCompleted ? "невиконане" : "виконане"}`);

    const contentDiv = document.createElement("div");
    contentDiv.className = "todo-item-content";

    const titleSpan = document.createElement("div");
    titleSpan.className = "todo-item-title";
    titleSpan.textContent = todo.title;
    contentDiv.appendChild(titleSpan);

    if (todo.description) {
      const descriptionSpan = document.createElement("div");
      descriptionSpan.className = "todo-item-description";
      descriptionSpan.textContent = todo.description;
      contentDiv.appendChild(descriptionSpan);
    }

    const metaDiv = document.createElement("div");
    metaDiv.className = "todo-item-meta";

    const prioritySpan = document.createElement("span");
    prioritySpan.className = `todo-priority priority-${todo.priority}`;
    prioritySpan.textContent = getPriorityLabel(todo.priority);
    metaDiv.appendChild(prioritySpan);

    contentDiv.appendChild(metaDiv);

    const deleteBtn = document.createElement("button");
    deleteBtn.type = "button";
    deleteBtn.className = "todo-item-delete";
    deleteBtn.setAttribute("aria-label", "Видалити завдання");
    deleteBtn.innerHTML = "🗑️";

    div.appendChild(checkbox);
    div.appendChild(contentDiv);
    div.appendChild(deleteBtn);

    // Обробники подій
    checkbox.addEventListener("change", () => handleTodoStatusChange(todo.id, checkbox.checked));
    deleteBtn.addEventListener("click", () => handleTodoDelete(todo.id));

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
      const endpoint = isCompleted ? `complete/${todoId}` : `incomplete/${todoId}`;
      const response = await fetch(`/api/todo/${endpoint}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        const errorData = await response.json();
        showError(errorData.message || MESSAGES.UPDATE_ERROR);
        loadTodos();
        return;
      }

      const message = isCompleted ? MESSAGES.TASK_COMPLETED : MESSAGES.TASK_INCOMPLETE;
      showSuccess(message);
      loadTodos();
    } catch (error) {
      console.error("Помилка при оновленні статусу TODO:", error);
      showError(MESSAGES.UPDATE_ERROR);
      loadTodos();
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
      loadTodos();
    } catch (error) {
      console.error("Помилка при видаленні TODO:", error);
      showError(MESSAGES.DELETE_ERROR);
    }
  };

  
  const handleFormSubmit = async (event) => {
    event.preventDefault();

    const titleInput = document.getElementById("todoTitle");
    const descriptionInput = document.getElementById("todoDescription");
    const priorityInput = document.querySelector('input[name="priority"]:checked');

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

    try {
      const response = await fetch("/api/todo/create", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          title,
          description: description || null,
          priority: parseInt(priority, 10),
        }),
      });

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

      showSuccess(MESSAGES.TASK_CREATED);
      closeModal();
      loadTodos();
    } catch (error) {
      console.error("Помилка при створенні TODO:", error);
      showError(MESSAGES.CREATE_ERROR);
    }
  };

  // Обробники подій
  openTodoModalBtn.addEventListener("click", openModal);
  closeTodoModalBtn.addEventListener("click", closeModal);
  cancelTodoBtn.addEventListener("click", closeModal);
  todoModal.addEventListener("click", handleModalBackdropClick);
  todoForm.addEventListener("submit", handleFormSubmit);

  loadTodos();
})();
