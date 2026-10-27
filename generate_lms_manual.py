#!/usr/bin/env python3
"""
EduDigiClass LMS - Comprehensive Multi-Role User Manual Generator
Generates:
1. EduDigi_LMS_Comprehensive_User_Manual.html (Interactive HTML + Printable CSS)
2. EduDigi_LMS_Comprehensive_User_Manual.md (Markdown format)
3. EduDigi_LMS_Comprehensive_User_Manual.pdf (Headless Chrome PDF generation)
"""

import os
import sys
import subprocess

OUTPUT_DIR = "/Users/sandeepkumar/Desktop/LMS"
ASSETS_DIR = "/Users/sandeepkumar/Desktop/LMS/manual_assets"
DOCS_DIR = "/Users/sandeepkumar/Desktop/LMS/docs"
CENTRAL_DOCS_DIR = "/Users/sandeepkumar/Desktop/Vaagmine Platfroms Documents"

# Ensure directories exist
os.makedirs(ASSETS_DIR, exist_ok=True)
os.makedirs(DOCS_DIR, exist_ok=True)
os.makedirs(CENTRAL_DOCS_DIR, exist_ok=True)

# Build HTML Content
html_content = """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>EduDigiClass LMS — Comprehensive Enterprise User Manual</title>
<style>
  @import url('https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@300;400;500;600;700;800&family=Fira+Code:wght@400;500;600&display=swap');

  :root {
    --primary: #4338ca;
    --primary-light: #6366f1;
    --primary-dark: #312e81;
    --secondary: #0d9488;
    --secondary-dark: #115e59;
    --accent: #f59e0b;
    --danger: #e11d48;
    --success: #10b981;
    --slate-900: #0f172a;
    --slate-800: #1e293b;
    --slate-700: #334155;
    --slate-600: #475569;
    --slate-500: #64748b;
    --slate-200: #e2e8f0;
    --slate-100: #f1f5f9;
    --slate-50: #f8fafc;
    --card-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.07), 0 2px 4px -2px rgba(0, 0, 0, 0.05);
    --hover-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -4px rgba(0, 0, 0, 0.05);
  }

  * {
    box-sizing: border-box;
    margin: 0;
    padding: 0;
  }

  body {
    font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    color: var(--slate-800);
    background-color: #f8fafc;
    line-height: 1.6;
    font-size: 14.5px;
  }

  /* Layout Container */
  .app-container {
    display: flex;
    min-height: 100vh;
  }

  /* Sidebar Navigation */
  .sidebar {
    width: 320px;
    background: #0f172a;
    color: #f8fafc;
    position: fixed;
    top: 0;
    bottom: 0;
    left: 0;
    overflow-y: auto;
    padding: 24px 20px;
    z-index: 100;
    border-right: 1px solid #1e293b;
    scrollbar-width: thin;
    scrollbar-color: #334155 #0f172a;
  }

  .sidebar-brand {
    display: flex;
    align-items: center;
    gap: 12px;
    padding-bottom: 20px;
    margin-bottom: 20px;
    border-bottom: 1px solid #1e293b;
  }

  .brand-logo-badge {
    width: 44px;
    height: 44px;
    border-radius: 10px;
    background: linear-gradient(135deg, #6366f1, #4338ca);
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 800;
    font-size: 20px;
    color: #ffffff;
    box-shadow: 0 4px 10px rgba(99, 102, 241, 0.4);
  }

  .brand-text h2 {
    font-size: 17px;
    font-weight: 700;
    color: #ffffff;
    letter-spacing: -0.02em;
  }

  .brand-text p {
    font-size: 11px;
    color: #94a3b8;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    font-weight: 600;
  }

  .search-box {
    margin-bottom: 20px;
    position: relative;
  }

  .search-box input {
    width: 100%;
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 8px;
    padding: 8px 12px 8px 34px;
    color: #f8fafc;
    font-size: 12.5px;
    outline: none;
    transition: all 0.2s;
  }

  .search-box input:focus {
    border-color: #6366f1;
    box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.2);
  }

  .search-icon {
    position: absolute;
    left: 10px;
    top: 50%;
    transform: translateY(-50%);
    font-size: 13px;
    color: #64748b;
  }

  .nav-group-title {
    font-size: 11px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: #94a3b8;
    margin: 20px 0 8px 6px;
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .nav-link {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 8px 12px;
    color: #cbd5e1;
    text-decoration: none;
    border-radius: 6px;
    font-size: 13px;
    font-weight: 500;
    margin-bottom: 3px;
    transition: all 0.15s ease;
  }

  .nav-link:hover {
    background: #1e293b;
    color: #ffffff;
    transform: translateX(3px);
  }

  .nav-link.active {
    background: #4338ca;
    color: #ffffff;
    font-weight: 600;
  }

  .nav-badge {
    margin-left: auto;
    font-size: 10px;
    font-weight: 700;
    padding: 2px 6px;
    border-radius: 4px;
    background: #334155;
    color: #94a3b8;
  }

  .badge-admin { background: rgba(239, 68, 68, 0.2); color: #f87171; }
  .badge-faculty { background: rgba(16, 185, 129, 0.2); color: #34d399; }
  .badge-student { background: rgba(59, 130, 246, 0.2); color: #60a5fa; }

  /* Main Content Area */
  .main-content {
    margin-left: 320px;
    flex: 1;
    padding: 40px 48px;
    max-width: 1300px;
  }

  /* Document Header */
  .doc-header {
    background: linear-gradient(135deg, #1e1b4b 0%, #312e81 60%, #4338ca 100%);
    border-radius: 16px;
    padding: 40px;
    color: #ffffff;
    margin-bottom: 40px;
    box-shadow: 0 10px 25px -5px rgba(67, 56, 202, 0.3);
    position: relative;
    overflow: hidden;
  }

  .doc-header::after {
    content: '';
    position: absolute;
    right: -50px;
    bottom: -50px;
    width: 250px;
    height: 250px;
    background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, rgba(255,255,255,0) 70%);
    border-radius: 50%;
  }

  .doc-meta-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin-bottom: 16px;
  }

  .meta-tag {
    background: rgba(255, 255, 255, 0.15);
    backdrop-filter: blur(8px);
    border: 1px solid rgba(255, 255, 255, 0.2);
    padding: 4px 12px;
    border-radius: 20px;
    font-size: 11.5px;
    font-weight: 600;
    color: #e0e7ff;
    display: flex;
    align-items: center;
    gap: 6px;
  }

  .doc-header h1 {
    font-size: 32px;
    font-weight: 800;
    letter-spacing: -0.03em;
    margin-bottom: 12px;
    line-height: 1.25;
  }

  .doc-header p {
    font-size: 15px;
    color: #c7d2fe;
    max-width: 850px;
    line-height: 1.6;
  }

  .header-actions {
    display: flex;
    gap: 12px;
    margin-top: 24px;
  }

  .btn {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 9px 18px;
    border-radius: 8px;
    font-size: 13px;
    font-weight: 600;
    text-decoration: none;
    cursor: pointer;
    transition: all 0.2s;
    border: none;
  }

  .btn-light {
    background: #ffffff;
    color: #312e81;
  }

  .btn-light:hover {
    background: #f1f5f9;
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
  }

  .btn-outline {
    background: rgba(255, 255, 255, 0.12);
    color: #ffffff;
    border: 1px solid rgba(255, 255, 255, 0.3);
  }

  .btn-outline:hover {
    background: rgba(255, 255, 255, 0.2);
  }

  /* Manual Sections */
  .section-card {
    background: #ffffff;
    border-radius: 14px;
    border: 1px solid var(--slate-200);
    padding: 36px 40px;
    margin-bottom: 36px;
    box-shadow: var(--card-shadow);
  }

  .section-card:hover {
    box-shadow: var(--hover-shadow);
    transition: box-shadow 0.25s ease;
  }

  .section-tag {
    display: inline-block;
    padding: 3px 10px;
    border-radius: 6px;
    font-size: 11px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin-bottom: 12px;
  }

  .tag-purple { background: #ede9fe; color: #5b21b6; }
  .tag-emerald { background: #d1fae5; color: #065f46; }
  .tag-blue { background: #dbeafe; color: #1e40af; }
  .tag-amber { background: #fef3c7; color: #92400e; }

  h2.section-title {
    font-size: 24px;
    font-weight: 800;
    color: var(--slate-900);
    letter-spacing: -0.02em;
    margin-bottom: 12px;
    display: flex;
    align-items: center;
    gap: 12px;
  }

  h3.subsection-title {
    font-size: 18px;
    font-weight: 700;
    color: var(--slate-800);
    margin: 28px 0 14px 0;
    padding-bottom: 8px;
    border-bottom: 2px solid var(--slate-100);
    display: flex;
    align-items: center;
    gap: 10px;
  }

  p {
    color: var(--slate-600);
    margin-bottom: 16px;
  }

  /* Alert / Callout Boxes */
  .callout {
    border-radius: 10px;
    padding: 16px 20px;
    margin: 20px 0;
    display: flex;
    gap: 14px;
    align-items: flex-start;
  }

  .callout-info {
    background: #eff6ff;
    border-left: 4px solid #3b82f6;
    color: #1e40af;
  }

  .callout-warning {
    background: #fffbeb;
    border-left: 4px solid #f59e0b;
    color: #92400e;
  }

  .callout-success {
    background: #ecfdf5;
    border-left: 4px solid #10b981;
    color: #065f46;
  }

  .callout-icon {
    font-size: 18px;
    line-height: 1;
    flex-shrink: 0;
  }

  .callout-content {
    font-size: 13.5px;
    line-height: 1.55;
  }

  .callout-content strong {
    display: block;
    margin-bottom: 3px;
    font-weight: 700;
  }

  /* Screenshot Showcase */
  .screenshot-figure {
    background: #ffffff;
    border: 1px solid var(--slate-200);
    border-radius: 12px;
    padding: 14px;
    margin: 24px 0 30px 0;
    box-shadow: 0 4px 12px rgba(15, 23, 42, 0.06);
  }

  .screenshot-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 12px;
    padding-bottom: 10px;
    border-bottom: 1px solid var(--slate-100);
  }

  .figure-title {
    font-size: 13.5px;
    font-weight: 700;
    color: var(--slate-800);
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .figure-badge {
    font-size: 11px;
    font-weight: 600;
    padding: 3px 8px;
    border-radius: 6px;
    background: var(--slate-100);
    color: var(--slate-600);
    font-family: 'Fira Code', monospace;
  }

  .screenshot-img-wrapper {
    position: relative;
    background: #0f172a;
    border-radius: 8px;
    overflow: hidden;
    border: 1px solid var(--slate-200);
  }

  .screenshot-img {
    width: 100%;
    height: auto;
    display: block;
    transition: transform 0.2s;
  }

  .screenshot-caption {
    margin-top: 10px;
    font-size: 12.5px;
    color: var(--slate-500);
    font-style: italic;
    line-height: 1.5;
  }

  /* Control Breakdown Table */
  .spec-table {
    width: 100%;
    border-collapse: separate;
    border-spacing: 0;
    margin: 20px 0 24px 0;
    border-radius: 8px;
    overflow: hidden;
    border: 1px solid var(--slate-200);
    font-size: 13px;
  }

  .spec-table th {
    background: #f8fafc;
    color: var(--slate-700);
    text-align: left;
    padding: 12px 16px;
    font-weight: 700;
    border-bottom: 1px solid var(--slate-200);
    text-transform: uppercase;
    font-size: 11px;
    letter-spacing: 0.05em;
  }

  .spec-table td {
    padding: 12px 16px;
    border-bottom: 1px solid var(--slate-200);
    color: var(--slate-600);
    vertical-align: top;
  }

  .spec-table tr:last-child td {
    border-bottom: none;
  }

  .spec-table tr:nth-child(even) td {
    background: #fafbfc;
  }

  .code-token {
    font-family: 'Fira Code', monospace;
    font-size: 11.5px;
    background: #f1f5f9;
    padding: 2px 6px;
    border-radius: 4px;
    color: #4338ca;
    font-weight: 600;
  }

  /* Step-by-Step Procedure Cards */
  .step-list {
    list-style: none;
    margin: 20px 0;
    position: relative;
    padding-left: 28px;
  }

  .step-list::before {
    content: '';
    position: absolute;
    top: 10px;
    bottom: 10px;
    left: 11px;
    width: 2px;
    background: #e2e8f0;
  }

  .step-item {
    position: relative;
    margin-bottom: 20px;
  }

  .step-number {
    position: absolute;
    left: -28px;
    top: 0;
    width: 24px;
    height: 24px;
    border-radius: 50%;
    background: var(--primary);
    color: #ffffff;
    font-size: 12px;
    font-weight: 700;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 0 0 4px #ffffff;
  }

  .step-title {
    font-weight: 700;
    color: var(--slate-900);
    font-size: 14px;
    margin-bottom: 4px;
  }

  .step-desc {
    font-size: 13.5px;
    color: var(--slate-600);
    line-height: 1.5;
  }

  /* Grid Layouts for Features */
  .grid-2 {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 20px;
    margin: 20px 0;
  }

  .grid-3 {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 20px;
    margin: 20px 0;
  }

  .feature-box {
    background: #f8fafc;
    border: 1px solid var(--slate-200);
    border-radius: 10px;
    padding: 20px;
    transition: all 0.2s;
  }

  .feature-box:hover {
    border-color: #cbd5e1;
    background: #ffffff;
    box-shadow: 0 4px 12px rgba(0,0,0,0.04);
  }

  .feature-icon {
    width: 36px;
    height: 36px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 18px;
    margin-bottom: 12px;
  }

  .icon-purple { background: #ede9fe; color: #4338ca; }
  .icon-teal { background: #ccfbf1; color: #0d9488; }
  .icon-blue { background: #dbeafe; color: #2563eb; }
  .icon-amber { background: #fef3c7; color: #d97706; }

  .feature-box h4 {
    font-size: 15px;
    font-weight: 700;
    color: var(--slate-900);
    margin-bottom: 6px;
  }

  .feature-box p {
    font-size: 13px;
    color: var(--slate-600);
    margin: 0;
  }

  /* UGC 10-Point Grade Scale Table */
  .grade-badge {
    display: inline-block;
    padding: 2px 8px;
    border-radius: 4px;
    font-weight: 700;
    font-size: 11px;
    text-align: center;
  }
  .grade-o { background: #dcfce7; color: #15803d; }
  .grade-aplus { background: #dbeafe; color: #1d4ed8; }
  .grade-a { background: #ede9fe; color: #6d28d9; }
  .grade-bplus { background: #fef3c7; color: #b45309; }
  .grade-b { background: #ffedd5; color: #c2410c; }
  .grade-c { background: #f1f5f9; color: #475569; }
  .grade-p { background: #e2e8f0; color: #334155; }
  .grade-f { background: #fee2e2; color: #b91c1c; }

  /* Footer */
  .doc-footer {
    border-top: 1px solid var(--slate-200);
    padding: 30px 0;
    margin-top: 60px;
    color: var(--slate-500);
    font-size: 12.5px;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  /* Printable Styles */
  @media print {
    .sidebar, .search-box, .header-actions, .btn-print, .no-print {
      display: none !important;
    }

    body {
      background: #ffffff;
      color: #000000;
      font-size: 10pt;
    }

    .main-content {
      margin-left: 0 !important;
      padding: 0 !important;
      max-width: 100% !important;
    }

    .doc-header {
      background: #312e81 !important;
      color: #ffffff !important;
      padding: 24px !important;
      margin-bottom: 24px !important;
      -webkit-print-color-adjust: exact;
      print-color-adjust: exact;
    }

    .section-card {
      border: 1px solid #cbd5e1 !important;
      box-shadow: none !important;
      padding: 20px !important;
      margin-bottom: 24px !important;
      page-break-inside: avoid;
    }

    .screenshot-figure {
      page-break-inside: avoid;
      border: 1px solid #cbd5e1 !important;
      box-shadow: none !important;
      padding: 10px !important;
    }

    .screenshot-img {
      max-height: 120mm !important;
      object-fit: contain;
    }

    .spec-table th, .spec-table td {
      padding: 8px 10px !important;
      font-size: 8.5pt !important;
    }

    h2.section-title {
      page-break-before: always;
      padding-top: 10mm;
    }

    .page-break {
      page-break-before: always;
    }
  }
</style>
</head>
<body>

<div class="app-container">
  <!-- Sidebar Navigation -->
  <aside class="sidebar">
    <div class="sidebar-brand">
      <div class="brand-logo-badge">ED</div>
      <div class="brand-text">
        <h2>EduDigiClass LMS</h2>
        <p>Enterprise User Manual</p>
      </div>
    </div>

    <div class="search-box">
      <span class="search-icon">🔍</span>
      <input type="text" id="manualSearch" placeholder="Filter sections, features..." onkeyup="filterNav()">
    </div>

    <div class="nav-section">
      <div class="nav-group-title">Executive Summary</div>
      <a href="#overview" class="nav-link active"><span>📌</span> Architecture & Overview</a>
      <a href="#access-credentials" class="nav-link"><span>🔑</span> Access & Port Architecture</a>

      <div class="nav-group-title">1. Authentication Gateway</div>
      <a href="#login-portal" class="nav-link"><span>🚪</span> Multi-Role Login Portal <span class="nav-badge">01</span></a>
      <a href="#bilingual-rtl" class="nav-link"><span>🌐</span> Bilingual (English/Urdu) RTL</a>

      <div class="nav-group-title">2. Super Admin Manual</div>
      <a href="#admin-dashboard" class="nav-link"><span>📊</span> Command Dashboard <span class="nav-badge badge-admin">02</span></a>
      <a href="#admin-users" class="nav-link"><span>👥</span> User Management <span class="nav-badge badge-admin">03</span></a>
      <a href="#admin-bulk-upload" class="nav-link"><span>📥</span> Bulk User Upload Center <span class="nav-badge badge-admin">04</span></a>
      <a href="#admin-curriculum" class="nav-link"><span>🎓</span> Academic Hub & Programs <span class="nav-badge badge-admin">05</span></a>
      <a href="#admin-content-library" class="nav-link"><span>📚</span> Global Content Library <span class="nav-badge badge-admin">06</span></a>
      <a href="#admin-enrollments" class="nav-link"><span>📝</span> Academic Enrollment Center <span class="nav-badge badge-admin">07</span></a>
      <a href="#admin-grade-cards" class="nav-link"><span>📜</span> Student Grade Cards <span class="nav-badge badge-admin">08</span></a>

      <div class="nav-group-title">3. Faculty Operations Manual</div>
      <a href="#faculty-dashboard" class="nav-link"><span>💼</span> Faculty Command Desk <span class="nav-badge badge-faculty">09</span></a>
      <a href="#faculty-curriculum" class="nav-link"><span>📖</span> Assigned Syllabus Structure <span class="nav-badge badge-faculty">10</span></a>
      <a href="#faculty-zoom" class="nav-link"><span>📹</span> Live Virtual Zoom Classes <span class="nav-badge badge-faculty">11</span></a>
      <a href="#faculty-ai-question-bank" class="nav-link"><span>🤖</span> AI Question Bank & Paper</a>
      <a href="#faculty-internal-assessments" class="nav-link"><span>📋</span> Internal Assessments Hub <span class="nav-badge badge-faculty">12</span></a>
      <a href="#faculty-assessment-draft" class="nav-link"><span>✏️</span> Assessment Authoring <span class="nav-badge badge-faculty">13</span></a>
      <a href="#faculty-ai-evaluation" class="nav-link"><span>🎯</span> Gemini AI Descriptive Grading</a>
      <a href="#faculty-analytics" class="nav-link"><span>📈</span> Learner Telemetry & Analytics <span class="nav-badge badge-faculty">14</span></a>

      <div class="nav-group-title">4. Student Learning Manual</div>
      <a href="#student-dashboard" class="nav-link"><span>🎒</span> Student Dashboard <span class="nav-badge badge-student">15</span></a>
      <a href="#student-calendar" class="nav-link"><span>📅</span> Interactive Calendar & Zoom <span class="nav-badge badge-student">16</span></a>
      <a href="#student-courses" class="nav-link"><span>📺</span> 4-Quadrant Course Hub <span class="nav-badge badge-student">17</span></a>
      <a href="#student-assessments" class="nav-link"><span>📝</span> Assessment & Quiz Center <span class="nav-badge badge-student">18</span></a>
      <a href="#student-attendance" class="nav-link"><span>⏱️</span> Automatic Attendance Log <span class="nav-badge badge-student">19</span></a>
      <a href="#student-progress-report" class="nav-link"><span>📊</span> Progress Report & Grade Card <span class="nav-badge badge-student">20</span></a>

      <div class="nav-group-title">5. Reference & Administration</div>
      <a href="#ugc-grading-scale" class="nav-link"><span>🏆</span> UGC 10-Point Grade Matrix</a>
      <a href="#troubleshooting-faq" class="nav-link"><span>🛠️</span> Troubleshooting & Support</a>
    </div>
  </aside>

  <!-- Main Content Body -->
  <main class="main-content">

    <!-- Document Header -->
    <header class="doc-header">
      <div class="doc-meta-tags">
        <span class="meta-tag">🏛️ University Enterprise Edition</span>
        <span class="meta-tag">⚡ ASP.NET Core (.NET 10.0)</span>
        <span class="meta-tag">🤖 Google Gemini AI Integrated</span>
        <span class="meta-tag">🌐 UGC 4-Quadrant Framework</span>
        <span class="meta-tag">🌍 English & Urdu Bilingual</span>
      </div>
      <h1>EduDigiClass LMS — Comprehensive Enterprise User Manual</h1>
      <p>
        The definitive operational standard and end-to-end user guide for University Administrators, Faculty Instructors, and Student Learners. Covers curriculum hierarchy, Zoom live lectures, AI Question Paper generation, Gemini automated evaluation, UGC 10-point grading, and login-based attendance telemetry.
      </p>
      <div class="header-actions">
        <button class="btn btn-light" onclick="window.print()">🖨️ Print / Save as PDF</button>
        <a href="#access-credentials" class="btn btn-outline">🔑 Default Credentials</a>
        <a href="#overview" class="btn btn-outline">📐 Architecture</a>
      </div>
    </header>

    <!-- Executive Architecture & Overview -->
    <section id="overview" class="section-card">
      <span class="section-tag tag-purple">Executive Summary</span>
      <h2 class="section-title">Institutional E-Learning Framework & Architecture</h2>
      <p>
        <strong>EduDigiClass</strong> is an enterprise-grade digital learning platform designed to fulfill university-level statutory requirements under the University Grants Commission (UGC) and Distance Education Bureau (DEB) guidelines. The platform integrates academic curriculum delivery, virtual classroom teleconferencing, artificial intelligence evaluation engines, and automated attendance logging into a unified multi-tenant ecosystem.
      </p>

      <div class="grid-3">
        <div class="feature-box">
          <div class="feature-icon icon-purple">🏛️</div>
          <h4>Super Admin / Management</h4>
          <p>Total governance over degree programs, course catalogs, faculty mappings, semester windows, and institutional grading policy configurations.</p>
        </div>
        <div class="feature-box">
          <div class="feature-icon icon-teal">👨‍🏫</div>
          <h4>Faculty & Evaluators</h4>
          <p>Curriculum authoring, Zoom live scheduling, AI-assisted question paper generation, rubric-guided descriptive assessment evaluation, and performance monitoring.</p>
        </div>
        <div class="feature-box">
          <div class="feature-icon icon-blue">🎓</div>
          <h4>Student Learners</h4>
          <p>Access to UGC 4-Quadrant course resources, interactive class calendars, timed online examinations, real-time attendance logs, and printable grade cards.</p>
        </div>
      </div>

      <div class="callout callout-info">
        <div class="callout-icon">💡</div>
        <div class="callout-content">
          <strong>Single Source of Truth (SSOT) Architecture</strong>
          All academic course structures (Degree Programs, Semesters, Modular Subjects, and Credit Weightages) authored in EduDigiClass serve as the authoritative baseline across the university ecosystem, directly synchronizing with the central ERP Examination Ledger and CRM Admissions repository.
        </div>
      </div>
    </section>

    <!-- System Access & Port Architecture -->
    <section id="access-credentials" class="section-card">
      <span class="section-tag tag-amber">Access Governance</span>
      <h2 class="section-title">System URLs, Ports & Default Credentials</h2>
      <p>
        The platform operates on Port <strong>82</strong> under IIS / Kestrel reverse proxy on the production VM host (<code>20.193.144.80</code>). Below are the dedicated endpoint routes and seeded university accounts:
      </p>

      <table class="spec-table">
        <thead>
          <tr>
            <th>Role</th>
            <th>Primary Portal URL</th>
            <th>Default Login ID</th>
            <th>Default Password</th>
            <th>Access Privilege Level</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong style="color: #e11d48;">Super Admin</strong></td>
            <td><span class="code-token">http://20.193.144.80:82/Admin/User</span></td>
            <td><span class="code-token">admin@TMS.com</span></td>
            <td><span class="code-token">Admin@123</span></td>
            <td>Full Institutional Root Access, User Deletion, Policy Control</td>
          </tr>
          <tr>
            <td><strong style="color: #059669;">Faculty / Teacher</strong></td>
            <td><span class="code-token">http://20.193.144.80:82/InternalAssessment/FacultyDashboard</span></td>
            <td><span class="code-token">faculty@edudigi.com</span></td>
            <td><span class="code-token">Faculty@123</span></td>
            <td>Course Content Uploads, Question Papers, Grading, Live Zoom</td>
          </tr>
          <tr>
            <td><strong style="color: #2563eb;">Student Learner</strong></td>
            <td><span class="code-token">http://20.193.144.80:82/MyCourses</span></td>
            <td><span class="code-token">student@edudigi.com</span></td>
            <td><span class="code-token">Student@123</span></td>
            <td>Learning Hub, Video Player, Quiz Attempts, Attendance Ledger</td>
          </tr>
        </tbody>
      </table>
    </section>

    <!-- CHAPTER 1: AUTHENTICATION GATEWAY -->
    <section id="login-portal" class="section-card">
      <span class="section-tag tag-purple">Chapter 1 • Authentication Gateway</span>
      <h2 class="section-title">Multi-Role Landing Page & Secure Entry</h2>
      <p>
        EduDigiClass provides a unified, branded entry portal where users select their designated institutional role to enter their tailored workspace.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 1: Online Learning Platform Gateway & Role Selector</div>
          <span class="figure-badge">Screenshot 2026-10-19 112709.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/01_login_portal_gateway.png" class="screenshot-img" alt="EduDigiClass Public Landing & Login Gateway">
        </div>
        <div class="screenshot-caption">Unified authentication hub displaying direct entry cards for Student Login, Faculty Login, Administrative Login, Language selector, and Mobile App access.</div>
      </div>

      <h3 class="subsection-title">Control Elements & Interface Breakdown</h3>
      <table class="spec-table">
        <thead>
          <tr>
            <th>Interface Component</th>
            <th>Element Type</th>
            <th>Functional Purpose & Behavior</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>Student Login Card</strong></td>
            <td>Primary Action Card</td>
            <td>Routes directly to the student portal authentication desk (<span class="code-token">/Auth/Login</span>). Grants access to course modules, study materials, live calendar, and assessments.</td>
          </tr>
          <tr>
            <td><strong>Faculty Login Card</strong></td>
            <td>Primary Action Card</td>
            <td>Authenticates verified instructional staff to the Faculty Command Workspace (<span class="code-token">/InternalAssessment/FacultyDashboard</span>) for classroom delivery and grading.</td>
          </tr>
          <tr>
            <td><strong>Administrative Login Card</strong></td>
            <td>Primary Action Card</td>
            <td>Secure gateway into the university's institutional control console (<span class="code-token">/Admin/User</span>) for governance, user mapping, and compliance audits.</td>
          </tr>
          <tr>
            <td><strong>Language Selector</strong></td>
            <td>Interactive Header Toggle</td>
            <td>Instant bilingual switching between 🇬🇧 English and 🇮🇳 اردو with dynamic Right-to-Left (RTL) styling across all portals.</td>
          </tr>
          <tr>
            <td><strong>Get App Button</strong></td>
            <td>Header Utility</td>
            <td>Provides direct QR codes and download links for the native Android and iOS mobile e-learning applications.</td>
          </tr>
        </tbody>
      </table>

      <h3 class="subsection-title" id="bilingual-rtl">Bilingual (English / Urdu) RTL Support</h3>
      <p>
        EduDigiClass is equipped with full bilingual localization:
      </p>
      <ul class="step-list">
        <li class="step-item">
          <div class="step-number">1</div>
          <div class="step-title">Language Toggle Click</div>
          <div class="step-desc">Click the language pill in the top header. Select <strong>اردو (Urdu)</strong> or <strong>English</strong>.</div>
        </li>
        <li class="step-item">
          <div class="step-number">2</div>
          <div class="step-title">Automatic RTL Layout Transformation</div>
          <div class="step-desc">The entire interface mirrors horizontally. The sidebar shifts to the right, text aligns Right-to-Left, and typography switches dynamically without page reloads.</div>
        </li>
        <li class="step-item">
          <div class="step-number">3</div>
          <div class="step-title">Session & LocalStorage Persistence</div>
          <div class="step-desc">The user's preferred language choice is stored in both HTTP cookies and browser LocalStorage, persisting across device reboots and navigation.</div>
        </li>
      </ul>
    </section>

    <!-- CHAPTER 2: SUPER ADMIN OPERATIONS MANUAL -->
    <section id="admin-dashboard" class="section-card">
      <span class="section-tag tag-emerald">Chapter 2 • Super Admin Operations</span>
      <h2 class="section-title">2.1 Institutional Command Dashboard</h2>
      <p>
        The Super Admin Dashboard provides central oversight of institutional health, active learner enrollments, faculty counts, and academic catalogs.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 2: Super Admin Platform Management Dashboard</div>
          <span class="figure-badge">Screenshot 2026-10-19 112930.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/02_admin_dashboard_overview.png" class="screenshot-img" alt="Super Admin Platform Dashboard">
        </div>
        <div class="screenshot-caption">Executive dashboard displaying real-time counters, user metrics, quick navigation shortcuts, and administrative action centers.</div>
      </div>

      <div class="grid-2">
        <div class="feature-box">
          <div class="feature-icon icon-purple">⚡</div>
          <h4>Universal Global Search (Ctrl + K)</h4>
          <p>Instant search overlay accessible via <span class="code-token">Ctrl + K</span>. Search across courses, syllabi, quizzes, faculty members, and students with sub-millisecond autocomplete.</p>
        </div>
        <div class="feature-box">
          <div class="feature-icon icon-teal">🛡️</div>
          <h4>Role-Based Access Control (RBAC)</h4>
          <p>Granular permissions matrix for Super Admin, Academic Dean, Exam Coordinator, Faculty, and Student roles.</p>
        </div>
      </div>
    </section>

    <!-- 2.2 User Directory & Management -->
    <section id="admin-users" class="section-card">
      <span class="section-tag tag-emerald">Super Admin Manual</span>
      <h2 class="section-title">2.2 User Directory & Account Governance</h2>
      <p>
        Administrators manage user lifecycles, assign institutional roles, reset credentials, and activate/deactivate accounts.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 3: User Management Directory Table</div>
          <span class="figure-badge">Screenshot 2026-10-19 113026.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/03_admin_user_management_table.png" class="screenshot-img" alt="All Users Directory Table">
        </div>
        <div class="screenshot-caption">Data table showcasing user accounts, pagination controls, role classification badges (Faculty Active, Student, Admin), search filters, and action triggers.</div>
      </div>

      <h3 class="subsection-title">Operational Controls & Action Buttons</h3>
      <table class="spec-table">
        <thead>
          <tr>
            <th>Table Column / Button</th>
            <th>Function</th>
            <th>Administrator Guidance</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>SL. NO.</strong></td>
            <td>Sequential Record Index</td>
            <td>Provides paginated ordering across total system user accounts (e.g. 1 to 10 of 42 entries).</td>
          </tr>
          <tr>
            <td><strong>ROLE</strong></td>
            <td>Security Classification Badge</td>
            <td>Visual badge displaying account privilege (e.g. <span class="code-token">Faculty (Active)</span>, <span class="code-token">Student</span>, <span class="code-token">Super Admin</span>).</td>
          </tr>
          <tr>
            <td><strong>NAME & EMAIL</strong></td>
            <td>User Identity Details</td>
            <td>Displays verified full name and institutional email address used for login and notifications.</td>
          </tr>
          <tr>
            <td><strong>Show [10/25/50] Entries</strong></td>
            <td>Page Length Selector</td>
            <td>Adjusts data table density for bulk audits.</td>
          </tr>
          <tr>
            <td><strong>Action Triggers (Edit / Reset)</strong></td>
            <td>Lifecycle Controls</td>
            <td>Allows updating contact information, assigning new course roles, or issuing one-click password resets.</td>
          </tr>
        </tbody>
      </table>
    </section>

    <!-- 2.3 Bulk User Upload Center -->
    <section id="admin-bulk-upload" class="section-card">
      <span class="section-tag tag-emerald">Super Admin Manual</span>
      <h2 class="section-title">2.3 Bulk User Onboarding Center (CSV / Excel)</h2>
      <p>
        Allows rapid batch provisioning of hundreds of students, faculty, and administrators simultaneously with built-in schema validation and duplicate detection.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 4: Bulk User Upload Drag & Drop Interface</div>
          <span class="figure-badge">Screenshot 2026-10-19 113136.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/04_admin_bulk_upload_users.png" class="screenshot-img" alt="Bulk User Upload Center">
        </div>
        <div class="screenshot-caption">4-step automated pipeline (1. Upload > 2. Preview > 3. Confirm > 4. Result) with sample template download and duplicate detection.</div>
      </div>

      <h3 class="subsection-title">4-Stage Onboarding Procedure</h3>
      <ul class="step-list">
        <li class="step-item">
          <div class="step-number">1</div>
          <div class="step-title">Stage 1: Download Standard Template</div>
          <div class="step-desc">Click <strong>Download Template</strong> to retrieve the pre-formatted CSV/Excel template containing mandatory headers: <span class="code-token">Name</span>, <span class="code-token">Email</span>, <span class="code-token">Phone</span>, <span class="code-token">Role</span>, and <span class="code-token">Password</span>.</div>
        </li>
        <li class="step-item">
          <div class="step-number">2</div>
          <div class="step-title">Stage 2: Drag & Drop Ingestion</div>
          <div class="step-desc">Drag your completed file into the designated upload zone. The system accepts <span class="code-token">.csv</span>, <span class="code-token">.xlsx</span>, and <span class="code-token">.xls</span> files.</div>
        </li>
        <li class="step-item">
          <div class="step-number">3</div>
          <div class="step-title">Stage 3: Pre-Validation & Duplicate Check</div>
          <div class="step-desc">Click <strong>Preview & Validate</strong>. The system scans for invalid email formats, missing mandatory fields, and existing database duplicates. Potential collisions are highlighted in amber.</div>
        </li>
        <li class="step-item">
          <div class="step-number">4</div>
          <div class="step-title">Stage 4: Execution & Audit Receipt</div>
          <div class="step-desc">Confirm creation. The system provisions the accounts, activates the <strong>Force Password Change on First Login</strong> flag, and generates a printable audit summary.</div>
        </li>
      </ul>
    </section>

    <!-- 2.4 Academic Hub & Program Hierarchy -->
    <section id="admin-curriculum" class="section-card">
      <span class="section-tag tag-emerald">Super Admin Manual</span>
      <h2 class="section-title">2.4 Academic Hub & Program Hierarchy</h2>
      <p>
        The central architectural command desk for establishing Degree Programs, Semester Structures, Subject Modules, and Credit Allocations.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 5: Academic Hub & Curriculum Hierarchy</div>
          <span class="figure-badge">Screenshot 2026-10-19 113208.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/05_admin_course_curriculum_hierarchy.png" class="screenshot-img" alt="Academic Hub Curriculum Hierarchy">
        </div>
        <div class="screenshot-caption">Curriculum catalog tracking 2 Programs, 13 Courses, 66 Total Credits, and 8 Semesters with + New Program and + Add Course action triggers.</div>
      </div>

      <div class="grid-2">
        <div class="feature-box">
          <div class="feature-icon icon-purple">🎓</div>
          <h4>Bachelor of Business Administration (BBA)</h4>
          <p>6 Semesters • 5 Active Courses • 57 Content Items across business management, accounting, and marketing principles.</p>
        </div>
        <div class="feature-box">
          <div class="feature-icon icon-teal">💻</div>
          <h4>Bachelor of Computer Applications (BCA)</h4>
          <p>6 Semesters • 6 Active Courses • Computer networks, IoT, database management, and programming foundations.</p>
        </div>
      </div>
    </section>

    <!-- 2.5 Global Content Center / Content Library -->
    <section id="admin-content-library" class="section-card">
      <span class="section-tag tag-emerald">Super Admin Manual</span>
      <h2 class="section-title">2.5 Global Content Center (Content Library)</h2>
      <p>
        A unified digital repository to manage all multimedia learning assets. The architectural philosophy is <strong>"Upload once, assign anywhere"</strong>.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 6: Global Content Center & Material Repository</div>
          <span class="figure-badge">Screenshot 2026-10-19 113248.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/06_admin_global_content_library.png" class="screenshot-img" alt="Global Content Center">
        </div>
        <div class="screenshot-caption">Central repository enabling multi-program material uploads, quadrant tagging, and cross-course resource linking.</div>
      </div>

      <div class="callout callout-success">
        <div class="callout-icon">📦</div>
        <div class="callout-content">
          <strong>Storage & Verification Guard</strong>
          Uploaded PDFs, DOCX, and PPTX files undergo binary signature checks to prevent malicious file uploads. Files are indexed and rendered securely via the embedded document streaming viewer.
        </div>
      </div>
    </section>

    <!-- 2.6 Academic Enrollment Center -->
    <section id="admin-enrollments" class="section-card">
      <span class="section-tag tag-emerald">Super Admin Manual</span>
      <h2 class="section-title">2.6 Academic Enrollment Center</h2>
      <p>
        Assign, manage, and audit student enrollments across Academic Programs and Individual Courses. Students instantly gain access to enrolled materials upon assignment.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 7: Academic Enrollment Center</div>
          <span class="figure-badge">Screenshot 2026-10-19 113956.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/07_admin_academic_enrollment_center.png" class="screenshot-img" alt="Academic Enrollment Center">
        </div>
        <div class="screenshot-caption">Enrollment cockpit monitoring 2 Programs, 13 Active Courses, 26 Students, and 38 Active Enrollments.</div>
      </div>
    </section>

    <!-- 2.7 Student Grade Cards -->
    <section id="admin-grade-cards" class="section-card">
      <span class="section-tag tag-emerald">Super Admin Manual</span>
      <h2 class="section-title">2.7 All Student Grade Cards & Evaluation Ledger</h2>
      <p>
        The institutional grade card repository tracks semester-wise student academic standings, marks earned across internal assessments, and UGC 10-point letter grades.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 8: All Student Grade Cards Directory</div>
          <span class="figure-badge">Screenshot 2026-10-19 114250.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/08_admin_student_grade_cards.png" class="screenshot-img" alt="All Student Grade Cards">
        </div>
        <div class="screenshot-caption">Grade card repository showcasing student profiles (e.g. Kunal Verma ID:30, Sandeep ID:6, Test Student ID:29), course titles, semesters, and assessment status badges (Published, Evaluated, Absent, Pending).</div>
      </div>
    </section>

    <!-- CHAPTER 3: FACULTY OPERATIONS MANUAL -->
    <section id="faculty-dashboard" class="section-card">
      <span class="section-tag tag-teal">Chapter 3 • Faculty Operations</span>
      <h2 class="section-title">3.1 Faculty Teaching Command Desk</h2>
      <p>
        Instructors log in to manage their assigned curriculum, schedule live Zoom virtual lectures, author assessments, and evaluate student work.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 9: Faculty Dashboard & Teaching Command Center</div>
          <span class="figure-badge">Screenshot 2026-10-19 114415.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/09_faculty_dashboard_overview.png" class="screenshot-img" alt="Faculty Dashboard Overview">
        </div>
        <div class="screenshot-caption">Faculty workspace displaying assigned courses, enrolled learners, upcoming virtual classes, and pending assessment evaluations.</div>
      </div>
    </section>

    <!-- 3.2 Faculty Curriculum -->
    <section id="faculty-curriculum" class="section-card">
      <span class="section-tag tag-teal">Faculty Manual</span>
      <h2 class="section-title">3.2 Assigned Courses & Modular Curriculum</h2>
      <p>
        Faculty view the syllabus tree for their assigned subjects, organized into modular units, topics, and UGC 4-quadrant resources.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 10: Faculty Assigned Curriculum & Course Hierarchy</div>
          <span class="figure-badge">Screenshot 2026-10-19 114449.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/10_faculty_courses_curriculum.png" class="screenshot-img" alt="Faculty Curriculum Hierarchy">
        </div>
        <div class="screenshot-caption">Curriculum catalog showing degree programs, course modules, credit allocations, and unit-level syllabus topics.</div>
      </div>
    </section>

    <!-- 3.3 Virtual Classroom & Live Zoom -->
    <section id="faculty-zoom" class="section-card">
      <span class="section-tag tag-teal">Faculty Manual</span>
      <h2 class="section-title">3.3 Virtual Classroom & Live Zoom Session Scheduling</h2>
      <p>
        Faculty schedule interactive lectures directly within LMS without needing to leave the platform. Secure meeting links are embedded directly into enrolled students' calendars.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 11: Virtual Classroom & Live Zoom Sessions</div>
          <span class="figure-badge">Screenshot 2026-10-19 114514.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/11_faculty_virtual_classroom_zoom.png" class="screenshot-img" alt="Virtual Classroom & Live Zoom Sessions">
        </div>
        <div class="screenshot-caption">Live virtual lecture hub featuring the "Schedule Zoom Class" trigger, upcoming class roster, and automated attendance integration.</div>
      </div>

      <h3 class="subsection-title">Step-by-Step Class Scheduling</h3>
      <ul class="step-list">
        <li class="step-item">
          <div class="step-number">1</div>
          <div class="step-title">Click "Schedule Zoom Class"</div>
          <div class="step-desc">Open the scheduling dialog on the Virtual Classroom page.</div>
        </li>
        <li class="step-item">
          <div class="step-number">2</div>
          <div class="step-title">Select Course & Topic</div>
          <div class="step-desc">Choose your assigned course (e.g. <em>BBA101 Principles and Practice of Management</em>), specify the topic title, and select date and time.</div>
        </li>
        <li class="step-item">
          <div class="step-number">3</div>
          <div class="step-title">Automated Meeting Creation & Roster Broadcast</div>
          <div class="step-desc">The system provisions the meeting and immediately updates the Class Calendar of every enrolled student with a 1-click <strong>Join Class</strong> button.</div>
        </li>
      </ul>
    </section>

    <!-- 3.4 AI Question Bank & Automatic Question Paper Generator -->
    <section id="faculty-ai-question-bank" class="section-card">
      <span class="section-tag tag-teal">Faculty Manual</span>
      <h2 class="section-title">3.4 AI Question Bank & Automatic Question Paper Generator</h2>
      <p>
        The platform features an advanced <strong>Google Gemini AI</strong> question generation engine and a formal <strong>Question Paper Generator</strong> adhering to institutional exam blueprints.
      </p>

      <div class="grid-2">
        <div class="feature-box">
          <div class="feature-icon icon-purple">🤖</div>
          <h4>Curriculum-Grounded AI Questions</h4>
          <p>Generate curriculum-aligned MCQs, Short Answer, and Long Descriptive questions filtered by Bloom's Taxonomy (Remember, Understand, Apply, Analyze, Evaluate, Create) and difficulty (Easy, Medium, Hard).</p>
        </div>
        <div class="feature-box">
          <div class="feature-icon icon-teal">📄</div>
          <h4>1-Click Blueprint Paper Auto-Assembly</h4>
          <p>Auto-assemble structured exam papers with configurable sections (Section A: MCQs, Section B: Short Notes, Section C: Long Problems) and print institutional hardcopies.</p>
        </div>
      </div>
    </section>

    <!-- 3.5 Internal Assessment Authoring -->
    <section id="faculty-internal-assessments" class="section-card">
      <span class="section-tag tag-teal">Faculty Manual</span>
      <h2 class="section-title">3.5 Internal Assessment Policy & Window Configuration</h2>
      <p>
        Faculty create formal assessments aligned with administrator-approved assessment windows, configuring submission types, deadlines, and passing thresholds.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 12: Internal Assessment Creation Modal</div>
          <span class="figure-badge">Screenshot 2026-10-19 114550.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/12_faculty_internal_assessments.png" class="screenshot-img" alt="Faculty Internal Assessment Form">
        </div>
        <div class="screenshot-caption">Authoring form with Title, Max Marks, Course selector, Admin-approved window, Multiple Assessment Types, and Late Submission toggle.</div>
      </div>

      <h3 class="subsection-title">Assessment Field Specification</h3>
      <table class="spec-table">
        <thead>
          <tr>
            <th>Field / Option</th>
            <th>Type</th>
            <th>Description & Business Rules</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>Title</strong></td>
            <td>Text Input</td>
            <td>Descriptive name of the assessment (e.g. <em>Formative Assessment 1 - Unit 2 Written Test</em>).</td>
          </tr>
          <tr>
            <td><strong>Course</strong></td>
            <td>Dropdown</td>
            <td>Selects the target course. Only courses mapped to the logged-in faculty appear in this list.</td>
          </tr>
          <tr>
            <td><strong>Admin Approved Window</strong></td>
            <td>Schedule Selector</td>
            <td>Links the assessment to an authorized university window ensuring compliance with term dates.</td>
          </tr>
          <tr>
            <td><strong>Assessment Types (Multiple)</strong></td>
            <td>Multi-Select Picker</td>
            <td>Select one or more: <span class="code-token">Assignment</span>, <span class="code-token">Written</span>, <span class="code-token">Practical</span>, <span class="code-token">Project</span>, <span class="code-token">Presentation</span>, <span class="code-token">Viva</span>, <span class="code-token">MCQ</span>.</td>
          </tr>
          <tr>
            <td><strong>Max Marks</strong></td>
            <td>Numeric Input</td>
            <td>Total maximum marks (e.g. 100.00). Total question marks must equal this value upon publication.</td>
          </tr>
          <tr>
            <td><strong>Allow Late Submission</strong></td>
            <td>Checkbox</td>
            <td>If enabled, submissions after the due date are flagged as "Late" but accepted for grading.</td>
          </tr>
        </tbody>
      </table>
    </section>

    <!-- 3.6 Assessment Drafting & Question Authoring -->
    <section id="faculty-assessment-draft" class="section-card">
      <span class="section-tag tag-teal">Faculty Manual</span>
      <h2 class="section-title">3.6 Assessment Drafting & Question Authoring</h2>
      <p>
        Manage drafts, configure rubrics, and define expected answers for Gemini AI evaluation.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 13: Created & Assigned Assessments Roster</div>
          <span class="figure-badge">Screenshot 2026-10-19 114657.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/13_faculty_create_assessment_draft.png" class="screenshot-img" alt="Created Assessments Table">
        </div>
        <div class="screenshot-caption">Assessment management table displaying Title, Type (Project, Written, Assignment), Course, Semester, and Action buttons (Submissions, Grade, Questions).</div>
      </div>

      <h3 class="subsection-title" id="faculty-ai-evaluation">Gemini AI Descriptive Grading Engine</h3>
      <p>
        EduDigiClass integrates a state-of-the-art Google Gemini AI evaluation engine:
      </p>
      <div class="grid-2">
        <div class="feature-box">
          <div class="feature-icon icon-purple">🎯</div>
          <h4>50% Conceptual Benchmark Threshold</h4>
          <p>Evaluates understanding depth. Submissions matching ≥ 50% of core concepts are flagged as Benchmark Met. Below 50% triggers mandatory faculty review.</p>
        </div>
        <div class="feature-box">
          <div class="feature-icon icon-teal">⚖️</div>
          <h4>Natural Academic Marks Ceiling</h4>
          <p>Prevents unrealistic 100% scores on subjective essays. Implements realistic increments (.00, .25, .50, .75) with natural ceilings (80-88% for exceptional work).</p>
        </div>
      </div>
    </section>

    <!-- 3.7 Student Progress & Learning Analytics -->
    <section id="faculty-analytics" class="section-card">
      <span class="section-tag tag-teal">Faculty Manual</span>
      <h2 class="section-title">3.7 Learner Telemetry & Performance Analytics Hub</h2>
      <p>
        Instructors track cohort engagement in real time, inspecting video lecture watch time, reading completion, and identifying at-risk learners.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 14: Student Progress & Learning Analytics Hub</div>
          <span class="figure-badge">Screenshot 2026-10-19 114732.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/14_faculty_student_analytics_hub.png" class="screenshot-img" alt="Student Progress & Analytics Hub">
        </div>
        <div class="screenshot-caption">Analytics cockpit displaying program-wide engagement metrics, video lecture consumption percentages, quiz scores, and at-risk learner highlights.</div>
      </div>
    </section>

    <!-- CHAPTER 4: STUDENT LEARNING MANUAL -->
    <section id="student-dashboard" class="section-card">
      <span class="section-tag tag-blue">Chapter 4 • Student Learning Manual</span>
      <h2 class="section-title">4.1 Student Portal Dashboard & Academic Journey</h2>
      <p>
        The student dashboard is the personalized home for every learner, providing instant visibility into daily schedules, course progress, and announcements.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 15: Student Portal Dashboard Overview</div>
          <span class="figure-badge">Screenshot 2026-10-19 114812.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/15_student_dashboard_overview.png" class="screenshot-img" alt="Student Portal Dashboard">
        </div>
        <div class="screenshot-caption">Student homepage showing "Welcome back, Test Student!", Last Login timestamp (19 Oct 2026, 04:47 am), enrolled courses, and navigation drawer.</div>
      </div>

      <h3 class="subsection-title">Student Navigation Structure</h3>
      <table class="spec-table">
        <thead>
          <tr>
            <th>Menu Item</th>
            <th>Section</th>
            <th>Function & Target Destination</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>Dashboard</strong></td>
            <td>LEARN</td>
            <td>Home view with summary cards, course progress widgets, and quick-start links.</td>
          </tr>
          <tr>
            <td><strong>Syllabus</strong></td>
            <td>LEARN</td>
            <td>Official digital syllabus outlines, course codes, and credit breakdowns.</td>
          </tr>
          <tr>
            <td><strong>Class Calendar</strong></td>
            <td>LEARN</td>
            <td>Schedule of all upcoming live Zoom classes, assignment deadlines, and exam dates.</td>
          </tr>
          <tr>
            <td><strong>Courses</strong></td>
            <td>LEARN</td>
            <td>UGC 4-Quadrant multimedia learning hub for all actively enrolled subjects.</td>
          </tr>
          <tr>
            <td><strong>Assignments & Exams</strong></td>
            <td>LEARN</td>
            <td>Assessment taking center for formative tests, quizzes, and end-term examinations.</td>
          </tr>
          <tr>
            <td><strong>My Grade Card</strong></td>
            <td>LEARN</td>
            <td>Interactive dashboard and A4 printable semester grade card with UGC 10-point scale.</td>
          </tr>
          <tr>
            <td><strong>My Attendance</strong></td>
            <td>LEARN</td>
            <td>Detailed login presence ledger tracking attendance percentage and daily logins.</td>
          </tr>
          <tr>
            <td><strong>My Progress Report</strong></td>
            <td>LEARN</td>
            <td>Telemetry breakdown across video watch time, reading completion, and quiz scores.</td>
          </tr>
        </tbody>
      </table>
    </section>

    <!-- 4.2 Interactive Calendar & Zoom -->
    <section id="student-calendar" class="section-card">
      <span class="section-tag tag-blue">Student Manual</span>
      <h2 class="section-title">4.2 Classroom Schedule & Live Zoom Sessions</h2>
      <p>
        Students access daily virtual classes via the interactive schedule. With 1-click Zoom integration, learners join live lectures directly from their browser or mobile app.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 16: Student Classroom Schedule & Live Sessions</div>
          <span class="figure-badge">Screenshot 2026-10-19 114835.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/16_student_classroom_schedule_zoom.png" class="screenshot-img" alt="Student Classroom Schedule">
        </div>
        <div class="screenshot-caption">Classroom calendar showing scheduled live meetings, start times, course names, and the direct Join Class button.</div>
      </div>
    </section>

    <!-- 4.3 My Courses & UGC 4-Quadrant Viewer -->
    <section id="student-courses" class="section-card">
      <span class="section-tag tag-blue">Student Manual</span>
      <h2 class="section-title">4.3 My Courses & UGC 4-Quadrant Learning Hub</h2>
      <p>
        All subjects follow the statutory **UGC 4-Quadrant E-Learning Framework**, organizing curriculum content into four distinct modes of instruction.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 17: My Courses & Quadrant Learning Hub</div>
          <span class="figure-badge">Screenshot 2026-10-19 114904.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/17_student_my_courses_viewer.png" class="screenshot-img" alt="Student My Courses Hub">
        </div>
        <div class="screenshot-caption">Enrolled course viewer displaying subject cards, progress indicators, syllabus units, and Continue Learning action buttons.</div>
      </div>

      <div class="grid-2">
        <div class="feature-box">
          <div class="feature-icon icon-purple">🎥</div>
          <h4>Quadrant 1: E-Tutorial (Video)</h4>
          <p>High-definition video lectures and recorded sessions with automatic watch time tracking and bookmarking.</p>
        </div>
        <div class="feature-box">
          <div class="feature-icon icon-teal">📖</div>
          <h4>Quadrant 2: E-Content (Reading)</h4>
          <p>Digital textbooks, downloadable PDF notes, case studies, and research compendiums rendered securely.</p>
        </div>
        <div class="feature-box">
          <div class="feature-icon icon-blue">📝</div>
          <h4>Quadrant 3: Self-Assessment</h4>
          <p>Topic quizzes, multiple-choice practice tests, and self-check questions with instant answer explanations.</p>
        </div>
        <div class="feature-box">
          <div class="feature-icon icon-amber">💬</div>
          <h4>Quadrant 4: Discussion Forum</h4>
          <p>Moderated course discussion boards, doubt-clearing threads, and peer collaboration with instructors.</p>
        </div>
      </div>
    </section>

    <!-- 4.4 Assessment & Quiz Center -->
    <section id="student-assessments" class="section-card">
      <span class="section-tag tag-blue">Student Manual</span>
      <h2 class="section-title">4.4 Assessments, Quizzes & Exam Taking Center</h2>
      <p>
        Learners attempt scheduled topic tests, submit written project files, and sit for formal semester examinations within a secure timed browser environment.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 18: Assessments & Quizzes Center</div>
          <span class="figure-badge">Screenshot 2026-10-19 114935.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/18_student_assessments_quizzes.png" class="screenshot-img" alt="Student Assessments Hub">
        </div>
        <div class="screenshot-caption">Unified assessment desk showing scheduled tests, duration, total marks, deadlines, and the "Attempt Assessment" trigger.</div>
      </div>

      <div class="callout callout-warning">
        <div class="callout-icon">⏱️</div>
        <div class="callout-content">
          <strong>Timed Assessment Security Rules</strong>
          Once initiated, the countdown timer runs continuously. Auto-save ensures answers are preserved even if the connection drops. Students submitting Project or Presentation tasks can upload PDF/DOCX files up to 8MB.
        </div>
      </div>
    </section>

    <!-- 4.5 Automatic Attendance Log -->
    <section id="student-attendance" class="section-card">
      <span class="section-tag tag-blue">Student Manual</span>
      <h2 class="section-title">4.5 Login-Based Automatic Attendance Telemetry</h2>
      <p>
        In addition to lecture presence, EduDigiClass features automatic login attendance logging. Whenever a student logs into the platform, their presence is recorded in the immutable institutional ledger.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 19: My Attendance Log & Telemetry Ledger</div>
          <span class="figure-badge">Screenshot 2026-10-19 115010.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/19_student_attendance_log.png" class="screenshot-img" alt="Student Attendance Log">
        </div>
        <div class="screenshot-caption">Attendance telemetry displaying current attendance rate (90.9%), Days Tracked (11), Days Present (10), Absences (1), and daily login timestamps.</div>
      </div>

      <h3 class="subsection-title">Attendance Policy Rules</h3>
      <table class="spec-table">
        <thead>
          <tr>
            <th>Telemetry Metric</th>
            <th>Current Value</th>
            <th>Compliance Standard</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>Current Attendance Rate</strong></td>
            <td><strong style="color: #059669; font-size: 15px;">90.9%</strong></td>
            <td>Minimum 75% required by statutory university policy to qualify for end-term examinations.</td>
          </tr>
          <tr>
            <td><strong>Total Days Tracked</strong></td>
            <td>11 Days</td>
            <td>Total instructional days elapsed since term enrollment.</td>
          </tr>
          <tr>
            <td><strong>Days Present</strong></td>
            <td>10 Days</td>
            <td>Days where successful login session was verified.</td>
          </tr>
          <tr>
            <td><strong>Late Arrivals / Absences</strong></td>
            <td>0 Late / 1 Absent</td>
            <td>Unrecorded or missed instructional days.</td>
          </tr>
        </tbody>
      </table>
    </section>

    <!-- 4.6 Student Progress Report -->
    <section id="student-progress-report" class="section-card">
      <span class="section-tag tag-blue">Student Manual</span>
      <h2 class="section-title">4.6 Progress Report & Grade Card Download</h2>
      <p>
        Students monitor overall learning metrics across videos, reading materials, quizzes, and assignments, with instant A4 PDF grade card generation.
      </p>

      <div class="screenshot-figure">
        <div class="screenshot-header">
          <div class="figure-title">📸 Figure 20: Test Student's Progress Report</div>
          <span class="figure-badge">Screenshot 2026-10-19 115033.png</span>
        </div>
        <div class="screenshot-img-wrapper">
          <img src="./manual_assets/20_student_progress_report.png" class="screenshot-img" alt="Student Progress Report">
        </div>
        <div class="screenshot-caption">Performance breakdown across all active subjects showing quiz scores, assignment grades, and video watch telemetry.</div>
      </div>
    </section>

    <!-- CHAPTER 5: REFERENCE, CREDENTIALS & TROUBLESHOOTING -->
    <section id="ugc-grading-scale" class="section-card">
      <span class="section-tag tag-purple">Chapter 5 • Reference & Appendix</span>
      <h2 class="section-title">5.1 Institutional UGC 10-Point Letter Grading Scale</h2>
      <p>
        The platform implements the statutory 10-point Choice Based Credit System (CBCS) grading scale:
      </p>

      <table class="spec-table">
        <thead>
          <tr>
            <th>Percentage Band</th>
            <th>Letter Grade</th>
            <th>Grade Point</th>
            <th>Performance Classification</th>
            <th>Academic Standing</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>≥ 90.00%</td>
            <td><span class="grade-badge grade-o">O</span></td>
            <td><strong>10.0</strong></td>
            <td>Outstanding</td>
            <td>Cleared with Distinction</td>
          </tr>
          <tr>
            <td>80.00% – 89.99%</td>
            <td><span class="grade-badge grade-aplus">A+</span></td>
            <td><strong>9.0</strong></td>
            <td>Excellent</td>
            <td>Cleared</td>
          </tr>
          <tr>
            <td>70.00% – 79.99%</td>
            <td><span class="grade-badge grade-a">A</span></td>
            <td><strong>8.0</strong></td>
            <td>Very Good</td>
            <td>Cleared</td>
          </tr>
          <tr>
            <td>60.00% – 69.99%</td>
            <td><span class="grade-badge grade-bplus">B+</span></td>
            <td><strong>7.0</strong></td>
            <td>Good</td>
            <td>Cleared</td>
          </tr>
          <tr>
            <td>50.00% – 59.99%</td>
            <td><span class="grade-badge grade-b">B</span></td>
            <td><strong>6.0</strong></td>
            <td>Above Average</td>
            <td>Cleared</td>
          </tr>
          <tr>
            <td>40.00% – 49.99%</td>
            <td><span class="grade-badge grade-c">C</span></td>
            <td><strong>5.0</strong></td>
            <td>Average</td>
            <td>Cleared</td>
          </tr>
          <tr>
            <td>33.00% – 39.99%</td>
            <td><span class="grade-badge grade-p">P</span></td>
            <td><strong>4.0</strong></td>
            <td>Pass</td>
            <td>Marginal Pass</td>
          </tr>
          <tr>
            <td>&lt; 33.00%</td>
            <td><span class="grade-badge grade-f">F</span></td>
            <td><strong>0.0</strong></td>
            <td>Fail</td>
            <td>Eligible for Compartmental Re-assessment</td>
          </tr>
        </tbody>
      </table>

      <div class="callout callout-info">
        <div class="callout-icon">⚖️</div>
        <div class="callout-content">
          <strong>Grace Marks & Compartmental Examination Engine</strong>
          Students failing within a narrow threshold (1–5 marks) can receive automated discretionary grace marks as governed by institutional policy. For students receiving an <strong>F</strong> grade, the system enables 1-click creation of a compartmental supplementary assessment (Attempt #2).
        </div>
      </div>
    </section>

    <!-- 5.2 Troubleshooting & FAQ -->
    <section id="troubleshooting-faq" class="section-card">
      <span class="section-tag tag-purple">Reference & Support</span>
      <h2 class="section-title">5.2 Frequently Asked Questions & Troubleshooting</h2>

      <div class="feature-box" style="margin-bottom: 16px;">
        <h4>Q: My Zoom live class link is not opening or shows "Host has not started meeting".</h4>
        <p>A: Live classes require the faculty host to start the room first. If you join earlier than 5 minutes before scheduled start time, wait on the meeting room page or refresh your browser.</p>
      </div>

      <div class="feature-box" style="margin-bottom: 16px;">
        <h4>Q: My file upload for the project assignment was rejected.</h4>
        <p>A: Ensure your file is in <span class="code-token">.pdf</span>, <span class="code-token">.docx</span>, <span class="code-token">.pptx</span>, or image formats, and does not exceed the <strong>8 MB</strong> limit. Encrypted or password-protected files cannot be processed by the server validator.</p>
      </div>

      <div class="feature-box" style="margin-bottom: 16px;">
        <h4>Q: How do I print my official semester grade card?</h4>
        <p>A: Navigate to <strong>My Grade Card</strong> in the student menu. Click the blue <strong>Print Grade Card</strong> button. In the browser print dialog, select <strong>Save as PDF</strong> with paper size set to <strong>A4 Portrait</strong>.</p>
      </div>
    </section>

    <!-- Footer -->
    <footer class="doc-footer">
      <div>
        <strong>EduDigiClass LMS</strong> — Unified University Learning Management Platform
        <br>Version 10.0 (net10.0 LTS) • Document Reference: <span class="code-token">ED-LMS-MANUAL-2026-V1</span>
      </div>
      <div>
        © 2026 Vagmine Pro Techno Pvt. Ltd. All rights reserved.
      </div>
    </footer>

  </main>
</div>

<script>
  function filterNav() {
    const input = document.getElementById('manualSearch').value.toLowerCase();
    const links = document.querySelectorAll('.sidebar .nav-link');
    links.forEach(link => {
      const text = link.innerText.toLowerCase();
      if (text.includes(input)) {
        link.style.display = 'flex';
      } else {
        link.style.display = 'none';
      }
    });
  }

  // Smooth scroll
  document.querySelectorAll('.sidebar .nav-link').forEach(anchor => {
    anchor.addEventListener('click', function(e) {
      document.querySelectorAll('.sidebar .nav-link').forEach(l => l.classList.remove('active'));
      this.classList.add('active');
    });
  });
</script>

</body>
</html>
"""

