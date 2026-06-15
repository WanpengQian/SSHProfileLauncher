using System.Runtime.InteropServices;

namespace SSHProfileLauncher.Interop;

/// <summary>
/// Best-effort: make English (US) the active input language right before launching
/// the terminal, so the newly created process inherits English input — the same way
/// Remote Desktop (mstsc) inherits whatever input method is active at launch time.
/// </summary>
internal static class Ime
{
    [DllImport("user32.dll")]
    private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetContext(IntPtr hWnd);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmSetOpenStatus(IntPtr hIMC, [MarshalAs(UnmanagedType.Bool)] bool fOpen);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

    private const uint KLF_ACTIVATE = 0x00000001;
    private const string EnglishUs = "00000409";

    /// <summary>
    /// Activate the English (US) keyboard layout (what a launched process inherits)
    /// and close any open IME on the given window as a fallback.
    /// </summary>
    public static void SwitchToEnglish(IntPtr hWnd)
    {
        var hkl = LoadKeyboardLayout(EnglishUs, KLF_ACTIVATE);
        if (hkl != IntPtr.Zero)
            ActivateKeyboardLayout(hkl, KLF_ACTIVATE);

        if (hWnd == IntPtr.Zero) return;

        var himc = ImmGetContext(hWnd);
        if (himc == IntPtr.Zero) return;
        try
        {
            ImmSetOpenStatus(himc, false);
        }
        finally
        {
            ImmReleaseContext(hWnd, himc);
        }
    }
}
