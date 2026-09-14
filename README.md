# BorderlessApp

A tiny Windows 11 tray app that makes the active window borderless fullscreen with a hotkey.

## Use

1. Run `BorderlessApp.exe`. It lives in the system tray (you may need to drag it out of the `^` overflow).
2. Focus a windowed game and press **Ctrl+Alt+Enter**. The window loses its borders and covers the monitor it's on.
3. Press the hotkey again to put the window back exactly how it was.

Tray menu (right-click the icon):

- **Restore all windows**: undo every window the app has changed. This also happens automatically on Exit.
- **Edit settings...**: opens `%APPDATA%\BorderlessApp\settings.json`. Change the hotkey (e.g. `"Win+Shift+F11"`, `"Ctrl+Alt+B"`), save, then click **Reload settings**.
- **Start with Windows**: toggles launching at sign-in.

## Build

Requires the .NET 10 SDK.

```powershell
# Run from source
dotnet run -c Release

# Single ~300 KB exe in .\publish (needs the .NET 10 Desktop Runtime on the machine)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

## Tips

- **Set the game to windowed mode** at your monitor's native resolution first. The app only resizes the window; the game decides what resolution it renders at.
- **Games running as administrator** (some launchers do this) can't be changed by a non-admin app. Windows blocks it, and the app shows a notification. Run BorderlessApp as administrator in that case.
- **Games that redraw their own borders** after going borderless: press the hotkey again after the game has finished loading.
- **Blurry game at high display scaling**: right-click the game exe → Properties → Compatibility → Change high DPI settings → tick *Override high DPI scaling behavior* → *Application*.