# Write HTML file
html_path = os.path.join(OUTPUT_DIR, "EduDigi_LMS_Comprehensive_User_Manual.html")
with open(html_path, "w", encoding="utf-8") as f:
    f.write(html_content)
print(f"Generated HTML manual: {html_path}")

# Build Markdown Content
md_content = """# EduDigiClass LMS — Comprehensive Enterprise User Manual

**Platform:** EduDigiClass Learning Management Platform (ASP.NET Core .NET 10.0)  
**Host Target:** `http://20.193.144.80:82`  
**Database:** Microsoft SQL Server (`LMS-EduDigi`)  
**Standard:** UGC 4-Quadrant Framework, CBCS 10-Point Letter Grading, Gemini AI Evaluation  
**Audience:** Super Administrators, Faculty / Teachers, Student Learners  
**Document Code:** `ED-LMS-MANUAL-2026-V1`

---

## Executive Summary & Architecture

EduDigiClass is an enterprise learning management system designed for universities and higher-education institutions. It unifies:
1. **Academic Program & Curriculum Master** (Degrees, Semesters, Modular Subjects, Credits)
2. **Virtual Classroom** with integrated 1-click Zoom scheduling
3. **UGC 4-Quadrant E-Learning Framework** (Video, E-Text, Self-Assessment, Discussion)
4. **AI-Powered Question Bank & Paper Generator** (Google Gemini integration with Bloom's Taxonomy)
5. **AI Descriptive Answer Evaluation** (50% conceptual benchmark threshold, natural scoring ceiling)
6. **Statutory UGC 10-Point CBCS Grading Engine** with Grace Marks & Compartmental exam creation
7. **Login-Based Automatic Attendance Telemetry** (Real-time tracking, 75% statutory compliance)
8. **Bilingual Localization** (English & Urdu) with real-time Right-to-Left (RTL) rendering

---

## System Access & Default Credentials

| Portal Role | URL Endpoint | Default Email | Default Password | Privileges |
| :--- | :--- | :--- | :--- | :--- |
| **Super Admin** | `http://20.193.144.80:82/Admin/User` | `admin@TMS.com` | `Admin@123` | Institutional governance, bulk upload, policies |
| **Faculty / Teacher** | `http://20.193.144.80:82/InternalAssessment/FacultyDashboard` | `faculty@edudigi.com` | `Faculty@123` | Course delivery, Zoom live classes, AI grading |
| **Student Learner** | `http://20.193.144.80:82/MyCourses` | `student@edudigi.com` | `Student@123` | 4-Quadrant content, live classes, exams, grade card |

---

## CHAPTER 1: Authentication Gateway & Bilingual RTL

### 1.1 Online Learning Platform Gateway & Role Selector
The gateway provides dedicated login entry points for each university stakeholder:
- **Student Login**: Direct entry to `/Auth/Login` for courses, attendance, and exam taking.
- **Faculty Login**: Secure entry to the Faculty Command Center.
- **Administrative Login**: University administrative management panel.
- **Bilingual Toggle**: Toggle between English and Urdu with automatic RTL layout transformation.
- **Get App**: Direct downloads for iOS and Android mobile learning apps.

![Figure 1: Authentication Hub](./manual_assets/01_login_portal_gateway.png)

---

## CHAPTER 2: Super Admin Operations Manual

### 2.1 Institutional Command Dashboard
The Super Admin dashboard monitors total platform metrics, active courses, enrolled students, certified instructors, and system health.
- **Universal Search (`Ctrl + K`)**: Instantly search courses, syllabi, quizzes, faculty, and students.
- **RBAC Matrix**: Enforce granular role privileges.

![Figure 2: Admin Dashboard](./manual_assets/02_admin_dashboard_overview.png)

### 2.2 User Directory & Governance
The user table enables administrators to audit, search, filter, edit, and reset passwords for any account.
- **Role Badges**: Identifies account types (`Faculty (Active)`, `Student`, `Super Admin`).
- **Pagination & Filters**: Search across thousands of entries with sub-second response times.

![Figure 3: All Users Directory](./manual_assets/03_admin_user_management_table.png)

### 2.3 Bulk User Onboarding Center (CSV / Excel)
Facilitates batch creation of hundreds of user accounts simultaneously:
1. **Download Template**: Retrieve standard CSV/Excel format with mandatory fields (`Name`, `Email`, `Phone`, `Role`, `Password`).
2. **Upload Data**: Drag & drop `.csv`, `.xlsx`, or `.xls` files.
3. **Preview & Validate**: Server-side validation detects format errors and existing duplicates.
4. **Execution & Confirmation**: Creates accounts and sets **Force Password Change on First Login**.

![Figure 4: Bulk User Upload Center](./manual_assets/04_admin_bulk_upload_users.png)

### 2.4 Academic Hub & Program Hierarchy
Author and organize degree programs, course catalogs, and syllabus structures:
- **Active Programs**: Bachelor of Business Administration (BBA - 6 Semesters, 5 Courses, 57 Content Items), Bachelor of Computer Applications (BCA - 6 Semesters, 6 Courses).
- **Curriculum Hierarchy Tree View**: Visual tree mapping Programs -> Semesters -> Courses -> Units -> Topics.

![Figure 5: Academic Hub Hierarchy](./manual_assets/05_admin_course_curriculum_hierarchy.png)

### 2.5 Global Content Center (Content Library)
Centralized repository following the "Upload once, assign anywhere" model:
- Supports PDF textbooks, video lectures, PPT presentations, and syllabus documents.
- Automatic binary signature inspection prevents corrupted or malicious uploads.

![Figure 6: Content Library](./manual_assets/06_admin_global_content_library.png)

### 2.6 Academic Enrollment Center
Assign and audit student enrollments across academic degree programs and individual modular courses. Enrolled learners immediately receive access to their assigned curriculum.

![Figure 7: Academic Enrollment Center](./manual_assets/07_admin_academic_enrollment_center.png)

### 2.7 All Student Grade Cards & Evaluation Ledger
Institutional grade repository displaying semester-wise grade distributions, published assessment marks, and UGC 10-point scale grades.

![Figure 8: Student Grade Cards](./manual_assets/08_admin_student_grade_cards.png)

---

## CHAPTER 3: Faculty Operations Manual

### 3.1 Faculty Teaching Command Desk
Faculty view assigned courses, active learners, upcoming Zoom lectures, and submissions awaiting evaluation.

![Figure 9: Faculty Dashboard](./manual_assets/09_faculty_dashboard_overview.png)

### 3.2 Assigned Courses & Syllabus Management
Inspect assigned subject modules, learning outcomes, credit weightages, and topic structures.

![Figure 10: Faculty Curriculum](./manual_assets/10_faculty_courses_curriculum.png)

### 3.3 Virtual Classroom & Live Zoom Session Scheduling
Schedule live interactive lectures without leaving the LMS:
1. Click **Schedule Zoom Class**.
2. Select course, define topic title, date, time, and duration.
3. System provisions a secure Zoom meeting room and adds a 1-click **Join Class** button to every enrolled student's calendar.

![Figure 11: Virtual Classroom Zoom Hub](./manual_assets/11_faculty_virtual_classroom_zoom.png)

### 3.4 AI Question Bank & Automatic Question Paper Generator
- **Gemini AI Suggested Questions**: Generate MCQs and descriptive questions mapped to Bloom's Taxonomy (Remember, Understand, Apply, Analyze, Evaluate, Create).
- **1-Click Blueprint Assembly**: Auto-assemble formal question papers with sections (Section A: MCQs, Section B: Short Notes, Section C: Long Problems) and print institutional hardcopies.

### 3.5 Internal Assessment Policy & Authoring
Create assessments linked to university-approved windows:
- **Assessment Types**: Multi-select combinations of `Assignment`, `Written`, `Practical`, `Project`, `Presentation`, `Viva`, and `MCQ`.
- **Submission Rules**: Configure maximum marks, due dates, and allow late submissions.

![Figure 12: Internal Assessment Form](./manual_assets/12_faculty_internal_assessments.png)

### 3.6 Assessment Drafting & Gemini AI Evaluation
- Configure marking rubrics and expected answers for descriptive questions.
- **Gemini AI Evaluation**: Automatically analyzes conceptual match depth.
- **50% Conceptual Threshold**: Flags answers meeting core concepts; triggers review if below.
- **Natural Marks Ceiling**: Prevents unrealistic 100% scores on subjective essays, utilizing quarter-mark increments (.00, .25, .50, .75).

![Figure 13: Assessment Drafts](./manual_assets/13_faculty_create_assessment_draft.png)

### 3.7 Student Progress & Learning Analytics Hub
Track video lecture watch time percentages, reading completion, and quiz scores, with proactive flags for at-risk learners.

![Figure 14: Analytics Hub](./manual_assets/14_faculty_student_analytics_hub.png)

---

## CHAPTER 4: Student Learning Manual

### 4.1 Student Portal Dashboard
Personalized learning desk displaying enrolled courses, daily class schedules, attendance percentage, and academic certificates.

![Figure 15: Student Dashboard](./manual_assets/15_student_dashboard_overview.png)

### 4.2 Classroom Schedule & Live Zoom Sessions
Interactive calendar with Month, Week, and Day views displaying upcoming classes and direct 1-click **Join Class** triggers.

![Figure 16: Student Calendar & Zoom](./manual_assets/16_student_classroom_schedule_zoom.png)

### 4.3 My Courses & UGC 4-Quadrant Multimedia Hub
Subjects are delivered in four statutory quadrants:
- **Quadrant 1 (Video)**: High-definition video lectures with watch time telemetry.
- **Quadrant 2 (Reading)**: Downloadable PDF textbooks and lecture notes.
- **Quadrant 3 (Self-Assessment)**: Practice quizzes and self-check tests.
- **Quadrant 4 (Discussion)**: Doubt-clearing forums and instructor interaction.

![Figure 17: My Courses Hub](./manual_assets/17_student_my_courses_viewer.png)

### 4.4 Assessments & Quizzes Taking Center
Complete timed topic quizzes and formal examinations within a secure browser window with automated draft preservation.

![Figure 18: Assessments Center](./manual_assets/18_student_assessments_quizzes.png)

### 4.5 Login-Based Automatic Attendance Telemetry
In addition to lecture attendance, the platform records presence automatically upon user login:
- Displays **Current Attendance Rate** (e.g. **90.9%**).
- Tracks Total Days Tracked, Days Present, Late Arrivals, and Absences.

![Figure 19: Attendance Log](./manual_assets/19_student_attendance_log.png)

### 4.6 Student Progress Report & Grade Card Download
Comprehensive telemetry report and 1-click printable A4 semester grade card.

![Figure 20: Progress Report](./manual_assets/20_student_progress_report.png)

---

## CHAPTER 5: Institutional Reference & Appendix

### 5.1 UGC Standard 10-Point CBCS Grading Scale

| Percentage Cutoff | Letter Grade | Grade Point | Classification | Result Status |
| :---: | :---: | :---: | :--- | :--- |
| **≥ 90.00%** | **O** | **10.0** | Outstanding | Distinction |
| **80.00% – 89.99%** | **A+** | **9.0** | Excellent | Cleared |
| **70.00% – 79.99%** | **A** | **8.0** | Very Good | Cleared |
| **60.00% – 69.99%** | **B+** | **7.0** | Good | Cleared |
| **50.00% – 59.99%** | **B** | **6.0** | Above Average | Cleared |
| **40.00% – 49.99%** | **C** | **5.0** | Average | Cleared |
| **33.00% – 39.99%** | **P** | **4.0** | Pass | Marginal Pass |
| **< 33.00%** | **F** | **0.0** | Fail | Compartmental Re-assessment |

### 5.2 Grace Marks & Compartmental Examination Engine
- **Grace Marks**: Discretionary grace marks (1–5 marks) automatically applied to borderline students per policy.
- **Compartmental Exams**: 1-click automated cloning of assessments into supplementary re-assessments (`AttemptNumber = 2`, `IsCompartmental = true`).

---
*© 2026 Vagmine Pro Techno Pvt. Ltd. All rights reserved.*
"""

