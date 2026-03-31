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
  const presetButtons = root.querySelectorAll("[data-preset]");

  if (
    !modeElement ||
    !timeElement ||
    !countElement ||
    !startButton ||
    !pauseButton ||
    !resetButton ||
    !skipButton ||
    !autoCheckbox ||
    presetButtons.length === 0
  ) {
    return;
  }

  const TODAY_STATS_API = "/api/pomodoro/today";
  const SESSION_API = "/api/pomodoro/session";
  const MODE_LABELS = {
    focus: "Фокус",
    shortBreak: "Коротка перерва",
    longBreak: "Довга перерва",
  };

  const PRESETS = {
    "25-5-15": { focus: 25, shortBreak: 5, longBreak: 15 },
    "50-10-20": { focus: 50, shortBreak: 10, longBreak: 20 },
  };

  let activePreset = "25-5-15";
  let durations = { ...PRESETS[activePreset] };
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

  const updateDailyCountView = () => {
    countElement.textContent = String(dailyCompletedCount);
  };

  const loadTodayStats = async () => {
    try {
      const response = await fetch(TODAY_STATS_API, {
        method: "GET",
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
      // Keep widget usable even when stats endpoint is temporarily unavailable.
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

  const applyPreset = (presetKey) => {
    const preset = PRESETS[presetKey];
    if (!preset) {
      return;
    }

    activePreset = presetKey;
    durations = { ...preset };

    for (const button of presetButtons) {
      const isActive = button.dataset.preset === activePreset;
      button.classList.toggle("is-active", isActive);
    }

    reset();
  };

  startButton.addEventListener("click", start);
  pauseButton.addEventListener("click", pause);
  resetButton.addEventListener("click", reset);
  skipButton.addEventListener("click", skip);

  for (const button of presetButtons) {
    button.addEventListener("click", () => {
      applyPreset(button.dataset.preset || "");
    });
  }

  render();
  void loadTodayStats();
})();
