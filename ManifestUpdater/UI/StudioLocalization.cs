namespace ManifestUpdater;

internal static class StudioLocalization
{
	public static readonly IReadOnlyList<StudioLanguage> AvailableLanguages =
	[
		new("en-US", "English"),
		new("es-ES", "Español"),
		new("fr-FR", "Français"),
		new("de-DE", "Deutsch"),
		new("pt-BR", "Português (Brasil)"),
		new("ja-JP", "日本語")
	];

	private static readonly Dictionary<string, string> Spanish = new(StringComparer.Ordinal)
	{
		["Winget Manifest Studio"] = "Winget Manifest Studio",
		["Create, inspect, validate, and submit Windows Package Manager manifests."] = "Crea, inspecciona, valida y envía manifiestos del Administrador de paquetes de Windows.",
		["Start Here"] = "Comenzar",
		["Package Details"] = "Detalles del paquete",
		["Installers & Hashes"] = "Instaladores y hashes",
		["Preview & Submit"] = "Vista previa y envío",
		["Test Center"] = "Centro de pruebas",
		["Help & Guide"] = "Ayuda y guía",
		["Official Tool Commands"] = "Comandos oficiales",
		["1  Start"] = "1  Inicio",
		["2  Package"] = "2  Paquete",
		["3  Installers"] = "3  Instaladores",
		["4  Review"] = "4  Revisar",
		["5  Test Center"] = "5  Centro de pruebas",
		["Help"] = "Ayuda",
		["Official Tools"] = "Herramientas",
		["New Project"] = "Nuevo proyecto",
		["Load Manifests"] = "Cargar manifiestos",
		["Open Profile"] = "Abrir perfil",
		["Save Profile"] = "Guardar perfil",
		["Choose Output"] = "Elegir carpeta",
		["Suggest Package ID"] = "Sugerir ID",
		["Add Release Files"] = "Agregar archivos",
		["Add URL-Only Row"] = "Agregar solo URL",
		["Attach File to Selected"] = "Adjuntar archivo",
		["Inspect & Fill Details"] = "Inspeccionar y completar",
		["Inspect & Fill Selected"] = "Inspeccionar selección",
		["Inspect Local Files"] = "Inspeccionar locales",
		["Inspect All Local Files"] = "Inspeccionar todos los archivos",
		["Verify Public URLs"] = "Verificar URL públicas",
		["Remove"] = "Eliminar",
		["Remove Selected"] = "Eliminar selección",
		["Preview Changes"] = "Vista previa",
		["Save Manifests"] = "Guardar manifiestos",
		["Validate Locally"] = "Validar localmente",
		["Open Test Center"] = "Abrir centro de pruebas",
		["Submit to Winget"] = "Enviar a Winget",
		["Open Output Folder"] = "Abrir carpeta",
		["Technical Details"] = "Detalles técnicos",
		["Simple Review"] = "Revisión sencilla",
		["Run Safe Preflight"] = "Ejecutar revisión segura",
		["Inspect Signatures"] = "Inspeccionar firmas",
		["Find Existing Package"] = "Buscar paquete existente",
		["Export Test Report"] = "Exportar informe",
		["Enable Local Testing"] = "Habilitar prueba local",
		["Test Install Here"] = "Probar instalación aquí",
		["Verify Installed Result"] = "Verificar instalación",
		["Test in Windows Sandbox"] = "Probar en Windows Sandbox",
		["Enable & Test Install"] = "Habilitar y probar instalación",
		["Local Testing Enabled"] = "Prueba local habilitada",
		["Check Test Setup"] = "Comprobar configuración",
		["Optional: Test in Sandbox"] = "Opcional: probar en Sandbox",
		["Use Current Project"] = "Usar proyecto actual",
		["Install WingetCreate"] = "Instalar WingetCreate",
		["Run"] = "Ejecutar",
		["Show Optional Fields"] = "Mostrar campos opcionales",
		["Hide Optional Fields"] = "Ocultar campos opcionales",
		["PACKAGE WORKSPACE"] = "ÁREA DEL PAQUETE",
		["PACKAGE IDENTITY"] = "IDENTIDAD DEL PAQUETE",
		["PUBLIC PACKAGE INFORMATION"] = "INFORMACIÓN PÚBLICA",
		["PROJECT LINKS & RELEASE"] = "ENLACES Y VERSIÓN",
		["INSTALLER BEHAVIOR"] = "COMPORTAMIENTO DEL INSTALADOR",
		["INSTALLER SWITCHES"] = "PARÁMETROS DEL INSTALADOR",
		["AGREEMENTS & DOCUMENTATION"] = "ACUERDOS Y DOCUMENTACIÓN",
		["DEPENDENCIES & AVAILABILITY"] = "DEPENDENCIAS Y DISPONIBILIDAD",
		["RETURN CODES & INSTALL DETECTION"] = "CÓDIGOS DE RETORNO Y DETECCIÓN",
		["PRIVATE SOURCE AUTHENTICATION"] = "AUTENTICACIÓN DE FUENTE PRIVADA",
		["YAML ESCAPE HATCH"] = "OPCIONES YAML AVANZADAS",
		["ALL OTHER SCHEMA FIELDS"] = "OTROS CAMPOS DEL ESQUEMA",
		["INSTALLER DETAILS ARE AUTOMATIC"] = "LOS DETALLES SON AUTOMÁTICOS",
		["REVIEW BEFORE SAVING"] = "REVISAR ANTES DE GUARDAR",
		["TEST BEFORE SUBMITTING"] = "PROBAR ANTES DE ENVIAR",
		["INSTALLATION TESTS REQUIRE YOUR CONFIRMATION"] = "LAS PRUEBAS DE INSTALACIÓN REQUIEREN CONFIRMACIÓN",
		["FOLLOW THESE TESTS IN ORDER"] = "SIGUE ESTAS PRUEBAS EN ORDEN",
		["EXTRA CHECKS"] = "COMPROBACIONES ADICIONALES",
		["FOLLOW THESE INSTALLER STEPS"] = "SIGUE ESTOS PASOS DEL INSTALADOR",
		["HOW TO USE THIS SOFTWARE"] = "CÓMO USAR ESTE PROGRAMA",
		["COMMON PROBLEMS"] = "PROBLEMAS COMUNES",
		["Language"] = "Idioma",
		["English"] = "English",
		["Español"] = "Español",
		["Package identifier"] = "Identificador del paquete",
		["Package version"] = "Versión del paquete",
		["Default locale"] = "Idioma predeterminado",
		["Winget schema"] = "Esquema de Winget",
		["Manifest output folder"] = "Carpeta de manifiestos",
		["Package name"] = "Nombre del paquete",
		["Publisher"] = "Editor",
		["Author"] = "Autor",
		["License"] = "Licencia",
		["Short description"] = "Descripción breve",
		["Full description"] = "Descripción completa",
		["Tags"] = "Etiquetas",
		["Commands"] = "Comandos",
		["Release notes"] = "Notas de la versión",
		["Installation notes"] = "Notas de instalación",
		["Moniker"] = "Alias corto",
		["Publisher URL"] = "URL del editor",
		["Support URL"] = "URL de soporte",
		["Privacy URL"] = "URL de privacidad",
		["Package URL"] = "URL del paquete",
		["License URL"] = "URL de la licencia",
		["Copyright"] = "Derechos de autor",
		["Copyright URL"] = "URL de derechos de autor",
		["Purchase URL"] = "URL de compra",
		["Release notes URL"] = "URL de notas de la versión",
		["Channel"] = "Canal",
		["Installer locale"] = "Idioma del instalador",
		["Platforms"] = "Plataformas",
		["Minimum Windows version"] = "Versión mínima de Windows",
		["Shared nested type"] = "Tipo interno compartido",
		["Shared ZIP contents"] = "Contenido ZIP compartido",
		["Protocols"] = "Protocolos",
		["File extensions"] = "Extensiones de archivo",
		["Unsupported architectures"] = "Arquitecturas no compatibles",
		["Extra success codes"] = "Códigos de éxito adicionales",
		["Package family name"] = "Nombre de familia del paquete",
		["Release date"] = "Fecha de publicación",
		["Repair behavior"] = "Comportamiento de reparación",
		["Installer aborts terminal"] = "El instalador cierra la terminal",
		["Install location required"] = "Ubicación de instalación obligatoria",
		["Require explicit upgrade"] = "Requerir actualización explícita",
		["Display install warnings"] = "Mostrar avisos de instalación",
		["Prohibit download command"] = "Prohibir comando de descarga",
		["Archive binaries depend on PATH"] = "Los binarios del archivo dependen de PATH",
		["Silent switch"] = "Parámetro silencioso",
		["Silent with progress"] = "Silencioso con progreso",
		["Interactive switch"] = "Parámetro interactivo",
		["Install-location switch"] = "Parámetro de ubicación",
		["Log switch"] = "Parámetro de registro",
		["Upgrade switch"] = "Parámetro de actualización",
		["Custom switch"] = "Parámetro personalizado",
		["Repair switch"] = "Parámetro de reparación",
		["Agreements"] = "Acuerdos",
		["Documentation links"] = "Enlaces de documentación",
		["Package dependencies"] = "Dependencias del paquete",
		["Windows features"] = "Características de Windows",
		["MSIX capabilities"] = "Capacidades de MSIX",
		["Restricted capabilities"] = "Capacidades restringidas",
		["Allowed markets"] = "Mercados permitidos",
		["Excluded markets"] = "Mercados excluidos",
		["Expected return codes"] = "Códigos de retorno esperados",
		["Unsupported Winget arguments"] = "Argumentos de Winget no compatibles",
		["Default install location"] = "Ubicación de instalación predeterminada",
		["Installed files"] = "Archivos instalados",
		["Authentication type"] = "Tipo de autenticación",
		["Entra resource"] = "Recurso de Entra",
		["Entra scope"] = "Ámbito de Entra",
		["Additional locale fields"] = "Campos de idioma adicionales",
		["Additional installer fields"] = "Campos de instalador adicionales",
		["Optional shared settings"] = "Configuración compartida opcional",
		["Shared installer type"] = "Tipo de instalador compartido",
		["Scope"] = "Ámbito",
		["Install modes"] = "Modos de instalación",
		["Upgrade behavior"] = "Comportamiento de actualización",
		["Elevation"] = "Elevación",
		["Allow HTTP URLs"] = "Permitir URL HTTP",
		["Off is safer. Enable only when HTTPS is unavailable."] = "Desactivado es más seguro. Actívalo solo si HTTPS no está disponible.",
		["Command"] = "Comando",
		["Arguments"] = "Argumentos",
		["Full WingetCreate access for New, Update, New-Locale, Update-Locale, Submit, Show, Token, Settings, Cache, Info, and DSC. Commands run directly without cmd.exe. Commands that ask questions open a real WingetCreate console so you can answer them."] = "Acceso completo a WingetCreate para crear, actualizar, idiomas, enviar, mostrar, token, configuración, caché, información y DSC. Los comandos se ejecutan directamente. Los que hacen preguntas abren una consola real para que puedas responder.",
		["LOCAL RELEASE FILE"] = "ARCHIVO LOCAL",
		["PUBLIC INSTALLER URL"] = "URL PÚBLICA DEL INSTALADOR",
		["ARCH"] = "ARQ.",
		["TYPE"] = "TIPO",
		["SCOPE"] = "ÁMBITO",
		["HASH SOURCE / STATUS"] = "ORIGEN DEL HASH / ESTADO",
		["INSTALLER ANALYSIS"] = "ANÁLISIS DEL INSTALADOR",
		["NESTED TYPE"] = "TIPO INTERNO",
		["ZIP CONTENTS"] = "CONTENIDO ZIP",
		["DIGITAL SIGNATURE"] = "FIRMA DIGITAL",
		["SIGNER"] = "FIRMANTE",
		["PRODUCT CODE"] = "CÓDIGO DE PRODUCTO",
		["UPGRADE CODE"] = "CÓDIGO DE ACTUALIZACIÓN",
		["MSIX SIGNATURE SHA-256"] = "SHA-256 DE FIRMA MSIX",
		["ADDITIONAL ROW YAML"] = "YAML ADICIONAL DE LA FILA",
		["The values shared by every manifest file."] = "Los valores compartidos por todos los archivos de manifiesto.",
		["Shown to users by Windows Package Manager."] = "Información que verán los usuarios en el Administrador de paquetes de Windows.",
		["Use public HTTPS links when available."] = "Usa enlaces HTTPS públicos cuando estén disponibles.",
		["Optional current Winget schema fields. Leave a field blank when it does not apply."] = "Campos opcionales del esquema actual de Winget. Deja un campo vacío cuando no corresponda.",
		["Winget uses these command-line switches for installer actions. Known Inno, Nullsoft, MSI, and MSIX types often need no custom values."] = "Winget usa estos parámetros para las acciones del instalador. Los tipos Inno, Nullsoft, MSI y MSIX conocidos normalmente no necesitan valores personalizados.",
		["Friendly one-line formats create the nested YAML for you. Use one entry per line; leave the entire box blank when it does not apply."] = "Los formatos sencillos crean el YAML anidado por ti. Usa una entrada por línea y deja el cuadro vacío cuando no corresponda.",
		["Optional rules for packages that depend on another Winget package or Windows feature, MSIX capabilities, or market restrictions."] = "Reglas opcionales para dependencias de paquetes o características de Windows, capacidades MSIX y restricciones de mercado.",
		["Describe uncommon installer results and installed files without writing YAML. These values are optional and official validation checks their schema."] = "Describe resultados poco comunes y archivos instalados sin escribir YAML. Son valores opcionales y la validación oficial comprueba su esquema.",
		["Only private Entra ID secured sources use these fields. Community repository packages should leave all three blank."] = "Solo las fuentes privadas protegidas con Entra ID usan estos campos. Los paquetes del repositorio comunitario deben dejar los tres vacíos.",
		["Only use these boxes for schema fields that still have no guided control. Existing custom keys remain preserved even when these boxes stay blank."] = "Usa estos cuadros solo para campos sin control guiado. Las claves personalizadas existentes se conservan aunque los dejes vacíos.",
		["Required format: Publisher.Application (example: Contoso.Sample)"] = "Formato obligatorio: Editor.Aplicación (ejemplo: Contoso.Sample)",
		["Do not include a leading v"] = "No incluyas una v al principio",
		["Usually en-US"] = "Normalmente en-US",
		["Choose any empty folder or an existing manifest folder"] = "Elige una carpeta vacía o una carpeta de manifiestos existente",
		["Example: MIT, Proprietary, Freeware"] = "Ejemplo: MIT, Proprietary o Freeware",
		["Comma-separated"] = "Valores separados por comas",
		["Comma-separated command aliases. Preserved during updates"] = "Alias de comandos separados por comas; se conservan al actualizar",
		["Shown to the user after installation"] = "Se muestra al usuario después de la instalación",
		["Schema version used by the generated YAML; 1.12.0 is recommended for Microsoft Winget community submissions"] = "Versión del esquema usada por el YAML; se recomienda 1.12.0 para los envíos a la comunidad de Microsoft Winget",
		["The public product name users see in Winget"] = "Nombre público del producto que los usuarios ven en Winget",
		["The company or person that publishes the application"] = "Empresa o persona que publica la aplicación",
		["The original application author when different from the publisher"] = "Autor original cuando es diferente del editor",
		["The license name, such as MIT, GPL-3.0, Proprietary, or Freeware"] = "Nombre de la licencia, como MIT, GPL-3.0, Proprietary o Freeware",
		["One clear sentence explaining what the application does"] = "Una frase clara que explique lo que hace la aplicación",
		["A longer public explanation of the application and its purpose"] = "Una explicación pública más amplia de la aplicación y su propósito",
		["A short command-friendly nickname used to find the package"] = "Alias corto y fácil de escribir para encontrar el paquete",
		["Search words separated with commas; do not add # symbols"] = "Palabras de búsqueda separadas por comas; no agregues #",
		["Command names installed by the package, separated with commas"] = "Nombres de comandos instalados por el paquete, separados por comas",
		["Public HTTPS home page for the publisher"] = "Página HTTPS pública del editor",
		["Public HTTPS page where users can get help"] = "Página HTTPS pública donde los usuarios pueden obtener ayuda",
		["Public HTTPS privacy-policy page"] = "Página HTTPS pública de la política de privacidad",
		["Public HTTPS home page for this application"] = "Página HTTPS pública de esta aplicación",
		["Public HTTPS page containing the license terms"] = "Página HTTPS pública con los términos de la licencia",
		["Copyright notice shown with the package"] = "Aviso de derechos de autor mostrado con el paquete",
		["Public HTTPS page containing copyright information"] = "Página HTTPS pública con información de derechos de autor",
		["Public HTTPS purchase page when the application is paid"] = "Página HTTPS pública de compra si la aplicación es de pago",
		["Public HTTPS page for this exact version's release notes"] = "Página HTTPS pública con las notas de esta versión exacta",
		["What changed in this exact release"] = "Cambios incluidos en esta versión exacta",
		["Instructions Winget shows after installation"] = "Instrucciones que Winget muestra después de instalar",
		["Example: stable or beta"] = "Ejemplo: stable o beta",
		["Example: en-US"] = "Ejemplo: en-US",
		["Comma-separated; usually Windows.Desktop"] = "Valores separados por comas; normalmente Windows.Desktop",
		["Example: 10.0.19041.0"] = "Ejemplo: 10.0.19041.0",
		["Semicolon-separated paths inside the ZIP; add | command after a portable file when needed"] = "Rutas dentro del ZIP separadas por punto y coma; agrega | comando tras un archivo portátil cuando sea necesario",
		["Comma-separated URL protocols"] = "Protocolos URL separados por comas",
		["Comma-separated, without dots"] = "Valores separados por comas y sin puntos",
		["Comma-separated whole numbers"] = "Números enteros separados por comas",
		["YYYY-MM-DD"] = "AAAA-MM-DD",
		["Optional shared type; inspected rows keep their own type, so leave this blank for mixed installers"] = "Tipo compartido opcional; cada fila conserva su propio tipo, así que déjalo vacío si hay instaladores distintos",
		["Real installer type inside a ZIP package"] = "Tipo real del instalador dentro del paquete ZIP",
		["Shared paths inside a ZIP; separate paths with semicolons and add | command only for portable files"] = "Rutas compartidas dentro del ZIP; sepáralas con punto y coma y usa | comando solo para archivos portátiles",
		["Optional shared scope; choose user for one account, machine for the whole computer, or leave blank when it varies by installer"] = "Ámbito compartido opcional; elige user para una cuenta, machine para todo el equipo o déjalo vacío si cambia por instalador",
		["Supported modes separated with commas: interactive, silent, silentWithProgress"] = "Modos compatibles separados por comas: interactive, silent, silentWithProgress",
		["Optional instruction for upgrades; leave blank unless the installer requires a specific behavior"] = "Instrucción opcional para actualizaciones; déjala vacía salvo que el instalador requiera un comportamiento específico",
		["Whether the installer requires elevation; leave blank when unknown"] = "Indica si el instalador requiere elevación; déjalo vacío si no lo sabes",
		["URL protocols registered by the app, separated with commas"] = "Protocolos URL registrados por la aplicación, separados por comas",
		["File extensions registered by the app, separated with commas and without dots"] = "Extensiones registradas por la aplicación, separadas por comas y sin puntos",
		["Architectures that cannot use this installer, separated with commas"] = "Arquitecturas que no pueden usar este instalador, separadas por comas",
		["Extra successful installer exit codes, separated with commas"] = "Códigos adicionales de salida correcta, separados por comas",
		["Microsoft Store or MSIX package family name"] = "Nombre de familia del paquete de Microsoft Store o MSIX",
		["Public release date in YYYY-MM-DD format"] = "Fecha pública de la versión en formato AAAA-MM-DD",
		["How Winget repairs the app: modify, uninstaller, or installer"] = "Cómo repara Winget la aplicación: modify, uninstaller o installer",
		["Enter true only if installation closes the user's terminal"] = "Escribe true solo si la instalación cierra la terminal del usuario",
		["Enter true only when a custom install location is mandatory"] = "Escribe true solo si es obligatoria una ubicación personalizada",
		["Enter true when Winget must not upgrade automatically"] = "Escribe true cuando Winget no deba actualizar automáticamente",
		["Enter true when Winget should show installer warnings"] = "Escribe true cuando Winget deba mostrar avisos del instalador",
		["Enter true when winget download must be blocked"] = "Escribe true cuando se deba bloquear winget download",
		["For archives, enter true when extracted commands depend on PATH"] = "Para archivos comprimidos, escribe true si los comandos extraídos dependen de PATH",
		["Installer argument for a completely silent installation"] = "Argumento del instalador para una instalación totalmente silenciosa",
		["Installer argument for quiet installation with progress"] = "Argumento para una instalación silenciosa con progreso",
		["Installer argument that forces the interactive interface"] = "Argumento que fuerza la interfaz interactiva del instalador",
		["Installer argument template for a custom install folder"] = "Plantilla del argumento para una carpeta de instalación personalizada",
		["Installer argument template for a log-file path"] = "Plantilla del argumento para la ruta del archivo de registro",
		["Installer argument used specifically during upgrades"] = "Argumento usado específicamente durante las actualizaciones",
		["Argument Winget must add to every install command"] = "Argumento que Winget debe agregar a cada comando de instalación",
		["Installer argument used for repair"] = "Argumento del instalador usado para reparar",
		["One agreement per line using label | HTTPS URL | agreement text"] = "Un acuerdo por línea con etiqueta | URL HTTPS | texto del acuerdo",
		["One documentation link per line using label | HTTPS URL"] = "Un enlace de documentación por línea con etiqueta | URL HTTPS",
		["One Winget dependency per line using Publisher.Application | minimum version"] = "Una dependencia de Winget por línea con Editor.Aplicación | versión mínima",
		["Windows feature names required by the application, separated with commas"] = "Características de Windows requeridas por la aplicación, separadas por comas",
		["MSIX capabilities required by the package, separated with commas"] = "Capacidades MSIX requeridas por el paquete, separadas por comas",
		["Restricted MSIX capabilities, separated with commas"] = "Capacidades MSIX restringidas, separadas por comas",
		["Market codes where installation is allowed, separated with commas"] = "Códigos de mercado donde se permite instalar, separados por comas",
		["Market codes where installation is blocked, separated with commas"] = "Códigos de mercado donde se bloquea la instalación, separados por comas",
		["One installer result per line using code | Winget response | optional HTTPS help URL"] = "Un resultado por línea con código | respuesta de Winget | URL HTTPS de ayuda opcional",
		["Choose log, location, or both only when the installer cannot support those Winget arguments"] = "Elige log, location o ambos solo si el instalador no admite esos argumentos de Winget",
		["The usual installed application folder; environment variables such as %ProgramFiles% are allowed"] = "Carpeta habitual de la aplicación; se permiten variables como %ProgramFiles%",
		["One installed file per line using relative path | file type | optional SHA-256 | optional argument | optional display name"] = "Un archivo instalado por línea con ruta relativa | tipo | SHA-256 opcional | argumento opcional | nombre opcional",
		["Authentication for a private source; community repository packages leave this blank"] = "Autenticación para una fuente privada; los paquetes del repositorio comunitario dejan esto vacío",
		["Microsoft Entra resource used by a private source"] = "Recurso de Microsoft Entra usado por una fuente privada",
		["Microsoft Entra scope used by a private source"] = "Ámbito de Microsoft Entra usado por una fuente privada",
		["Advanced locale YAML only; most users should leave this blank"] = "Solo YAML avanzado de idioma; la mayoría de usuarios debe dejarlo vacío",
		["Advanced installer YAML only; most users should leave this blank"] = "Solo YAML avanzado del instalador; la mayoría de usuarios debe dejarlo vacío",
		["Leave blank when this value does not apply or is unknown"] = "Déjalo vacío si no corresponde o no conoces el valor",
		["Ready"] = "Listo",
		["INTERFACE LANGUAGE"] = "IDIOMA DE LA INTERFAZ",
		["Interface language"] = "Idioma de la interfaz",
		["Choose the language used by the Studio. Package data and generated YAML are never translated or changed."] = "Elige el idioma de Studio. Los datos del paquete y el YAML generado nunca se traducen ni se modifican.",
		["APPLICATION UPDATES"] = "ACTUALIZACIONES DE LA APLICACIÓN",
		["Installed with StudioSetup.msi. Updates use the matching MSI from the official GitHub release."] = "Instalado con StudioSetup.msi. Las actualizaciones usan el MSI correspondiente de la versión oficial de GitHub.",
		["Portable copy. Updates replace this EXE with the matching file from the official GitHub release."] = "Copia portátil. Las actualizaciones reemplazan este EXE con el archivo correspondiente de la versión oficial de GitHub.",
		["Checking the latest stable GitHub release..."] = "Buscando la última versión estable de GitHub...",
		["Checking..."] = "Comprobando...",
		["You have the latest stable version."] = "Ya tienes la última versión estable.",
		["Check again"] = "Comprobar de nuevo",
		["Version {0} is available: {1}"] = "La versión {0} está disponible: {1}",
		["Update to {0}"] = "Actualizar a {0}",
		["Update check needs attention: {0}"] = "La comprobación necesita atención: {0}",
		["Try again"] = "Intentar de nuevo",
		["Downloading and verifying the selected update..."] = "Descargando y verificando la actualización seleccionada...",
		["Downloading..."] = "Descargando...",
		["Updates are checked quietly after the Studio opens. You can also check now."] = "Las actualizaciones se comprueban en segundo plano después de abrir Studio. También puedes comprobar ahora.",
		["Check for updates"] = "Buscar actualizaciones",
		["Install Studio update?"] = "¿Instalar la actualización de Studio?",
		["Winget Manifest Studio {0} is available."] = "Winget Manifest Studio {0} está disponible.",
		["File: {0} ({1})"] = "Archivo: {0} ({1})",
		["StudioSetup.msi will update the installed copy."] = "StudioSetup.msi actualizará la copia instalada.",
		["The new portable EXE will replace this file after the Studio closes. A backup is restored automatically if replacement fails."] = "El nuevo EXE portátil reemplazará este archivo después de cerrar Studio. Si falla, se restaurará una copia de seguridad.",
		["Download and install it now?"] = "¿Descargar e instalar ahora?",
		["Interface language changed to {0}."] = "Idioma de la interfaz cambiado a {0}.",
		["Downloading the verified Studio update from GitHub..."] = "Descargando desde GitHub la actualización verificada de Studio...",
		["Downloading... {0}%"] = "Descargando... {0}%",
		["Downloading and checking {0}: {1}%"] = "Descargando y comprobando {0}: {1}%",
		["The verified update is ready. Winget Manifest Studio is closing so the update can finish."] = "La actualización verificada está lista. Winget Manifest Studio se cerrará para terminarla.",
		["The update download was canceled. No application files were changed."] = "La descarga se canceló. No se modificó ningún archivo de la aplicación.",
		["Build a Winget submission without editing YAML by hand."] = "Crea un envío de Winget sin editar YAML manualmente.",
		["Create a new three-file manifest set or safely update an existing one. Local release files provide the real SHA-256 hash; public URLs tell Winget where users will download them."] = "Crea un conjunto nuevo de tres manifiestos o actualiza uno existente de forma segura. Los archivos locales proporcionan el SHA-256 real y las URL públicas indican a Winget dónde descargarlos.",
		["LOCAL-FIRST\n\nGitHub token stays in Windows Credential Manager\nNo manifest overwritten without backup\nNo installer downloaded without confirmation"] = "PRIMERO LOCAL\n\nEl token de GitHub permanece en el Administrador de credenciales\nNingún manifiesto se reemplaza sin copia de seguridad\nNingún instalador se descarga sin confirmación",
		["Choose how to start"] = "Elige cómo comenzar",
		["Create a blank package, load YAML files already on this computer, or enter an existing Winget package ID to download its current manifests into a new working copy."] = "Crea un paquete vacío, carga archivos YAML de este equipo o escribe un ID de paquete de Winget para descargar sus manifiestos actuales en una copia de trabajo nueva.",
		["Load existing manifests"] = "Cargar manifiestos existentes",
		["Import existing Winget package"] = "Importar paquete existente de Winget",
		["Create a new project"] = "Crear un proyecto nuevo",
		["Fill release information"] = "Completa la información de la versión",
		["Enter package details yourself, or paste a public GitHub release URL. The importer fills only blank fields and asks before downloading supported release assets for hashes and installer inspection."] = "Escribe los datos del paquete o pega la URL pública de una versión de GitHub. El importador solo completa campos vacíos y pide permiso antes de descargar archivos para calcular hashes e inspeccionarlos.",
		["Import a GitHub release"] = "Importar una versión de GitHub",
		["Open Package Details"] = "Abrir detalles del paquete",
		["Add the release installers"] = "Agrega los instaladores de la versión",
		["Choose the local MSI, EXE, MSIX, APPX, ZIP, portable app, or font files that you will upload. The Studio reads those exact files and calculates their SHA-256 hashes. Then enter the public download URL for each file."] = "Elige los archivos MSI, EXE, MSIX, APPX, ZIP, portátiles o de fuentes que publicarás. Studio lee esos archivos exactos y calcula sus hashes SHA-256. Después escribe la URL pública de cada archivo.",
		["Open Installers & Hashes"] = "Abrir instaladores y hashes",
		["Review before anything is changed"] = "Revisa antes de cambiar archivos",
		["Preview builds all three manifests in memory. Save writes them only after validation and keeps timestamped backups of files that already exist."] = "La vista previa crea los tres manifiestos en memoria. Guardar los escribe de forma segura y conserva copias con fecha y hora de los archivos existentes.",
		["Open Preview & Submit"] = "Abrir revisión y envío",
		["Test in the numbered order, then submit"] = "Realiza las pruebas numeradas y después envía",
		["Open Official Tools"] = "Abrir herramientas oficiales",
		["Need help?"] = "¿Necesitas ayuda?",
		["Open the built-in beginner guide for field meanings, installer IDs, hashes, validation, and submission."] = "Abre la guía para principiantes sobre campos, identificadores de instalador, hashes, validación y envío.",
		["Open Help & Guide"] = "Abrir ayuda y guía",
		["Keep Winget Manifest Studio up to date"] = "Mantén Winget Manifest Studio actualizado",
		["The Start page checks the latest stable GitHub release after the window is already open. An installed copy uses StudioSetup.msi; a portable copy replaces only its WingetManifestStudio.exe. Nothing downloads or installs until you choose the update button and confirm."] = "La página Inicio comprueba la última versión estable de GitHub después de abrir la ventana. Una copia instalada usa StudioSetup.msi; una copia portátil solo reemplaza su WingetManifestStudio.exe. Nada se descarga ni se instala hasta que eliges el botón de actualización y confirmas.",
		["Open application updates"] = "Abrir actualizaciones de la aplicación",
		["PACKAGE WORKSPACE"] = "ÁREA DE TRABAJO DEL PAQUETE",
		["Every box below is editable. Loading a folder reads its YAML files only; it never downloads installers or changes the manifests."] = "Todos los campos siguientes se pueden editar. Cargar una carpeta solo lee sus YAML; nunca descarga instaladores ni cambia los manifiestos.",
		["Winget schema"] = "Esquema de Winget",
		["Manifest output folder"] = "Carpeta de salida de manifiestos",
		["Optional advanced package fields"] = "Campos avanzados opcionales",
		["Most beginners do not need installer behavior overrides, custom switches, or raw advanced YAML. Open this section only when the installer documentation or an existing manifest requires one of these values."] = "La mayoría de principiantes no necesita modificar el comportamiento, los parámetros ni el YAML avanzado. Abre esta sección solo si la documentación o un manifiesto existente lo requiere.",
		["FOLLOW THESE INSTALLER STEPS"] = "SIGUE ESTOS PASOS DEL INSTALADOR",
		["1 Add each exact release file. 2 Paste its direct public HTTPS URL. 3 Inspect it to fill the hash and metadata. 4 Verify URLs after uploading. Architecture, type, and scope stay visible beside the URL and can be corrected from their dropdowns."] = "1 Agrega cada archivo exacto. 2 Pega su URL HTTPS pública directa. 3 Inspecciónalo para completar hash y metadatos. 4 Verifica las URL tras publicarlo. Puedes corregir arquitectura, tipo y ámbito en sus listas.",
		["1 Add Release Files"] = "1 Agregar archivos",
		["2 Enter Public URL"] = "2 Escribir URL pública",
		["3 Inspect & Fill Selected"] = "3 Inspeccionar selección",
		["4 Verify Public URLs"] = "4 Verificar URL públicas",
		["REVIEW AND SAVE SAFELY"] = "REVISAR Y GUARDAR DE FORMA SEGURA",
		["Use the single highlighted action below. Review never changes files until you choose Save, and existing manifests are backed up before replacement."] = "Usa la única acción resaltada. La revisión no cambia archivos hasta que eliges Guardar y los manifiestos existentes se copian antes de reemplazarlos.",
		["Preview"] = "Vista previa",
		["Save safely"] = "Guardar de forma segura",
		["Validate"] = "Validar",
		["Test & submit"] = "Probar y enviar",
		["REVIEW CHECKLIST"] = "LISTA DE REVISIÓN",
		["The Studio unlocks these in the correct order."] = "Studio los habilita en el orden correcto.",
		["1  Preview"] = "1  Vista previa",
		["Builds the proposed YAML in memory"] = "Crea el YAML propuesto en memoria",
		["2  Save safely"] = "2  Guardar de forma segura",
		["Creates backups before replacing files"] = "Crea copias antes de reemplazar archivos",
		["3  Validate"] = "3  Validar",
		["Runs the official Winget validator"] = "Ejecuta el validador oficial de Winget",
		["4  Test & submit"] = "4  Probar y enviar",
		["Continues in the guided Test Center"] = "Continúa en el Centro de pruebas guiado",
		["VIEW OPTIONS\r\nThe plain-language review stays selected by default."] = "OPCIONES DE VISTA\r\nLa revisión sencilla permanece seleccionada de forma predeterminada.",
		["Show technical YAML"] = "Mostrar YAML técnico",
		["Show plain-language review"] = "Mostrar revisión sencilla",
		["Open output folder"] = "Abrir carpeta de salida",
		["Open backup folder"] = "Abrir carpeta de copias",
		["PLAIN-LANGUAGE REVIEW"] = "REVISIÓN EN LENGUAJE SENCILLO",
		["Fix the package information"] = "Corrige la información del paquete",
		["The Studio will return you to the correct page."] = "Studio te llevará a la página correcta.",
		["REQUIRED · Preview stays locked until this is corrected"] = "OBLIGATORIO · La vista previa permanece bloqueada hasta corregirlo",
		["REQUIRED · Testing stays locked until this is corrected"] = "OBLIGATORIO · Las pruebas permanecen bloqueadas hasta corregirlo",
		["Package Version is required and must not begin with v"] = "La versión del paquete es obligatoria y no debe comenzar con v",
		["Package Name is required"] = "El nombre del paquete es obligatorio",
		["Publisher is required"] = "El editor es obligatorio",
		["Short Description is required"] = "La descripción breve es obligatoria",
		["License is required"] = "La licencia es obligatoria",
		["Choose a manifest output folder"] = "Elige una carpeta de salida para los manifiestos",
		["Add at least one installer"] = "Agrega al menos un instalador",
		["Open the field to fix"] = "Abrir el campo que debes corregir",
		["Fix the validation problem"] = "Corrige el problema de validación",
		["The plain-language result below names the problem and where to correct it. Then preview and save again."] = "El resultado sencillo indica el problema y dónde corregirlo. Después vuelve a previsualizar y guardar.",
		["STOP · Submission remains locked until validation passes"] = "ALTO · El envío permanece bloqueado hasta superar la validación",
		["Open the fields to fix"] = "Abrir los campos que debes corregir",
		["Preview the proposed changes"] = "Revisa los cambios propuestos",
		["Builds the exact manifest changes in memory and explains them below. No files are written."] = "Crea en memoria los cambios exactos y los explica abajo. No escribe ningún archivo.",
		["SAFE · Preview does not change any files"] = "SEGURO · La vista previa no cambia archivos",
		["Preview changes"] = "Previsualizar cambios",
		["Save the reviewed manifests"] = "Guarda los manifiestos revisados",
		["Writes the reviewed YAML to the output folder after creating recoverable backups of existing files."] = "Escribe el YAML revisado después de crear copias recuperables de los archivos existentes.",
		["PROTECTED · Existing manifests are backed up first"] = "PROTEGIDO · Primero se copian los manifiestos existentes",
		["Save manifests"] = "Guardar manifiestos",
		["Validate with Winget"] = "Validar con Winget",
		["Runs Microsoft's Winget validator against a clean temporary copy. It does not install the package."] = "Ejecuta el validador de Winget sobre una copia temporal limpia. No instala el paquete.",
		["SAFE · Validation does not change the saved manifests"] = "SEGURO · La validación no cambia los manifiestos guardados",
		["Validate locally"] = "Validar localmente",
		["Continue to Test Center"] = "Continuar al Centro de pruebas",
		["Run safe preflight, test the installation, verify the result, and submit from one guided screen."] = "Ejecuta la comprobación previa, prueba la instalación, verifica el resultado y envía desde una sola pantalla guiada.",
		["NEXT · Testing and submission continue without returning here"] = "SIGUIENTE · Las pruebas y el envío continúan sin volver aquí",
		["Ready to submit in Test Center"] = "Listo para enviar en el Centro de pruebas",
		["All required review and installation checks passed. The submission action is ready in Test Center."] = "Todas las revisiones y pruebas obligatorias se completaron. La acción de envío está lista en el Centro de pruebas.",
		["READY · Microsoft's WingetCreate handles the submission"] = "LISTO · WingetCreate de Microsoft gestiona el envío",
		["WINGET FOUND A PROBLEM   •   NOTHING WAS SUBMITTED"] = "WINGET ENCONTRÓ UN PROBLEMA   •   NO SE ENVIÓ NADA",
		["PREVIEW READY   •   NOTHING HAS BEEN SAVED"] = "VISTA PREVIA LISTA   •   TODAVÍA NO SE HA GUARDADO NADA",
		["SAVED SAFELY   •   READY FOR OFFICIAL VALIDATION"] = "GUARDADO DE FORMA SEGURA   •   LISTO PARA VALIDACIÓN OFICIAL",
		["VALIDATION PASSED   •   READY FOR TEST CENTER"] = "VALIDACIÓN SUPERADA   •   LISTO PARA EL CENTRO DE PRUEBAS",
		["ALL REVIEW AND INSTALLATION TESTS PASSED"] = "TODAS LAS REVISIONES Y PRUEBAS DE INSTALACIÓN SE SUPERARON",
		["Open Test Center to submit"] = "Abrir Centro de pruebas para enviar",
		["TEST AND FINISH"] = "PROBAR Y FINALIZAR",
		["Follow the progress line, then use the single highlighted action below. The Studio unlocks each test in the correct order and enables submission when all four pass."] = "Sigue la línea de progreso y usa la única acción resaltada. Studio habilita cada prueba en orden y permite enviar cuando las cuatro se completan.",
		["Safe preflight"] = "Comprobación previa",
		["Allow testing"] = "Permitir pruebas",
		["Test install"] = "Probar instalación",
		["Verify result"] = "Verificar resultado",
		["REQUIRED CHECKLIST"] = "LISTA OBLIGATORIA",
		["These are completed automatically in order."] = "Se completan automáticamente en orden.",
		["1  Safe preflight"] = "1  Comprobación previa",
		["Manifest, hash, signature, and repository checks"] = "Comprueba manifiesto, hash, firma y repositorio",
		["2  Local testing"] = "2  Pruebas locales",
		["One-time Windows setting"] = "Configuración única de Windows",
		["3  Test install"] = "3  Probar instalación",
		["Installs this exact release through Winget"] = "Instala esta versión exacta mediante Winget",
		["4  Installed result"] = "4  Resultado instalado",
		["Confirms the installed version"] = "Confirma la versión instalada",
		["Show optional tools"] = "Mostrar herramientas opcionales",
		["Hide optional tools"] = "Ocultar herramientas opcionales",
		["OPTIONAL DIAGNOSTICS\r\nExtra detail only — these are not required steps."] = "DIAGNÓSTICOS OPCIONALES\r\nSolo ofrecen detalles; no son pasos obligatorios.",
		["Check Winget setup"] = "Comprobar configuración de Winget",
		["Inspect signatures"] = "Inspeccionar firmas",
		["Find existing package"] = "Buscar paquete existente",
		["Sandbox install only"] = "Sandbox: solo instalar",
		["Sandbox install + uninstall"] = "Sandbox: instalar y desinstalar",
		["Export test report"] = "Exportar informe de pruebas",
		["RESULTS AND INSTRUCTIONS"] = "RESULTADOS E INSTRUCCIONES",
		["Repair the Winget test setup"] = "Repara la configuración de pruebas de Winget",
		["Windows Package Manager is not ready. Run the setup check to see the exact repair instructions."] = "El Administrador de paquetes de Windows no está listo. Ejecuta la comprobación para ver las instrucciones exactas.",
		["SAFE · This only checks Winget and changes nothing"] = "SEGURO · Solo comprueba Winget y no cambia nada",
		["Run safe preflight"] = "Ejecutar comprobación previa",
		["Checks YAML, file hashes, signatures, official Winget validation, and whether this package already exists."] = "Comprueba YAML, hashes, firmas, la validación oficial y si el paquete ya existe.",
		["SAFE · Nothing will be installed or changed"] = "SEGURO · No se instalará ni cambiará nada",
		["Allow local manifest testing"] = "Permitir pruebas de manifiestos locales",
		["Windows requires one administrator approval before Winget can install a manifest from this computer."] = "Windows requiere una aprobación de administrador antes de que Winget instale un manifiesto local.",
		["ONE-TIME SETUP · Approve the Windows prompt"] = "CONFIGURACIÓN ÚNICA · Aprueba el aviso de Windows",
		["Enable local testing"] = "Habilitar pruebas locales",
		["Test install this release"] = "Probar la instalación de esta versión",
		["Runs winget install --manifest with the exact generated files. Review the installer console, then close it."] = "Ejecuta winget install --manifest con los archivos generados. Revisa la consola del instalador y después ciérrala.",
		["CONFIRMATION REQUIRED · This installs software on this PC"] = "REQUIERE CONFIRMACIÓN · Instala software en este equipo",
		["Test install here"] = "Probar instalación aquí",
		["Confirm the installed result"] = "Confirmar el resultado instalado",
		["Checks the Winget package ID, then the MSI identity or installed application name when needed."] = "Comprueba el ID de Winget y, si hace falta, la identidad MSI o el nombre de la aplicación instalada.",
		["SAFE · Verification does not reinstall the package"] = "SEGURO · La verificación no reinstala el paquete",
		["Verify installation"] = "Verificar instalación",
		["All tests passed — ready to submit"] = "Todas las pruebas superadas — listo para enviar",
		["Start Microsoft's official WingetCreate submission without returning to the Review page."] = "Inicia el envío oficial con WingetCreate sin volver a la página Revisión.",
		["READY · WingetCreate handles sign-in and pull-request creation"] = "LISTO · WingetCreate gestiona el inicio de sesión y la solicitud de cambios",
		["PASSED"] = "SUPERADA",
		["ENABLED"] = "HABILITADO",
		["INSTALLED"] = "INSTALADO",
		["VERIFIED"] = "VERIFICADO",
		["PREVIEWED"] = "PREVISUALIZADO",
		["SAVED"] = "GUARDADO",
		["VALIDATED"] = "VALIDADO",
		["COMPLETE"] = "COMPLETO",
		["NEEDS ATTENTION"] = "REQUIERE ATENCIÓN",
		["NEXT"] = "SIGUIENTE",
		["WAITING"] = "EN ESPERA",
		["FIX FIRST"] = "CORREGIR",
		["Backup folder"] = "Carpeta de copias",
		["No backups yet"] = "Todavía no hay copias",
		["Sandbox install and uninstall test"] = "Prueba de instalación y desinstalación en Sandbox",
		["This guide explains every screen and the information Winget needs. You can read it at any time; the buttons only take you to the screen being described."] = "Esta guía explica cada pantalla y la información que necesita Winget. Puedes consultarla en cualquier momento; los botones solo te llevan a la pantalla descrita.",
		["Start or open a manifest project"] = "Inicia o abre un proyecto de manifiestos",
		["For a first release, choose New Project. For an update, load a local YAML folder or choose Import Existing Winget Package and enter its exact package ID. Repository import downloads the newest manifests into a separate working-copy folder and never overwrites an existing manifest folder."] = "Para una primera versión, elige Nuevo proyecto. Para actualizar, carga una carpeta YAML o elige Importar paquete existente de Winget y escribe su ID exacto. La importación crea una copia de trabajo separada y nunca reemplaza una carpeta existente.",
		["Go to Package Details"] = "Ir a detalles del paquete",
		["Enter the package identity"] = "Escribe la identidad del paquete",
		["Package Identifier is the permanent Winget name, normally Publisher.Application. Enter Publisher and Package Name first, then use Suggest Package ID if you want help. Package Version has no leading v. Keep the identifier unchanged for updates."] = "El identificador es el nombre permanente de Winget, normalmente Editor.Aplicación. Escribe primero Editor y Nombre del paquete y usa Sugerir ID si necesitas ayuda. La versión no lleva una v inicial. Conserva el mismo identificador en las actualizaciones.",
		["Edit Package Identity"] = "Editar identidad del paquete",
		["Complete the public package information"] = "Completa la información pública",
		["Package Name, Publisher, License, and Short Description are required. Enter them yourself or use Import a GitHub Release from Start. The importer fills only blank fields and asks before temporarily downloading supported release assets. Optional guided fields create dependencies, agreements, documentation, return codes, market rules, and install-detection YAML without manual YAML editing."] = "Nombre, Editor, Licencia y Descripción breve son obligatorios. Escríbelos o usa Importar una versión de GitHub. El importador solo completa campos vacíos y pide permiso antes de descargar temporalmente archivos. Los campos opcionales crean dependencias, acuerdos, documentación, códigos de retorno, mercados y detección sin editar YAML.",
		["Edit Package Information"] = "Editar información del paquete",
		["INSTALLER FILES AND DOWNLOAD LINKS"] = "ARCHIVOS DEL INSTALADOR Y ENLACES",
		["Winget downloads from a public URL, but the Studio uses your matching local release file to calculate the trusted SHA-256 value."] = "Winget descarga desde una URL pública, pero Studio usa el archivo local correspondiente para calcular el SHA-256 confiable.",
		["Add the exact release file"] = "Agrega el archivo exacto de la versión",
		["Choose Add Release Files for every installer you publish. Select the same MSI, EXE, MSIX, APPX, bundle, ZIP, portable app, or font file that will be uploaded. Use one row for each architecture, scope, or installer variation. Nothing is assumed to be x64."] = "Elige Agregar archivos para cada instalador que publiques. Selecciona exactamente el mismo MSI, EXE, MSIX, APPX, paquete, ZIP, aplicación portátil o fuente que subirás. Usa una fila por arquitectura, ámbito o variante. No se supone que todo sea x64.",
		["Enter its public HTTPS URL"] = "Escribe su URL HTTPS pública",
		["Paste the direct download URL for each installer—not a web page containing a download button. The URL must remain public and must download the exact local file in that row. GitHub release asset URLs are suitable."] = "Pega la URL de descarga directa de cada instalador, no una página con un botón. Debe ser pública y descargar exactamente el archivo local de esa fila. Las URL de archivos de versiones de GitHub son adecuadas.",
		["Enter Download URLs"] = "Escribir URL de descarga",
		["Inspect and verify the published installer"] = "Inspecciona y verifica el instalador publicado",
		["Inspect & Fill Details calculates SHA-256, reports signed or unsigned status, and detects MSI, MSIX, Inno, NSIS, WiX Burn, Squirrel, Velopack, InstallShield, Advanced Installer, and self-extracting EXE clues. Unsigned EXE/MSI files are supported and shown as a warning; MSIX/APPX packages still require their package signature. ZIP files show nested paths. Verify Public URLs proves the published file matches the hash."] = "Inspeccionar y completar calcula SHA-256, indica si está firmado y detecta MSI, MSIX, Inno, NSIS, WiX Burn, Squirrel, Velopack, InstallShield, Advanced Installer y EXE autoextraíbles. Los EXE/MSI sin firma se admiten con una advertencia; MSIX/APPX sí requieren firma. Los ZIP muestran sus rutas internas. Verificar URL confirma que el archivo publicado coincide con el hash.",
		["Inspect Installer Files"] = "Inspeccionar archivos del instalador",
		["SPECIAL PACKAGE TYPES"] = "TIPOS DE PAQUETE ESPECIALES",
		["Portable EXEs may look like normal EXE installers, so choose portable in the row when needed. Font packages use Microsoft's separate fonts manifest root and have stricter submission rules. PWA support can vary by Winget client and repository policy; always keep the official validation and install-test result."] = "Los EXE portátiles pueden parecer instaladores normales; elige portable cuando corresponda. Las fuentes usan una raíz de manifiestos separada y tienen reglas más estrictas. La compatibilidad PWA depende del cliente y la política; conserva siempre el resultado de validación y prueba oficial.",
		["REVIEW, SAVE, AND PUBLISH"] = "REVISAR, GUARDAR Y PUBLICAR",
		["The preview is your safety check. It creates the proposed YAML in memory without writing to the selected folder."] = "La vista previa es tu comprobación de seguridad. Crea el YAML propuesto en memoria sin escribir en la carpeta elegida.",
		["Follow Project Readiness, then preview"] = "Sigue el estado del proyecto y previsualiza",
		["The readiness panel counts anything still required and marks problem fields. When it says READY, choose Preview Changes and review the identifier, old and new versions, URLs, architectures, installer types, hashes, and filenames."] = "El panel de estado cuenta lo que falta y marca los campos con problemas. Cuando indique LISTO, elige Previsualizar cambios y revisa identificador, versiones, URL, arquitecturas, tipos, hashes y nombres de archivo.",
		["Review the Preview"] = "Revisar la vista previa",
		["Save with recoverable backups"] = "Guardar con copias recuperables",
		["Choose Save Manifests only after the preview is correct. New files are created in the output folder. Existing files are copied into a timestamped .manifest-backups folder before they are replaced."] = "Elige Guardar manifiestos solo cuando la vista previa sea correcta. Los archivos nuevos se crean en la carpeta de salida. Los existentes se copian a .manifest-backups con fecha y hora antes de reemplazarlos.",
		["Save or Validate"] = "Guardar o validar",
		["Validate before submission"] = "Validar antes del envío",
		["Validate Locally runs the official Winget validator against a clean temporary copy. If it reports an error, fix the related field and validate again. Validation does not modify the saved manifests."] = "Validar localmente ejecuta el validador oficial sobre una copia temporal limpia. Si informa un error, corrige el campo y repite. La validación no modifica los manifiestos guardados.",
		["Open Validation"] = "Abrir validación",
		["Run test step 1 — Safe Preflight"] = "Ejecuta la prueba 1 — Comprobación previa",
		["The Test Center first checks whether Winget itself works, then rechecks attached file hashes and signatures, runs official validation, and searches Winget plus microsoft/winget-pkgs for the exact package identifier. It does not install anything."] = "El Centro de pruebas comprueba Winget, vuelve a comprobar hashes y firmas, ejecuta la validación oficial y busca el identificador exacto en Winget y microsoft/winget-pkgs. No instala nada.",
		["Run test steps 2, 3, and 4"] = "Ejecuta las pruebas 2, 3 y 4",
		["Enable Local Testing requests one Windows administrator approval. Test Install Here validates again before running winget install --manifest. Verify Installation checks the Winget ID, then falls back to the exact MSI ProductCode or installed application name when Winget does not retain the local manifest ID."] = "Habilitar pruebas locales solicita una aprobación de administrador. Probar instalación vuelve a validar antes de ejecutar winget install --manifest. Verificar instalación comprueba el ID y, si Winget no lo conserva, usa el ProductCode MSI exacto o el nombre instalado.",
		["Open Installation Tests"] = "Abrir pruebas de instalación",
		["Use Windows Sandbox when available"] = "Usar Windows Sandbox cuando esté disponible",
		["Sandbox install runs Microsoft's official SandboxTest.ps1 in a disposable environment. Sandbox install + uninstall also verifies removal before the Sandbox closes. The first run can take several minutes while Microsoft dependencies are prepared. A manifest using elevationProhibited must use Test Install Here instead because Microsoft's Sandbox runs Winget as Administrator."] = "La instalación en Sandbox ejecuta el SandboxTest.ps1 oficial en un entorno desechable. Instalar y desinstalar también verifica la eliminación antes de cerrar. La primera ejecución puede tardar varios minutos mientras prepara las dependencias. Un manifiesto con elevationProhibited debe usar Probar instalación aquí porque Sandbox ejecuta Winget como Administrador.",
		["Open Sandbox Test"] = "Abrir prueba en Sandbox",
		["Submit directly from Test Center"] = "Enviar directamente desde el Centro de pruebas",
		["After all four required tests pass, choose Submit to Winget at the bottom of the Test Center steps. It opens Microsoft's WingetCreate workflow for sign-in and pull-request creation. The GitHub token stays in Windows Credential Manager."] = "Cuando superes las cuatro pruebas, elige Enviar a Winget en el Centro de pruebas. Se abre el flujo oficial de WingetCreate para iniciar sesión y crear la solicitud. El token permanece en el Administrador de credenciales.",
		["Do not use a leading v in the version, a release web-page URL instead of the direct asset URL, a hash from a different file, or the wrong architecture. For ZIP packages, review NESTED TYPE and ZIP CONTENTS. Reattach and inspect the exact published file whenever it changes."] = "No uses una v inicial, una página de versión en lugar de la URL directa, un hash de otro archivo ni una arquitectura incorrecta. En ZIP revisa TIPO INTERNO y CONTENIDO ZIP. Vuelve a adjuntar e inspeccionar el archivo exacto cuando cambie."
	};

