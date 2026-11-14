using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL2;
using static SDL2.SDL;

namespace C64UViewer.Services;

public class CrossPlatformAudioPlayer : IDisposable
{
    private uint _deviceId;
    private SDL_AudioSpec _wantedSpec;
    private bool _isInitialized = false;

    public void Init()
    {
        if (_isInitialized) return;

        // SDL2 initialisieren
        if (SDL_InitSubSystem(SDL_INIT_AUDIO) < 0)
        {
            throw new InvalidOperationException($"SDL audio initialization failed: {SDL_GetError()}");
        }

        // Ultimate 64 Audio-Spezifikation: 48kHz, 16-Bit Signed, Stereo
        _wantedSpec = new SDL_AudioSpec
        {
            freq = 48000,
            format = AUDIO_S16SYS, // System-Native 16-bit
            channels = 2,
            samples = 1024 // Kleiner Puffer für geringe Latenz
        };

        // Audiogerät öffnen (null = Standardgerät)
        _deviceId = SDL_OpenAudioDevice(IntPtr.Zero, 0, ref _wantedSpec, out _, 0);
        
        if (_deviceId == 0)
        {
            var error = SDL_GetError();
            SDL_QuitSubSystem(SDL_INIT_AUDIO);
            throw new InvalidOperationException($"SDL could not open the default playback device: {error}");
        }

        // Wiedergabe entpausieren (SDL startet standardmäßig pausiert)
        SDL_PauseAudioDevice(_deviceId, 0);
        _isInitialized = true;
    }

    public void EnqueueSamples(ReadOnlySpan<byte> pcmData)
    {
        if (!_isInitialized || _deviceId == 0 || pcmData.Length == 0) return;

        // 1. Jitter-Schutz: Wenn der Puffer zu voll ist, leeren wir ihn (ca. 0.5s Puffer)
        uint currentQueuedSize = SDL_GetQueuedAudioSize(_deviceId);
        if (currentQueuedSize > 48000 * 2 * 2 * 0.5) 
        {
            SDL_ClearQueuedAudio(_deviceId);
        }

        // 2. Der Fix für den "nint" Fehler:
        // Wir "pinnen" das Byte-Array im Speicher, damit der Garbage Collector es nicht verschiebt,
        // während SDL darauf zugreift.
        unsafe
        {
            fixed (byte* p = pcmData)
            {
                // Wir casten den Pointer auf (IntPtr), was unter .NET 8/9 als 'nint' interpretiert wird
                SDL_QueueAudio(_deviceId, (IntPtr)p, (uint)pcmData.Length);
            }
        }
    }

    public void ClearBuffer()
    {
        if (_isInitialized && _deviceId != 0)
            SDL_ClearQueuedAudio(_deviceId);
    }

    public void Dispose()
    {
        if (_isInitialized)
        {
            SDL_CloseAudioDevice(_deviceId);
            SDL_QuitSubSystem(SDL_INIT_AUDIO);
            _isInitialized = false;
        }
    }
}