#define MyAppName "YapıLab Arazi Oluşturucu"
#define MyAppVersion "1.1.1"
#define MyAppPublisher "YapıLab"

[Setup]
AppId={{8A80F770-7948-4D70-89D5-9E6F4B79D6A1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={userappdata}\Autodesk\ApplicationPlugins\YapiLabCadTools.bundle
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=AraziOlusturucu-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
Uninstallable=yes
CreateUninstallRegKey=yes
SetupLogging=yes
ArchitecturesAllowed=x64compatible
CloseApplications=no

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Files]
Source: "payload\YapiLabCadTools.bundle\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Code]
function IsAutoCADInstalled(): Boolean;
begin
  Result :=
    DirExists(ExpandConstant('{pf}\Autodesk\AutoCAD 2025')) or
    DirExists(ExpandConstant('{pf}\Autodesk\AutoCAD 2026')) or
    DirExists(ExpandConstant('{pf}\Autodesk\AutoCAD 2027'));
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsAutoCADInstalled() then
    MsgBox(
      'AutoCAD 2025, 2026 veya 2027 bulunamadı.' + #13#10 + #13#10 +
      'Kurulum yine de devam edebilir; eklenti desteklenen bir AutoCAD sürümü kurulduğunda hazır olacaktır.',
      mbInformation, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    MsgBox(
      'Kurulum tamamlandı.' + #13#10 + #13#10 +
      'AutoCAD açıksa kapatıp yeniden açın ve komut satırına YL yazın.',
      mbInformation, MB_OK);
end;
