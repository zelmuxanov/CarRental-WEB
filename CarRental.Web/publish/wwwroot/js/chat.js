const config = window.chatConfig || {};
let currentChatId = config.chatId;
let tempUserId = config.tempUserId;

document.addEventListener('DOMContentLoaded', function() {
    console.log('chat.js инициализация, chatId:', currentChatId);
    
    // Если chatId не задан или равен пустой строке/Guid.Empty – создаём новый чат
    if (!currentChatId || currentChatId === '00000000-0000-0000-0000-000000000000') {
        initChat();
    } else {
        loadChatMessages(currentChatId);
    }
    
    setupFormSubmission();
    setupFileAttachment();
});

// Создание нового чата (для авторизованных и гостей)
async function initChat() {
    try {
        const container = document.getElementById('chatContainer');
        if (container) {
            container.innerHTML = '<div class="text-center text-muted py-5"><i class="fas fa-spinner fa-spin fa-2x mb-3"></i><p>Загрузка чата...</p></div>';
        }
        
        let url = '/Chat/GetOrCreate';
        if (config.isAuthenticated && config.userId) {
            url += `?userId=${config.userId}`;
        } else if (tempUserId) {
            url += `?tempUserId=${tempUserId}`;
        }
        
        const response = await fetch(url);
        const data = await response.json();
        
        if (!data.success) {
            throw new Error(data.error || 'Не удалось создать чат');
        }
        
        currentChatId = data.chatId;
        if (!config.isAuthenticated && data.tempUserId) {
            tempUserId = data.tempUserId;
            setCookie('ChatTempUserId', tempUserId, 24);
        }
        
        await loadChatMessages(currentChatId);
    } catch (err) {
        console.error('Ошибка инициализации чата:', err);
        const container = document.getElementById('chatContainer');
        if (container) {
            container.innerHTML = '<div class="alert alert-danger">Не удалось загрузить чат. Попробуйте позже.</div>';
        }
    }
}

// Загрузка истории сообщений
async function loadChatMessages(chatId) {
    try {
        const response = await fetch(`/Chat/Messages?chatId=${chatId}`);
        const data = await response.json();
        
        if (data.error) {
            throw new Error(data.error);
        }
        
        if (data.messages) {
            renderMessages(data.messages);
        } else {
            renderMessages([]);
        }
    } catch (err) {
        console.error('Ошибка загрузки сообщений:', err);
        const container = document.getElementById('chatContainer');
        if (container) {
            container.innerHTML = '<div class="alert alert-danger">Ошибка загрузки сообщений</div>';
        }
    }
}

// Отрисовка всех сообщений
function renderMessages(messages) {
    const container = document.getElementById('chatContainer');
    if (!container) return;
    
    if (!messages || messages.length === 0) {
        container.innerHTML = '<div class="text-center text-muted py-5"><i class="fas fa-comments fa-3x mb-3"></i><p>Напишите нам, и мы ответим!</p></div>';
        return;
    }
    
    container.innerHTML = '';
    messages.forEach(msg => addMessageToDOM(msg, false));
    scrollToBottom();
}

