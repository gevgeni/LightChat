// js/auth.js

import { state, resetState } from './state.js';
import { authApi, UnauthorizedError } from './api.js';
import { parseJwt } from './utils.js';
import { TOKEN_STORAGE_KEY } from './config.js';
import { connect, disconnect } from './signalr.js';

// Импортируем функцию инициализации из main.js (передаётся через DI-паттерн)
let initCallback = null;

export function registerInitCallback(callback) {
    initCallback = callback;
}

/**
 * Логин по логину/паролю
 */
export async function loginAndLoadApp() {
    const username = document.getElementById("usernameInput").value.trim();
    const password = document.getElementById("passwordInput").value;

    if (!username || !password) {
        alert("Введите имя пользователя и пароль");
        return;
    }

    try {
        const data = await authApi.login(username, password);
        state.jwtToken = data.tokenString;
        localStorage.setItem(TOKEN_STORAGE_KEY, state.jwtToken);

        const payload = parseJwt(state.jwtToken);
        state.currentUserId = payload.sub;
        state.currentUsername = payload.unique_name;

        console.log("[Auth] JWT получен успешно!");
        await initCallback();
    } catch (err) {
        console.error("[Auth] Ошибка входа:", err);
        alert("Ошибка: " + err.message);
    }
}

/**
 * Регистрация нового пользователя
 */
export async function registerAccount() {
    const username = document.getElementById("regUsernameInput").value.trim();
    const email = document.getElementById("regEmailInput").value.trim();
    const password = document.getElementById("regPasswordInput").value;
    const passwordConfirm = document.getElementById("regPasswordConfirmInput").value;

    if (!username || !password || !passwordConfirm || !email) {
        alert("Пожалуйста, заполните все поля формы регистрации");
        return;
    }

    if (password !== passwordConfirm) {
        alert("Введенные пароли не совпадают!");
        return;
    }

    try {
        await authApi.register(username, email, password);
        alert("Регистрация прошла успешно! Теперь вы можете войти в свой аккаунт.");
        document.getElementById("usernameInput").value = username;
        toggleAuthMode(false);
        document.getElementById("passwordInput").focus();
    } catch (err) {
        console.error("[Auth] Ошибка регистрации:", err);
        alert("Ошибка: " + err.message);
    }
}

/**
 * Выход из аккаунта
 */
export async function logout() {
    await disconnect();
    resetState();
    localStorage.removeItem(TOKEN_STORAGE_KEY);

    document.getElementById("mainInterface").style.display = "none";
    document.getElementById("authSection").style.display = "flex";
}

/**
 * Переключение между формами входа/регистрации
 */
export function toggleAuthMode(isRegister) {
    document.getElementById("loginBox").style.display = isRegister ? "none" : "block";
    document.getElementById("registerBox").style.display = isRegister ? "block" : "none";

    if (isRegister) {
        document.getElementById("regUsernameInput").value = "";
        document.getElementById("regPasswordInput").value = "";
        document.getElementById("regPasswordConfirmInput").value = "";
    } else {
        document.getElementById("usernameInput").value = "";
        document.getElementById("passwordInput").value = "";
    }
}

/**
 * Восстановление сессии по сохранённому токену
 * Возвращает true, если сессия восстановлена
 */
export async function initializeFromSavedToken() {
    const savedToken = localStorage.getItem(TOKEN_STORAGE_KEY);
    if (!savedToken) return false;

    const parsed = parseJwt(savedToken);
    if (!parsed || !parsed.exp) {
        localStorage.removeItem(TOKEN_STORAGE_KEY);
        return false;
    }

    const currentTime = Math.floor(Date.now() / 1000);
    if (parsed.exp <= currentTime) {
        localStorage.removeItem(TOKEN_STORAGE_KEY);
        return false;
    }

    state.jwtToken = savedToken;
    state.currentUserId = parsed.sub;
    state.currentUsername = parsed.unique_name;

    await initCallback();
    return true;
}