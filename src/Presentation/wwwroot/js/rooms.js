(() => {
  console.log("🚀 rooms.js завантажено!");

  // ===== DOM Elements =====
  const roomsModal = document.getElementById("roomsModal");
  const roomsListModal = document.getElementById("roomsListModal");
  const openCreateRoomBtn = document.getElementById("openCreateRoomBtn");
  const openMyRoomsBtn = document.getElementById("openMyRoomsBtn");
  const openCreateRoomFromFriendsBtn = document.getElementById("openCreateRoomFromFriendsBtn");
  const openMyRoomsFromFriendsBtn = document.getElementById("openMyRoomsFromFriendsBtn");
  const closeRoomsModalBtn = document.getElementById("closeRoomsModal");
  const closeRoomsListModalBtn = document.getElementById("closeRoomsListModal");
  const friendsModal = document.getElementById("friendsModal");
  const createRoomForm = document.getElementById("createRoomForm");
  const roomsList = document.getElementById("roomsList");

  // ===== Constants =====
  const MESSAGES = {
    CREATE_ERROR: "Не вдалося створити кімнату",
    LOAD_ERROR: "Не вдалося завантажити кімнати",
    DELETE_ERROR: "Не вдалося видалити кімнату",
    EMPTY_INPUT: "Вкажіть назву кімнати",
    NO_COMMENT_TEXT: "Коментар не може бути порожнім",
    NO_TASK_TITLE: "Вкажіть назву завдання",
    NO_TASK_ASSIGNEE: "Виберіть учасника для призначення",
    NO_MEMBER_SELECTED: "Виберіть користувача зі списку друзів",
    ADD_COMMENT_FAILED: "Не вдалося додати коментар",
    ADD_TASK_FAILED: "Не вдалося додати завдання",
    ADD_MEMBER_FAILED: "Не вдалося додати учасника",
  };

  const API_ENDPOINTS = {
    ROOMS: "/api/rooms",
    ROOM_DETAILS: (id) => `/api/rooms/${id}`,
    ADD_TASK: (roomId) => `/api/rooms/${roomId}/tasks`,
    ADD_COMMENT: (taskId) => `/api/rooms/tasks/${taskId}/comments`,
    ADD_MEMBER: (roomId) => `/api/rooms/${roomId}/members/add`,
    FRIENDS: "/api/friends",
  };

  const TIMEOUTS = {
    MODAL_CLOSE: 300,
    PAGE_RELOAD: 500,
    SUCCESS_MESSAGE: 5000,
    ERROR_MESSAGE: 5000,
  };

  // ===== Validation =====
  if (!roomsModal || !roomsListModal) {
    console.error("❌ Не знайдені основні елементи модалей!");
    return;
  }

  // ===== Utility Functions =====
  const escapeHtml = (unsafe) => {
    if (!unsafe) return "";
    return unsafe
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
  };

  const showNotification = (message, type = "success") => {
    const notifDiv = document.createElement("div");
    const notifClass = type === "success" ? "alert alert-success" : "alert alert-danger";
    notifDiv.className = notifClass;
    notifDiv.setAttribute("role", "alert");
    notifDiv.textContent = `${type === "success" ? "✅" : "❌"} ${message}`;
    document.body.insertBefore(notifDiv, document.body.firstChild);
    setTimeout(() => notifDiv.remove(), TIMEOUTS[`${type.toUpperCase()}_MESSAGE`]);
  };

  const showError = (message) => {
    console.error("❌", message);
    showNotification(message, "error");
  };

  const showSuccess = (message) => {
    console.log("✅", message);
    showNotification(message, "success");
  };

  // ===== Modal Handlers =====
  const openCreateRoomModal = () => {
    console.log("📝 Відкриття модалі створення кімнати");
    roomsModal.style.display = "flex";
    roomsModal.setAttribute("aria-hidden", "false");
    if (friendsModal) {
      friendsModal.style.display = "none";
      friendsModal.setAttribute("aria-hidden", "true");
    }
    if (createRoomForm) {
      createRoomForm.reset();
    }
  };

  const closeCreateRoomModal = () => {
    console.log("Закриття модалі створення кімнати");
    roomsModal.style.display = "none";
    roomsModal.setAttribute("aria-hidden", "true");
  };

  const openRoomsListModal = () => {
    console.log("👥 Відкриття модалі списку кімнат");
    roomsListModal.style.display = "flex";
    roomsListModal.setAttribute("aria-hidden", "false");
    if (friendsModal) {
      friendsModal.style.display = "none";
      friendsModal.setAttribute("aria-hidden", "true");
    }
    loadRooms();
  };

  const closeRoomsListModal = () => {
    console.log("Закриття модалі списку кімнат");
    roomsListModal.style.display = "none";
    roomsListModal.setAttribute("aria-hidden", "true");
  };

  // ===== Data Loading =====
  const loadRooms = async () => {
    console.log("📥 Завантаження кімнат...");
    try {
      const response = await fetch(API_ENDPOINTS.ROOMS, {
        method: "GET",
        credentials: "include",
        cache: "no-store",
      });

      console.log("API response status:", response.status);

      if (!response.ok) {
        showError(MESSAGES.LOAD_ERROR);
        return;
      }

      const data = await response.json();
      console.log("📊 Отримані кімнати:", data);

      if (!data.success) {
        showError(data.message || MESSAGES.LOAD_ERROR);
        return;
      }

      const rooms = Array.isArray(data.data) ? data.data : [];
      console.log(`✅ Кімнат завантажено: ${rooms.length}`);

      if (rooms.length === 0) {
        if (roomsList) {
          roomsList.innerHTML = '<li class="leaderboard-empty">Кімнат не знайдено</li>';
        }
        return;
      }

      if (roomsList) {
        roomsList.innerHTML = "";
        rooms.forEach((room) => {
          const item = document.createElement("li");
          item.className = "leaderboard-item rooms-list-item";

          const nameSpan = document.createElement("span");
          nameSpan.className = "leaderboard-name rooms-name";
          nameSpan.textContent = room.title || "Кімната";

          const membersSpan = document.createElement("span");
          membersSpan.className = "rooms-members-count";
          membersSpan.textContent = `${room.members?.length || 0} учасників`;

          const openBtn = document.createElement("button");
          openBtn.type = "button";
          openBtn.className = "btn btn-primary rooms-open-btn";
          openBtn.textContent = "Відкрити";
          openBtn.addEventListener("click", (e) => {
            e.preventDefault();
            e.stopPropagation();
            console.log("🔓 Клік на кнопку Відкрити для кімнати:", room.id);
            openRoomDetails(room.id);
          });

          item.append(nameSpan, membersSpan, openBtn);
          roomsList.appendChild(item);
        });
      }
    } catch (error) {
      console.error("❌ Помилка завантаження кімнат", error);
      showError(MESSAGES.LOAD_ERROR);
    }
  };

  const openRoomDetails = async (roomId) => {
    console.log("🔓 Відкриття деталей кімнати ID:", roomId);
    try {
      const response = await fetch(API_ENDPOINTS.ROOM_DETAILS(roomId), {
        method: "GET",
        credentials: "include",
        cache: "no-store",
      });

      console.log("Деталі кімнати - API response status:", response.status);

      if (!response.ok) {
        showError(MESSAGES.LOAD_ERROR);
        return;
      }

      const data = await response.json();
      console.log("📋 Отримані деталі кімнати:", data);

      if (!data.success) {
        showError(data.message || MESSAGES.LOAD_ERROR);
        return;
      }

      const room = data.data;
      console.log("🏠 Відображення деталей кімнати:", room.title);
      displayRoomDetailsModal(room);
    } catch (error) {
      console.error("❌ Помилка завантаження деталей кімнати", error);
      showError(MESSAGES.LOAD_ERROR);
    }
  };

  const displayRoomDetailsModal = (room) => {
    console.log("🎨 Створення модалі деталей кімнати");

    const modal = document.createElement("div");
    modal.className = "modal-overlay";
    modal.id = `room-details-${room.id}`;
    modal.setAttribute("aria-hidden", "false");

    // Встановлюємо важливі стилі для відображення БЕЗ темного фону
    modal.style.cssText = `
      display: flex !important;
      position: fixed !important;
      top: 0 !important;
      left: 0 !important;
      width: 100% !important;
      height: 100% !important;
      background-color: transparent !important;
      align-items: center !important;
      justify-content: center !important;
      z-index: 9999 !important;
    `;

    const membersHtml = (room.members || [])
        .map((m) => `<div class="badge badge-info" style="margin: 4px;">${m.userName}</div>`)
        .join("");

    const tasksHtml = (room.tasks || [])
        .map(
            (task) => `
      <div class="card" style="margin-bottom: 16px; border: 1px solid var(--dojo-border); border-radius: 14px; background: var(--dojo-surface);">
        <div class="card-header" style="padding: 16px; border-bottom: 1px solid var(--dojo-border); display: flex; justify-content: space-between; align-items: center;">
          <h5 style="margin: 0; color: var(--dojo-ink); font-weight: 600;">${task.title}</h5>
          <span class="badge badge-secondary" style="background: var(--dojo-primary); color: white; padding: 6px 12px; border-radius: 8px; font-size: 12px; font-weight: 600;">${task.assignedToUserName}</span>
        </div>
        <div class="card-body" style="padding: 16px;">
          ${task.description ? `<p style="color: var(--dojo-ink); margin-bottom: 12px;">${task.description}</p>` : ""}
          <div class="room-task-comments-section" style="margin-top: 12px; padding-top: 12px; border-top: 1px solid var(--dojo-border);">
            <strong style="color: var(--dojo-ink);">Коментарі:</strong>
            <div style="background: #f9f5f2; padding: 12px; border-radius: 10px; margin: 8px 0; min-height: 30px; max-height: 150px; overflow-y: auto;">
              ${
                task.comments && task.comments.length > 0
                    ? task.comments
                        .map((c) => `<div style="margin: 4px 0; color: var(--dojo-ink); font-size: 14px;"><strong>${c.authorUserName}:</strong> ${c.text}</div>`)
                        .join("")
                    : '<p style="color: var(--dojo-muted); margin: 0; font-size: 14px;">Немає коментарів</p>'
            }
            </div>
            <div class="input-group" style="margin-top: 8px; display: flex; gap: 8px;">
              <input type="text" class="form-field room-comment-input" placeholder="Додайте коментар..." data-task-id="${task.id}" style="flex: 1; padding: 10px 12px; border: 1px solid var(--dojo-border); border-radius: 10px; font-size: 14px;" />
              <button class="btn btn-sm btn-primary room-add-comment-btn" data-task-id="${task.id}" type="button" style="background: var(--dojo-accent); color: white; border: none; padding: 10px 16px; border-radius: 10px; cursor: pointer; font-weight: 600;">Додати</button>
            </div>
          </div>
        </div>
      </div>
    `,
        )
        .join("");

    modal.innerHTML = `
      <div class="modal-content" style="width: 90%; max-width: 800px; max-height: 85vh; overflow-y: auto; background: var(--dojo-surface); border-radius: 20px; box-shadow: var(--dojo-shadow); border: 1px solid var(--dojo-border);">
        <div class="modal-header" style="display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; border-bottom: 1px solid var(--dojo-border); position: sticky; top: 0; background: var(--dojo-surface); border-radius: 20px 20px 0 0; z-index: 1;">
          <h2 style="margin: 0; color: var(--dojo-ink); font-size: 24px; font-weight: 700;">🏠 ${room.title}</h2>
          <button type="button" class="close" style="background: none; border: none; font-size: 28px; cursor: pointer; color: var(--dojo-muted); padding: 0; width: 36px; height: 36px; display: flex; align-items: center; justify-content: center;">×</button>
        </div>
        <div class="modal-body" style="padding: 24px;">
          ${room.description ? `<p style="color: var(--dojo-muted); margin-bottom: 20px; font-size: 15px;"><strong style="color: var(--dojo-ink);">Опис:</strong> ${room.description}</p>` : ""}
          
          <div style="margin-bottom: 28px;">
            <h4 style="color: var(--dojo-ink); margin-bottom: 12px; font-weight: 700;">Учасники (${room.members?.length || 0})</h4>
            <div style="display: flex; flex-wrap: wrap; gap: 8px; margin: 12px 0; min-height: 32px;">
              ${membersHtml || '<p style="color: var(--dojo-muted); margin: 0;">Немає учасників</p>'}
            </div>
            <button class="btn btn-primary room-add-member-btn" data-room-id="${room.id}" style="width: 100%; margin-top: 12px; padding: 12px 16px; background: var(--dojo-accent); color: white; border: none; border-radius: 12px; cursor: pointer; font-weight: 700; transition: background 0.2s ease;" type="button">+ Додати учасника</button>
          </div>
          
          <div style="margin-top: 20px;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
              <h4 style="margin: 0; color: var(--dojo-ink); font-weight: 700;">Завдання (${room.tasks?.length || 0})</h4>
              <button class="btn btn-primary room-add-task-btn" data-room-id="${room.id}" type="button" style="padding: 10px 16px; background: var(--dojo-primary); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 700; white-space: nowrap;">+ Додати завдання</button>
            </div>
            <div style="display: flex; flex-direction: column; gap: 12px;">
              ${tasksHtml || '<p style="color: var(--dojo-muted);">Немає завдань</p>'}
            </div>
          </div>
        </div>
      </div>
    `;

    document.body.appendChild(modal);
    console.log("✅ Модаль деталей додана до DOM, z-index:", modal.style.zIndex);

    const closeBtn = modal.querySelector(".close");
    if (closeBtn) {
      closeBtn.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        console.log("Закриття деталей кімнати");
        modal.remove();
      });
    }

    modal.addEventListener("click", (e) => {
      if (e.target === modal) {
        e.preventDefault();
        e.stopPropagation();
        console.log("Клік за межами модалі - закриття");
        modal.remove();
      }
    });

    // Обробники коментарів
    const commentBtns = modal.querySelectorAll(".room-add-comment-btn");
    commentBtns.forEach((btn) => {
      btn.addEventListener("click", async (e) => {
        e.preventDefault();
        e.stopPropagation();
        const taskId = parseInt(e.target.dataset.taskId, 10);
        const input = modal.querySelector(`[data-task-id="${taskId}"].room-comment-input`);
        const text = input?.value.trim();

        if (!text) {
          showError(MESSAGES.NO_COMMENT_TEXT);
          return;
        }

        try {
          const response = await fetch(API_ENDPOINTS.ADD_COMMENT(taskId), {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
            body: JSON.stringify({ text }),
          });

          if (!response.ok) {
            showError(MESSAGES.ADD_COMMENT_FAILED);
            return;
          }

          const data = await response.json();
          if (data.success) {
            showSuccess("Коментар додано!");
            input.value = "";
            setTimeout(() => {
              modal.remove();
              openRoomDetails(room.id);
            }, TIMEOUTS.MODAL_CLOSE);
          } else {
            showError(data.message || MESSAGES.ADD_COMMENT_FAILED);
          }
        } catch (error) {
          console.error("❌ Помилка додавання коментаря", error);
          showError("Помилка при додаванні коментаря");
        }
      });
    });

    // Обробник кнопки додавання завдання
    const addTaskBtn = modal.querySelector(".room-add-task-btn");
    if (addTaskBtn) {
      addTaskBtn.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        console.log("Клік - додавання завдання до кімнати ID:", room.id);
        showCreateTaskModal(room.id, room.members || []);
      });
    }

    // Обробник кнопки додавання учасника
    const addMemberBtn = modal.querySelector(".room-add-member-btn");
    if (addMemberBtn) {
      addMemberBtn.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        console.log("Клік - додавання учасника до кімнати ID:", room.id);
        showAddMemberModal(room.id);
      });
    }
  };

  const showCreateTaskModal = (roomId, members) => {
    console.log("📝 Відкриття модалі створення завдання");
    const taskModal = document.createElement("div");
    taskModal.className = "modal-overlay";
    taskModal.id = "addTaskModal";
    taskModal.style.cssText = `
      display: flex !important;
      position: fixed !important;
      top: 0 !important;
      left: 0 !important;
      width: 100% !important;
      height: 100% !important;
      background-color: rgba(0, 0, 0, 0.5) !important;
      align-items: center !important;
      justify-content: center !important;
      z-index: 10001 !important;
    `;

    // Переконаємся що members це масив з правильною структурою
    const validMembers = Array.isArray(members) ? members : [];
    const membersOptions = validMembers
        .map((m) => {
          const userId = m.userId || m.UserId || m.id;
          const userName = m.userName || m.UserName || m.name || "Unknown";
          return `<option value="${userId}">${userName}</option>`;
        })
        .join("");

    taskModal.innerHTML = `
      <div class="modal-content" style="width: 90%; max-width: 600px; background: var(--dojo-surface); border-radius: 20px; box-shadow: var(--dojo-shadow); border: 1px solid var(--dojo-border);">
        <div class="modal-header" style="display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; border-bottom: 1px solid var(--dojo-border); border-radius: 20px 20px 0 0;">
          <h2 style="margin: 0; color: var(--dojo-ink); font-size: 20px; font-weight: 700;">📝 Додати завдання</h2>
          <button type="button" class="close" style="background: none; border: none; font-size: 28px; cursor: pointer; color: var(--dojo-muted); padding: 0; width: 36px; height: 36px; display: flex; align-items: center; justify-content: center;">×</button>
        </div>
        <form id="addTaskFormModal" style="padding: 24px;">
          <div class="form-group" style="margin-bottom: 18px;">
            <label for="taskTitleInput" style="display: block; margin-bottom: 8px; font-weight: 600; color: var(--dojo-ink); font-size: 14px;">Назва завдання *</label>
            <input type="text" id="taskTitleInput" class="form-field" maxlength="255" placeholder="Введіть назву" required style="width: 100%; padding: 12px 14px; border: 1px solid var(--dojo-border); border-radius: 12px; font-size: 14px; color: var(--dojo-ink); background: #fffdfa;" />
          </div>
          <div class="form-group" style="margin-bottom: 18px;">
            <label for="taskDescriptionInput" style="display: block; margin-bottom: 8px; font-weight: 600; color: var(--dojo-ink); font-size: 14px;">Опис</label>
            <textarea id="taskDescriptionInput" class="form-field" rows="3" placeholder="Введіть опис" style="width: 100%; padding: 12px 14px; border: 1px solid var(--dojo-border); border-radius: 12px; font-size: 14px; color: var(--dojo-ink); background: #fffdfa; font-family: inherit; resize: vertical;"></textarea>
          </div>
          <div class="form-group" style="margin-bottom: 18px;">
            <label for="taskAssigneeSelect" style="display: block; margin-bottom: 8px; font-weight: 600; color: var(--dojo-ink); font-size: 14px;">Призначити *</label>
            <select id="taskAssigneeSelect" class="form-field" required style="width: 100%; padding: 12px 14px; border: 1px solid var(--dojo-border); border-radius: 12px; font-size: 14px; color: var(--dojo-ink); background: #fffdfa; cursor: pointer;">
              <option value="">Виберіть учасника</option>
              ${membersOptions}
            </select>
          </div>
          <div class="modal-footer" style="display: flex; gap: 12px; justify-content: flex-end; padding-top: 20px; border-top: 1px solid var(--dojo-border);">
            <button type="button" class="btn btn-secondary" id="cancelTaskBtnModal" style="padding: 12px 24px; background: var(--dojo-surface); color: var(--dojo-ink); border: 1px solid var(--dojo-border); border-radius: 12px; cursor: pointer; font-weight: 700; font-size: 14px; transition: background 0.2s ease;">Скасувати</button>
            <button type="submit" class="btn btn-primary" style="padding: 12px 24px; background: var(--dojo-accent); color: white; border: none; border-radius: 12px; cursor: pointer; font-weight: 700; font-size: 14px; transition: background 0.2s ease;">Додати завдання</button>
          </div>
        </form>
      </div>
    `;

    document.body.appendChild(taskModal);
    console.log("✅ Модаль додавання завдання додана до DOM");

    const closeBtn = taskModal.querySelector(".close");
    if (closeBtn) {
      closeBtn.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        console.log("Закриття модалі завдання");
        taskModal.remove();
      });
    }

    const cancelBtn = taskModal.querySelector("#cancelTaskBtnModal");
    if (cancelBtn) {
      cancelBtn.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        console.log("Скасування створення завдання");
        taskModal.remove();
      });
    }

    taskModal.addEventListener("click", (e) => {
      if (e.target === taskModal) {
        e.preventDefault();
        e.stopPropagation();
        console.log("Клік за межами - закриття модалі завдання");
        taskModal.remove();
      }
    });

    const form = taskModal.querySelector("#addTaskFormModal");
    if (form) {
      form.addEventListener("submit", async (e) => {
        e.preventDefault();
        console.log("🎯 Submit форми додавання завдання");

        const titleInput = taskModal.querySelector("#taskTitleInput");
        const descriptionInput = taskModal.querySelector("#taskDescriptionInput");
        const assigneeInput = taskModal.querySelector("#taskAssigneeSelect");

        const title = titleInput ? titleInput.value.trim() : "";
        const description = descriptionInput ? descriptionInput.value.trim() : "";
        const assignedToUserId = parseInt(assigneeInput ? assigneeInput.value : 0, 10);

        if (!title) {
          showError(MESSAGES.NO_TASK_TITLE);
          return;
        }

        if (!assignedToUserId || assignedToUserId <= 0) {
          showError(MESSAGES.NO_TASK_ASSIGNEE);
          return;
        }

        console.log("Отправка завдання: title=", title, "assignedTo=", assignedToUserId, "roomId=", roomId);

        try {
          const response = await fetch(API_ENDPOINTS.ADD_TASK(roomId), {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
            body: JSON.stringify({
              title,
              description: description || null,
              assignedToUserId: assignedToUserId,
              dueDate: null,
            }),
          });

          console.log("Створення завдання - API response status:", response.status);

          if (!response.ok) {
            const errData = await response.json().catch(() => ({}));
            console.error("API Error:", errData);
            showError(errData.message || MESSAGES.ADD_TASK_FAILED);
            return;
          }

          const roomTaskData = await response.json();
          console.log("Відповідь сервера при створенні завдання:", roomTaskData);

          if (!roomTaskData.success) {
            showError(roomTaskData.message || "Помилка при додаванні завдання");
            return;
          }

          showSuccess("Завдання додано!");
          taskModal.remove();
          setTimeout(() => {
            openRoomDetails(roomId);
          }, TIMEOUTS.MODAL_CLOSE);
        } catch (error) {
          console.error("❌ Помилка додавання завдання", error);
          showError("Помилка при додаванні завдання");
        }
      });
    } else {
      console.error("❌ Форма додавання завдання не знайдена!");
    }
  };

  const showAddMemberModal = (roomId) => {
    console.log("👤 Відкриття модалі додавання учасника");
    const memberModal = document.createElement("div");
    memberModal.className = "modal-overlay";
    memberModal.style.cssText = `
    display: flex !important;
    position: fixed !important;
    top: 0 !important;
    left: 0 !important;
    width: 100% !important;
    height: 100% !important;
    background-color: rgba(0, 0, 0, 0.5) !important;
    align-items: center !important;
    justify-content: center !important;
    z-index: 10001 !important;
  `;

    memberModal.innerHTML = `
    <div class="modal-content" style="width: 90%; max-width: 500px; background: var(--dojo-surface); border-radius: 20px; box-shadow: var(--dojo-shadow); border: 1px solid var(--dojo-border);">
      <div class="modal-header" style="display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; border-bottom: 1px solid var(--dojo-border); border-radius: 20px 20px 0 0;">
        <h2 style="margin: 0; color: var(--dojo-ink); font-size: 20px; font-weight: 700;">👤 Додати учасника</h2>
        <button type="button" class="close" style="background: none; border: none; font-size: 28px; cursor: pointer; color: var(--dojo-muted); padding: 0; width: 36px; height: 36px; display: flex; align-items: center; justify-content: center;">×</button>
      </div>
      <form id="addMemberFormModal" style="padding: 24px;">
        <div class="form-group" style="margin-bottom: 24px; position: relative;">
          <label for="memberSearchInput" style="display: block; margin-bottom: 8px; font-weight: 600; color: var(--dojo-ink); font-size: 14px;">Пошук користувача *</label>
          <input type="text" id="memberSearchInput" class="form-field" placeholder="Завантаження..." autocomplete="off" disabled style="width: 100%; padding: 12px 14px; border: 1px solid var(--dojo-border); border-radius: 12px; font-size: 14px; color: var(--dojo-ink); background: #fffdfa;" />
          <div id="memberSuggestions" style="position: absolute; top: 100%; left: 0; right: 0; background: var(--dojo-surface); border: 1px solid var(--dojo-border); border-top: none; border-radius: 0 0 12px 12px; max-height: 200px; overflow-y: auto; z-index: 1000; display: none; margin-top: -1px;"></div>
        </div>
        <input type="hidden" id="memberSelectedUserId" value="" />
        <div id="memberSelectedDisplay" style="margin-bottom: 16px; padding: 12px 14px; background: var(--dojo-border); border-radius: 12px; display: none; color: var(--dojo-ink); font-weight: 500;">
          Вибрано: <span id="memberSelectedName"></span>
        </div>
        <div class="modal-footer" style="display: flex; gap: 12px; justify-content: flex-end; padding-top: 20px; border-top: 1px solid var(--dojo-border);">
          <button type="button" class="btn btn-secondary" id="cancelMemberBtnModal" style="padding: 12px 24px; background: var(--dojo-surface); color: var(--dojo-ink); border: 1px solid var(--dojo-border); border-radius: 12px; cursor: pointer; font-weight: 700; font-size: 14px; transition: background 0.2s ease;">Скасувати</button>
          <button type="submit" class="btn btn-primary" style="padding: 12px 24px; background: var(--dojo-accent); color: white; border: none; border-radius: 12px; cursor: pointer; font-weight: 700; font-size: 14px; transition: background 0.2s ease;">Додати</button>
        </div>
      </form>
    </div>
  `;

    document.body.appendChild(memberModal);
    console.log("✅ Модаль додавання учасника додана до DOM");

    let allFriends = [];
    let suggestionTimeoutId = null;

    const loadFriendsForSearch = async () => {
      console.log("📥 Завантаження списку друзів для пошуку");
      const searchInput = memberModal.querySelector("#memberSearchInput");
      try {
        const response = await fetch(API_ENDPOINTS.FRIENDS, {
          method: "GET",
          credentials: "include",
          cache: "no-store",
        });

        if (!response.ok) {
          showError("Не вдалося завантажити друзів");
          if (searchInput) {
            searchInput.placeholder = "Помилка завантаження";
          }
          return;
        }

        const data = await response.json();
        allFriends = Array.isArray(data.data) ? data.data : [];
        console.log(`✅ Друзів завантажено: ${allFriends.length}`);
        console.log("Повні дані друзів:", allFriends);

        allFriends.forEach(f => {
          const userName = f.userName || f.UserName || f.name || "Unknown";
          const userId = f.userId || f.UserId || f.id || "No ID";
          console.log(`👤 Ім'я: "${userName}" | ID: ${userId}`);
        });

        if (searchInput) {
          searchInput.disabled = false;
          searchInput.placeholder = "Введіть ім'я користувача";
        }
      } catch (error) {
        console.error("❌ Помилка завантаження друзів", error);
        showError("Помилка при завантаженні друзів");
        if (searchInput) {
          searchInput.placeholder = "Помилка завантаження";
        }
      }
    };

    const searchInput = memberModal.querySelector("#memberSearchInput");
    const suggestionsDiv = memberModal.querySelector("#memberSuggestions");

    if (searchInput) {
      searchInput.addEventListener("input", (e) => {
        clearTimeout(suggestionTimeoutId);
        const query = e.target.value.trim();

        console.log("🔍 Пошук:", query, "Друзів доступно:", allFriends.length);

        if (query.length < 1) {
          suggestionsDiv.style.display = "none";
          return;
        }

        if (allFriends.length === 0) {
          suggestionsDiv.innerHTML = '<div style="padding: 12px 14px; color: var(--dojo-muted);">Немає доданих друзів</div>';
          suggestionsDiv.style.display = "block";
          return;
        }

        suggestionTimeoutId = setTimeout(() => {
          const queryLower = query.toLowerCase();
          const filtered = allFriends.filter((friend) => {
            const userName = (friend.userName || friend.UserName || friend.name || "").toLowerCase();
            console.log(`Порівняння: "${userName}" містить "${queryLower}" = ${userName.includes(queryLower)}`);
            return userName.includes(queryLower);
          });

          console.log(`Результат фільтрації: ${filtered.length} знайдено`);

          if (filtered.length === 0) {
            suggestionsDiv.innerHTML = '<div style="padding: 12px 14px; color: var(--dojo-muted);">Користувачі не знайдені</div>';
            suggestionsDiv.style.display = "block";
            return;
          }

          suggestionsDiv.innerHTML = filtered
              .map((friend) => {
                const userId = friend.userId || friend.UserId || friend.id;
                const userName = friend.userName || friend.UserName || friend.name || "Unknown";
                return `
              <div class="suggestion-item" data-user-id="${userId}" data-user-name="${userName}" style="padding: 12px 14px; cursor: pointer; border-bottom: 1px solid var(--dojo-border); transition: background 0.2s ease;">
                ${userName}
              </div>
            `;
              })
              .join("");

          suggestionsDiv.style.display = "block";

          suggestionsDiv.querySelectorAll(".suggestion-item").forEach((item) => {
            item.addEventListener("mouseover", () => {
              item.style.background = "var(--dojo-border)";
            });
            item.addEventListener("mouseout", () => {
              item.style.background = "transparent";
            });
            item.addEventListener("click", (e) => {
              e.preventDefault();
              e.stopPropagation();
              const userId = item.dataset.userId;
              const userName = item.dataset.userName;

              memberModal.querySelector("#memberSelectedUserId").value = userId;
              memberModal.querySelector("#memberSelectedName").textContent = userName;
              memberModal.querySelector("#memberSelectedDisplay").style.display = "block";
              searchInput.value = "";
              suggestionsDiv.style.display = "none";
              console.log("✅ Вибрано користувача:", userName, "ID:", userId);
            });
          });
        }, 300);
      });

      document.addEventListener("click", (e) => {
        if (!memberModal.contains(e.target)) {
          suggestionsDiv.style.display = "none";
        }
      });
    }

    const closeBtn = memberModal.querySelector(".close");
    if (closeBtn) {
      closeBtn.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        memberModal.remove();
      });
    }

    const cancelBtn = memberModal.querySelector("#cancelMemberBtnModal");
    if (cancelBtn) {
      cancelBtn.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        memberModal.remove();
      });
    }

    memberModal.addEventListener("click", (e) => {
      if (e.target === memberModal) {
        e.preventDefault();
        e.stopPropagation();
        memberModal.remove();
      }
    });

    const form = memberModal.querySelector("#addMemberFormModal");
    if (form) {
      form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const userId = parseInt(memberModal.querySelector("#memberSelectedUserId").value, 10);

        if (!userId || userId <= 0) {
          showError(MESSAGES.NO_MEMBER_SELECTED);
          return;
        }

        try {
          const response = await fetch(API_ENDPOINTS.ADD_MEMBER(roomId), {
            method: "POST",
            credentials: "include",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify({ userId }),
          });

          if (!response.ok) {
            showError(MESSAGES.ADD_MEMBER_FAILED);
            return;
          }

          const data = await response.json();
          if (data.success) {
            showSuccess("Учасника додано!");
            memberModal.remove();
            setTimeout(() => {
              location.reload();
            }, TIMEOUTS.PAGE_RELOAD);
          } else {
            showError(data.message || MESSAGES.ADD_MEMBER_FAILED);
          }
        } catch (error) {
          console.error("❌ Помилка додавання учасника", error);
          showError("Помилка при додаванні учасника");
        }
      });
    }

    loadFriendsForSearch();
  };




  // Ініціалізація обробників подій
  if (openCreateRoomBtn) {
    openCreateRoomBtn.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      console.log("Клік на кнопку 'Створити кімнату'");
      openCreateRoomModal();
    });
  }

  if (closeRoomsModalBtn) {
    closeRoomsModalBtn.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      console.log("Клік на кнопку закриття - закриття модалі створення");
      closeCreateRoomModal();
    });
  }

  if (openMyRoomsBtn) {
    openMyRoomsBtn.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      console.log("Клік на кнопку 'Мої кімнати'");
      openRoomsListModal();
    });
  }

  if (openCreateRoomFromFriendsBtn) {
    openCreateRoomFromFriendsBtn.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      console.log("Клік на кнопку 'Створити кімнату' (з друзів)");
      openCreateRoomModal();
    });
  }

  if (openMyRoomsFromFriendsBtn) {
    openMyRoomsFromFriendsBtn.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      console.log("Клік на кнопку 'Мої кімнати' (з друзів)");
      openRoomsListModal();
    });
  }

  if (closeRoomsListModalBtn) {
    closeRoomsListModalBtn.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      console.log("Клік на кнопку закриття - закриття модалі списку");
      closeRoomsListModal();
    });
  }

  // Обробник форми створення кімнати
  if (createRoomForm) {
    console.log("✅ Форма створення кімнати знайдена, додаються слухачі подій");

    createRoomForm.addEventListener("submit", async (e) => {
      e.preventDefault();
      console.log("📝 Submit форми створення кімнати");

      const titleInput = document.getElementById("roomTitle");
      const descriptionInput = document.getElementById("roomDescription");

      if (!titleInput) {
        showError("Поле 'Назва' не знайдено");
        return;
      }

      const title = titleInput.value.trim();
      const description = descriptionInput ? descriptionInput.value.trim() : "";

      if (!title) {
        showError(MESSAGES.EMPTY_INPUT);
        return;
      }

      console.log("Отправка даних: title=", title, "description=", description);

      try {
        const response = await fetch("/api/rooms/create", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            title,
            description: description || null,
            memberUserIds: [],
          }),
        });

        console.log("API response status:", response.status);

        if (!response.ok) {
          const errData = await response.json().catch(() => ({}));
          console.error("API Error:", errData);
          showError(errData.message || MESSAGES.CREATE_ERROR);
          return;
        }

        const data = await response.json();
        console.log("API response data:", data);

        if (!data.success) {
          showError(data.message || MESSAGES.CREATE_ERROR);
          return;
        }

        showSuccess("Кімнату створено!");
        createRoomForm.reset();
        closeCreateRoomModal();

        // Завантажуємо оновлений список кімнат
        setTimeout(() => {
          openRoomsListModal();
        }, TIMEOUTS.PAGE_RELOAD);
      } catch (error) {
        console.error("❌ Помилка створення кімнати", error);
        showError(MESSAGES.CREATE_ERROR);
      }
    });
  } else {
    console.warn("⚠️ Форма створення кімнати не знайдена!");
  }

  // Закриття модалей при кліку поза ними
  if (roomsModal) {
    roomsModal.addEventListener("click", (e) => {
      if (e.target === roomsModal) {
        e.preventDefault();
        e.stopPropagation();
        closeCreateRoomModal();
      }
    });
  }

  if (roomsListModal) {
    roomsListModal.addEventListener("click", (e) => {
      if (e.target === roomsListModal) {
        e.preventDefault();
        e.stopPropagation();
        closeRoomsListModal();
      }
    });
  }

  console.log("✅ Усі обробники подій зареєстровані");

  // Завантаження кімнат при відкритті модалі
  const urlParams = new URLSearchParams(window.location.search);
  const roomIdParam = urlParams.get("roomId");
  if (roomIdParam) {
    const roomId = parseInt(roomIdParam, 10);
    if (!isNaN(roomId)) {
      console.log("🔓 Завантаження деталей кімнати за запитом (roomId з URL):", roomId);
      openRoomDetails(roomId);
    }
  }
})();
