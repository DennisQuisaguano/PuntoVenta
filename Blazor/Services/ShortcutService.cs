using System;

namespace Blazor.Services;

public class ShortcutService
{
    public event Action<string>? OnShortcutPressed;

    public void NotifyShortcut(string key)
    {
        OnShortcutPressed?.Invoke(key);
    }
}
