# Botón Micrófono

Botón flotante para Windows que reenvía una entrada de micrófono hacia una salida seleccionada.

## Uso

- Primer inicio: selecciona el micrófono y la salida.
- Clic izquierdo: mutear o desmutear el envío.
- Arrastrar: mover el botón.
- Clic derecho: configurar, cambiar «siempre visible» o salir.
- En el menú también puedes activar «Iniciar con Windows» y consultar la autoría en «Acerca de».
- «Volumen y ganancia» controla únicamente el audio reenviado: 0–100 % regula la salida y 101–200 % añade hasta +20 dB de ganancia con limitación suave.
- Al transmitir, las barras del botón muestran el nivel de audio en escala logarítmica.
- La reducción automática aplica un fundido de bajada de 600 ms y uno de recuperación de 900 ms a otras aplicaciones que estaban al 100 %, sin tocar el volumen maestro.
- «Procesamiento de voz» incluye compresor, limitador de picos y ecualizador de tres bandas, todos configurables.
- «Reducción de ruido» combina un filtro pasa-altos de 80 Hz y un expansor suave con umbral e intensidad configurables.
- «Tema y tamaño» permite escoger entre cuatro colores y cuatro tamaños del botón.
- Rojo: muteado. Verde: transmitiendo.

La aplicación no cambia el mute global de Windows; solo controla su propio canal de reenvío.

## Compilación

```powershell
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

El ejecutable queda en `bin\Release\net8.0-windows\win-x64\publish\BotonMicrofono.exe`.

## Instalador

El archivo `installer.iss` se compila con Inno Setup 6 y crea un instalador normal de Windows,
con desinstalador, entrada en el menú Inicio y acceso directo opcional en el escritorio.

© 2026 LY. Todos los derechos reservados.
