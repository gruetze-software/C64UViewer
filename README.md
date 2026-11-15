# C64U Slim-Viewer

[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

Watch live VIC video and listen to audio from your Commodore 64 Ultimate or Ultimate 64 over your local network. C64U Slim-Viewer runs on Windows, Linux and Apple Silicon Macs and is built with .NET 10 and Avalonia.

![C64U Slim-Viewer displaying a live C64 stream](https://github.com/user-attachments/assets/28e98855-bf88-4be1-96b0-f32d154cf99f)

## Features

- **Live video and optional audio.** The viewer can receive both streams simultaneously on separate UDP ports. You start the streams on the C64U.
- **Adjustable CRT effects.** Turn scanlines and screen glow on independently and set their strength in the **CRT** dialog. **Apply** saves your settings.
- **Native Wayland on Linux.** The viewer tries Wayland first and falls back to X11. Set `C64UVIEWER_LINUX_BACKEND=x11` or `C64UVIEWER_LINUX_BACKEND=wayland` to select a backend explicitly.
- **Release notifications.** The viewer checks GitHub for updates.

## Installation

Download the package for your platform from [GitHub Releases](https://github.com/gruetze-software/C64UViewer/releases).

| Platform | Installation |
| --- | --- |
| Windows x64 | Extract the ZIP and run `c64uviewer.exe`. Keep the included `SDL2.dll` next to the EXE for audio. |
| Debian, Ubuntu or Linux Mint (x86-64) | Install the DEB with `sudo apt install ./c64uviewer_VERSION_amd64.deb`. Replace `VERSION` with the version in the downloaded filename. |
| macOS (Apple Silicon) | Extract the ZIP and open `C64UViewer.app`. For audio, install SDL2 with `brew install sdl2`. |

The release packages include the .NET runtime.

## Set up video and audio

Connect the C64U and the computer running the viewer to the same local network. Use wired Ethernet on the C64U for streaming. The viewer displays the destination address next to **PC IP**.

| Stream | Default destination port | Viewer setting |
| --- | --- | --- |
| VIC video | UDP `11000` | **Video Port** |
| Audio | UDP `11001` | Enable **Audio** and check **Port** next to it |

1. On the C64U, open **Data Streams** in the configuration menu. Set **Stream VIC to** to the viewer's IP address and video port, for example `192.168.178.50:11000`.
2. For sound, set the audio stream destination to the same IP address and the audio port, for example `192.168.178.50:11001`.
3. Open the C64U's **Streams** menu and start the **VIC** stream. Start the **Audio** stream there as well if you want sound. Menu labels and shortcuts can vary by firmware.
4. With both streams active, the viewer shows **STREAMING ACTIVE (VIDEO + AUDIO)**. Play a game or demo with sound to check audio output.

If video works and audio is silent, check that the **Audio** stream is running on the C64U and that its destination matches the viewer's audio port. If a firewall blocks incoming traffic, allow the viewer on your private network in Windows or allow the relevant UDP ports. With UFW, for example:

```sh
sudo ufw allow 11000/udp
sudo ufw allow 11001/udp
```

### CRT effects

Open **CRT** while video is running. **Scanlines** add dark horizontal lines. **Screen glow** softens bright edges. Set the strength for each effect and select **Apply** to save your choices for the next launch.

## Build from source

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run:

```sh
git clone https://github.com/gruetze-software/C64UViewer.git
cd C64UViewer
dotnet build -c Release
```

## Credits and license

Developed by **Grütze-Software** and released under the [MIT License](LICENSE). Thanks to Gideon's Logic for the Ultimate 64 hardware and to [Perifractic (Retro Recipes)](https://www.youtube.com/@RetroRecipes) for inspiring projects that bring the C64 into the modern era.

[Support development on Ko-fi](https://ko-fi.com/gruetzesoftware)
