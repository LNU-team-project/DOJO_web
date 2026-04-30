(() => {
  const friendsModal = document.getElementById("friendsModal");
  const openFriendsModalBtn = document.getElementById("openFriendsModal");
  const closeFriendsModalBtn = document.getElementById("closeFriendsModal");
  const friendsList = document.getElementById("friendsList");
  const friendsAddForm = document.getElementById("friendsAddForm");
  const friendUserNameInput = document.getElementById("friendUserName");
  const friendSuggestions = document.getElementById("friendSuggestions");

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
    EMPTY_INPUT: "Вкажіть ім'я користувача",
  };

  let suggestionTimeoutId = null;

  const showError = (message) => {
    const errorDiv = document.createElement("div");
    errorDiv.className = "alert alert-error";
    errorDiv.setAttribute("role", "alert");
    errorDiv.textContent = `❌ Помилка: ${message}`;
    document.body.insertBefore(errorDiv, document.body.firstChild);
    globalThis.setTimeout(() => errorDiv.remove(), 5000);
  };

  const renderEmptyState = () => {
    friendsList.innerHTML =
        '<li class="leaderboard-empty">Поки немає друзів</li>';
  };

  const renderSuggestionsEmptyState = () => {
    if (!friendSuggestions) {
      return;
    }

    friendSuggestions.innerHTML = "";
    friendSuggestions.classList.remove("show");
  };

  const openModal = () => {
    friendsModal.style.display = "flex";
    friendsModal.setAttribute("aria-hidden", "false");
    renderSuggestionsEmptyState();
    void loadFriends();
  };

  const closeModal = () => {
    friendsModal.style.display = "none";
    friendsModal.setAttribute("aria-hidden", "true");
    renderSuggestionsEmptyState();
  };

  const loadFriends = async () => {
    try {
      const response = await fetch("/api/friends", {
        method: "GET",
        credentials: "include",
        cache: "no-store",
      });
      if (!response.ok) {
        showError(MESSAGES.LOAD_ERROR);
        return;
      }

      const data = await response.json();
      if (!data.success) {
        showError(data.message || MESSAGES.LOAD_ERROR);
        return;
      }

      const friends = Array.isArray(data.data) ? data.data : [];
      if (friends.length === 0) {
        renderEmptyState();
        return;
      }

      friendsList.innerHTML = "";
      friends.forEach((friend) => {
        const item = document.createElement("li");
        item.className = "leaderboard-item friends-list-item";

        const avatar = document.createElement("img");
        avatar.className = "leaderboard-avatar friends-avatar";
        avatar.alt = "Аватар друга";
        avatar.src =
            friend.avatarUrl ||
            "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='%23ff7a90'%3E%3Cpath d='M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z'/%3E%3C/svg%3E";

        const name = document.createElement("span");
        name.className = "leaderboard-name friends-name";
        name.textContent = friend.friendUserName || "Користувач";

        const removeBtn = document.createElement("button");
        removeBtn.type = "button";
        removeBtn.className = "btn-secondary friends-remove-btn";
        removeBtn.textContent = "Видалити";
        removeBtn.addEventListener("click", async () => {
          await removeFriend(friend.friendUserId);
        });

        item.append(avatar, name, removeBtn);
        friendsList.appendChild(item);
      });
    } catch (error) {
      console.error("Помилка завантаження друзів", error);
      showError(MESSAGES.LOAD_ERROR);
    }
  };

  const addFriend = async (friendUserName) => {
    try {
      const response = await fetch("/api/friends/add", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ friendUserName }),
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

      if (friendUserNameInput) {
        friendUserNameInput.value = "";
      }
      renderSuggestionsEmptyState();
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
        credentials: "include",
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

  const fetchSuggestions = async (query) => {
    if (!friendSuggestions) {
      return;
    }

    const trimmedQuery = query.trim();
    if (trimmedQuery.length < 2) {
      renderSuggestionsEmptyState();
      return;
    }

    try {
      const response = await fetch(
          `/api/friends/search?query=${encodeURIComponent(trimmedQuery)}&limit=5`,
          {
            method: "GET",
            credentials: "include",
          },
      );

      if (!response.ok) {
        renderSuggestionsEmptyState();
        return;
      }

      const data = await response.json();
      if (
          !data.success ||
          !Array.isArray(data.data) ||
          data.data.length === 0
      ) {
        renderSuggestionsEmptyState();
        return;
      }

      friendSuggestions.innerHTML = "";
      data.data.forEach((user) => {
        const item = document.createElement("li");
        item.className = "friends-suggestion-item";

        const avatar = document.createElement("img");
        avatar.className = "friends-suggestion-avatar";
        avatar.alt = "Аватар користувача";
        avatar.src =
            user.avatarUrl ||
            "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='%23ff7a90'%3E%3Cpath d='M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z'/%3E%3C/svg%3E";

        const name = document.createElement("span");
        name.className = "friends-suggestion-name";
        name.textContent = user.userName || "Користувач";

        item.append(avatar, name);
        item.addEventListener("click", () => {
          if (friendUserNameInput) {
            friendUserNameInput.value = user.userName || "";
            friendUserNameInput.focus();
          }
          renderSuggestionsEmptyState();
        });

        friendSuggestions.appendChild(item);
      });

      friendSuggestions.classList.add("show");
    } catch (error) {
      console.error("Помилка пошуку користувачів", error);
      renderSuggestionsEmptyState();
    }
  };

  const handleAddSubmit = (event) => {
    event.preventDefault();
    const friendUserName = friendUserNameInput?.value.trim() || "";

    if (!friendUserName) {
      showError(MESSAGES.EMPTY_INPUT);
      return;
    }

    void addFriend(friendUserName);
  };

  const handleSuggestionInput = (event) => {
    const value = event.target.value;
    if (suggestionTimeoutId !== null) {
      globalThis.clearTimeout(suggestionTimeoutId);
    }

    suggestionTimeoutId = globalThis.setTimeout(() => {
      void fetchSuggestions(value);
    }, 250);
  };

  const handleOutsideClick = (event) => {
    if (!friendSuggestions || !friendUserNameInput) {
      return;
    }

    const target = event.target;
    const clickedInsideSuggestions = friendSuggestions.contains(target);
    const clickedInput = friendUserNameInput.contains(target);

    if (!clickedInsideSuggestions && !clickedInput) {
      renderSuggestionsEmptyState();
    }
  };

  openFriendsModalBtn.addEventListener("click", () => {
    openModal();
  });

  closeFriendsModalBtn?.addEventListener("click", () => {
    closeModal();
  });

  friendsModal.addEventListener("click", (event) => {
    if (event.target === friendsModal) {
      closeModal();
    }
  });

  friendsAddForm.addEventListener("submit", handleAddSubmit);
  friendUserNameInput?.addEventListener("input", handleSuggestionInput);
  document.addEventListener("click", handleOutsideClick);
})();
