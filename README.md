# C64U Slim-Viewer
[![Platform: Linux | Windows](https://img.shields.io/badge/platform-Linux%20%7C%20Windows-blue)](#)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](#)

A high-performance, lightweight remote video stream viewer specifically designed for the **Commodore 64 Ultimate (U64/U64E)**. Built with .NET 8 and Avalonia UI.

<img width="800" height="604" alt="image" src="https://github.com/user-attachments/assets/ad4347cd-e98f-4274-95f0-e2efa5e19d1e" />


## 📺 Overview
The **C64U Slim-Viewer** allows you to stream the VIC-II video output of your Ultimate 64 directly to your modern linux or windows desktop via your local network.

### Key Features
* 🚀 **Low Latency:** Optimized for a responsive experience using the U64's native data streams.
* 🪶 **Minimalist Design:** A distraction-free window perfect for coding or gaming.
* 🔄 **Auto-Update Check:** In-app notifications for new GitHub releases.
* 🐧 **Cross-Platform:** Native support for Linux (.deb) and Windows.

---

## 🔌 Setup Instructions
Follow these steps to establish a connection between your C64U and this viewer.

### 1. Network Requirements
* Ensure your **Commodore 64 Ultimate (C64U)** is on the same local network as this PC.
* **Important:** Your C64U must be connected via **LAN**. Disable WLAN on the C64U to ensure a stable stream.

### 2. IP Configuration
* Find the IP address of your C64U in the hardware settings.
* Enter this IP into the top-left corner of the C64U Slim-Viewer.
* *Recommendation:* Configure your router to assign a static IP address to your C64U.

### 3. Firewall Settings
* Ensure that **Port 11000 for UDP** is open in your PC's firewall. This port is required to receive the incoming data stream.

### 4. Configure the C64U Hardware
Now, you need to tell your Ultimate 64 where to send the video data:

**Step A: Configure Data Streams**
* In the C64U main menu, press **F3** (or SHIFT+F2).
* Navigate to `Data Streams` -> `Stream VIC to:`.
* Replace the existing IP address with your **Client-IP** (your PC's IP) and port **11000**.

> [!NOTE]
> *Insert Screenshot of F3 Menu here*

**Step B: Start the Stream**
* In the C64U main menu, press **F1**.
* Navigate to `Streams` -> `VIC Streams` -> `Send to [Client-IP]:11000`.
* Press **Enter**.

> [!NOTE]
> *Insert Screenshot of F1 Menu here*

### 5. Final Step
* Click the **'Start Stream'** button within the C64U Slim-Viewer application.

---

## 📦 Installation

### Linux (Debian/Ubuntu/Mint)
1. Download the latest `.deb` package from the [Releases](https://github.com/gruetze-software/C64UViewer/releases) page.
2. Install via double click or terminal: `sudo dpkg -i c64uviewer_*.deb`

### Windows
1. Download the `C64UViewer_Windows.zip` from [Releases](https://github.com/gruetze-software/C64UViewer/releases).
2. Extract and run `C64UViewer.exe`.

---

## 🛠️ Build from Source
1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Clone: `git clone https://github.com/gruetze-software/C64UViewer.git`
3. Build: `dotnet build -c Release`

---

## 📄 License
This project is licensed under the **MIT License**.

## ❤️ Credits
Developed by **Grütze-Software**. Special thanks to the C64 community and Gideon's Logic.
