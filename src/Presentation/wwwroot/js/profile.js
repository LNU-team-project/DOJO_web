(() => {
  console.log("✅ Profile.js завантажився успішно!");

  const API_BASE = "/api/profile";

  // DOM Elements
  const profileModal = document.getElementById("profileModal");
  const profileModalOverlay = document.getElementById("profileModalOverlay");
  const openProfileModalBtn = document.getElementById("openProfileModal");
  const closeProfileModalBtn = document.getElementById("closeProfileModal");
  const profileLogoutBtn = document.getElementById("profileLogoutBtn");
  const profileSettingsBtn = document.getElementById("profileSettingsBtn");
  const profileAvatarUploadBtn = document.getElementById(
    "profileAvatarUploadBtn",
  );
  const avatarFileInput = document.getElementById("avatarFileInput");

  // Profile display elements
  const profileUsername = document.getElementById("profileUsername");
  const profileAvatarImg = document.getElementById("profileAvatarImg");
  const profileDisplayUsername = document.getElementById(
    "profileDisplayUsername",
  );
  const profileDisplayEmail = document.getElementById("profileDisplayEmail");
  const profileDisplayLevel = document.getElementById("profileDisplayLevel");
  const profileDisplayExpPoints = document.getElementById(
    "profileDisplayExpPoints",
  );
  const profileDisplayStreak = document.getElementById("profileDisplayStreak");
  const profileModalAvatar = document.getElementById("profileModalAvatar");

  // Settings modal elements
  const settingsModal = document.getElementById("profileSettingsModal");
  const settingsModalOverlay = document.getElementById(
    "profileSettingsModalOverlay",
  );
  const closeSettingsModalBtn = document.getElementById(
    "closeProfileSettingsModal",
  );
  const settingsUserNameInput = document.getElementById("settingsUserName");
  const settingsSaveUserNameBtn = document.getElementById("saveUserNameBtn");
  const settingsAvatarInput = document.getElementById(
    "settingsAvatarFileInput",
  );
  const settingsAvatarUploadBtn = document.getElementById(
    "settingsAvatarUploadBtn",
  );
  const settingsResetPasswordBtn = document.getElementById(
    "settingsResetPasswordBtn",
  );
  const settingsDeleteAccountBtn = document.getElementById(
    "settingsDeleteAccountBtn",
  );
  const settingsEditUserNameBtn = document.getElementById("editUserNameBtn");
  const closeSettingsModalFooterBtn = document.getElementById(
    "closeProfileSettingsModalFooter",
  );
  const settingsAvatarImg = document.getElementById("profileSettingsAvatar");
  const emailConfirmStatus = document.getElementById("emailConfirmStatus");
  const sendEmailConfirmationBtn = document.getElementById(
    "sendEmailConfirmationBtn",
  );

  console.log("🔍 DOM Elements статус:", {
    profileModal: !!profileModal,
    openProfileModalBtn: !!openProfileModalBtn,
    profileLogoutBtn: !!profileLogoutBtn,
    profileAvatarUploadBtn: !!profileAvatarUploadBtn,
  });

  if (!profileModal || !openProfileModalBtn || !profileLogoutBtn) {
    console.error("❌ ПОМИЛКА: Не знайдено потрібні DOM елементи!");
    console.log("Перевіри чи в Dashboard.cshtml є все потрібне!");
    return;
  }

  if (!settingsModal || !settingsModalOverlay) {
    console.error("❌ ПОМИЛКА: Не знайдено DOM елементи модалки налаштувань!");
  }

  console.log("✅ Всі основні DOM елементи знайдені, ініціалізуємо...");

  /**
   * Відкриває модальне вікно профіля
   */
  const openProfileModal = () => {
    console.log("🔓 Відкриваємо модаль профіля");
    profileModal.classList.add("show");
    profileModal.setAttribute("aria-hidden", "false");
    loadUserProfile();
  };

  /**
   * Закриває модальне вікно профіля
   */
  const closeProfileModal = () => {
    console.log("🔒 Закриваємо модаль профіля");
    profileModal.classList.remove("show");
    profileModal.setAttribute("aria-hidden", "true");
  };

  /**
   * Відкриває модальне вікно налаштувань профіля
   */
  const openSettingsModal = () => {
    console.log("🛠️ Відкриваємо модаль налаштувань");
    settingsModal.classList.add("show");
    settingsModal.setAttribute("aria-hidden", "false");
    if (settingsUserNameInput && profileDisplayUsername?.textContent) {
      settingsUserNameInput.value = profileDisplayUsername.textContent;
      settingsUserNameInput.readOnly = true;
    }
    if (settingsAvatarImg && profileModalAvatar?.src) {
      settingsAvatarImg.src = profileModalAvatar.src;
    }
    if (settingsSaveUserNameBtn) {
      settingsSaveUserNameBtn.style.display = "none";
    }
  };

  /**
   * Закриває модальне вікно налаштувань профіля
   */
  const closeSettingsModal = () => {
    settingsModal.classList.remove("show");
    settingsModal.setAttribute("aria-hidden", "true");
  };

  /**
   * Показує повідомлення про помилку
   */
  const showError = (message) => {
    console.error("❌ Помилка: " + message);
    alert("❌ Помилка:\n" + message);
  };

  /**
   * Показує повідомлення про успіх
   */
  const showSuccess = (message) => {
    console.log("✅ Успіх: " + message);
  };

  const navigateTo = (url) => {
    globalThis.location.href = url;
  };

  const setTextContent = (element, value) => {
    if (element) {
      element.textContent = value;
    }
  };

  const setImageSource = (image, source) => {
    if (image && source) {
      image.src = source;
    }
  };

  const bindEventIfPresent = (element, eventName, handler, logMessage) => {
    if (!element) {
      return;
    }

    element.addEventListener(eventName, handler);
    if (logMessage) {
      console.log(logMessage);
    }
  };

  /**
   * Завантажує профіль користувача
   */
  const loadUserProfile = async () => {
    console.log("📥 Завантажуємо профіль...");
    try {
      const response = await fetch(`${API_BASE}/me`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
      });

      console.log("📡 Відповідь з сервера:", response.status);

      if (!response.ok) {
        if (response.status === 401) {
          console.error("❌ Не авторизовано, перенаправляємо на логін");
          navigateTo("/Account/Login");
          return;
        }
        throw new Error(`HTTP ${response.status}`);
      }

      const result = await response.json();
      console.log("📦 Отримані дані:", result);

      if (result.success && result.data) {
        displayUserProfile(result.data);
      } else {
        showError(result.message || "Не вдалося завантажити профіль");
      }
    } catch (error) {
      console.error("❌ Помилка при завантаженні профіля:", error);
      showError("Помилка при завантаженні профіля: " + error.message);
    }
  };

  /**
   * Виводить дані профіля користувача на екран
   */
  const displayUserProfile = (profile) => {
    console.log("🎨 Виводимо профіль:", profile);

    const textMappings = [
      [profileUsername, profile.userName || "Користувач"],
      [profileDisplayUsername, profile.userName || "-"],
      [profileDisplayEmail, profile.email || "-"],
      [profileDisplayLevel, `${profile.level || 1} 🎖️`],
      [profileDisplayExpPoints, `${profile.expPoints || 0} ⭐`],
      [profileDisplayStreak, `${profile.currentStreak || 0} 🔥`],
    ];

    textMappings.forEach(([element, value]) => {
      setTextContent(element, value);
    });

    // Email статус
    if (emailConfirmStatus) {
      if (profile.emailConfirmed) {
        emailConfirmStatus.textContent = "Пошту підтверджено";
        emailConfirmStatus.style.color = "green";
      } else {
        emailConfirmStatus.textContent = "Непідтверджено";
        emailConfirmStatus.style.color = "";
      }
    }

    // Оновлення аватара (якщо він збережено)
    if (profile.avatarUrl) {
      console.log("🖼️ Встановлюємо аватар:", profile.avatarUrl);
      [profileAvatarImg, profileModalAvatar, settingsAvatarImg].forEach(
        (image) => {
          setImageSource(image, profile.avatarUrl);
        },
      );
    }
  };

  /**
   * Завантажує новий аватар
   */
  const handleAvatarUpload = async (file) => {
    console.log("📤 Завантажуємо аватар:", file.name, file.size, file.type);

    if (!file) {
      return;
    }

    // Перевірка типу файлу
    const allowedTypes = ["image/jpeg", "image/png", "image/webp"];
    if (!allowedTypes.includes(file.type)) {
      showError("Дозволені тільки jpg, png та webp формати");
      return;
    }

    // Перевірка розміру файлу (5MB)
    const maxSize = 5 * 1024 * 1024;
    if (file.size > maxSize) {
      showError("Розмір файлу не може перевищувати 5MB");
      return;
    }

    // 🎨 ПОКАЗУЄМО ПОПЕРЕДНІЙ ПЕРЕГЛЯД АВАТАРА ОДРАЗУ!
    const fileReader = new FileReader();
    fileReader.onload = (e) => {
      const imageUrl = e.target.result;
      console.log("🖼️ Показуємо попередній перегляд");

      // Оновлюємо аватар в модальному вікні одразу (ДО завантаження на сервер)
      if (profileModalAvatar) {
        profileModalAvatar.src = imageUrl;
      }

      // Оновлюємо аватар в кружочку профіля одразу (ДО завантаження на сервер)
      if (profileAvatarImg) {
        profileAvatarImg.src = imageUrl;
      }
    };
    fileReader.readAsDataURL(file);

    try {
      const formData = new FormData();
      formData.append("avatar", file);

      console.log("📡 Відправляємо файл на сервер...");
      const response = await fetch(`${API_BASE}/avatar`, {
        method: "POST",
        body: formData,
        credentials: "include",
      });

      console.log("📡 Статус відповіді:", response.status);

      if (!response.ok) {
        if (response.status === 401) {
          console.error("❌ Не авторизовано");
          navigateTo("/Account/Login");
          return;
        }
        throw new Error(`HTTP ${response.status}`);
      }

      const result = await response.json();
      console.log("📦 Результат:", result);

      if (result.success) {
        showSuccess("Аватар успішно змінено");
        await loadUserProfile(); // підтягнути актуальний avatarUrl з бекенду і зберегти відображення між сесіями
      } else {
        showError(result.message || "Не вдалося завантажити аватар");
        loadUserProfile();
      }
    } catch (error) {
      console.error("❌ Помилка при завантаженні аватара:", error);
      showError("Помилка при завантаженні аватара: " + error.message);
      loadUserProfile();
    }

    // Очищення інпута файлу
    avatarFileInput.value = "";
  };

  /**
   * Виконує вихід користувача
   */
  const handleLogout = async () => {
    if (!confirm("Ви впевнені, що хочете вийти?")) {
      return;
    }

    console.log("🚪 Виконуємо вихід...");

    try {
      const response = await fetch("/Account/Logout", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      console.log("✅ Вихід успішно виконаний");
      navigateTo("/Account/Register");
    } catch (error) {
      console.error("❌ Помилка при виході:", error);
      showError("Помилка при виході з системи: " + error.message);
    }
  };

  /**
   * Оновлює ім\'я користувача
   */
  const updateUserName = async () => {
    const newName = settingsUserNameInput?.value?.trim();
    if (!newName) {
      showError("Ім'я користувача не може бути порожнім");
      return;
    }

    try {
      const response = await fetch(`${API_BASE}/settings/username`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ userName: newName }),
      });

      if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(err.message || `HTTP ${response.status}`);
      }

      showSuccess("Ім'я користувача оновлено");
      await loadUserProfile();
      settingsUserNameInput.readOnly = true;
      if (settingsSaveUserNameBtn) {
        settingsSaveUserNameBtn.style.display = "none";
      }
      closeSettingsModal();
    } catch (error) {
      showError(error.message);
    }
  };

  /**
   * Завантажує аватар з модалки налаштувань
   */
  const updateAvatarFromSettings = async () => {
    const file = settingsAvatarInput?.files?.[0];
    if (!file) {
      showError("Оберіть файл аватара");
      return;
    }

    const allowedTypes = ["image/jpeg", "image/png", "image/webp"];
    if (!allowedTypes.includes(file.type)) {
      showError("Дозволені тільки jpg, png та webp формати");
      return;
    }

    const maxSize = 5 * 1024 * 1024;
    if (file.size > maxSize) {
      showError("Розмір файлу не може перевищувати 5MB");
      return;
    }

    // Попередній перегляд у модалці налаштувань та в шапці/профілі
    const reader = new FileReader();
    reader.onload = (e) => {
      const imageUrl = e.target?.result;
      if (!imageUrl) return;
      if (settingsAvatarImg) settingsAvatarImg.src = imageUrl;
      if (profileModalAvatar) profileModalAvatar.src = imageUrl;
      if (profileAvatarImg) profileAvatarImg.src = imageUrl;
    };
    reader.readAsDataURL(file);

    try {
      const formData = new FormData();
      formData.append("avatar", file);

      const response = await fetch(`${API_BASE}/settings/avatar`, {
        method: "POST",
        body: formData,
        credentials: "include",
      });

      if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(err.message || `HTTP ${response.status}`);
      }

      showSuccess("Аватар оновлено");
      await loadUserProfile();
    } catch (error) {
      showError(error.message);
    }
  };

  /**
   * Відправляє посилання для скидання паролю
   */
  const sendPasswordResetLink = async () => {
    try {
      const response = await fetch(`${API_BASE}/settings/password-reset-link`, {
        method: "POST",
        credentials: "include",
      });

      if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(err.message || `HTTP ${response.status}`);
      }

      const data = await response.json();
      if (data.resetUrl) {
        navigateTo(data.resetUrl);
        return;
      }

      showSuccess("Посилання для скидання паролю надіслано");
    } catch (error) {
      showError(error.message);
    }
  };

  /**
   * Видаляє акаунт поточного користувача (self-service)
   */
  const deleteOwnAccount = async () => {
    const hasConfirmed = confirm(
      "Видалити ваш акаунт? Всі пов'язані дані буде втрачено без можливості відновлення.",
    );

    if (!hasConfirmed) {
      return;
    }

    try {
      const response = await fetch(`${API_BASE}/me`, {
        method: "DELETE",
        credentials: "include",
      });

      if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(err.message || `HTTP ${response.status}`);
      }

      navigateTo("/Account/Register");
    } catch (error) {
      showError(error.message);
    }
  };

  /**
   * Відправляє запит на підтвердження електронної пошти
   */
  const sendEmailConfirmation = async () => {
    try {
      const response = await fetch("/Account/SendEmailConfirmation", {
        method: "POST",
        headers: { RequestVerificationToken: getAntiForgeryToken() },
        credentials: "include",
      });

      if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(err.message || `HTTP ${response.status}`);
      }

      showSuccess("Лист з підтвердженням надіслано. Перевірте пошту.");
    } catch (error) {
      showError(error.message);
    }
  };

  const getAntiForgeryToken = () => {
    const tokenInput = document.querySelector(
      'input[name="__RequestVerificationToken"]',
    );
    return tokenInput ? tokenInput.value : "";
  };

  // Оновлюємо профіль одразу при завантаженні сторінки
  loadUserProfile();

  // ==================== EVENT LISTENERS ====================
  console.log("🔗 Підключаємо Event Listeners...");

  bindEventIfPresent(
    openProfileModalBtn,
    "click",
    () => {
      console.log("🖱️ Натиск на кнопку профіля");
      openProfileModal();
    },
    "✅ Event listener на openProfileModalBtn додано",
  );

  bindEventIfPresent(
    closeProfileModalBtn,
    "click",
    closeProfileModal,
    "✅ Event listener на closeProfileModalBtn додано",
  );
  bindEventIfPresent(
    profileModalOverlay,
    "click",
    closeProfileModal,
    "✅ Event listener на profileModalOverlay додано",
  );
  bindEventIfPresent(
    profileLogoutBtn,
    "click",
    handleLogout,
    "✅ Event listener на profileLogoutBtn додано",
  );
  bindEventIfPresent(
    profileSettingsBtn,
    "click",
    openSettingsModal,
    "✅ Event listener на profileSettingsBtn додано",
  );

  bindEventIfPresent(
    profileAvatarUploadBtn,
    "click",
    () => {
      console.log("🖱️ Натиск на кнопку завантаження аватара");
      avatarFileInput?.click();
    },
    "✅ Event listener на profileAvatarUploadBtn додано",
  );

  bindEventIfPresent(
    avatarFileInput,
    "change",
    (e) => {
      console.log("📁 Файл вибрано");
      const file = e.target.files?.[0];
      if (file) {
        handleAvatarUpload(file);
      }
    },
    "✅ Event listener на avatarFileInput додано",
  );

  bindEventIfPresent(
    closeSettingsModalBtn,
    "click",
    closeSettingsModal,
    "✅ Event listener на closeSettingsModalBtn додано",
  );
  bindEventIfPresent(
    settingsModalOverlay,
    "click",
    closeSettingsModal,
    "✅ Event listener на settingsModalOverlay додано",
  );
  bindEventIfPresent(
    settingsSaveUserNameBtn,
    "click",
    updateUserName,
    "✅ Event listener на settingsSaveUserNameBtn додано",
  );
  bindEventIfPresent(
    settingsAvatarUploadBtn,
    "click",
    () => settingsAvatarInput?.click(),
    "✅ Event listener на settingsAvatarUploadBtn додано",
  );
  bindEventIfPresent(
    settingsAvatarInput,
    "change",
    updateAvatarFromSettings,
    "✅ Event listener на settingsAvatarInput додано",
  );
  bindEventIfPresent(
    settingsResetPasswordBtn,
    "click",
    sendPasswordResetLink,
    "✅ Event listener на settingsResetPasswordBtn додано",
  );
  bindEventIfPresent(
    settingsDeleteAccountBtn,
    "click",
    deleteOwnAccount,
    "✅ Event listener на settingsDeleteAccountBtn додано",
  );

  bindEventIfPresent(
    settingsEditUserNameBtn,
    "click",
    () => {
      if (settingsUserNameInput) {
        settingsUserNameInput.readOnly = false;
        settingsUserNameInput.focus();
        settingsUserNameInput.select();
      }
      if (settingsSaveUserNameBtn) {
        settingsSaveUserNameBtn.style.display = "inline-block";
      }
    },
    "✅ Event listener на editUserNameBtn додано",
  );

  bindEventIfPresent(
    closeSettingsModalFooterBtn,
    "click",
    closeSettingsModal,
    "✅ Event listener на closeSettingsModalFooterBtn додано",
  );
  bindEventIfPresent(
    sendEmailConfirmationBtn,
    "click",
    sendEmailConfirmation,
    "✅ Event listener на sendEmailConfirmationBtn додано",
  );

  // Закриття модального вікна при натисканні Escape
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && profileModal.classList.contains("show")) {
      console.log("⌨️ Натиск Escape, закриваємо модаль");
      closeProfileModal();
    }
    if (e.key === "Escape" && settingsModal.classList.contains("show")) {
      console.log("⌨️ Натиск Escape, закриваємо модаль налаштувань");
      closeSettingsModal();
    }
  });
  console.log("✅ Event listener на Escape додано");

  console.log("✅ Profile.js ПОВНІСТЮ ІНІЦІАЛІЗОВАНО!");
})();
