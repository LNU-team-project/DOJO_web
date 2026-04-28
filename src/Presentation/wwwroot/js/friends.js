(() => {
  const friendsModal = document.getElementById("friendsModal");
  const openFriendsModalBtn = document.getElementById("openFriendsModal");
  const closeFriendsModalBtn = document.getElementById("closeFriendsModal");
  const friendsList = document.getElementById("friendsList");
  const friendsAddForm = document.getElementById("friendsAddForm");
  const friendUserNameInput = document.getElementById("friendUserName");
  const friendUserIdInput = document.getElementById("friendUserId");

  if (
    !friendsModal ||
    !openFriendsModalBtn ||
    !friendsList ||
    !friendsAddForm
  ) {
    return;
  }

  const MESSAGES = {
    LOAD_ERROR: "Не вдалося завантажити список друзів",
    ADD_ERROR: "Не вдалося додати друга",
    REMOVE_ERROR: "Не вдалося видалити друга",
    EMPTY_INPUT: "Вкажіть ім'я або ID користувача",
  };

  const showError = (message) => {
    const errorDiv = document.createElement("div");
    errorDiv.className = "alert alert-error";
    errorDiv.setAttribute("role", "alert");
    errorDiv.textContent = `❌ Помилка: ${message}`;
    document.body.insertBefore(errorDiv, document.body.firstChild);
    setTimeout(() => errorDiv.remove(), 5000);
  };

  const openModal = () => {
    friendsModal.style.display = "flex";
    friendsModal.setAttribute("aria-hidden", "false");
    loadFriends();
  };

  const closeModal = () => {
    friendsModal.style.display = "none";
    friendsModal.setAttribute("aria-hidden", "true");
  };

  const renderEmptyState = () => {
    friendsList.innerHTML =
      '<p class="todo-empty-message">Поки немає друзів</p>';
  };

  const renderFriendItem = (friend) => {
    const item = document.createElement("div");
    item.className = "friends-list-item";

    const avatar = document.createElement("img");
    avatar.className = "friends-avatar";
    avatar.alt = "Аватар друга";
    avatar.src =
      friend.avatarUrl ||
      "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='%23ff7a90'%3E%3Cpath d='M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z'/%3E%3C/svg%3E";

    const info = document.createElement("div");
    info.className = "friends-info";

    const name = document.createElement("div");
    name.className = "friends-name";
    name.textContent = friend.friendUserName || "Користувач";

    const meta = document.createElement("div");
    meta.className = "friends-meta";
    meta.textContent = `ID: ${friend.friendUserId}`;

    info.append(name, meta);

    const removeBtn = document.createElement("button");
    removeBtn.type = "button";
    removeBtn.className = "btn-secondary friends-remove-btn";
    removeBtn.textContent = "Видалити";
    removeBtn.addEventListener("click", async () => {
      await removeFriend(friend.friendUserId);
    });

    item.append(avatar, info, removeBtn);
    return item;
  };

  const renderFriends = (friends) => {
    if (!Array.isArray(friends) || friends.length === 0) {
      renderEmptyState();
      return;
    }

    friendsList.innerHTML = "";
    friends.forEach((friend) => {
      friendsList.appendChild(renderFriendItem(friend));
    });
  };

  const loadFriends = async () => {
    try {
      const response = await fetch("/api/friends", { method: "GET" });
      if (!response.ok) {
        showError(MESSAGES.LOAD_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.LOAD_ERROR);
        return;
      }

      renderFriends(data.data || []);
    } catch (error) {
      console.error("Помилка завантаження друзів", error);
      showError(MESSAGES.LOAD_ERROR);
    }
  };

  const addFriend = async (payload) => {
    try {
      const response = await fetch("/api/friends/add", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.ADD_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.ADD_ERROR);
        return;
      }

      if (friendUserNameInput) friendUserNameInput.value = "";
      if (friendUserIdInput) friendUserIdInput.value = "";
      await loadFriends();
    } catch (error) {
      console.error("Помилка додавання друга", error);
      showError(MESSAGES.ADD_ERROR);
    }
  };

  const removeFriend = async (friendUserId) => {
    try {
      const response = await fetch(`/api/friends/remove/${friendUserId}`, {
        method: "DELETE",
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        showError(errorData.message || MESSAGES.REMOVE_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.REMOVE_ERROR);
        return;
      }

      await loadFriends();
    } catch (error) {
      console.error("Помилка видалення друга", error);
      showError(MESSAGES.REMOVE_ERROR);
    }
  };

  friendsAddForm.addEventListener("submit", (event) => {
    event.preventDefault();
    const friendUserName = friendUserNameInput?.value.trim() || "";
    const friendUserId = Number.parseInt(friendUserIdInput?.value || "0", 10);

    if (!friendUserName && friendUserId <= 0) {
      showError(MESSAGES.EMPTY_INPUT);
      return;
    }

    const payload = friendUserName ? { friendUserName } : { friendUserId };

    void addFriend(payload);
  });

  openFriendsModalBtn.addEventListener("click", openModal);
  closeFriendsModalBtn?.addEventListener("click", closeModal);

  friendsModal.addEventListener("click", (event) => {
    if (event.target === friendsModal) {
      closeModal();
    }
  });
})();
