document.addEventListener('DOMContentLoaded', function () {
    console.log('admin-chat.js инициализация');

    // Открытие чата при клике на элемент списка
    const chatItems = document.querySelectorAll('.chat-list-item');
    chatItems.forEach(item => {
        item.addEventListener('click', function (e) {
            e.preventDefault();
            const chatId = this.dataset.chatId;
            if (chatId) {
                window.location.href = `/Admin/Chat?chatId=${chatId}`;
            }
        });
    });

    // Элементы формы
    const sendForm = document.getElementById('adminMessageForm');
    const input = document.getElementById('adminMessageInput');
    const attachBtn = document.getElementById('adminAttachButton');
    const fileInput = document.getElementById('adminFileInput');
    const chatIdHidden = document.getElementById('adminChatId');

    if (sendForm) {
        let isSending = false;

        // Прикрепление файла
        if (attachBtn) {
            attachBtn.addEventListener('click', () => {
                fileInput.click();
            });
        }

        sendForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            if (isSending) return;

            const message = input.value.trim();
            const file = fileInput?.files[0];
            if (!message && !file) return;

            const chatId = chatIdHidden?.value;
            if (!chatId) {
                alert('Чат не выбран');
                return;
            }

            const token = getAntiForgeryToken();
            if (!token) {
                alert('Ошибка безопасности: токен не найден');
                return;
            }

            isSending = true;
            input.disabled = true;
            if (attachBtn) attachBtn.disabled = true;

            const formData = new FormData();
            formData.append('chatId', chatId);
            formData.append('message', message);
            if (file) {
                formData.append('attachment', file);
            }

            try {
                const response = await fetch('/Admin/Chat/SendMessageWithAttachment', {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token
                    },
                    body: formData
                });

                const data = await response.json();
                if (data.success) {
                    input.value = '';
                    if (fileInput) fileInput.value = '';
                    await loadAdminMessages(chatId);
                } else {
                    alert('Ошибка: ' + (data.error || 'Неизвестная ошибка'));
                }
            } catch (err) {
                console.error('Send failed', err);
                alert('Ошибка отправки сообщения');
            } finally {
                input.disabled = false;
                if (attachBtn) attachBtn.disabled = false;
                isSending = false;
                input.focus();
            }
        });
    }

    // Загрузка сообщений
    async function loadAdminMessages(chatId) {
        try {
            const res = await fetch(`/Chat/Messages?chatId=${chatId}`);
            const data = await res.json();
            if (data.error) {
                console.error(data.error);
                return;
            }
            renderAdminMessages(data.messages || []);
            markMessagesAsRead(chatId);
        } catch (err) {
            console.error(err);
        }
    }

    async function markMessagesAsRead(chatId) {
        try {
            const token = getAntiForgeryToken();
            await fetch('/Chat/MarkAsRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ chatId: chatId })
            });
        } catch (err) {
            console.error(err);
        }
    }

    // Функция для преобразования ссылок в кликабельные
    function linkify(text) {
        if (!text) return text;
        const urlPattern = /(\b(https?:\/\/|www\.)[^\s]+)|(\/[^\s]*)/g;
        return text.replace(urlPattern, function(match) {
            if (match.startsWith('http')) {
                return `<a href="${match}" target="_blank" rel="noopener noreferrer">${match}</a>`;
            } else if (match.startsWith('www')) {
                return `<a href="http://${match}" target="_blank" rel="noopener noreferrer">${match}</a>`;
            } else if (match.startsWith('/')) {
                return `<a href="${match}" target="_blank">${match}</a>`;
            }
            return match;
        });
    }

    function renderAdminMessages(messages) {
        const container = document.getElementById('chatMessages');
        if (!container) return;

        let html = '';
        messages.forEach(msg => {
            const isUser = msg.messageType === 1; // User = 1
            const time = new Date(msg.createdAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });

            let messageHtml = `<div class="message mb-2 ${isUser ? 'user-message' : 'admin-message'}">
                        <div class="d-flex ${isUser ? 'justify-content-start' : 'justify-content-end'}">
                            <div class="message-content p-2 rounded ${isUser ? 'bg-light' : 'bg-primary text-white'}">
                                <div class="d-flex justify-content-between">
                                    <strong>${escapeHtml(msg.senderName)}</strong>
                                    <small>${time}</small>
                                </div>
                                <div>${linkify(escapeHtml(msg.message))}</div>`;

            if (msg.attachmentUrl) {
                if (msg.attachmentType && msg.attachmentType.startsWith('image/')) {
                    messageHtml += `<div class="mt-2">
                        <img src="${msg.attachmentUrl}" class="img-fluid rounded" style="max-height: 150px;" onclick="window.open(this.src)" />
                    </div>`;
                } else {
                    messageHtml += `<div class="mt-2">
                        <a href="${msg.attachmentUrl}" target="_blank" class="btn btn-sm btn-outline-secondary">
                            <i class="fas fa-file"></i> Файл
                        </a>
                    </div>`;
                }
            }

            messageHtml += `</div></div></div>`;
            html += messageHtml;
        });

        container.innerHTML = html;
        container.scrollTop = container.scrollHeight;
    }

    // Удаление чата (кнопка внутри чата)
    const deleteBtn = document.getElementById('deleteChatBtn');
    if (deleteBtn) {
        let isDeleting = false;
        deleteBtn.addEventListener('click', async function () {
            if (isDeleting) return;
            if (!confirm('Вы уверены, что хотите удалить этот чат? Все сообщения будут безвозвратно удалены.')) return;
            isDeleting = true;

            const chatId = chatIdHidden?.value;
            if (!chatId) {
                isDeleting = false;
                return;
            }

            const token = getAntiForgeryToken();
            try {
                const response = await fetch('/Admin/Chat/DeleteChat', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify({ chatId: chatId })
                });
                const data = await response.json();
                if (data.success) {
                    window.location.href = '/Admin/Chat';
                } else {
                    alert('Ошибка: ' + data.error);
                    isDeleting = false;
                }
            } catch (err) {
                console.error('Delete failed', err);
                alert('Ошибка при удалении чата');
                isDeleting = false;
            }
        });
    }

    // Удаление чата из списка (кнопки-корзины)
    document.querySelectorAll('.delete-chat-from-list').forEach(btn => {
        let isDeleting = false;
        btn.addEventListener('click', async function(e) {
            e.stopPropagation();
            if (isDeleting) return;
            if (!confirm('Удалить этот чат? Все сообщения будут безвозвратно удалены.')) return;
            isDeleting = true;

            const chatId = this.dataset.chatId;
            const token = getAntiForgeryToken();
            try {
                const response = await fetch('/Admin/Chat/DeleteChat', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify({ chatId: chatId })
                });
                const data = await response.json();
                if (data.success) {
                    window.location.href = '/Admin/Chat';
                } else {
                    alert('Ошибка: ' + data.error);
                }
            } catch (err) {
                console.error('Delete failed', err);
                alert('Ошибка при удалении чата');
            } finally {
                isDeleting = false;
            }
        });
    });

    function getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    }

    function escapeHtml(unsafe) {
        if (!unsafe) return '';
        return unsafe.replace(/[&<>"]/g, function(m) {
            if (m === '&') return '&amp;';
            if (m === '<') return '&lt;';
            if (m === '>') return '&gt;';
            if (m === '"') return '&quot;';
            return m;
        });
    }

    // Если на странице уже есть chatId, загружаем сообщения
    const initialChatId = chatIdHidden?.value;
    if (initialChatId) {
        loadAdminMessages(initialChatId);
    }
});