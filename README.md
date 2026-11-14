# C64U Slim-Viewer
[![Platform: Linux | Windows](https://img.shields.io/badge/platform-Linux%20%7C%20Windows-blue)](#)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](#)

A lightweight video and audio stream viewer for the Ultimate 64 (U64) and C64 Ultimate. Built with .NET 10 and Avalonia UI.

On Linux, the viewer tries native Wayland and falls back to X11. Set
`C64UVIEWER_LINUX_BACKEND=x11` or `C64UVIEWER_LINUX_BACKEND=wayland`
to select a specific backend.

### Build local Flatpak and Snap packages

Run these commands from the repository root on Linux x86-64 with the .NET 10 SDK.

```sh
bash packaging/prepare-linux.sh

# Flatpak (requires flatpak-builder and the Flathub remote).
flatpak-builder --user --install --install-deps-from=flathub --force-clean artifacts/flatpak-build flatpak/de.gruetze_software.C64UViewer.yaml
flatpak run de.gruetze_software.C64UViewer

# Snap (requires Snapcraft).
snapcraft
sudo snap install --dangerous ./c64uviewer_1.3.0_amd64.snap
```

The Flatpak manifest consumes a local publish directory. A Flathub submission
still needs pinned build sources and offline NuGet inputs. During testing, verify
video on UDP 11000 and audio on UDP 11001 on both a Wayland and an X11 session.

<img width="864" height="682" alt="grafik" src="https://github.com/user-attachments/assets/28e98855-bf88-4be1-96b0-f32d154cf99f" />


## 📺 Overview
The **C64U Slim-Viewer** allows you to stream the VIC-II video output of your Ultimate 64 directly to your modern linux or windows desktop via your local network.

### Key Features
* 🚀 **Low Latency:** The tool operates as a passive UDP listener. It no longer sends active control commands to the hardware, minimizing network overhead and avoiding conflicts with other tools. You control the stream directly from your C64. As soon as the hardware starts sending data, the viewer displays the frame.
* 🪶 **Minimalist Design:** A distraction-free window perfect for coding or gaming.
* 🔄 **Auto-Update Check:** In-app notifications for new GitHub releases.
* 🐧 **Cross-Platform:** Native support for Linux (.deb) and Windows.

### 🕹️ Supported Hardware
* **Ultimate 64** / **Ultimate 64 Elite** (Full support)
* **Ultimate-II+** (Standard version with Ethernet)
* ⚠️ **Note:** The **Ultimate-II+L** (L-version) is **NOT supported**, as it lacks the video streaming capabilities in its hardware/firmware.
  

### ☕ Support
This software is a passion project by a private developer. If you find it useful, your support is greatly appreciated!

**[➡️ Buy me a coffee on Ko-fi](https://ko-fi.com/gruetzesoftware)**

---

## 🔌 Setup Instructions
Follow these steps to establish a connection between your C64U and this viewer.

### 1. Network Requirements
* Ensure your **Commodore 64 Ultimate (C64U)** is on the same local network as this PC.
* **Important:** Your C64U must be connected via **LAN**. Disable WLAN on the C64U to ensure a stable stream.

### 2. Firewall Settings
* Ensure that **Port 11000 for UDP** is open in your PC's firewall. This port is required to receive the incoming data stream.
* **Windows:** You usually don't need to do anything. Windows Firewall will automatically ask for permission on the first startup. Ensure you allow access.
* <img width="400" height="400" alt="grafik" src="https://github.com/user-attachments/assets/86ad588c-1366-42ca-b1be-bd057872ba39" />
* **Linux:** Most distributions (like Ubuntu, Mint, Debian) don't block this by default. However, if you have a strict firewall enabled (like `ufw`), you must manually allow UDP traffic:
    ```bash
    sudo ufw allow 11000/udp
    ```
### 3. Configure the C64U Hardware
Now, you need to tell your Ultimate 64 where to send the video data:

**Step A: Configure Data Streams**
* In the C64U main menu, press **F2** (SHIFT+F1).
* Navigate to `Data Streams` -> `Stream VIC to:`.
* Replace the existing IP address with your **Client-IP** (your PC's IP) and port **11000**.

<img width="600" height="372" alt="grafik" src="https://github.com/user-attachments/assets/c8241b3a-8497-4ccb-9e07-38edec35a359" />
<img width="600" height="387" alt="grafik" src="https://github.com/user-attachments/assets/5325ef9b-976e-4030-af0b-9e5c21d1a85a" />

**Step B: Start the Stream**
* In the C64U main menu, press **F1**.
* Navigate to `Streams` -> `VIC Streams` -> `Send to [Client-IP]:11000`.
* Press **Enter**.
<img width="600" height="378" alt="grafik" src="https://github.com/user-attachments/assets/06b160c0-1922-40c4-be4b-4f82b7a9f360" />


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
* Developed by **Grütze-Software**. 
* **Special Thanks to [Perifractic (Retro Recipes)](https://www.youtube.com/@RetroRecipes):** For his incredible passion, for bringing the C64 into the modern era, and for inspiring projects that bridge the gap between retro and contemporary computing.
* **Gideon's Logic:** For creating the amazing Ultimate 64 hardware.

## 📜 Disclaimer
C64U Slim-Viewer is provided "as is," without warranty of any kind, express or implied. The author is not liable for any damages or losses arising from the use or inability to use the software.
This software is a hobby project developed in the author's spare time.
It is not a commercial product and comes with no official support.
Use it at your own risk.
