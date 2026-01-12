; VapourSynth Studio Installer Script for Inno Setup
; https://jrsoftware.org/isinfo.php

#define MyAppName "VapourSynth Studio"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "VapourSynth Studio"
#define MyAppURL "https://github.com/Atomic-Trash/vapoursynth-gui"
#define MyAppExeName "VapourSynthPortable.exe"

[Setup]
; Application identity
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases

; Installation settings
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir=..\dist\installer
OutputBaseFilename=VapourSynthStudio-{#MyAppVersion}-Setup
SetupIconFile=..\src\gui\VapourSynthPortable\Resources\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Privileges
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Windows version requirement
MinVersion=10.0.17763

; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main application files (from self-contained publish)
Source: "..\src\gui\VapourSynthPortable\bin\publish\win-x64-self-contained\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; VapourSynth distribution (if bundled)
Source: "..\dist\vapoursynth\*"; DestDir: "{app}\vapoursynth"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: DirExists(ExpandConstant('..\dist\vapoursynth'))
Source: "..\dist\python\*"; DestDir: "{app}\python"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: DirExists(ExpandConstant('..\dist\python'))
Source: "..\dist\plugins\*"; DestDir: "{app}\plugins"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: DirExists(ExpandConstant('..\dist\plugins'))

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up user data on uninstall (optional - disabled by default)
; Type: filesandordirs; Name: "{userappdata}\VapourSynthStudio"

[Code]
function DirExists(const DirName: String): Boolean;
begin
  Result := DirExists(ExpandConstant(DirName));
end;
