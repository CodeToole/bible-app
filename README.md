# 📖 Bible Study App

A high-speed, multi-platform King James Version (KJV) scripture study engine and note-expansion platform engineered with **.NET 10**, **Blazor WebAssembly**, **Azure Static Web Apps**, and **.NET MAUI Blazor Hybrid**. Developed by **Waitaminute Digital**.

---

## 🚀 Access & Downloads

### 🌐 Web Application (No Installation Required)
Use the web app directly inside Chrome, Edge, Safari, or mobile browsers:  
👉 **[Launch Live Web App](https://nice-coast-05e9a7e10.7.azurestaticapps.net)**

### 💻 Windows Desktop Application
Download the native Windows setup wizard (`.exe`) for offline reading and local SQLite note storage:
* **ARM64 (Snapdragon Copilot+ PCs):** Download installer from GitHub Actions artifacts.
* **x64 / x86 Windows Desktop:** Download installer from GitHub Actions artifacts.

---

## ✨ Key Features

* ⚡ **Client-Side Blazor WASM Engine:** Loads all 66 books and 31,102 KJV verses directly in memory for instant multi-word searches and chapter rendering with zero server latency.
* 🧠 **Smart Auto-Expansion Study Notes:** Type or paste raw scripture references (e.g., `Ex 20:1-17`, `Mat 24:1-5, 11-13, 22:1-5`, `Ps 69:8-9, 20-22`), OCR image scans, or handwritten notes. The engine automatically strips noise, handles abbreviations, and expands full passage sheets.
* 🔐 **Firebase Multi-Provider Authentication:** Sign in seamlessly with **Google**, **Microsoft**, or instant **Guest (Anonymous)** mode.
* 📄 **Free Note & Passage Exports:**
  * **Export to PDF:** Built-in CSS print engine to generate clean PDF study sheets.
  * **Download Text (.txt):** Download formatted plain-text files for Microsoft Word or Notepad.
  * **Copy to Clipboard:** One-click text copying for lesson preparation.
* 💾 **Dual Storage Architecture:** Uses browser `localStorage` on the web and native local SQLite (`user_data.db`) on desktop to save highlights, bookmarks, reading history, and notes.

---

## 🛠 Tech Stack & Architecture

| Component | Technology |
| :--- | :--- |
| **Shared UI & Logic** | .NET 10 Razor Class Library (`LumenScriptura.Shared`) |
| **Web Target** | Standalone Blazor WebAssembly (`LumenScriptura.Web`) |
| **Desktop Target** | .NET MAUI Blazor Hybrid (`LumenScriptura`) |
| **Web Cloud Hosting** | Azure Static Web Apps (CDN & SPA routing fallback) |
| **Authentication** | Firebase Auth v10 (Google, Microsoft OAuth, Guest) |
| **Scripture Database** | In-Memory JSON (Web) / Local SQLite `kjv.sqlite` (Desktop) |

---

## 🔧 Local Development & Building

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/)
* Windows 10/11 (for desktop builds)

### Clone & Run
```bash
# Clone repository
git clone https://github.com/CodeToole/bible-app.git
cd bible-app

# Restore solution dependencies
dotnet restore LumenScriptura.slnx

# Run Blazor WebAssembly Client (Web)
dotnet run --project LumenScriptura.Web/LumenScriptura.Web.csproj

# Run MAUI Desktop Client (Windows)
dotnet run --project LumenScriptura.csproj -f net10.0-windows10.0.19041.0
```
