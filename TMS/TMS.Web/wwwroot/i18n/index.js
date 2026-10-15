/**
 * Centralized i18n Localization Manifest for LMS Admin & Faculty Portals
 * Contains modular bundle configurations for English and Urdu dictionaries.
 */
(function(window) {
    'use strict';
    window.LMS_LOCALES = window.LMS_LOCALES || {};
    window.LMS_LOCALES.available = ['en', 'ur'];
    window.LMS_LOCALES.defaultLocale = 'en';
    window.LMS_LOCALES.modules = ['common', 'admin', 'faculty', 'courses', 'attendance', 'assessments'];
})(typeof window !== 'undefined' ? window : this);
