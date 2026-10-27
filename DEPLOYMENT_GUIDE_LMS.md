# LMS EduDigi Platform - VM Deployment Guide

**Target VM IP:** `20.193.144.80`  
**Database:** Microsoft SQL Server (`127.0.0.1,1433`, Database: `LMS-EduDigi`, User: `sa`, Password: `Vagmine@1234`)  
**Architecture:** ASP.NET Core MVC (net10.0) + Razor Views + Bilingual (English & Urdu)

---

## 🚀 Port & Route Architecture

| Component | Port | Production URL on VM |
| :--- | :---: | :--- |
| **LMS Web Platform** | **`82`** | `http://20.193.144.80:82` |
| **Login Portal** | **`82`** | `http://20.193.144.80:82/Auth/Login` |
| **Admin Portal** | **`82`** | `http://20.193.144.80:82/Admin/User` |
| **Faculty Portal** | **`82`** | `http://20.193.144.80:82/InternalAssessment/FacultyDashboard` |
| **Student Portal** | **`82`** | `http://20.193.144.80:82/MyCourses` |
| **Internal Assessment & AI Evaluation** | **`82`** | `http://20.193.144.80:82/InternalAssessment/AdminDashboard` |

---

## 📦 Step-by-Step Deployment Instructions

### Method 1: Automated IIS Deployment (Recommended)

1. **Copy the zip file to the VM:**
   - VM me RDP (`20.193.144.80:3389`) se login karein.
   - `LMS_VM_Deploy.zip` ko VM me copy karein aur extract karein (jaise `C:\LMS_VM_Deploy`).

2. **Open PowerShell as Administrator:**
   - Windows Start menu kholein -> Search karein **PowerShell** -> Right Click karke **"Run as Administrator"** select karein.

3. **Navigate to the extracted folder and run:**
   ```powershell
   cd C:\LMS_VM_Deploy
   powershell -ExecutionPolicy Bypass -File .\LMS_Setup_IIS.ps1
   ```

4. **Script kya-kya automate karta h:**
   - `C:\inetpub\wwwroot\LMS-EduDigi` folder banata h.
   - Nayi compiled production files copy karta h.
   - Purana `uploads` aur `InternalAssessmentUploads` folder safely preserve rakhta h.
   - IIS Application Pool (`LMS_AppPool` - No Managed Code, 64-bit) configure karta h.
   - IIS Website (`LMS-EduDigi`) ko Port **82** par bind karta h.
   - NTFS Permissions (`IIS_IUSRS` & `IUSR`) set karta h.
   - Windows Firewall me Port **82** inbound allow kar deta h.
   - IIS site restart karke health check karta h.

---

### Method 2: Direct Standalone Runner (No IIS Required)

Agar aapko bina IIS ke direct chalana ho:
1. Extract kiye hue folder me jakar **`Run_LMS_Direct.bat`** par double click karein.
2. LMS Kestrel engine par Port **82** par live ho jayega (`http://20.193.144.80:82`).

---

## ☁️ Azure NSG Firewall Check (Important)

Bahar ke browser se access karne ke liye ensure kar lein ki Azure NSG me Port **82** allowed ho:
1. **Azure Portal** -> Virtual Machine (`20.193.144.80`) -> **Networking** -> **Inbound port rules**.
2. Check karein ki Destination Port **82** (TCP) Allow rule me included hai ya nahi (e.g., port range `80-90` me already included ho sakta hai).

---

## 🔑 Default Login Credentials

| Role | Email | Password |
| :--- | :--- | :--- |
| **Super Admin** | `admin@TMS.com` | `Admin@123` |
| **Faculty** | `faculty@edudigi.com` | `Faculty@123` |
| **Student** | `student@edudigi.com` | `Student@123` |

---

## ✨ Features Included in This Build

1. **English & Urdu Language Switcher**:
   - Header me interactive pill toggle (🇬🇧 English / 🇮🇳 اردو)
   - Real-time RTL (Right-to-Left) layout switching for Admin, Faculty, and Student portals
   - Per-user session and localStorage persistence
2. **Internal Assessment & AI Conceptual Evaluation**:
   - Gemini AI evaluation engine with 50% conceptual benchmark & natural academic scoring
   - Question rubric configuration
   - Project & presentation document uploads/downloads
   - Dynamic MCQ visibility
3. **Database Pre-configured**:
   - VM SQL Server (`127.0.0.1:1433`) connection pre-set in `appsettings.json` and `appsettings.Production.json`.
