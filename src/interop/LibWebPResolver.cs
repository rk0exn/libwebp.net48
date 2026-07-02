#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Libwebp.Net.Interop
{
    /// <summary>
    /// Preloads the native libwebp library for .NET Framework, which does not expose
    /// NativeLibrary.SetDllImportResolver. The build copies native libraries next to
    /// the assembly for normal use; this resolver also supports local source-tree
    /// codecs/{platform}/ folders during development and tests.
    /// </summary>
    internal static class LibWebPResolver
    {
        private static bool _registered;
        private static readonly object _lock = new object();
        private static nint _handle;

        public static void EnsureRegistered()
        {
            if (_registered) return;
            lock (_lock)
            {
                if (_registered) return;
                _handle = TryLoadLibWebP();
                _registered = true;
            }
        }

        private static nint TryLoadLibWebP()
        {
            string fileName;
            string codecsSubfolder;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName = "libwebp.dll";
                codecsSubfolder = "win";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                fileName = "libwebp.so";
                codecsSubfolder = "linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                fileName = "libwebp.dylib";
                codecsSubfolder = "osx";
            }
            else
            {
                return 0;
            }

            string assemblyDir = Path.GetDirectoryName(typeof(LibWebPNative).Assembly.Location) ?? ".";
            nint handle = LoadLibraryFromPath(Path.Combine(assemblyDir, fileName));
            if (handle != 0) return handle;

            string? dir = assemblyDir;
            for (int i = 0; i < 8 && dir != null; i++)
            {
                handle = LoadLibraryFromPath(Path.Combine(dir, "codecs", codecsSubfolder, fileName));
                if (handle != 0) return handle;
                dir = Path.GetDirectoryName(dir);
            }

            return 0;
        }

        private static nint LoadLibraryFromPath(string path)
        {
            if (!File.Exists(path)) return 0;
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? LoadLibrary(path)
                : dlopen(path, RTLD_NOW | RTLD_GLOBAL);
        }

        private const int RTLD_NOW = 2;
        private const int RTLD_GLOBAL = 0x100;

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern nint LoadLibrary(string lpFileName);

        [DllImport("libdl", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern nint dlopen(string fileName, int flags);
    }
}
