# Créditos

Modelos 3D, materiales, música instrumental lounge y sonidos originales generados para esta copia con asistencia de ChatGPT/Codex. No se incluyeron modelos de los paquetes de las imágenes de referencia ni recursos descargados de Kenney o tiendas de assets.

Guion y cifras: escenario 08 de Semana10_VR_AR_CASO_BANCO_ANDINO, material suministrado por el usuario. Operaciones, origen OR-08, umbral y tipo de cambio ilustrativos. Un ROS por sí solo no congela automáticamente fondos: aquí se representa una defensa con bloqueo simulado.

Google Cardboard / HelloCardboard: Google, Apache License 2.0. Unity, URP y otros paquetes: sus licencias respectivas. Se conservaron los avisos originales.

Equipo: completar nombres, NRC y roles antes de entregar.

Música generada mediante síntesis y composición procedural original; código reproducible en Tools/make_music.py (requiere Python y NumPy). Sin grabaciones ni melodías de terceros.

Tipografías: DejaVu Serif Bold y DejaVu Sans. Bitstream, Inc. / DejaVu contributors. Licencia Bitstream Vera y cambios DejaVu de dominio público. Avisos completos en Assets/Casino/Fonts/LICENSE-DejaVu.txt. Rótulos originales dibujados para este proyecto con esas fuentes.

V5: controles en DejaVu Sans Bold, bajo la misma licencia DejaVu incluida. Texturas de cartas y gráficos de interfaz originales de esta entrega. Interfaz nativa Unity UI.

## Escenario 08 · Narración

El antecedente histórico se basa en el [resumen público del Departamento de Justicia de Estados Unidos](https://www.justice.gov/usao-cdca/pr/north-korean-regime-backed-programmer-charged-conspiracy-conduct-multiple-cyberattacks). Banco Andino, OR-08 y las condiciones del detector son datos del ejercicio.

Unity reproduce los clips de `Assets/Casino/Resources/CasinoNarracion/` en un único `AudioSource`. Cuando falta un clip, el respaldo provisional usa una voz del dispositivo. Android requiere una voz de español descargada para uso sin conexión. La prueba en Windows usa una voz de español instalada en SAPI. Si el sistema no puede hablar, el tutorial sigue con los subtítulos. El texto de cada archivo y la dirección de locución están en el [guion de grabación](GUION_NARRACION_CASINO08.md).

Para incorporar una grabación humana se requieren los archivos y permiso para distribuirlos con el proyecto y el APK. Para producir audio con Azure Speech se necesita una suscripción y un recurso Speech de pago para aplicar los derechos de uso de las voces neurales predefinidas. `es-PE-CamilaNeural` es una opción en español peruano. Azure cobra la síntesis por carácter según región y oferta. Los clips incorporados se reproducen sin acceso a Azure desde Android. [Voces](https://learn.microsoft.com/azure/ai-services/speech-service/language-support?tabs=tts) · [Precios](https://azure.microsoft.com/en-us/pricing/details/speech/) · [Términos de uso](https://www.microsoft.com/licensing/terms/product/MicrosoftAzure/all).
