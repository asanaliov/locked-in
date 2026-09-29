; Inno Setup script for Locked In. Built by .github/workflows/release.yml:
;   dotnet publish src/LockedIn.App -c Release -r win-x64 --self-contained -o artifacts/publish
;   ISCC.exe /DAppVersion=1.2.3 installer\LockedIn.iss
;
; The app updates itself by running this installer with /VERYSILENT /SUPPRESSMSGBOXES /UPDATE=1:
; setup then waits for the running app to exit and starts the new version when it's done.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#define AppName "Locked In"
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
LicenseFile=..\LICENSE
SetupIconFile=..\src\LockedIn.App\lockedin.ico
UninstallDisplayIcon={app}\{#AppExe}
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
OutputDir=..\artifacts
OutputBaseFilename=locked-in-setup-{#AppVersion}

[Tasks]
Name: "startup"; Description: "Start Locked In when I sign in to Windows"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
; Shortcuts from versions before the app was named "Locked In". Existing installs keep their folder.
Type: files; Name: "{autoprograms}\locked-in.lnk"
Type: files; Name: "{autodesktop}\locked-in.lnk"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Same value the tray menu's "Start with Windows" writes (StartupRegistration.cs).
Root: HKCU; Subkey: "{#RunKey}"; ValueType: string; ValueName: "LockedIn"; ValueData: """{app}\{#AppExe}"" --tray"; Tasks: startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\{#AppExe}"; Flags: nowait; Check: IsUpdate

[Code]
// Matches the single-instance mutex in SingleInstance.cs.
const
  RunningMutex = 'LockedIn.App';

function IsUpdate: Boolean;
begin
  Result := ExpandConstant('{param:update|0}') = '1';
end;

// Asks the user to exit Locked In from its tray icon until they do or cancel.
function AppClosedByUser: Boolean;
begin
  Result := True;
  while CheckForMutexes(RunningMutex) do
    if SuppressibleMsgBox('Locked In is running. Exit it from its tray icon menu, then click Retry.',
      mbError, MB_RETRYCANCEL, IDCANCEL) = IDCANCEL then
    begin
      Result := False;
      Exit;
    end;
end;

// An update is started by the app itself just before it exits, so give it up to 30 seconds.
function AppExitedWithin(Seconds: Integer): Boolean;
var
  Waited: Integer;
begin
  Waited := 0;
  while CheckForMutexes(RunningMutex) and (Waited < Seconds * 1000) do
  begin
    Sleep(250);
    Waited := Waited + 250;
  end;
  Result := not CheckForMutexes(RunningMutex);
end;

function InitializeSetup: Boolean;
begin
  if IsUpdate then
    Result := AppExitedWithin(30)
  else
    Result := AppClosedByUser;
end;

function InitializeUninstall: Boolean;
begin
  Result := AppClosedByUser;
end;

// The Run value may also have been set from the tray menu, so always remove it on uninstall.
// Tracking data in %LOCALAPPDATA%\LockedIn is kept.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RegDeleteValue(HKEY_CURRENT_USER, '{#RunKey}', 'LockedIn');
end;
