namespace Loupedeck.PCMonitorPlugin.Helpers
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;

    internal static class IconHelper
    {
        [DllImport("shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern Int32 ExtractIconEx(String file, Int32 iconIndex, out IntPtr iconLarge, out IntPtr iconSmall, Int32 icons);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern Boolean DestroyIcon(IntPtr hIcon);

        public static BitmapImage ExtractIconFromExecutable(String executablePath, Int32 size)
        {
            try
            {
                if (String.IsNullOrEmpty(executablePath) || !System.IO.File.Exists(executablePath))
                {
                    return null;
                }

                // Extract icon from executable
                var result = ExtractIconEx(executablePath, 0, out IntPtr largeIcon, out IntPtr smallIcon, 1);

                if (result > 0 && largeIcon != IntPtr.Zero)
                {
                    try
                    {
                        using (var icon = Icon.FromHandle(largeIcon))
                        {
                            using (var bitmap = icon.ToBitmap())
                            {
                                using (var resized = new Bitmap(bitmap, new Size(size, size)))
                                {
                                    // Convert Bitmap to BitmapImage using ImageConverter
                                    var converter = new ImageConverter();
                                    var imageBytes = (Byte[])converter.ConvertTo(resized, typeof(Byte[]));
                                    return BitmapImage.FromArray(imageBytes);
                                }
                            }
                        }
                    }
                    finally
                    {
                        // Clean up icon handles
                        if (largeIcon != IntPtr.Zero)
                        {
                            DestroyIcon(largeIcon);
                        }
                        if (smallIcon != IntPtr.Zero)
                        {
                            DestroyIcon(smallIcon);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Could not extract icon from {executablePath}: {ex.Message}");
            }

            return null;
        }
    }
}