// Добавление одного сообщения в DOM
function addMessageToDOM(message, scroll = true) {
    const container = document.getElementById('chatContainer');
    if (!container) return;
    
    const isUser = message.messageType === 1; // 1-User, 2-Admin, 3-System
    const time = new Date(message.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    
    const messageDiv = document.createElement('div');
    messageDiv.className = `message mb-2 ${isUser ? 'user-message' : 'admin-message'}`;
    
    let attachmentHtml = '';
    if (message.attachmentUrl) {
        if (message.attachmentType && message.attachmentType.startsWith('image/')) {
            attachmentHtml = `<div class="mt-2"><img src="${message.attachmentUrl}" class="img-fluid rounded" style="max-height:150px" onclick="window.open(this.src)"></div>`;
        } else {
            attachmentHtml = `<div class="mt-2"><a href="${message.attachmentUrl}" target="_blank" class="btn btn-sm btn-outline-secondary"><i class="fas fa-file"></i> Файл</a></div>`;
        }
    }
    
    messageDiv.innerHTML = `
        <div class="d-flex ${isUser ? 'justify-content-end' : 'justify-content-start'}">
            <div class="message-content p-2 rounded ${isUser ? 'bg-primary text-white' : 'bg-light'}">
                <div class="d-flex justify-content-between">
                    <strong>${escapeHtml(message.senderName)}</strong>
                    <small>${time}</small>
                </div>
                <div>${linkify(escapeHtml(message.message))}</div>
                ${attachmentHtml}
            </div>
        </div>
    `;
    
    container.appendChild(messageDiv);
    if (scroll) scrollToBottom();
}

// Отправка сообщения (текст + файл)
function setupFormSubmission() {
    const form = document.getElementById('messageForm');
    const input = document.getElementById('messageInput');
    const fileInput = document.getElementById('fileInput');
    
    if (!form) return;
    
    form.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const messageText = input.value.trim();
        const file = fileInput.files[0];
        
        // Разрешаем отправку, если есть текст ИЛИ файл
        if (!messageText && !file) return;
        
        // Блокируем форму
        const submitBtn = form.querySelector('button[type="submit"]');
        input.disabled = true;
        if (submitBtn) submitBtn.disabled = true;
        
        const formData = new FormData();
        formData.append('chatId', currentChatId);
        formData.append('message', messageText);
        if (file) formData.append('attachment', file);
        if (!config.isAuthenticated) formData.append('tempUserId', tempUserId);
        
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        try {
            const response = await fetch('/Chat/SendMessageWithAttachment', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token },
                body: formData
            });
            
            const data = await response.json();
            
            if (data.success) {
                // Добавляем отправленное сообщение в чат
                if (data.message) {
                    addMessageToDOM(data.message, true);
                }
                // Очищаем поля
                input.value = '';
                fileInput.value = '';
                const previewDiv = document.getElementById('filePreview');
                if (previewDiv) previewDiv.innerHTML = '';
            } else {
                alert('Ошибка: ' + (data.error || 'Не удалось отправить сообщение'));
            }
        } catch (err) {
            console.error('Ошибка отправки:', err);
            alert('Ошибка отправки сообщения. Проверьте соединение.');
        } finally {
            input.disabled = false;
            if (submitBtn) submitBtn.disabled = false;
            input.focus();
        }
    });
}

// Превью выбранного файла
function setupFileAttachment() {
    const attachBtn = document.getElementById('attachFileButton');
    const fileInput = document.getElementById('fileInput');
    const previewDiv = document.getElementById('filePreview');
    
    if (!attachBtn) return;
    
    attachBtn.addEventListener('click', () => fileInput.click());
    
    fileInput.addEventListener('change', function() {
        if (this.files && this.files.length > 0) {
            const file = this.files[0];
            previewDiv.innerHTML = `
                <div class="alert alert-secondary py-1 d-flex justify-content-between align-items-center">
                    <span><i class="fas fa-paperclip me-1"></i> ${escapeHtml(file.name)}</span>
                    <button type="button" class="btn-close btn-sm" id="clearFileBtn"></button>
                </div>
            `;
            const clearBtn = document.getElementById('clearFileBtn');
            if (clearBtn) {
                clearBtn.addEventListener('click', () => {
                    fileInput.value = '';
                    previewDiv.innerHTML = '';
                });
            }
        } else {
            previewDiv.innerHTML = '';
        }
    });
}

// Прокрутка вниз
function scrollToBottom() {
    const container = document.getElementById('chatContainer');
    if (container) {
        setTimeout(() => {
            container.scrollTop = container.scrollHeight;
        }, 50);
    }
}

// Вспомогательные функции
function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/[&<>]/g, function(m) {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}

function linkify(text) {
    if (!text) return text;
    const urlPattern = /(\b(https?:\/\/|www\.)[^\s]+)/gi;
    return text.replace(urlPattern, function(match) {
        let url = match;
        if (!url.startsWith('http')) url = 'http://' + url;
        return `<a href="${url}" target="_blank" rel="noopener noreferrer">${match}</a>`;
    });
}

function setCookie(name, value, hours) {
    const expires = new Date();
    expires.setTime(expires.getTime() + (hours * 60 * 60 * 1000));
    document.cookie = `${name}=${value};expires=${expires.toUTCString()};path=/;SameSite=Lax`;
}