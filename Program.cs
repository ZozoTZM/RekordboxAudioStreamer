using System;
using AudioToolbox;
using CoreAudio;
using Foundation;
using System.Runtime.InteropServices;

class Program
{
    static void Main()
    {
        Console.WriteLine("Starting Audio Capture from BlackHole...");

        // Set up the audio session
        SetupAudioSession();

        // Capture audio from BlackHole and route it to the system
        CaptureAudioFromBlackHole();

        // Play the captured audio (or simply route it back out to the system or BlackHole)
        PlayCapturedAudio();
    }

    static void SetupAudioSession()
    {
        // Configure your audio session for playback and recording
        var audioSession = AVAudioSession.SharedInstance();
        audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord);
        audioSession.SetActive(true);
    }

    static void CaptureAudioFromBlackHole()
    {
        // Setup AudioUnit to capture from BlackHole device
        var audioComponentDescription = new AudioComponentDescription
        {
            ComponentType = AudioComponentType.Output,
            ComponentSubType = AudioComponentSubType.GenericOutput,
            ComponentManufacturer = AudioComponentManufacturerType.Apple
        };

        // Find BlackHole device (we will filter devices to find BlackHole)
        AudioComponent component = AudioComponent.FindNextComponent(ref audioComponentDescription);
        if (component == null)
        {
            Console.WriteLine("Error: Unable to find audio component for BlackHole.");
            return;
        }

        // Create the AudioUnit
        var unit = AudioUnit.Create(component);
        if (unit == null)
        {
            Console.WriteLine("Error: Unable to create AudioUnit.");
            return;
        }

        // Get the BlackHole input device (use actual device querying here)
        AudioDeviceID deviceID = GetBlackHoleInputDevice();
        unit.SetAudioInputDevice(deviceID);

        // Start capturing audio from BlackHole
        unit.Start();
        Console.WriteLine("Capturing audio from BlackHole...");
    }

    static void PlayCapturedAudio()
    {
        // Set up playback on the system's output or BlackHole
        var audioSession = AVAudioSession.SharedInstance();
        audioSession.SetCategory(AVAudioSessionCategoryPlayback);
        audioSession.SetActive(true);

        // Play captured audio through the system output or BlackHole
        Console.WriteLine("Playing captured audio. Press Enter to stop.");
        Console.ReadLine();
    }

    static AudioDeviceID GetBlackHoleInputDevice()
    {
        // Enumerate devices and find the correct BlackHole device
        AudioDeviceID[] deviceIDs = GetAudioDeviceIDs();
        foreach (var deviceID in deviceIDs)
        {
            string deviceName = GetDeviceName(deviceID);
            if (deviceName.Contains("BlackHole"))
            {
                Console.WriteLine($"Found BlackHole device: {deviceName}");
                return deviceID;
            }
        }
        throw new Exception("BlackHole device not found.");
    }

    static AudioDeviceID[] GetAudioDeviceIDs()
    {
        // Get the available audio devices
        uint deviceCount = 0;
        var err = AudioHardwareService.GetAudioDevicesCount(out deviceCount);
        if (err != 0)
        {
            Console.WriteLine("Error: Unable to get device count.");
            return Array.Empty<AudioDeviceID>();
        }

        var devices = new AudioDeviceID[deviceCount];
        err = AudioHardwareService.GetAudioDevices(devices);
        if (err != 0)
        {
            Console.WriteLine("Error: Unable to retrieve audio devices.");
            return Array.Empty<AudioDeviceID>();
        }

        return devices;
    }

    static string GetDeviceName(AudioDeviceID deviceID)
    {
        // Get the device name from AudioDeviceID
        uint size = 0;
        AudioHardwareService.GetDeviceName(deviceID, ref size, IntPtr.Zero);

        IntPtr namePtr = Marshal.AllocHGlobal((int)size);
        try
        {
            AudioHardwareService.GetDeviceName(deviceID, ref size, namePtr);
            return Marshal.PtrToStringAnsi(namePtr);
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }
}
