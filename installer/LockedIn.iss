; Inno Setup script for locked-in. Built by .github/workflows/release.yml:
;   dotnet publish src/LockedIn.App -c Release -r win-x64 --self-contained -o artifacts/publish
;   ISCC.exe /DAppVersion=1.2.3 installer\LockedIn.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#define AppName "locked-in"
#define AppExe "LockedIn.exe"
#define RunKey "Software\Microsoft\Windows\CurrentVersion\Run"

[Setup]
AppId={{02CD6BDA-6825-4CA9-B1F7-9370383DB5CB}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Asan
AppPublisherURL=https://github.com/asanaliov/locked-in
AppSupportURL=https://github.com/asanaliov/locked-in/issues
; Per-user install: no admin prompt, and the app can update its own settings file.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Matches the single-instance mutex in SingleInstance.cs, so setup asks to close a running copy.
AppMutex=LockedIn.App
LicenseFile=..\LICENSE
SetupIconFile=..\src\LockedIn.App\lockedin.ico
UninstallDisplayIcon={app}\{#AppExe}
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
OutputDir=..\artifacts
OutputBaseFilename=locked-in-setup-{#AppVersion}

[Tasks]
Name: "startup"; Description: "Start locked-in when I sign in to Windows"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Same value the tray menu's "Start with Windows" writes (StartupRegistration.cs).
Root: HKCU; Subkey: "{#RunKey}"; ValueType: string; ValueName: "LockedIn"; ValueData: """{app}\{#AppExe}"" --tray"; Tasks: startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// The Run value may also have been set from the tray menu, so always remove it on uninstall.
// Tracking data in %LOCALAPPDATA%\LockedIn is kept.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RegDeleteValue(HKEY_CURRENT_USER, '{#RunKey}', 'LockedIn');
end;
