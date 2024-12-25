set windows-shell := ["pwsh", "-NoProfile", "-Command"]
build:
    dotnet build -c Release -o $Env:AppData\FlowLauncher\Plugins\Flow.Launcher.Plugin.Cider2