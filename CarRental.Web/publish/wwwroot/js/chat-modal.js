document.addEventListener('DOMContentLoaded', function() {
    const openChatBtn = document.getElementById('openChatButton');
    const modalElement = document.getElementById('chatModal');
    if (!openChatBtn || !modalElement) return;

    const chatModal = new bootstrap.Modal(modalElement);
    let isAuthenticated = openChatBtn.dataset.isAuthenticated === 'true';
    let userId = openChatBtn.dataset.userId || null;
    let tempUserId = null;
    let currentChatId = null;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    let form = document.getElementById('chatMessageForm');
    let input = document.getElementById('chatMessageInput');
    let fileInput = document.getElementById('fileInput');
    let attachBtn = document.getElementById('attachFileButton');
    let chatIdHidden = document.getElementById('chatId');
    let messagesContainer = document.getElementById('chatMessages');

    // ---------- Вспомогательные функции ----------
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
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

    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    // Надёжная прокрутка вниз
    function scrollToBottom() {
        if (!messagesContainer) return;
        // Сброс текущей позиции и плавная прокрутка
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
        // Дополнительная попытка после возможного рендера изображений
        setTimeout(() => {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }, 100);
    }

    // ---------- Превью файла ----------
    let previewContainer = null;
    function showFilePreview(file) {
        if (!previewContainer) {
            previewContainer = document.createElement('div');
            previewContainer.className = 'file-preview mt-2 p-2 bg-light rounded d-flex align-items-center gap-2 flex-wrap';
            form.insertBefore(previewContainer, form.firstChild);
        }
        previewContainer.innerHTML = '';
        if (!file) return;

        const fileNameSpan = document.createElement('span');
        fileNameSpan.innerHTML = `<i class="fas fa-paperclip me-1"></i> ${escapeHtml(file.name)}`;
        previewContainer.appendChild(fileNameSpan);

        if (file.type.startsWith('image/')) {
            const img = document.createElement('img');
            img.style.maxHeight = '50px';
            img.style.borderRadius = '4px';
            img.src = URL.createObjectURL(file);
            previewContainer.appendChild(img);
        }

        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'btn btn-sm btn-link text-danger';
        removeBtn.innerHTML = '<i class="fas fa-times"></i>';
        removeBtn.onclick = () => {
            fileInput.value = '';
            previewContainer.remove();
            previewContainer = null;
        };
        previewContainer.appendChild(removeBtn);
    }

    if (fileInput) {
        fileInput.addEventListener('change', function() {
            if (this.files.length) showFilePreview(this.files[0]);
            else if (previewContainer) previewContainer.remove();
        });
    }

    // ---------- Добавление сообщения в DOM ----------
    function addMessageToDOM(message) {
        if (!messagesContainer) return;
        const isUser = message.messageType === 1; // 1-User, 2-Admin, 3-System
        const time = new Date(message.createdAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
        
        let attachmentHtml = '';
        if (message.attachmentUrl) {
            if (message.attachmentType && message.attachmentType.startsWith('image/')) {
                attachmentHtml = `<div class="message-attachment mt-2"><img src="${message.attachmentUrl}" class="img-fluid rounded" style="max-height:200px" onclick="window.open(this.src)"></div>`;
            } else {
                attachmentHtml = `<div class="message-attachment mt-2"><a href="${message.attachmentUrl}" target="_blank" class="btn btn-sm btn-outline-secondary"><i class="fas fa-file"></i> Файл</a></div>`;
            }
        }
        
        const messageHtml = `
            <div class="message mb-3 ${isUser ? 'user-message' : 'admin-message'}">
                <div class="d-flex ${isUser ? 'justify-content-end' : 'justify-content-start'}">
                    <div class="message-bubble p-3 rounded-3 ${isUser ? 'bg-primary text-white' : 'message-bubble-admin'}">
                        <div class="message-header mb-2">
                            <span class="message-sender fw-bold">${escapeHtml(message.senderName)}</span>
                            <span class="message-time text-muted ms-2">${time}</span>
                        </div>
                        <div class="message-body">${linkify(escapeHtml(message.message))}</div>
                        ${attachmentHtml}
                    </div>
                </div>
            </div>
        `;
        messagesContainer.insertAdjacentHTML('beforeend', messageHtml);
        scrollToBottom();
    }

    // ---------- Загрузка истории ----------
    async function loadMessages() {
        if (!currentChatId) return;
        try {
            const response = await fetch(`/Chat/Messages?chatId=${currentChatId}`);
            const data = await response.json();
            if (data.error) throw new Error(data.error);
            renderMessages(data.messages || []);
        } catch (err) {
            console.error(err);
            messagesContainer.innerHTML = '<div class="alert alert-danger m-3">Ошибка загрузки сообщений</div>';
        }
    }

    function renderMessages(messages) {
        if (!messagesContainer) return;
        if (messages.length === 0) {
            messagesContainer.innerHTML = '<div class="text-center text-muted py-5"><i class="fas fa-comments fa-3x mb-3 opacity-50"></i><h5>Чат с поддержкой</h5><p>Задайте вопрос, и мы ответим!</p></div>';
            return;
        }
        let html = '';
        messages.forEach(m => {
            const isUser = m.messageType === 1;
            const time = new Date(m.createdAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
            let attachmentHtml = '';
            if (m.attachmentUrl) {
                if (m.attachmentType && m.attachmentType.startsWith('image/')) {
                    attachmentHtml = `<div class="message-attachment mt-2"><img src="${m.attachmentUrl}" class="img-fluid rounded" style="max-height:200px" onclick="window.open(this.src)"></div>`;
                } else {
                    attachmentHtml = `<div class="message-attachment mt-2"><a href="${m.attachmentUrl}" target="_blank" class="btn btn-sm btn-outline-secondary"><i class="fas fa-file"></i> Файл</a></div>`;
                }
            }
            html += `
                <div class="message mb-3 ${isUser ? 'user-message' : 'admin-message'}">
                    <div class="d-flex ${isUser ? 'justify-content-end' : 'justify-content-start'}">
                        <div class="message-bubble p-3 rounded-3 ${isUser ? 'bg-primary text-white' : 'bg-light'}">
                            <div class="message-header mb-2">
                                <span class="message-sender fw-bold">${escapeHtml(m.senderName)}</span>
                                <span class="message-time text-muted ms-2">${time}</span>
                            </div>
                            <div class="message-body">${linkify(escapeHtml(m.message))}</div>
                            ${attachmentHtml}
                        </div>
                    </div>
                </div>
            `;
        });
        messagesContainer.innerHTML = html;
        scrollToBottom();
    }

    // ---------- Инициализация чата ----------
    async function initChat() {
        try {
            messagesContainer.innerHTML = '<div class="text-center text-muted py-5"><i class="fas fa-spinner fa-spin fa-2x mb-3"></i><p>Загрузка чата...</p></div>';
            let url = '/Chat/GetOrCreate';
            if (isAuthenticated && userId) {
                url += `?userId=${userId}`;
            } else {
                tempUserId = getCookie('ChatTempUserId');
                if (tempUserId) url += `?tempUserId=${tempUserId}`;
            }
            const response = await fetch(url);
            const data = await response.json();
            if (!data.success) throw new Error(data.error || 'Не удалось создать чат');
            currentChatId = data.chatId;
            if (chatIdHidden) chatIdHidden.value = currentChatId;
            if (!isAuthenticated && data.tempUserId) {
                tempUserId = data.tempUserId;
                setCookie('ChatTempUserId', tempUserId, 24);
            }
            await loadMessages();
        } catch (error) {
            console.error(error);
            messagesContainer.innerHTML = '<div class="alert alert-danger m-3">Не удалось загрузить чат. Попробуйте позже.</div>';
        }
    }

    // ---------- Отправка сообщения (только AJAX, без SignalR) ----------
    if (form) {
        // Полностью пересоздаём обработчик, чтобы избежать дублирования
        const newForm = form.cloneNode(true);
        form.parentNode.replaceChild(newForm, form);
        form = newForm;
        input = newForm.querySelector('#chatMessageInput');
        fileInput = newForm.querySelector('#fileInput');
        attachBtn = newForm.querySelector('#attachFileButton');
        
        // Обработка отправки
        newForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            const messageText = input.value.trim();
            const file = fileInput.files[0];
            
            // ✅ Разрешаем отправку, если есть текст ИЛИ файл
            if (!messageText && !file) return;

            // Блокируем форму
            input.disabled = true;
            if (attachBtn) attachBtn.disabled = true;
            const submitBtn = newForm.querySelector('button[type="submit"]');
            if (submitBtn) submitBtn.disabled = true;

            const formData = new FormData();
            formData.append('chatId', currentChatId);
            formData.append('message', messageText);  // может быть пустой строкой
            if (file) formData.append('attachment', file);
            if (!isAuthenticated) formData.append('tempUserId', tempUserId);

            try {
                const response = await fetch('/Chat/SendMessageWithAttachment', {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': token },
                    body: formData
                });
                const data = await response.json();
                if (data.success) {
                    if (data.message) addMessageToDOM(data.message);
                    // Очистка
                    input.value = '';
                    fileInput.value = '';
                    if (previewContainer) {
                        previewContainer.remove();
                        previewContainer = null;
                    }
                } else {
                    alert('Ошибка: ' + (data.error || 'Не удалось отправить'));
                }
            } catch (err) {
                console.error(err);
                alert('Ошибка отправки сообщения');
            } finally {
                input.disabled = false;
                if (attachBtn) attachBtn.disabled = false;
                if (submitBtn) submitBtn.disabled = false;
                input.focus();
            }
        });
        
        // Кнопка прикрепления файла
        if (attachBtn && fileInput) {
            attachBtn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                fileInput.click();
            });
        }
    }

    // ---------- Открытие модального окна ----------
    openChatBtn.addEventListener('click', async function() {
        chatModal.show();
        if (!currentChatId) await initChat();
    });

    // ---------- Закрытие модального окна ----------
    const closeBtn = modalElement.querySelector('.btn-close');
    if (closeBtn) closeBtn.addEventListener('click', () => chatModal.hide());
    modalElement.addEventListener('keydown', (e) => { if (e.key === 'Escape') chatModal.hide(); });
    modalElement.addEventListener('click', (e) => { if (e.target === modalElement) chatModal.hide(); });
});