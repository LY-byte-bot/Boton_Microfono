Unicode True
!include "MUI2.nsh"

!define APPNAME "Botón Micrófono"
!define COMPANY "LY"
!define VERSION "1.1.0"
!define EXE "BotonMicrofono.exe"

Name "${APPNAME}"
OutFile "Instalador\Instalar-Boton-Microfono.exe"
InstallDir "$LOCALAPPDATA\Programs\Boton Microfono"
RequestExecutionLevel user
SetCompressor /SOLID lzma
BrandingText "LY derechos reservados"

VIProductVersion "1.1.0.0"
VIAddVersionKey "ProductName" "${APPNAME}"
VIAddVersionKey "CompanyName" "${COMPANY}"
VIAddVersionKey "FileDescription" "Instalador de ${APPNAME}"
VIAddVersionKey "FileVersion" "${VERSION}"
VIAddVersionKey "LegalCopyright" "© 2026 LY. Todos los derechos reservados."

!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\${EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Abrir ${APPNAME}"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Spanish"

Section "Programa" SecMain
  SetOutPath "$INSTDIR"
  File "/oname=${EXE}" "Aplicacion\BotonMicrofono.exe"
  WriteUninstaller "$INSTDIR\Desinstalar.exe"

  CreateDirectory "$SMPROGRAMS\${APPNAME}"
  CreateShortcut "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${EXE}"
  CreateShortcut "$SMPROGRAMS\${APPNAME}\Desinstalar.lnk" "$INSTDIR\Desinstalar.exe"
  CreateShortcut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\${EXE}"

  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY" "DisplayName" "${APPNAME}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY" "Publisher" "${COMPANY}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY" "DisplayIcon" "$INSTDIR\${EXE}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY" "UninstallString" '"$INSTDIR\Desinstalar.exe"'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY" "NoRepair" 1
SectionEnd

Section "Uninstall"
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "BotonMicrofonoLY"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY"
  Delete "$DESKTOP\${APPNAME}.lnk"
  RMDir /r "$SMPROGRAMS\${APPNAME}"
  Delete "$INSTDIR\${EXE}"
  Delete "$INSTDIR\Desinstalar.exe"
  RMDir "$INSTDIR"
SectionEnd