	public static bool IsSupported(string language) => AvailableLanguages.Any(item =>
		item.Code.Equals(language, StringComparison.OrdinalIgnoreCase));

	public static int IndexOf(string language)
	{
		int index = AvailableLanguages.ToList().FindIndex(item => item.Code.Equals(language, StringComparison.OrdinalIgnoreCase));
		return index >= 0 ? index : 0;
	}

	public static string CodeAt(int index) => index >= 0 && index < AvailableLanguages.Count
		? AvailableLanguages[index].Code
		: "en-US";

	public static string Translate(string english, string language)
	{
		IReadOnlyDictionary<string, string>? translations = GetTranslations(language);
		if (translations is null) return english;
		if (TryGetTranslation(translations, language, english, out string translated)) return translated;
		(string requiredPrefix, string optionalPrefix, string requiredWord) = StudioAdditionalTranslations.Grammar(language);
		foreach ((string EnglishPrefix, string LocalizedPrefix) in new[]
		{
			("Required. ", requiredPrefix),
			("Optional. ", optionalPrefix)
		})
		{
			if (!english.StartsWith(EnglishPrefix, StringComparison.Ordinal)) continue;
			string body = english[EnglishPrefix.Length..];
			bool period = body.EndsWith(".", StringComparison.Ordinal);
			if (period) body = body[..^1];
			string localizedBody = TryGetTranslation(translations, language, body, out string localized) ? localized : body;
			return LocalizedPrefix + localizedBody + (period ? "." : string.Empty);
		}
		int prefixLength = 0;
		while (prefixLength < english.Length && char.IsDigit(english[prefixLength])) prefixLength++;
		if (prefixLength > 0 && prefixLength < english.Length && char.IsWhiteSpace(english[prefixLength]))
		{
			while (prefixLength < english.Length && char.IsWhiteSpace(english[prefixLength])) prefixLength++;
			string action = english[prefixLength..];
			if (TryGetTranslation(translations, language, action, out translated)) return english[..prefixLength] + translated;
		}
		const string requiredSuffix = "  * Required";
		if (english.EndsWith(requiredSuffix, StringComparison.Ordinal))
		{
			string field = english[..^requiredSuffix.Length];
			return (TryGetTranslation(translations, language, field, out string localized) ? localized : field) + "  * " + requiredWord;
		}
		const string requiredMarker = " *";
		if (english.EndsWith(requiredMarker, StringComparison.Ordinal))
		{
			string field = english[..^requiredMarker.Length];
			return (TryGetTranslation(translations, language, field, out string localized) ? localized : field) + requiredMarker;
		}
		const string returnSuffix = ". The Studio will return you to the correct page.";
		if (english.EndsWith(returnSuffix, StringComparison.Ordinal))
		{
			string message = english[..^returnSuffix.Length];
			if (TryGetTranslation(translations, language, message, out string localizedMessage)
				&& TryGetTranslation(translations, language, returnSuffix[2..], out string localizedSuffix))
				return localizedMessage + ". " + localizedSuffix;
		}
		return english;
	}

