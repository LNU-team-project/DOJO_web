(() => {
  const root = document.querySelector("[data-pomodoro-root]");
  if (!root) {
    return;
  }

  const modeElement = root.querySelector("[data-pomodoro-mode]");
  const timeElement = root.querySelector("[data-pomodoro-time]");
  const countElement = root.querySelector("[data-pomodoro-count]");
  const startButton = root.querySelector("[data-pomodoro-start]");
  const pauseButton = root.querySelector("[data-pomodoro-pause]");
  const resetButton = root.querySelector("[data-pomodoro-reset]");
  const skipButton = root.querySelector("[data-pomodoro-skip]");
  const autoCheckbox = root.querySelector("[data-pomodoro-auto]");
  const presetSelect =
    root.querySelector("[data-pomodoro-preset-select]") ||
    root.querySelector("#pomodoroPresetSelect");
  const customPresetList = root.querySelector("[data-pomodoro-custom-presets]");
  const openPresetModalButton = document.getElementById(
    "openPomodoroPresetModal",
  );
  const presetModal = document.getElementById("pomodoroPresetModal");
  const presetModalTitle = document.getElementById("pomodoroPresetModalTitle");
  const closePresetModalButton = document.getElementById(
    "closePomodoroPresetModal",
  );
  const presetForm = document.getElementById("pomodoroPresetForm");
  const presetSubmitButton = presetForm.querySelector("button[type='submit']");
  const presetNameInput = document.getElementById("pomodoroPresetName");
  const presetFocusInput = document.getElementById("pomodoroPresetFocus");
  const presetShortBreakInput = document.getElementById(
    "pomodoroPresetShortBreak",
  );
  const presetLongBreakInput = document.getElementById(
    "pomodoroPresetLongBreak",
  );
  const presetError = root.querySelector("[data-pomodoro-preset-error]");

  if (
    !modeElement ||
    !timeElement ||
    !countElement ||
    !startButton ||
    !pauseButton ||
    !resetButton ||
    !skipButton ||
    !autoCheckbox ||
    !presetSelect ||
    !customPresetList ||
    !openPresetModalButton ||
    !presetModal ||
    !presetModalTitle ||
    !closePresetModalButton ||
    !presetForm ||
    !presetSubmitButton ||
    !presetNameInput ||
    !presetFocusInput ||
    !presetShortBreakInput ||
    !presetLongBreakInput
  ) {
    return;
  }

  const TODAY_STATS_API = "/api/pomodoro/today";
  const SESSION_API = "/api/pomodoro/session";
  const PRESETS_API = "/api/pomodoro/presets";
  const MODE_LABELS = {
    focus: "Фокус",
    shortBreak: "Коротка перерва",
    longBreak: "Довга перерва",
  };

  const BUILTIN_PRESETS = [
    {
      key: "builtin:25-5-15",
      name: "25 / 5 / 15",
      focus: 25,
      shortBreak: 5,
      longBreak: 15,
    },
    {
      key: "builtin:50-10-20",
      name: "50 / 10 / 20",
      focus: 50,
      shortBreak: 10,
      longBreak: 20,
    },
  ];

  let customPresets = [];
  let activePresetKey = BUILTIN_PRESETS[0].key;
  let editingPresetId = null;
  let durations = {
    focus: BUILTIN_PRESETS[0].focus,
    shortBreak: BUILTIN_PRESETS[0].shortBreak,
    longBreak: BUILTIN_PRESETS[0].longBreak,
  };
  let currentMode = "focus";
  let secondsLeft = durations.focus * 60;
  let completedFocusSessions = 0;
  let dailyCompletedCount = 0;
  let focusStartedAt = new Date();
  let timerId = null;

  const formatTime = (totalSeconds) => {
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
  };

  const toPositiveNumber = (value, fallback) => {
    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
  };

  const normalizePreset = (preset) => {
    const id = Number.parseInt(preset?.id, 10);
    return {
      id: Number.isFinite(id) ? id : 0,
      name: String(preset?.name ?? "").trim(),
      focus: toPositiveNumber(
        preset?.focus ?? preset?.focusMinutes,
        BUILTIN_PRESETS[0].focus,
      ),
      shortBreak: toPositiveNumber(
        preset?.shortBreak ?? preset?.shortBreakMinutes,
        BUILTIN_PRESETS[0].shortBreak,
      ),
      longBreak: toPositiveNumber(
        preset?.longBreak ?? preset?.longBreakMinutes,
        BUILTIN_PRESETS[0].longBreak,
      ),
      createdAt: preset?.createdAt ?? null,
    };
  };

  const getActiveCustomPresetId = () => {
    if (!activePresetKey.startsWith("custom:")) {
      return null;
    }

    const presetId = Number.parseInt(
      activePresetKey.slice("custom:".length),
      10,
    );
    return Number.isFinite(presetId) ? presetId : null;
  };

  const getPresetMinutes = (preset) => [
    `${preset.focus} хв`,
    `${preset.shortBreak} хв`,
    `${preset.longBreak} хв`,
  ];

  const setPresetError = (message) => {
    if (!presetError) {
      return;
    }

    presetError.textContent = message || "";
  };

  const updateDailyCountView = () => {
    countElement.textContent = String(dailyCompletedCount);
  };

  const getPresetByKey = (key) => {
    const builtin = BUILTIN_PRESETS.find((preset) => preset.key === key);
    if (builtin) {
      return builtin;
    }

    const customKey = key.startsWith("custom:")
      ? key.slice("custom:".length)
      : "";
    const presetId = Number.parseInt(customKey, 10);
    if (!Number.isFinite(presetId)) {
      return null;
    }

    return customPresets.find((preset) => preset.id === presetId) ?? null;
  };

  const renderPresetSelect = () => {
    const previousValue = presetSelect.value || activePresetKey;
    presetSelect.innerHTML = "";

    const builtinGroup = document.createElement("optgroup");
    builtinGroup.label = "Стандартні";
    for (const preset of BUILTIN_PRESETS) {
      const option = document.createElement("option");
      option.value = preset.key;
      option.textContent = preset.name;
      builtinGroup.appendChild(option);
    }

    presetSelect.appendChild(builtinGroup);

    const customGroup = document.createElement("optgroup");
    customGroup.label = "Мої пресети";
    if (customPresets.length === 0) {
      const emptyOption = document.createElement("option");
      emptyOption.disabled = true;
      emptyOption.textContent = "Ще немає збережених пресетів";
      customGroup.appendChild(emptyOption);
    } else {
      for (const preset of customPresets) {
        const option = document.createElement("option");
        option.value = `custom:${preset.id}`;
        option.textContent = preset.name;
        customGroup.appendChild(option);
      }
    }

    presetSelect.appendChild(customGroup);

    const nextValue = getPresetByKey(previousValue)
      ? previousValue
      : activePresetKey;
    presetSelect.value = nextValue;
  };

  const renderCustomPresetList = () => {
    customPresetList.innerHTML = "";

    if (customPresets.length === 0) {
      const emptyMessage = document.createElement("p");
      emptyMessage.className = "pomodoro-custom-presets-empty";
      emptyMessage.textContent = "Тут з'являться ваші збережені пресети.";
      customPresetList.appendChild(emptyMessage);
      return;
    }

    const activeCustomPresetId = getActiveCustomPresetId();

    for (const preset of customPresets) {
      const isActive = preset.id === activeCustomPresetId;
      const card = document.createElement("article");
      card.className = `pomodoro-preset-card${isActive ? " is-active" : ""}`;

      const summary = document.createElement("button");
      summary.type = "button";
      summary.className = "pomodoro-preset-card-summary";
      summary.setAttribute("aria-expanded", String(isActive));
      const summaryName = document.createElement("span");
      summaryName.className = "pomodoro-preset-card-name";
      summaryName.textContent = preset.name;

      const summaryMeta = document.createElement("span");
      summaryMeta.className = "pomodoro-preset-card-meta";
      summaryMeta.textContent = getPresetMinutes(preset).join(" · ");

      summary.append(summaryName, summaryMeta);
      summary.addEventListener("click", () => {
        applyPresetByKey(`custom:${preset.id}`);
      });

      card.appendChild(summary);

      const body = document.createElement("div");
      body.className = "pomodoro-preset-card-body";
      body.hidden = !isActive;

      const settings = document.createElement("div");
      settings.className = "pomodoro-preset-settings";
      const focusSetting = document.createElement("div");
      focusSetting.className = "pomodoro-preset-setting";
      const focusLabel = document.createElement("span");
      focusLabel.className = "pomodoro-preset-setting-label";
      focusLabel.textContent = "Фокус";
      const focusValue = document.createElement("span");
      focusValue.className = "pomodoro-preset-setting-value";
      focusValue.textContent = `${preset.focus} хв`;
      focusSetting.append(focusLabel, focusValue);

      const shortBreakSetting = document.createElement("div");
      shortBreakSetting.className = "pomodoro-preset-setting";
      const shortBreakLabel = document.createElement("span");
      shortBreakLabel.className = "pomodoro-preset-setting-label";
      shortBreakLabel.textContent = "Коротка перерва";
      const shortBreakValue = document.createElement("span");
      shortBreakValue.className = "pomodoro-preset-setting-value";
      shortBreakValue.textContent = `${preset.shortBreak} хв`;
      shortBreakSetting.append(shortBreakLabel, shortBreakValue);

      const longBreakSetting = document.createElement("div");
      longBreakSetting.className = "pomodoro-preset-setting";
      const longBreakLabel = document.createElement("span");
      longBreakLabel.className = "pomodoro-preset-setting-label";
      longBreakLabel.textContent = "Довга перерва";
      const longBreakValue = document.createElement("span");
      longBreakValue.className = "pomodoro-preset-setting-value";
      longBreakValue.textContent = `${preset.longBreak} хв`;
      longBreakSetting.append(longBreakLabel, longBreakValue);

      settings.append(focusSetting, shortBreakSetting, longBreakSetting);

      const actions = document.createElement("div");
      actions.className = "pomodoro-preset-actions";

      const startAction = document.createElement("button");
      startAction.type = "button";
      startAction.className =
        "pomodoro-btn pomodoro-preset-action pomodoro-preset-action-start";
      startAction.textContent = "Запустити";
      startAction.addEventListener("click", () => {
        applyPresetByKey(`custom:${preset.id}`);
        start();
      });

      const editAction = document.createElement("button");
      editAction.type = "button";
      editAction.className =
        "pomodoro-btn pomodoro-preset-action pomodoro-preset-action-edit";
      editAction.textContent = "Редагувати";
      editAction.addEventListener("click", () => {
        openPresetModal(preset);
      });

      const deleteAction = document.createElement("button");
      deleteAction.type = "button";
      deleteAction.className =
        "pomodoro-btn pomodoro-preset-action pomodoro-preset-action-delete";
      deleteAction.textContent = "Видалити";
      deleteAction.addEventListener("click", () => {
        void deletePreset(preset.id, preset.name);
      });

      actions.append(startAction, editAction, deleteAction);
      body.append(settings, actions);
      card.appendChild(body);

      customPresetList.appendChild(card);
    }
  };

  const refreshPresetViews = () => {
    renderPresetSelect();
    renderCustomPresetList();
  };

  const syncPresetSelectionFallback = () => {
    if (getPresetByKey(activePresetKey)) {
      return;
    }

    activePresetKey = BUILTIN_PRESETS[0].key;
    durations = {
      focus: BUILTIN_PRESETS[0].focus,
      shortBreak: BUILTIN_PRESETS[0].shortBreak,
      longBreak: BUILTIN_PRESETS[0].longBreak,
    };
    presetSelect.value = activePresetKey;
  };

  const applyPresetByKey = (key, resetTimer = true) => {
    const preset = getPresetByKey(key);
    if (!preset) {
      return;
    }

    activePresetKey = key;
    durations = {
      focus: preset.focus,
      shortBreak: preset.shortBreak,
      longBreak: preset.longBreak,
    };

    presetSelect.value = key;
    renderCustomPresetList();

    if (resetTimer) {
      reset();
    } else {
      render();
    }
  };

  const loadTodayStats = async () => {
    try {
      const response = await fetch(TODAY_STATS_API, { method: "GET" });
      if (!response.ok) {
        return;
      }

      const payload = await response.json();
      if (!payload.success || !payload.data) {
        return;
      }

      dailyCompletedCount = Number(payload.data.completedFocusSessions || 0);
      updateDailyCountView();
    } catch {
      // Keep widget usable even when stats endpoint is temporarily unavailable.
    }
  };

  const loadPresets = async () => {
    try {
      const response = await fetch(PRESETS_API, {
        method: "GET",
        credentials: "include",
      });
      if (!response.ok) {
        return;
      }

      const payload = await response.json();
      if (!payload.success || !Array.isArray(payload.data)) {
        return;
      }

      customPresets = payload.data
        .map(normalizePreset)
        .filter((preset) => preset.id > 0 && preset.name);
      syncPresetSelectionFallback();
      refreshPresetViews();
    } catch {
      refreshPresetViews();
    }
  };

  const persistFocusSession = async (startTime, endTime, durationMinutes) => {
    const body = {
      startTime: startTime.toISOString(),
      endTime: endTime.toISOString(),
      durationMinutes,
      workCycles: 1,
    };

    try {
      const response = await fetch(SESSION_API, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
      });

      if (!response.ok) {
        return;
      }

      const payload = await response.json();
      if (!payload.success || !payload.data) {
        return;
      }

      dailyCompletedCount = Number(payload.data.completedFocusSessions || 0);
      updateDailyCountView();
    } catch {
      // Ignore transient network errors; timer should not break because of API failures.
    }
  };

  const playCompletionSound = () => {
    try {
      const audioContext = new AudioContext();
      const oscillator = audioContext.createOscillator();
      const gain = audioContext.createGain();

      oscillator.type = "sine";
      oscillator.frequency.value = 880;
      gain.gain.value = 0.05;

      oscillator.connect(gain);
      gain.connect(audioContext.destination);

      oscillator.start();
      oscillator.stop(audioContext.currentTime + 0.15);
    } catch {
      // Ignore if browser blocks audio until user gesture.
    }
  };

  const stopTimer = () => {
    if (timerId !== null) {
      globalThis.clearInterval(timerId);
      timerId = null;
    }
  };

  const getModeDurationSeconds = (mode) => {
    if (mode === "focus") {
      return durations.focus * 60;
    }

    if (mode === "shortBreak") {
      return durations.shortBreak * 60;
    }

    return durations.longBreak * 60;
  };

  const syncButtonStates = () => {
    const isRunning = timerId !== null;
    startButton.disabled = isRunning;
    pauseButton.disabled = !isRunning;
  };

  const render = () => {
    modeElement.textContent = MODE_LABELS[currentMode];
    timeElement.textContent = formatTime(secondsLeft);
    updateDailyCountView();
    syncButtonStates();
  };

  const moveToNextMode = () => {
    if (currentMode === "focus") {
      const finishedAt = new Date();
      const startedAt =
        focusStartedAt ??
        new Date(finishedAt.getTime() - durations.focus * 60 * 1000);

      completedFocusSessions += 1;

      void persistFocusSession(startedAt, finishedAt, durations.focus);

      currentMode =
        completedFocusSessions % 4 === 0 ? "longBreak" : "shortBreak";
    } else {
      currentMode = "focus";
      focusStartedAt = new Date();
    }

    secondsLeft = getModeDurationSeconds(currentMode);
    render();
    playCompletionSound();
  };

  const tick = () => {
    if (secondsLeft <= 0) {
      moveToNextMode();

      if (!autoCheckbox.checked) {
        stopTimer();
        syncButtonStates();
      }

      return;
    }

    secondsLeft -= 1;
    render();
  };

  const start = () => {
    if (timerId !== null) {
      return;
    }

    if (
      currentMode === "focus" &&
      secondsLeft === getModeDurationSeconds("focus")
    ) {
      focusStartedAt = new Date();
    }

    timerId = globalThis.setInterval(tick, 1000);
    syncButtonStates();
  };

  const pause = () => {
    stopTimer();
    syncButtonStates();
  };

  const reset = () => {
    stopTimer();
    currentMode = "focus";
    secondsLeft = getModeDurationSeconds(currentMode);
    completedFocusSessions = 0;
    focusStartedAt = new Date();
    render();
  };

  const skip = () => {
    stopTimer();
    moveToNextMode();
    if (autoCheckbox.checked) {
      start();
    }
  };

  const openPresetModal = (preset = null) => {
    editingPresetId = preset?.id ?? null;
    presetModalTitle.textContent = editingPresetId
      ? "Редагувати пресет"
      : "Додати пресет";
    presetSubmitButton.textContent = editingPresetId
      ? "Зберегти зміни"
      : "Зберегти пресет";

    const values = preset ?? {
      focus: durations.focus,
      shortBreak: durations.shortBreak,
      longBreak: durations.longBreak,
    };

    presetNameInput.value = preset?.name ?? "";
    presetFocusInput.value = String(values.focus);
    presetShortBreakInput.value = String(values.shortBreak);
    presetLongBreakInput.value = String(values.longBreak);
    setPresetError("");
    presetModal.style.display = "flex";
    presetModal.setAttribute("aria-hidden", "false");
    presetNameInput.focus();
  };

  const closePresetModal = () => {
    editingPresetId = null;
    presetModalTitle.textContent = "Додати пресет";
    presetSubmitButton.textContent = "Зберегти пресет";
    presetModal.style.display = "none";
    presetModal.setAttribute("aria-hidden", "true");
    setPresetError("");
  };

  const savePreset = async (model) => {
    const isEditing = editingPresetId !== null;
    const endpoint = isEditing
      ? `${PRESETS_API}/${editingPresetId}`
      : PRESETS_API;

    const response = await fetch(endpoint, {
      method: isEditing ? "PUT" : "POST",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
      body: JSON.stringify(model),
    });

    if (!response.ok) {
      const payload = await response.json().catch(() => null);
      return payload?.message ?? "Не вдалося зберегти пресет";
    }

    const payload = await response.json();
    if (!payload.success || !payload.data) {
      return payload.message ?? "Не вдалося зберегти пресет";
    }

    return normalizePreset(payload.data);
  };

  const deletePreset = async (presetId, presetName) => {
    const confirmed = globalThis.confirm(`Видалити пресет «${presetName}»?`);
    if (!confirmed) {
      return;
    }

    try {
      const response = await fetch(`${PRESETS_API}/${presetId}`, {
        method: "DELETE",
        credentials: "include",
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        setPresetError(payload?.message ?? "Не вдалося видалити пресет");
        return;
      }

      const payload = await response.json();
      if (!payload.success) {
        setPresetError(payload.message ?? "Не вдалося видалити пресет");
        return;
      }

      customPresets = customPresets.filter((preset) => preset.id !== presetId);
      if (activePresetKey === `custom:${presetId}`) {
        syncPresetSelectionFallback();
        reset();
      }

      setPresetError("");
      refreshPresetViews();
    } catch {
      setPresetError("Не вдалося видалити пресет");
    }
  };

  const handlePresetSubmit = async (event) => {
    event.preventDefault();

    const model = {
      name: presetNameInput.value.trim(),
      focusMinutes: Number.parseInt(presetFocusInput.value, 10),
      shortBreakMinutes: Number.parseInt(presetShortBreakInput.value, 10),
      longBreakMinutes: Number.parseInt(presetLongBreakInput.value, 10),
    };

    if (!model.name) {
      setPresetError("Вкажіть назву пресету");
      return;
    }

    if (
      !Number.isFinite(model.focusMinutes) ||
      !Number.isFinite(model.shortBreakMinutes) ||
      !Number.isFinite(model.longBreakMinutes)
    ) {
      setPresetError("Вкажіть коректні тривалості");
      return;
    }

    setPresetError("");

    try {
      const savedPreset = await savePreset(model);
      if (typeof savedPreset === "string") {
        setPresetError(savedPreset);
        return;
      }

      customPresets = [
        ...customPresets.filter((preset) => preset.id !== savedPreset.id),
        savedPreset,
      ].sort((left, right) => left.name.localeCompare(right.name, "uk"));

      refreshPresetViews();
      applyPresetByKey(`custom:${savedPreset.id}`);
      closePresetModal();
    } catch {
      setPresetError("Не вдалося зберегти пресет");
    }
  };

  startButton.addEventListener("click", start);
  pauseButton.addEventListener("click", pause);
  resetButton.addEventListener("click", reset);
  skipButton.addEventListener("click", skip);
  presetSelect.addEventListener("change", () => {
    applyPresetByKey(presetSelect.value);
  });
  openPresetModalButton.addEventListener("click", () => openPresetModal());
  closePresetModalButton.addEventListener("click", closePresetModal);
  presetModal.addEventListener("click", (event) => {
    if (event.target === presetModal) {
      closePresetModal();
    }
  });
  presetForm.addEventListener("submit", handlePresetSubmit);

  renderPresetSelect();
  renderCustomPresetList();
  render();
  void loadTodayStats();
  void loadPresets();
})();
