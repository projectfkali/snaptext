# SnapText V4 - Ultra Modern OCR Tool

<p align="center">
  <img src="Assets/logo.png" width="128" height="128" alt="SnapText Logo">
</p>

[English](#english) | [Türkçe](#türkçe)

---

## English

**SnapText V4** is a high-performance, ultra-modern screen text extraction (OCR) tool developed for the Windows platform. Featuring a premium **Glassmorphism** design and advanced productivity tools, it allows you to extract, search, translate, and listen to any text on your screen.

### 🚀 New in V4 & Features

- **Ultra Modern UI:** Premium interface designed with **Glassmorphism** (transparency & blur) and vibrant gradients.
- **Global Hotkey:** Access the selection tool instantly using `Ctrl + Shift + S`.
- **Floating Quick Actions (Smart Bar):** 
  - **Copy:** Instant clipboard copy.
  - **Search:** Search the extracted text on Google with one click.
  - **Translate:** Translate the text to your target language via Google Translate.
  - **Listen (TTS):** Read the text aloud using the built-in Text-to-Speech engine.
- **Dynamic OCR Engine:** Automatically detects and supports all OCR languages installed on your Windows system.
- **Autostart:** Option to launch SnapText automatically when Windows starts.
- **DPI Scaling Fix:** Works perfectly on 4K and multi-monitor setups with different scaling.
- **System Tray:** Minimal footprint, runs in the background.

### 🛠 Tech Stack

- **Language:** C# (.NET 9)
- **Framework:** WPF (Windows Presentation Foundation)
- **UI Architecture:** Custom Glassmorphism Template + [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- **OCR Engine:** Windows Media OCR API
- **Dependencies:** System.Speech (for TTS), Windows Registry (for Autostart)

### 📦 Installation & Run

To run the project locally:

```powershell
git clone https://github.com/projectfkali/snaptext.git
cd snaptext
dotnet run
```

---

## Türkçe

**SnapText V4**, Windows platformu için geliştirilmiş, yüksek performanslı ve ultra modern bir ekran metni okuma (OCR) aracıdır. Yeni nesil **Glassmorphism** tasarımı ve gelişmiş verimlilik araçlarıyla ekranınızdaki her metni çıkarmanıza, aratmanıza, çevirmenize ve dinlemenize olanak tanır.

### 🚀 V4 Yenilikleri ve Özellikler

- **Ultra Modern UI:** Şeffaflık ve buzlu cam (Glassmorphism) efektleriyle tasarlanmış, premium kullanıcı arayüzü.
- **Global Kısayol:** `Ctrl + Shift + S` kombinasyonu ile anında ekran yakalamayı başlatın.
- **Yüzer Hızlı İşlem Barı (Smart Bar):**
  - **Kopyala:** Metni anında panoya kopyalayın.
  - **Ara:** Metni tek tıkla Google'da aratın.
  - **Tercüme Et:** Belirlediğiniz hedef dile Google Translate üzerinden çevirin.
  - **Dinle (TTS):** Dahili metinden sese (Text-to-Speech) motoruyla metni sesli dinleyin.
- **Dinamik OCR Motoru:** Windows sisteminizde yüklü olan tüm OCR dillerini otomatik olarak tanır ve destekler.
- **Başlangıçta Çalıştır:** Windows açıldığında uygulamanın otomatik olarak başlatılmasını sağlar.
- **DPI Uyumluluğu:** 4K ve farklı ölçeklendirilmiş çoklu monitörlerde kusursuz çalışır.
- **Sistem Tepsisi:** Arka planda çalışarak sistem kaynaklarını yormaz.

### 🛠 Teknoloji Yığını

- **Dil:** C# (.NET 9)
- **Framework:** WPF (Windows Presentation Foundation)
- **UI Mimarisi:** Özel Cam Efekti Şablonu + [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- **OCR Motoru:** Windows Media OCR API
- **Bağımlılıklar:** System.Speech (TTS için), Windows Registry (Başlangıç Ayarı için)

### 📦 Kurulum ve Çalıştırma

Projeyi yerelinizde çalıştırmak için:

```powershell
git clone https://github.com/projectfkali/snaptext.git
cd snaptext
dotnet run
```

---
Developed by: **projectfkali**