	public static bool HasCompleteTranslation(string english, string language)
	{
		IReadOnlyDictionary<string, string>? translations = GetTranslations(language);
		if (translations is null) return language.Equals("en-US", StringComparison.OrdinalIgnoreCase);
		if (TryGetTranslation(translations, language, english, out _)) return true;
		foreach (string prefix in new[] { "Required. ", "Optional. " })
		{
			if (!english.StartsWith(prefix, StringComparison.Ordinal)) continue;
			string body = english[prefix.Length..].TrimEnd('.');
			return TryGetTranslation(translations, language, body, out _);
		}
		int prefixLength = 0;
		while (prefixLength < english.Length && char.IsDigit(english[prefixLength])) prefixLength++;
		if (prefixLength > 0 && prefixLength < english.Length && char.IsWhiteSpace(english[prefixLength]))
		{
			while (prefixLength < english.Length && char.IsWhiteSpace(english[prefixLength])) prefixLength++;
			return TryGetTranslation(translations, language, english[prefixLength..], out _);
		}
		const string requiredSuffix = "  * Required";
		if (english.EndsWith(requiredSuffix, StringComparison.Ordinal))
			return TryGetTranslation(translations, language, english[..^requiredSuffix.Length], out _);
		const string requiredMarker = " *";
		if (english.EndsWith(requiredMarker, StringComparison.Ordinal))
			return TryGetTranslation(translations, language, english[..^requiredMarker.Length], out _);
		const string returnSuffix = ". The Studio will return you to the correct page.";
		if (english.EndsWith(returnSuffix, StringComparison.Ordinal))
			return TryGetTranslation(translations, language, english[..^returnSuffix.Length], out _)
				&& TryGetTranslation(translations, language, returnSuffix[2..], out _);
		return false;
	}

	private static IReadOnlyDictionary<string, string>? GetTranslations(string language) =>
		language.Equals("es-ES", StringComparison.OrdinalIgnoreCase) ? Spanish : StudioAdditionalTranslations.Get(language);

	private static bool TryGetTranslation(
		IReadOnlyDictionary<string, string> translations,
		string language,
		string english,
		out string translated)
	{
		IReadOnlyDictionary<string, string>? runtime = StudioRuntimeTranslations.Get(language);
		if (runtime is not null && runtime.TryGetValue(english, out string? runtimeValue) && runtimeValue is not null)
		{
			translated = runtimeValue;
			return true;
		}
		if (translations.TryGetValue(english, out string? resourceValue) && resourceValue is not null)
		{
			translated = resourceValue;
			return true;
		}
		translated = english;
		return false;
	}
}

internal sealed record StudioLanguage(string Code, string DisplayName);
