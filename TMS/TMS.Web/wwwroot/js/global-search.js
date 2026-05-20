/**
 * global-search.js — Edudigi LMS Global Search
 * Production-level autocomplete with keyboard navigation, debounce, and redirect.
 */
 
(function () {
    'use strict';
 
    /* ─────────────────────────────────────────────────────────
       SEARCH INDEX — All navigable LMS modules
    ───────────────────────────────────────────────────────── */
    const SEARCH_INDEX = [
        // ── Learn ──────────────────────────────────────────────
        {
            title: 'My Courses',
            subtitle: 'Browse & continue your enrolled courses',
            url: '/StudentCourseContent/Index',
            icon: 'fa-solid fa-layer-group',
            iconBg: '#f5f3ff', iconColor: '#8b5cf6',
            tags: ['courses', 'my courses', 'subject', 'lesson', 'learn', 'enrolled']
        },
        {
            title: 'Class Calendar',
            subtitle: 'View your class schedule and events',
            url: '/Calendar/Index',
            icon: 'fa-solid fa-calendar-days',
            iconBg: '#ecfdf5', iconColor: '#10b981',
            tags: ['calendar', 'schedule', 'class', 'timetable', 'date', 'events']
        },
        {
            title: 'Virtual Classroom',
            subtitle: 'Join live online sessions and meetings',
            url: '/VirtualClassroom/Index',
            icon: 'fa-solid fa-chalkboard-user',
            iconBg: '#fff1f2', iconColor: '#f43f5e',
            tags: ['virtual', 'classroom', 'live', 'session', 'online class', 'video', 'meeting', 'zoom']
        },
 
        // ── Assessments ───────────────────────────────────────
        {
            title: 'Today\'s Exams',
            subtitle: 'Check exams scheduled for today',
            url: '/Home/TodayExams',
            icon: 'fa-solid fa-journal-check',
            iconBg: '#fef9c3', iconColor: '#ca8a04',
            tags: ['exam', 'exams', 'today', 'test', 'assessment', 'paper']
        },
        {
            title: 'Assignments & Quizzes',
            subtitle: 'View and submit your assignments',
            url: '/Assignments/Index',
            icon: 'fa-solid fa-file-pen',
            iconBg: '#ecfdf5', iconColor: '#10b981',
            tags: ['assignment', 'assignments', 'quiz', 'quizzes', 'homework', 'task', 'submit']
        },
        {
            title: 'Internal Assessment',
            subtitle: 'Internal marks and IA records',
            url: '/InternalAssessment/Index',
            icon: 'fa-solid fa-clipboard-list',
            iconBg: '#eff6ff', iconColor: '#3b82f6',
            tags: ['internal', 'assessment', 'ia', 'marks', 'cia']
        },
        {
            title: 'Exam Form',
            subtitle: 'Fill and submit your exam form',
            url: '/Exam/ExamForm',
            icon: 'fa-solid fa-clipboard-check',
            iconBg: '#f5f3ff', iconColor: '#8b5cf6',
            tags: ['exam form', 'form', 'hall ticket', 'registration', 'enroll exam']
        },
        {
            title: 'External Exam',
            subtitle: 'External examination schedule and info',
            url: '/Exam/ExternalExam',
            icon: 'fa-solid fa-file-contract',
            iconBg: '#fff7ed', iconColor: '#f97316',
            tags: ['external exam', 'university exam', 'semester exam', 'board']
        },
 
        // ── Attendance ────────────────────────────────────────
        {
            title: 'My Attendance',
            subtitle: 'Track your subject-wise attendance',
            url: '/Attendance/MyAttendance',
            icon: 'fa-solid fa-calendar-check',
            iconBg: '#ecfdf5', iconColor: '#10b981',
            tags: ['attendance', 'present', 'absent', 'leave', 'track', 'percentage', 'record']
        },
 
        // ── Results / Report Card ──────────────────────────────
        {
            title: 'Personalized Report Card',
            subtitle: 'Download your official report card',
            url: '/Report/Personalized',
            icon: 'fa-solid fa-id-card',
            iconBg: '#fdf2f8', iconColor: '#ec4899',
            tags: ['report', 'report card', 'result', 'grade', 'marks', 'scorecard', 'transcript', 'gpa', 'cgpa']
        },
        {
            title: 'Progress Report',
            subtitle: 'Detailed progress analytics',
            url: '/Report/Progress',
            icon: 'fa-solid fa-chart-line',
            iconBg: '#f5f3ff', iconColor: '#6366f1',
            tags: ['progress', 'analytics', 'performance', 'graph', 'trend', 'report']
        },
        {
            title: 'Badges & Achievements',
            subtitle: 'View earned badges and milestones',
            url: '/Report/Badges',
            icon: 'fa-solid fa-star',
            iconBg: '#fefce8', iconColor: '#f59e0b',
            tags: ['badge', 'badges', 'achievement', 'award', 'milestone', 'points']
        },
        {
            title: 'Certificates',
            subtitle: 'Download your course certificates',
            url: '/Report/Certificate',
            icon: 'fa-solid fa-award',
            iconBg: '#ecfdf5', iconColor: '#10b981',
            tags: ['certificate', 'certificates', 'certification', 'download', 'completion']
        },
 
        // ── Fees ──────────────────────────────────────────────
        {
            title: 'Online Fee Payment',
            subtitle: 'Pay your semester and tuition fees',
            url: '/Fees/OnlinePayment',
            icon: 'fa-solid fa-credit-card',
            iconBg: '#eff6ff', iconColor: '#6366f1',
            tags: ['fees', 'fee', 'payment', 'pay', 'online payment', 'tuition', 'challan', 'invoice']
        },
        {
            title: 'Bank Transfer Payment',
            subtitle: 'Fee payment via bank transfer / NEFT',
            url: '/Fees/BankTransfer',
            icon: 'fa-solid fa-money-bill-transfer',
            iconBg: '#f0fdf4', iconColor: '#16a34a',
            tags: ['bank', 'bank transfer', 'neft', 'rtgs', 'imps', 'fee transfer']
        },
 
        // ── Collaborate ───────────────────────────────────────
        {
            title: 'Notifications & Inbox',
            subtitle: 'Messages, alerts and announcements',
            url: '/Notifications/Index',
            icon: 'fa-solid fa-bell',
            iconBg: '#fef9c3', iconColor: '#ca8a04',
            tags: ['notification', 'notifications', 'inbox', 'alert', 'message', 'announcement']
        },
        {
            title: 'Discussion Forum',
            subtitle: 'Participate in course discussions',
            url: '/Discussion/Index',
            icon: 'fa-solid fa-comments',
            iconBg: '#eff6ff', iconColor: '#2563eb',
            tags: ['discussion', 'forum', 'talk', 'thread', 'post', 'q&a', 'doubt']
        },
        {
            title: 'Announcements',
            subtitle: 'Official announcements from institution',
            url: '/Announcement/Board',
            icon: 'fa-solid fa-bullhorn',
            iconBg: '#fff7ed', iconColor: '#ea580c',
            tags: ['announcement', 'announcements', 'notice', 'news', 'board', 'official']
        },
        {
            title: 'Chat & Meeting',
            subtitle: 'Chat with peers and instructors',
            url: '/Chat/Index',
            icon: 'fa-solid fa-user-group',
            iconBg: '#fdf4ff', iconColor: '#a855f7',
            tags: ['chat', 'meeting', 'message', 'talk', 'group', 'video call', 'peer']
        },
 
        // ── Support ───────────────────────────────────────────
        {
            title: 'FAQ / Help',
            subtitle: 'Frequently asked questions and help center',
            url: '/Support/FAQ',
            icon: 'fa-solid fa-headphones',
            iconBg: '#f0fdf4', iconColor: '#16a34a',
            tags: ['support', 'faq', 'help', 'question', 'problem', 'issue', 'guide', 'how to']
        },
        {
            title: 'Contact Mentor',
            subtitle: 'Reach out directly to your mentor',
            url: '/Support/ContactMentor',
            icon: 'fa-solid fa-circle-question',
            iconBg: '#eff6ff', iconColor: '#2563eb',
            tags: ['mentor', 'contact', 'teacher', 'faculty', 'help', 'support', 'advisor']
        },
        {
            title: 'Raise a Support Ticket',
            subtitle: 'Report technical issues or requests',
            url: '/Support/RaiseTicket',
            icon: 'fa-solid fa-life-ring',
            iconBg: '#fff7ed', iconColor: '#ea580c',
            tags: ['ticket', 'raise ticket', 'complaint', 'report', 'issue', 'bug', 'technical']
        },
 
        // ── Dashboard ─────────────────────────────────────────
        {
            title: 'Dashboard',
            subtitle: 'Student home and overview',
            url: '/Home/StudentDashboard',
            icon: 'fa-solid fa-gauge-high',
            iconBg: '#eff6ff', iconColor: '#3b82f6',
            tags: ['dashboard', 'home', 'overview', 'main', 'start', 'portal']
        },
 
        // ── Instructions ──────────────────────────────────────
        {
            title: 'Instructions & How To',
            subtitle: 'Platform usage guides and instructions',
            url: '/Instructions/Index',
            icon: 'fa-solid fa-circle-info',
            iconBg: '#fff7ed', iconColor: '#f97316',
            tags: ['instructions', 'how to', 'guide', 'tutorial', 'manual', 'steps']
        },
    ];
 
    const MAX_RESULTS = 5;
    let debounceTimer = null;
    let focusedIndex = -1;
    let currentResults = [];
 
    /* ─────────────────────────────────────────────────────────
       DOM REFERENCES (resolved after DOMContentLoaded)
    ───────────────────────────────────────────────────────── */
    let wrapper, iconBadge, searchBar, inputEl, closeBtn, dropdown;
 
    function init() {
        wrapper   = document.getElementById('globalSearchWrapper');
        iconBadge = document.getElementById('globalSearchIcon');
        searchBar = document.getElementById('globalSearchBar');
        inputEl   = document.getElementById('globalSearchInput');
        closeBtn  = document.getElementById('globalSearchClose');
        dropdown  = document.getElementById('globalSearchDropdown');
 
        if (!wrapper) return; // layout not present on this page
 
        // Open search on icon click
        iconBadge.addEventListener('click', openSearch);
 
        // Close on ✕ button
        closeBtn.addEventListener('click', closeSearch);
 
        // Input: debounce + search
        inputEl.addEventListener('input', function () {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(function () {
                runSearch(inputEl.value.trim());
            }, 300);
        });
 
        // Keyboard navigation
        inputEl.addEventListener('keydown', handleKeyboard);
 
        // Close on outside click
        document.addEventListener('click', function (e) {
            if (!wrapper.contains(e.target)) {
                closeSearch();
            }
        });
 
        // Close on Escape key globally
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeSearch();
        });
    }
 
    /* ─────────────────────────────────────────────────────────
       OPEN / CLOSE
    ───────────────────────────────────────────────────────── */
    function openSearch() {
        wrapper.classList.add('search-active');
        inputEl.focus();
        if (inputEl.value.trim().length > 0) {
            runSearch(inputEl.value.trim());
        }
    }
 
    function closeSearch() {
        wrapper.classList.remove('search-active');
        hideDropdown();
        // Reset state but keep text so re-opening preserves it
    }
 
    /* ─────────────────────────────────────────────────────────
       SEARCH LOGIC
    ───────────────────────────────────────────────────────── */
    function runSearch(query) {
        focusedIndex = -1;
 
        if (!query || query.length < 1) {
            hideDropdown();
            return;
        }
 
        const q = query.toLowerCase();
 
        const scored = SEARCH_INDEX.map(function (item) {
            let score = 0;
            const titleLower = item.title.toLowerCase();
            const subtitleLower = item.subtitle.toLowerCase();
 
            // Exact title match → highest score
            if (titleLower === q) score += 100;
            // Title starts with query
            else if (titleLower.startsWith(q)) score += 60;
            // Title contains query
            else if (titleLower.includes(q)) score += 40;
            // Subtitle contains query
            else if (subtitleLower.includes(q)) score += 20;
 
            // Tag match
            item.tags.forEach(function (tag) {
                if (tag === q) score += 80;
                else if (tag.startsWith(q)) score += 50;
                else if (tag.includes(q)) score += 30;
            });
 
            return { item: item, score: score };
        }).filter(function (r) { return r.score > 0; })
          .sort(function (a, b) { return b.score - a.score; })
          .slice(0, MAX_RESULTS)
          .map(function (r) { return r.item; });
 
        currentResults = scored;
 
        if (scored.length === 0) {
            renderEmpty(query);
        } else {
            renderResults(scored, query);
        }
 
        showDropdown();
    }
 
    /* ─────────────────────────────────────────────────────────
       RENDER
    ───────────────────────────────────────────────────────── */
    function renderResults(results, query) {
        var listHtml = results.map(function (item, idx) {
            var titleHtml = highlightMatch(item.title, query);
            return '<li role="option" aria-selected="false">' +
                '<button class="search-result-item" data-idx="' + idx + '" data-url="' + item.url + '"' +
                ' style="width:100%;" tabindex="-1">' +
                '<span class="sri-icon" style="background:' + item.iconBg + '; color:' + item.iconColor + ';">' +
                '<i class="' + item.icon + '"></i>' +
                '</span>' +
                '<span class="sri-body">' +
                '<span class="sri-title">' + titleHtml + '</span>' +
                '<span class="sri-subtitle">' + escapeHtml(item.subtitle) + '</span>' +
                '</span>' +
                '<i class="fa-solid fa-arrow-right sri-arrow"></i>' +
                '</button></li>';
        }).join('');
 
        dropdown.innerHTML =
            '<div class="search-dropdown-header">' +
            '<span class="sd-label">Pages &amp; Modules</span>' +
            '<span class="search-match-count">' + results.length + ' result' + (results.length > 1 ? 's' : '') + '</span>' +
            '</div>' +
            '<ul class="search-results-list" role="listbox">' + listHtml + '</ul>' +
            '<div class="search-dropdown-footer">' +
            '<span class="search-key-hint"><kbd>↑</kbd><kbd>↓</kbd> navigate</span>' +
            '<span class="search-key-hint" style="margin-left:8px;"><kbd>↵</kbd> open</span>' +
            '<span class="search-key-hint" style="margin-left:8px;"><kbd>Esc</kbd> close</span>' +
            '</div>';
 
        // Attach click handlers
        dropdown.querySelectorAll('.search-result-item').forEach(function (btn) {
            btn.addEventListener('click', function () {
                navigateTo(btn.getAttribute('data-url'));
            });
        });
    }
 
    function renderEmpty(query) {
        dropdown.innerHTML =
            '<div class="search-no-results">' +
            '<i class="fa-solid fa-magnifying-glass"></i>' +
            '<p>No results for &ldquo;' + escapeHtml(query) + '&rdquo;</p>' +
            '<p style="font-size:11px; margin-top:4px; color:#cbd5e1;">Try: courses, attendance, exam, fees&hellip;</p>' +
            '</div>';
    }
 
    /* ─────────────────────────────────────────────────────────
       KEYBOARD NAVIGATION
    ───────────────────────────────────────────────────────── */
    function handleKeyboard(e) {
        var items = dropdown.querySelectorAll('.search-result-item');
        var total = items.length;
 
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            focusedIndex = (focusedIndex + 1) % total;
            updateFocus(items, focusedIndex);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            focusedIndex = (focusedIndex - 1 + total) % total;
            updateFocus(items, focusedIndex);
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (focusedIndex >= 0 && currentResults[focusedIndex]) {
                navigateTo(currentResults[focusedIndex].url);
            } else if (currentResults.length === 1) {
                navigateTo(currentResults[0].url);
            } else if (inputEl.value.trim()) {
                // Fallback: search nothing found; do nothing (already empty state)
            }
        } else if (e.key === 'Escape') {
            closeSearch();
        }
    }
 
    function updateFocus(items, idx) {
        items.forEach(function (el, i) {
            el.classList.toggle('item-focused', i === idx);
        });
        if (items[idx]) {
            items[idx].scrollIntoView({ block: 'nearest' });
        }
    }
 
    /* ─────────────────────────────────────────────────────────
       DROPDOWN VISIBILITY
    ───────────────────────────────────────────────────────── */
    function showDropdown() {
        dropdown.classList.add('dropdown-visible');
    }
 
    function hideDropdown() {
        dropdown.classList.remove('dropdown-visible');
        focusedIndex = -1;
    }
 
    /* ─────────────────────────────────────────────────────────
       NAVIGATION
    ───────────────────────────────────────────────────────── */
    function navigateTo(url) {
        if (url) {
            window.location.href = url;
        }
    }
 
    /* ─────────────────────────────────────────────────────────
       UTILITIES
    ───────────────────────────────────────────────────────── */
    function highlightMatch(text, query) {
        if (!query) return escapeHtml(text);
        var escaped = escapeHtml(text);
        var escapedQuery = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        try {
            var re = new RegExp('(' + escapedQuery + ')', 'gi');
            return escaped.replace(re, '<mark>$1</mark>');
        } catch (e) {
            return escaped;
        }
    }
 
    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }
 
    /* ─────────────────────────────────────────────────────────
       BOOT
    ───────────────────────────────────────────────────────── */
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
 
})();
