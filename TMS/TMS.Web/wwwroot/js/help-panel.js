/**
* help-panel.js — Edudigi LMS Help & Support Panel
* Toggle dropdown + Quick Guide modal logic.
*/
 
(function () {
    'use strict';
 
    var trigger, panel, guideOverlay, guideOpenBtn, guideCloseBtn;
 
    function init() {
        trigger       = document.getElementById('helpPanelTrigger');
        panel         = document.getElementById('helpPanel');
        guideOverlay  = document.getElementById('helpGuideOverlay');
        guideOpenBtn  = document.getElementById('helpGuideOpen');
        guideCloseBtn = document.getElementById('helpGuideClose');
 
        if (!trigger || !panel) return;
 
        /* Toggle panel on trigger click */
        trigger.addEventListener('click', function (e) {
            e.stopPropagation();
            togglePanel();
        });
 
        /* Close on outside click */
        document.addEventListener('click', function (e) {
            if (!trigger.contains(e.target) && !panel.contains(e.target)) {
                closePanel();
            }
        });
 
        /* Close on Escape */
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                closePanel();
                closeGuide();
            }
        });
 
        /* Quick Guide open */
        if (guideOpenBtn && guideOverlay) {
            guideOpenBtn.addEventListener('click', function (e) {
                e.preventDefault();
                closePanel();
                openGuide();
            });
        }
 
        /* Quick Guide close */
        if (guideCloseBtn && guideOverlay) {
            guideCloseBtn.addEventListener('click', closeGuide);
 
            /* Close on overlay backdrop click */
            guideOverlay.addEventListener('click', function (e) {
                if (e.target === guideOverlay) closeGuide();
            });
        }
    }
 
    function openPanel() {
        panel.classList.add('panel-visible');
        trigger.classList.add('help-open');
        trigger.setAttribute('aria-expanded', 'true');
    }
 
    function closePanel() {
        panel.classList.remove('panel-visible');
        trigger.classList.remove('help-open');
        trigger.setAttribute('aria-expanded', 'false');
    }
 
    function togglePanel() {
        if (panel.classList.contains('panel-visible')) {
            closePanel();
        } else {
            openPanel();
        }
    }
 
    function openGuide() {
        if (!guideOverlay) return;
        guideOverlay.classList.add('guide-visible');
        document.body.style.overflow = 'hidden';
    }
 
    function closeGuide() {
        if (!guideOverlay) return;
        guideOverlay.classList.remove('guide-visible');
        document.body.style.overflow = '';
    }
 
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
