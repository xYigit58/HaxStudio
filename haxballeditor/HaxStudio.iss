#define MyAppName "HaxStudio"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Yiğit Hasan Çıtak"
#define MyAppExeName "HaxStudio.exe"

[Setup]
AppId={{74D64A37-67D7-4B2F-9445-HAXSTUDIO0001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\HaxStudio
DefaultGroupName=HaxStudio
DisableProgramGroupPage=yes
OutputDir=dist\installer
OutputBaseFilename=HaxStudio-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\HaxStudio"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\HaxStudio"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch HaxStudio"; Flags: nowait postinstall skipifsilent
