using System;
using System.IO;
using System.Reflection;

namespace Noah;

public static class AppInfo
{
    public static string AssemblyName =>
        Assembly.GetExecutingAssembly().GetName().Name ?? "Noah";

    public static string DataPath
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AssemblyName);
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string DeviceId
    {
        get
        {
            var path = Path.Combine(DataPath, "device_id.txt");
            if (File.Exists(path))
                return File.ReadAllText(path).Trim();

            var id = Guid.NewGuid().ToString();
            File.WriteAllText(path, id);
            try { File.SetAttributes(path, FileAttributes.ReadOnly | FileAttributes.Hidden); }
            catch { /* ignore on failure */ }
            return id;
        }
    }

    public static string DbPath => Path.Combine(DataPath, "system.db");
    public static string LogPath => Path.Combine(DataPath, "logs");
    public static string DefaultSaveFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NOAH");

    public const string DefaultServerUrl = "https://glady-nonferrous-nonsimilarly.ngrok-free.dev";
    public const string AppVersion = "0.1.0";
}
