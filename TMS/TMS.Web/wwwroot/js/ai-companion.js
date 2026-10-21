/**
 * ═══════════════════════════════════════════════════════════════════════
 * EDUDIGI AI LEARNING COMPANION — Universal Floating Tutor Controller
 * Context-aware, session-persisted, Markdown-rendered, bilingual support.
 * ═══════════════════════════════════════════════════════════════════════
 */

(function () {
    'use strict';

    const STORAGE_KEY = 'edudigi_ai_chat_session';
    const STATE_KEY = 'edudigi_ai_chat_state';

    const Companion = {
        isOpen: false,
        isMinimized: false,
        isSending: false,
        activeCourseId: null,
        activeUnitId: null,
        activeMaterialId: null,
        activeQuadrantId: null,
        enrolledCourses: [],
        messages: [],

        init: function () {
            this.cacheDom();
            if (!this.dom.launcher) return;

            this.detectContextFromUrl();
            this.bindEvents();
            this.loadSession();
            this.fetchEnrolledCourses();
            this.fetchActiveContext();
        },

        cacheDom: function () {
            this.dom = {
                launcher: document.getElementById('aiCompanionLauncher'),
                drawer: document.getElementById('aiCompanionDrawer'),
                btnClose: document.getElementById('aiBtnClose'),
                btnMinimize: document.getElementById('aiBtnMinimize'),
                btnClear: document.getElementById('aiBtnClear'),
                messagesBody: document.getElementById('aiDrawerBody'),
                quickActions: document.getElementById('aiQuickActions'),
                textarea: document.getElementById('aiInputTextarea'),
                btnSend: document.getElementById('aiBtnSend'),
                contextCourseName: document.getElementById('aiContextCourseName'),
                contextUnitName: document.getElementById('aiContextUnitName'),
                btnSwitchCourse: document.getElementById('aiBtnSwitchCourse'),
                courseSwitcher: document.getElementById('aiCourseSwitcher'),
                courseList: document.getElementById('aiCourseList'),
                typingIndicator: document.getElementById('aiTypingIndicator')
            };
        },

        detectContextFromUrl: function () {
            const path = window.location.pathname;

            // Pattern 1: /SubjectHub/{courseId}
            const matchHub = path.match(/\/SubjectHub\/(\d+)/i);
            if (matchHub) {
                this.activeCourseId = parseInt(matchHub[1], 10);
            }

            // Pattern 2: /Subject/{courseId}/Unit/{unitId}
            const matchUnit = path.match(/\/Subject\/(\d+)\/Unit\/(\d+)/i);
            if (matchUnit) {
                this.activeCourseId = parseInt(matchUnit[1], 10);
                this.activeUnitId = parseInt(matchUnit[2], 10);
            }

            // Pattern 3: /Quadrant/{quadrantId}
            const matchQuad = path.match(/\/Quadrant\/(\d+)/i);
            if (matchQuad) {
                this.activeQuadrantId = parseInt(matchQuad[1], 10);
            }

            // Also check query string e.g. ?courseId=...&unitId=...
            const urlParams = new URLSearchParams(window.location.search);
            if (urlParams.has('courseId') && !this.activeCourseId) {
                this.activeCourseId = parseInt(urlParams.get('courseId'), 10);
            }
            if (urlParams.has('unitId') && !this.activeUnitId) {
                this.activeUnitId = parseInt(urlParams.get('unitId'), 10);
            }
            if (urlParams.has('materialId')) {
                this.activeMaterialId = parseInt(urlParams.get('materialId'), 10);
            }
        },

        bindEvents: function () {
            const self = this;

            this.dom.launcher.addEventListener('click', function () {
                self.toggleOpen();
            });

            this.dom.btnClose.addEventListener('click', function () {
                self.close();
            });

            this.dom.btnMinimize.addEventListener('click', function () {
                self.toggleMinimize();
            });

            this.dom.btnClear.addEventListener('click', function () {
                if (confirm('Clear current tutoring conversation history?')) {
                    self.clearConversation();
                }
            });

            if (this.dom.btnSwitchCourse) {
                this.dom.btnSwitchCourse.addEventListener('click', function () {
                    self.dom.courseSwitcher.classList.toggle('ai-show');
                });
            }

            this.dom.textarea.addEventListener('input', function () {
                self.adjustTextareaHeight();
                self.dom.btnSend.disabled = !this.value.trim() || self.isSending;
            });

            this.dom.textarea.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    if (!self.dom.btnSend.disabled) {
                        self.sendMessage();
                    }
                }
            });

            this.dom.btnSend.addEventListener('click', function () {
                self.sendMessage();
            });
        },

        adjustTextareaHeight: function () {
            const el = this.dom.textarea;
            el.style.height = 'auto';
            el.style.height = Math.min(el.scrollHeight, 100) + 'px';
        },

        toggleOpen: function () {
            if (this.isOpen) {
                if (this.isMinimized) {
                    this.isMinimized = false;
                    this.dom.drawer.classList.remove('ai-minimized');
                } else {
                    this.close();
                }
            } else {
                this.open();
            }
        },

        open: function () {
            this.isOpen = true;
            this.isMinimized = false;
            this.dom.drawer.classList.remove('ai-hidden');
            this.dom.drawer.classList.remove('ai-minimized');
            this.dom.textarea.focus();
            this.scrollToBottom();
            this.saveState();
        },

        close: function () {
            this.isOpen = false;
            this.dom.drawer.classList.add('ai-hidden');
            this.saveState();
        },

        toggleMinimize: function () {
            this.isMinimized = !this.isMinimized;
            this.dom.drawer.classList.toggle('ai-minimized', this.isMinimized);
            this.saveState();
        },

        loadSession: function () {
            try {
                const saved = sessionStorage.getItem(STORAGE_KEY);
                if (saved) {
                    this.messages = JSON.parse(saved);
                    this.renderAllMessages();
                } else {
                    this.addWelcomeMessage();
                }

                const state = sessionStorage.getItem(STATE_KEY);
                if (state) {
                    const parsed = JSON.parse(state);
                    if (parsed.isOpen) {
                        this.open();
                        if (parsed.isMinimized) {
                            this.toggleMinimize();
                        }
                    }
                }
            } catch (e) {
                this.addWelcomeMessage();
            }
        },

        saveSession: function () {
            try {
                sessionStorage.setItem(STORAGE_KEY, JSON.stringify(this.messages));
            } catch (e) {}
        },

        saveState: function () {
            try {
                sessionStorage.setItem(STATE_KEY, JSON.stringify({
                    isOpen: this.isOpen,
                    isMinimized: this.isMinimized
                }));
            } catch (e) {}
        },

        addWelcomeMessage: function () {
            const isUrdu = document.documentElement.getAttribute('dir') === 'rtl';
            const welcomeText = isUrdu
                ? 'السلام علیکم! میں آپ کا **ایڈو ڈیجی لرننگ ساتھی** (AI Companion) ہوں۔\n\nآپ مجھ سے اپنے تعلیمی ریکارڈ کے بارے میں پوچھ سکتے ہیں:\n- 📊 **حاضری** ("میری حاضری کتنی ہے؟")\n- 📝 **مارکس اور نتائج** ("Show my grades")\n- 📚 **کورسز** ("What are my enrolled courses?")\n- ⏰ **اسائنمنٹس** ("What assignments are due?")\n- 📅 **کلاس کا شیڈول** ("When is my next class?")\n\nاور کورس کے اہم تصورات و نوٹس سمجھنے کے لیے سوال کریں۔'
                : 'Hello! I am your **EduDigi AI Learning Companion**.\n\nI can retrieve your official LMS records directly without requiring external AI keys:\n- 📊 **"What is my attendance?"** *(Classroom & daily sessions)*\n- 📝 **"Show my grades"** *(Internal assessments & exam results)*\n- 📚 **"What are my enrolled courses?"**\n- ⏰ **"What assignments are due?"**\n- 📅 **"What is my class schedule?"**\n\nYou can also ask me conceptual questions about your course materials and lecture notes!';

            this.messages = [{
                role: 'assistant',
                text: welcomeText,
                time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
            }];
            this.renderAllMessages();
            this.saveSession();
        },

        clearConversation: function () {
            this.messages = [];
            this.addWelcomeMessage();
            this.dom.courseSwitcher.classList.remove('ai-show');
        },

        fetchEnrolledCourses: function () {
            const self = this;
            fetch('/AiLearningAssistant/EnrolledCourses')
                .then(r => r.json())
                .then(data => {
                    if (data && data.success && data.courses) {
                        self.enrolledCourses = data.courses;
                        self.renderCourseSwitcher();
                    }
                })
                .catch(() => {});
        },

        fetchActiveContext: function () {
            const self = this;
            const params = new URLSearchParams({
                pageUrl: window.location.pathname,
                courseId: self.activeCourseId || 0,
                unitId: self.activeUnitId || 0,
                materialId: self.activeMaterialId || 0
            });

            fetch('/AiLearningAssistant/Context?' + params.toString())
                .then(r => r.json())
                .then(data => {
                    if (data && data.success && data.context) {
                        self.updateContextBar(data.context);
                    }
                })
                .catch(() => {});
        },

        renderCourseSwitcher: function () {
            const self = this;
            if (!this.dom.courseList) return;

            let html = '';
            if (this.enrolledCourses.length === 0) {
                html = '<div style="font-size:11px;color:#94a3b8;padding:6px;">No enrolled courses found.</div>';
            } else {
                this.enrolledCourses.forEach(c => {
                    const isActive = self.activeCourseId === c.courseId ? 'active' : '';
                    html += `
                        <div class="ai-course-item ${isActive}" data-course-id="${c.courseId}">
                            <span><strong>${self.escapeHtml(c.courseCode)}</strong> - ${self.escapeHtml(c.courseName)}</span>
                            ${isActive ? '<i class="fa-solid fa-check text-primary"></i>' : ''}
                        </div>`;
                });
            }

            this.dom.courseList.innerHTML = html;

            // Bind click to switch course
            this.dom.courseList.querySelectorAll('.ai-course-item').forEach(el => {
                el.addEventListener('click', function () {
                    const cId = parseInt(this.getAttribute('data-course-id'), 10);
                    self.activeCourseId = cId;
                    self.activeUnitId = null;
                    self.dom.courseSwitcher.classList.remove('ai-show');
                    self.fetchActiveContext();
                    self.renderCourseSwitcher();

                    const selected = self.enrolledCourses.find(c => c.courseId === cId);
                    if (selected) {
                        self.addMessage('assistant', `Switched active course focus to **${self.escapeHtml(selected.courseName)}** (${self.escapeHtml(selected.courseCode)}). What would you like to explore?`);
                    }
                });
            });
        },

        updateContextBar: function (ctx) {
            if (ctx.courseName) {
                this.dom.contextCourseName.textContent = (ctx.courseCode ? ctx.courseCode + ': ' : '') + ctx.courseName;
            } else {
                this.dom.contextCourseName.textContent = ctx.pageName || 'LMS Portal';
            }

            if (ctx.unitName) {
                this.dom.contextUnitName.textContent = '• ' + ctx.unitName;
                this.dom.contextUnitName.style.display = 'inline';
            } else {
                this.dom.contextUnitName.style.display = 'none';
            }
        },

        renderAllMessages: function () {
            const self = this;
            this.dom.messagesBody.innerHTML = '';
            this.messages.forEach(m => {
                self.renderMessageNode(m);
            });
            this.scrollToBottom();
        },

        renderMessageNode: function (m) {
            const msgEl = document.createElement('div');
            msgEl.className = 'ai-message ' + (m.role === 'assistant' ? 'ai-msg-assistant' : 'ai-msg-user');

            let bodyHtml = '';
            if (m.role === 'assistant') {
                bodyHtml = this.parseMarkdown(m.text);
            } else {
                bodyHtml = this.escapeHtml(m.text).replace(/\n/g, '<br/>');
            }

            let sourcesHtml = '';
            if (m.sources && m.sources.length > 0) {
                sourcesHtml = `<div class="ai-msg-sources"><i class="fa-solid fa-book-bookmark me-1"></i>Sources: ${this.escapeHtml(m.sources.join(' • '))}</div>`;
            }

            msgEl.innerHTML = `
                <div class="ai-msg-bubble">
                    ${bodyHtml}
                    ${sourcesHtml}
                </div>
                <div class="ai-msg-meta">
                    <span>${m.time || ''}</span>
                </div>
            `;

            this.dom.messagesBody.appendChild(msgEl);
        },

        addMessage: function (role, text, sources, suggestedActions) {
            const msg = {
                role: role,
                text: text,
                sources: sources || null,
                time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
            };
            this.messages.push(msg);
            this.renderMessageNode(msg);
            this.saveSession();
            this.scrollToBottom();

            if (suggestedActions && suggestedActions.length > 0) {
                this.renderQuickActions(suggestedActions);
            }
        },

        renderQuickActions: function (actions) {
            const self = this;
            if (!this.dom.quickActions) return;

            let html = '';
            actions.forEach(a => {
                const icon = a.icon ? `<i class="${self.escapeHtml(a.icon)}"></i>` : '<i class="fa-solid fa-sparkles"></i>';
                html += `
                    <button type="button" class="ai-pill-btn" data-prompt="${self.escapeHtml(a.prompt)}">
                        ${icon} ${self.escapeHtml(a.label)}
                    </button>`;
            });

            this.dom.quickActions.innerHTML = html;

            this.dom.quickActions.querySelectorAll('.ai-pill-btn').forEach(btn => {
                btn.addEventListener('click', function () {
                    const prompt = this.getAttribute('data-prompt');
                    if (prompt && !self.isSending) {
                        self.dom.textarea.value = prompt;
                        self.sendMessage();
                    }
                });
            });
        },

        sendMessage: function () {
            const self = this;
            const text = this.dom.textarea.value.trim();
            if (!text || this.isSending) return;

            // 1. Append user message to UI
            this.addMessage('user', text);
            this.dom.textarea.value = '';
            this.dom.textarea.style.height = 'auto';
            this.dom.btnSend.disabled = true;
            this.isSending = true;

            // 2. Show typing indicator
            this.showTyping(true);

            // 3. Prepare payload with recent conversation history
            const history = this.messages.slice(-8).map(m => ({
                role: m.role === 'assistant' ? 'model' : 'user',
                text: m.text
            }));

            const payload = {
                message: text,
                courseId: this.activeCourseId,
                unitId: this.activeUnitId,
                quadrantId: this.activeQuadrantId,
                materialId: this.activeMaterialId,
                pageUrl: window.location.pathname,
                pageTitle: document.title,
                conversationHistory: history
            };

            fetch('/AiLearningAssistant/Ask', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            })
            .then(r => r.json())
            .then(data => {
                self.showTyping(false);
                self.isSending = false;
                self.dom.btnSend.disabled = !self.dom.textarea.value.trim();

                if (data && data.success) {
                    self.addMessage('assistant', data.reply, data.sources, data.suggestedActions);
                    if (data.detectedContext) {
                        self.updateContextBar(data.detectedContext);
                    }
                } else {
                    const errReply = data.reply || (data.errorMessage ? `⚠️ ${data.errorMessage}` : 'I could not process that request. Please try again.');
                    self.addMessage('assistant', errReply);
                }
            })
            .catch(err => {
                self.showTyping(false);
                self.isSending = false;
                self.dom.btnSend.disabled = !self.dom.textarea.value.trim();
                self.addMessage('assistant', '⚠️ Network connection issue. Please check your internet connection and try again.');
            });
        },

        showTyping: function (show) {
            if (!this.dom.typingIndicator) return;
            this.dom.typingIndicator.style.display = show ? 'inline-flex' : 'none';
            if (show) this.scrollToBottom();
        },

        scrollToBottom: function () {
            const el = this.dom.messagesBody;
            if (el) {
                el.scrollTop = el.scrollHeight;
            }
        },

        parseMarkdown: function (raw) {
            if (!raw) return '';
            if (typeof window.marked !== 'undefined' && typeof window.marked.parse === 'function') {
                try {
                    return window.marked.parse(raw);
                } catch (e) {}
            }

            // Fallback safe basic markdown parser
            let txt = this.escapeHtml(raw);
            txt = txt.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
            txt = txt.replace(/\*(.*?)\*/g, '<em>$1</em>');
            txt = txt.replace(/`([^`]+)`/g, '<code>$1</code>');
            txt = txt.replace(/\n\n/g, '<br/><br/>');
            txt = txt.replace(/\n- (.*)/g, '<br/>• $1');
            return txt;
        },

        escapeHtml: function (str) {
            if (!str) return '';
            return str
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#039;');
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        Companion.init();
    });

    window.EduDigiCompanion = Companion;
})();
