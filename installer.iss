#define MyAppName "Bible Study App"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Waitaminute Digital"
#define MyAppExeName "LumenScriptura.exe"

#ifndef TargetArch
  #define TargetArch "win-arm64"
#endif

#ifndef PublishDir
  #ifdef TargetPublishDir
    #define PublishDir TargetPublishDir
  #else
    #define PublishDir "artifacts\" + TargetArch
  #endif
#endif

#ifndef OutputBaseFilename
  #ifdef OutputFileName
    #define OutputBaseFilename OutputFileName
  #elif TargetArch == "win-arm64" || TargetArch == "arm64"
    #define OutputBaseFilename "BibleStudyApp-Setup-Wizard-arm64"
  #elif TargetArch == "win-x64" || TargetArch == "x64"
    #define OutputBaseFilename "BibleStudyApp-Setup-Wizard-x64"
  #else
    #define OutputBaseFilename "BibleStudyApp-Setup-Wizard-" + TargetArch
  #endif
#endif

[Setup]
AppId={{A9E8D12F-4C5A-4B9D-8E12-7F3A910B8C7D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright (C) 2026 {#MyAppPublisher}
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=./artifacts/installer
OutputBaseFilename={#OutputBaseFilename}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64 arm64
ArchitecturesInstallIn64BitMode=x64 arm64

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
