// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(() => {
  const form = document.querySelector("#register-form");
  if (!form) {
    return;
  }

  const submitButton = form.querySelector("#register-submit");
  const inputs = Array.from(form.querySelectorAll(".form-field"));
  const confirmInputSelector = "[data-validate='confirm']";

  const isValidEmail = (value) => {
    const emailProbe = document.createElement("input");
    emailProbe.type = "email";
    emailProbe.required = true;
    emailProbe.value = value.trim();
    return emailProbe.checkValidity();
  };

  const validators = {
    username: (value) => value.trim().length >= 3,
    email: (value) => isValidEmail(value),
    min6: (value) => value.length >= 6,
    confirm: (value, input) => {
      const matchId = input.dataset.match;
      const matchInput = matchId ? document.getElementById(matchId) : null;
      return matchInput ? value === matchInput.value : false;
    },
  };

  const messages = {
    username: "Ім'я користувача має містити щонайменше 3 символи.",
    email: "Введіть коректну адресу пошти.",
    min6: "Поле має містити щонайменше 6 символів.",
    confirm: "Паролі не співпадають.",
  };

  const setValidity = (input, isValid, message) => {
    input.classList.toggle("is-valid", isValid);
    input.classList.toggle("is-invalid", !isValid);
    const error = form.querySelector(`[data-error-for="${input.id}"]`);
    if (error) {
      error.textContent = isValid ? "" : message;
    }
  };

  const validateInput = (input) => {
    const key = input.dataset.validate;
    if (!key || !validators[key]) {
      return true;
    }
    const value = input.value;
    const isValid = validators[key](value, input);
    const message = messages[key] ?? "Поле заповнено некоректно.";
    setValidity(input, isValid, message);
    return isValid;
  };

  const updateButtonState = () => {
    const allValid = inputs.every((input) => validateInput(input));
    submitButton.disabled = !allValid;
  };

  inputs.forEach((input) => {
    input.addEventListener("input", () => {
      validateInput(input);
      if (input.dataset.validate === "min6") {
        const confirmInput = form.querySelector(confirmInputSelector);
        if (confirmInput) {
          validateInput(confirmInput);
        }
      }
      updateButtonState();
    });

    input.addEventListener("blur", () => {
      validateInput(input);
      updateButtonState();
    });
  });

  form.addEventListener("submit", (event) => {
    if (!inputs.every((input) => validateInput(input))) {
      event.preventDefault();
      updateButtonState();
    }
  });

  updateButtonState();
})();