# Write Markdown file
md_path = os.path.join(OUTPUT_DIR, "EduDigi_LMS_Comprehensive_User_Manual.md")
with open(md_path, "w", encoding="utf-8") as f:
    f.write(md_content)
print(f"Generated Markdown manual: {md_path}")

# Copy into docs/
shutil_cp = f"cp '{html_path}' '{DOCS_DIR}/'; cp '{md_path}' '{DOCS_DIR}/'"
subprocess.run(shutil_cp, shell=True)

# Generate PDF using Google Chrome Headless
pdf_path = os.path.join(OUTPUT_DIR, "EduDigi_LMS_Comprehensive_User_Manual.pdf")
chrome_bin = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"

if os.path.exists(chrome_bin):
    print(f"Generating PDF via Google Chrome Headless...")
    chrome_cmd = [
        chrome_bin,
        "--headless=new",
        "--disable-gpu",
        "--no-pdf-header-footer",
        f"--print-to-pdf={pdf_path}",
        html_path
    ]
    res = subprocess.run(chrome_cmd, capture_output=True, text=True)
    if os.path.exists(pdf_path) and os.path.getsize(pdf_path) > 1000:
        print(f"Successfully generated PDF manual: {pdf_path} ({os.path.getsize(pdf_path)} bytes)")
        # Also copy to docs/ and central documents directory
        subprocess.run(f"cp '{pdf_path}' '{DOCS_DIR}/'", shell=True)
        subprocess.run(f"cp '{pdf_path}' '{CENTRAL_DOCS_DIR}/'", shell=True)
        subprocess.run(f"cp '{html_path}' '{CENTRAL_DOCS_DIR}/'", shell=True)
        subprocess.run(f"cp '{md_path}' '{CENTRAL_DOCS_DIR}/'", shell=True)
        print(f"Copied PDF & HTML to {CENTRAL_DOCS_DIR}")
    else:
        print(f"Chrome PDF stderr: {res.stderr}")
else:
    print(f"Chrome not found at {chrome_bin}")

print("=== User Manual Generation Complete ===")
