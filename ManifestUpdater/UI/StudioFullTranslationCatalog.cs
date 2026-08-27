namespace ManifestUpdater;

internal static class StudioFullTranslationCatalog
{
    private static readonly string[] English =
    [
        "Winget Manifest Studio", // 001
        "Enable & Test Install", // 002
        "Local Testing Enabled", // 003
        "Optional: Test in Sandbox", // 004
        "INSTALLATION TESTS REQUIRE YOUR CONFIRMATION", // 005
        "English", // 006
        "Español", // 007
        "Moniker", // 008
        "Copyright", // 009
        "Copyright URL", // 010
        "Purchase URL", // 011
        "Channel", // 012
        "Shared nested type", // 013
        "Shared ZIP contents", // 014
        "Protocols", // 015
        "File extensions", // 016
        "Unsupported architectures", // 017
        "Extra success codes", // 018
        "Package family name", // 019
        "Repair behavior", // 020
        "Installer aborts terminal", // 021
        "Install location required", // 022
        "Require explicit upgrade", // 023
        "Display install warnings", // 024
        "Prohibit download command", // 025
        "Archive binaries depend on PATH", // 026
        "Silent switch", // 027
        "Silent with progress", // 028
        "Interactive switch", // 029
        "Install-location switch", // 030
        "Log switch", // 031
        "Upgrade switch", // 032
        "Custom switch", // 033
        "Repair switch", // 034
        "Agreements", // 035
        "Documentation links", // 036
        "Restricted capabilities", // 037
        "Allowed markets", // 038
        "Excluded markets", // 039
        "Expected return codes", // 040
        "Unsupported Winget arguments", // 041
        "Default install location", // 042
        "Installed files", // 043
        "Authentication type", // 044
        "Entra resource", // 045
        "Entra scope", // 046
        "Additional locale fields", // 047
        "Additional installer fields", // 048
        "Off is safer. Enable only when HTTPS is unavailable.", // 049
        "Full WingetCreate access for New, Update, New-Locale, Update-Locale, Submit, Show, Token, Settings, Cache, Info, and DSC. Commands run directly without cmd.exe. Commands that ask questions open a real WingetCreate console so you can answer them.", // 050
        "MSIX SIGNATURE SHA-256", // 051
        "The values shared by every manifest file.", // 052
        "Shown to users by Windows Package Manager.", // 053
        "Use public HTTPS links when available.", // 054
        "Optional current Winget schema fields. Leave a field blank when it does not apply.", // 055
        "Winget uses these command-line switches for installer actions. Known Inno, Nullsoft, MSI, and MSIX types often need no custom values.", // 056
        "Friendly one-line formats create the nested YAML for you. Use one entry per line; leave the entire box blank when it does not apply.", // 057
        "Optional rules for packages that depend on another Winget package or Windows feature, MSIX capabilities, or market restrictions.", // 058
        "Describe uncommon installer results and installed files without writing YAML. These values are optional and official validation checks their schema.", // 059
        "Only private Entra ID secured sources use these fields. Community repository packages should leave all three blank.", // 060
        "Only use these boxes for schema fields that still have no guided control. Existing custom keys remain preserved even when these boxes stay blank.", // 061
        "Required format: Publisher.Application (example: Contoso.Sample)", // 062
        "Do not include a leading v", // 063
        "Usually en-US", // 064
        "Choose any empty folder or an existing manifest folder", // 065
        "Example: MIT, Proprietary, Freeware", // 066
        "Comma-separated", // 067
        "Comma-separated command aliases. Preserved during updates", // 068
        "Shown to the user after installation", // 069
        "Schema version used by the generated YAML; 1.12.0 is recommended for Microsoft Winget community submissions", // 070
        "The public product name users see in Winget", // 071
        "The company or person that publishes the application", // 072
        "The original application author when different from the publisher", // 073
        "The license name, such as MIT, GPL-3.0, Proprietary, or Freeware", // 074
        "One clear sentence explaining what the application does", // 075
        "A longer public explanation of the application and its purpose", // 076
        "A short command-friendly nickname used to find the package", // 077
        "Search words separated with commas; do not add # symbols", // 078
        "Command names installed by the package, separated with commas", // 079
        "Public HTTPS home page for the publisher", // 080
        "Public HTTPS page where users can get help", // 081
        "Public HTTPS privacy-policy page", // 082
        "Public HTTPS home page for this application", // 083
        "Public HTTPS page containing the license terms", // 084
        "Copyright notice shown with the package", // 085
        "Public HTTPS page containing copyright information", // 086
        "Public HTTPS purchase page when the application is paid", // 087
        "Public HTTPS page for this exact version's release notes", // 088
        "What changed in this exact release", // 089
        "Instructions Winget shows after installation", // 090
        "Example: stable or beta", // 091
        "Example: en-US", // 092
        "Comma-separated; usually Windows.Desktop", // 093
        "Example: 10.0.19041.0", // 094
        "Semicolon-separated paths inside the ZIP; add | command after a portable file when needed", // 095
        "Comma-separated URL protocols", // 096
        "Comma-separated, without dots", // 097
        "Comma-separated whole numbers", // 098
        "YYYY-MM-DD", // 099
        "Optional shared type; inspected rows keep their own type, so leave this blank for mixed installers", // 100
        "Real installer type inside a ZIP package", // 101
        "Shared paths inside a ZIP; separate paths with semicolons and add | command only for portable files", // 102
        "Optional shared scope; choose user for one account, machine for the whole computer, or leave blank when it varies by installer", // 103
        "Supported modes separated with commas: interactive, silent, silentWithProgress", // 104
        "Optional instruction for upgrades; leave blank unless the installer requires a specific behavior", // 105
        "Whether the installer requires elevation; leave blank when unknown", // 106
        "URL protocols registered by the app, separated with commas", // 107
        "File extensions registered by the app, separated with commas and without dots", // 108
        "Architectures that cannot use this installer, separated with commas", // 109
        "Extra successful installer exit codes, separated with commas", // 110
        "Microsoft Store or MSIX package family name", // 111
        "Public release date in YYYY-MM-DD format", // 112
        "How Winget repairs the app: modify, uninstaller, or installer", // 113
        "Enter true only if installation closes the user's terminal", // 114
        "Enter true only when a custom install location is mandatory", // 115
        "Enter true when Winget must not upgrade automatically", // 116
        "Enter true when Winget should show installer warnings", // 117
        "Enter true when winget download must be blocked", // 118
        "For archives, enter true when extracted commands depend on PATH", // 119
        "Installer argument for a completely silent installation", // 120
        "Installer argument for quiet installation with progress", // 121
        "Installer argument that forces the interactive interface", // 122
        "Installer argument template for a custom install folder", // 123
        "Installer argument template for a log-file path", // 124
        "Installer argument used specifically during upgrades", // 125
        "Argument Winget must add to every install command", // 126
        "Installer argument used for repair", // 127
        "One agreement per line using label | HTTPS URL | agreement text", // 128
        "One documentation link per line using label | HTTPS URL", // 129
        "One Winget dependency per line using Publisher.Application | minimum version", // 130
        "Windows feature names required by the application, separated with commas", // 131
        "MSIX capabilities required by the package, separated with commas", // 132
        "Restricted MSIX capabilities, separated with commas", // 133
        "Market codes where installation is allowed, separated with commas", // 134
        "Market codes where installation is blocked, separated with commas", // 135
        "One installer result per line using code | Winget response | optional HTTPS help URL", // 136
        "Choose log, location, or both only when the installer cannot support those Winget arguments", // 137
        "The usual installed application folder; environment variables such as %ProgramFiles% are allowed", // 138
        "One installed file per line using relative path | file type | optional SHA-256 | optional argument | optional display name", // 139
        "Authentication for a private source; community repository packages leave this blank", // 140
        "Microsoft Entra resource used by a private source", // 141
        "Microsoft Entra scope used by a private source", // 142
        "Advanced locale YAML only; most users should leave this blank", // 143
        "Advanced installer YAML only; most users should leave this blank", // 144
        "Leave blank when this value does not apply or is unknown", // 145
        "Ready", // 146
        "Choose the language used by the Studio. Package data and generated YAML are never translated or changed.", // 147
        "Update check needs attention: {0}", // 148
        "Downloading and verifying the selected update...", // 149
        "Downloading the verified Studio update from GitHub...", // 150
        "Downloading... {0}%", // 151
        "Downloading and checking {0}: {1}%", // 152
        "The verified update is ready. Winget Manifest Studio is closing so the update can finish.", // 153
        "The update download was canceled. No application files were changed.", // 154
        "Build a Winget submission without editing YAML by hand.", // 155
        "Create a new three-file manifest set or safely update an existing one. Local release files provide the real SHA-256 hash; public URLs tell Winget where users will download them.", // 156
        "LOCAL-FIRST\n\nGitHub token stays in Windows Credential Manager\nNo manifest overwritten without backup\nNo installer downloaded without confirmation", // 157
        "Create a blank package, load YAML files already on this computer, or enter an existing Winget package ID to download its current manifests into a new working copy.", // 158
        "Load existing manifests", // 159
        "Import existing Winget package", // 160
        "Create a new project", // 161
        "Enter package details yourself, or paste a public GitHub release URL. The importer fills only blank fields and asks before downloading supported release assets for hashes and installer inspection.", // 162
        "Import a GitHub release", // 163
        "Open Package Details", // 164
        "Choose the local MSI, EXE, MSIX, APPX, ZIP, portable app, or font files that you will upload. The Studio reads those exact files and calculates their SHA-256 hashes. Then enter the public download URL for each file.", // 165
        "Open Installers & Hashes", // 166
        "Preview builds all three manifests in memory. Save writes them only after validation and keeps timestamped backups of files that already exist.", // 167
        "Open Preview & Submit", // 168
        "Open Official Tools", // 169
        "Open the built-in beginner guide for field meanings, installer IDs, hashes, validation, and submission.", // 170
        "Keep Winget Manifest Studio up to date", // 171
        "The Start page checks the latest stable GitHub release after the window is already open. An installed copy uses StudioSetup.msi; a portable copy replaces only its WingetManifestStudio.exe. Nothing downloads or installs until you choose the update button and confirm.", // 172
        "Every box below is editable. Loading a folder reads its YAML files only; it never downloads installers or changes the manifests.", // 173
        "Optional advanced package fields", // 174
        "Most beginners do not need installer behavior overrides, custom switches, or raw advanced YAML. Open this section only when the installer documentation or an existing manifest requires one of these values.", // 175
        "1 Add each exact release file. 2 Paste its direct public HTTPS URL. 3 Inspect it to fill the hash and metadata. 4 Verify URLs after uploading. Architecture, type, and scope stay visible beside the URL and can be corrected from their dropdowns.", // 176
        "1 Add Release Files", // 177
        "2 Enter Public URL", // 178
        "3 Inspect & Fill Selected", // 179
        "4 Verify Public URLs", // 180
        "REVIEW AND SAVE SAFELY", // 181
        "Use the single highlighted action below. Review never changes files until you choose Save, and existing manifests are backed up before replacement.", // 182
        "REVIEW CHECKLIST", // 183
        "The Studio unlocks these in the correct order.", // 184
        "1  Preview", // 185
        "Builds the proposed YAML in memory", // 186
        "2  Save safely", // 187
        "Creates backups before replacing files", // 188
        "3  Validate", // 189
        "Runs the official Winget validator", // 190
        "4  Test & submit", // 191
        "Continues in the guided Test Center", // 192
        "VIEW OPTIONS\r\nThe plain-language review stays selected by default.", // 193
        "Show technical YAML", // 194
        "Show plain-language review", // 195
        "Open backup folder", // 196
        "PLAIN-LANGUAGE REVIEW", // 197
        "Fix the package information", // 198
        "The Studio will return you to the correct page.", // 199
        "REQUIRED · Preview stays locked until this is corrected", // 200
        "REQUIRED · Testing stays locked until this is corrected", // 201
        "Package Version is required and must not begin with v", // 202
        "Package Name is required", // 203
        "Publisher is required", // 204
        "Short Description is required", // 205
        "License is required", // 206
        "Choose a manifest output folder", // 207
        "Add at least one installer", // 208
        "Open the field to fix", // 209
        "Fix the validation problem", // 210
        "The plain-language result below names the problem and where to correct it. Then preview and save again.", // 211
        "STOP · Submission remains locked until validation passes", // 212
        "Open the fields to fix", // 213
        "Preview the proposed changes", // 214
        "Builds the exact manifest changes in memory and explains them below. No files are written.", // 215
        "SAFE · Preview does not change any files", // 216
        "Save the reviewed manifests", // 217
        "Writes the reviewed YAML to the output folder after creating recoverable backups of existing files.", // 218
        "PROTECTED · Existing manifests are backed up first", // 219
        "Validate with Winget", // 220
        "Runs Microsoft's Winget validator against a clean temporary copy. It does not install the package.", // 221
        "SAFE · Validation does not change the saved manifests", // 222
        "Continue to Test Center", // 223
        "Run safe preflight, test the installation, verify the result, and submit from one guided screen.", // 224
        "NEXT · Testing and submission continue without returning here", // 225
        "Ready to submit in Test Center", // 226
        "All required review and installation checks passed. The submission action is ready in Test Center.", // 227
        "READY · Microsoft's WingetCreate handles the submission", // 228
        "WINGET FOUND A PROBLEM   •   NOTHING WAS SUBMITTED", // 229
        "PREVIEW READY   •   NOTHING HAS BEEN SAVED", // 230
        "SAVED SAFELY   •   READY FOR OFFICIAL VALIDATION", // 231
        "VALIDATION PASSED   •   READY FOR TEST CENTER", // 232
        "ALL REVIEW AND INSTALLATION TESTS PASSED", // 233
        "Open Test Center to submit", // 234
        "TEST AND FINISH", // 235
        "Follow the progress line, then use the single highlighted action below. The Studio unlocks each test in the correct order and enables submission when all four pass.", // 236
        "REQUIRED CHECKLIST", // 237
        "These are completed automatically in order.", // 238
        "1  Safe preflight", // 239
        "Manifest, hash, signature, and repository checks", // 240
        "2  Local testing", // 241
        "One-time Windows setting", // 242
        "3  Test install", // 243
        "Installs this exact release through Winget", // 244
        "4  Installed result", // 245
        "Confirms the installed version", // 246
        "OPTIONAL DIAGNOSTICS\r\nExtra detail only — these are not required steps.", // 247
        "Check Winget setup", // 248
        "Sandbox install only", // 249
        "Sandbox install + uninstall", // 250
        "RESULTS AND INSTRUCTIONS", // 251
        "Repair the Winget test setup", // 252
        "Windows Package Manager is not ready. Run the setup check to see the exact repair instructions.", // 253
        "SAFE · This only checks Winget and changes nothing", // 254
        "Checks YAML, file hashes, signatures, official Winget validation, and whether this package already exists.", // 255
        "SAFE · Nothing will be installed or changed", // 256
        "Allow local manifest testing", // 257
        "Windows requires one administrator approval before Winget can install a manifest from this computer.", // 258
        "ONE-TIME SETUP · Approve the Windows prompt", // 259
        "Test install this release", // 260
        "Runs winget install --manifest with the exact generated files. Review the installer console, then close it.", // 261
        "CONFIRMATION REQUIRED · This installs software on this PC", // 262
        "Confirm the installed result", // 263
        "Checks the Winget package ID, then the MSI identity or installed application name when needed.", // 264
        "SAFE · Verification does not reinstall the package", // 265
        "Verify installation", // 266
        "All tests passed — ready to submit", // 267
        "Start Microsoft's official WingetCreate submission without returning to the Review page.", // 268
        "READY · WingetCreate handles sign-in and pull-request creation", // 269
        "Backup folder", // 270
        "No backups yet", // 271
        "Sandbox install and uninstall test", // 272
        "This guide explains every screen and the information Winget needs. You can read it at any time; the buttons only take you to the screen being described.", // 273
        "Start or open a manifest project", // 274
        "For a first release, choose New Project. For an update, load a local YAML folder or choose Import Existing Winget Package and enter its exact package ID. Repository import downloads the newest manifests into a separate working-copy folder and never overwrites an existing manifest folder.", // 275
        "Go to Package Details", // 276
        "Enter the package identity", // 277
        "Package Identifier is the permanent Winget name, normally Publisher.Application. Enter Publisher and Package Name first, then use Suggest Package ID if you want help. Package Version has no leading v. Keep the identifier unchanged for updates.", // 278
        "Edit Package Identity", // 279
        "Complete the public package information", // 280
        "Package Name, Publisher, License, and Short Description are required. Enter them yourself or use Import a GitHub Release from Start. The importer fills only blank fields and asks before temporarily downloading supported release assets. Optional guided fields create dependencies, agreements, documentation, return codes, market rules, and install-detection YAML without manual YAML editing.", // 281
        "Edit Package Information", // 282
        "INSTALLER FILES AND DOWNLOAD LINKS", // 283
        "Winget downloads from a public URL, but the Studio uses your matching local release file to calculate the trusted SHA-256 value.", // 284
        "Add the exact release file", // 285
        "Choose Add Release Files for every installer you publish. Select the same MSI, EXE, MSIX, APPX, bundle, ZIP, portable app, or font file that will be uploaded. Use one row for each architecture, scope, or installer variation. Nothing is assumed to be x64.", // 286
        "Enter its public HTTPS URL", // 287
        "Paste the direct download URL for each installer—not a web page containing a download button. The URL must remain public and must download the exact local file in that row. GitHub release asset URLs are suitable.", // 288
        "Enter Download URLs", // 289
        "Inspect and verify the published installer", // 290
        "Inspect & Fill Details calculates SHA-256, reports signed or unsigned status, and detects MSI, MSIX, Inno, NSIS, WiX Burn, Squirrel, Velopack, InstallShield, Advanced Installer, and self-extracting EXE clues. Unsigned EXE/MSI files are supported and shown as a warning; MSIX/APPX packages still require their package signature. ZIP files show nested paths. Verify Public URLs proves the published file matches the hash.", // 291
        "Inspect Installer Files", // 292
        "SPECIAL PACKAGE TYPES", // 293
        "Portable EXEs may look like normal EXE installers, so choose portable in the row when needed. Font packages use Microsoft's separate fonts manifest root and have stricter submission rules. PWA support can vary by Winget client and repository policy; always keep the official validation and install-test result.", // 294
        "REVIEW, SAVE, AND PUBLISH", // 295
        "The preview is your safety check. It creates the proposed YAML in memory without writing to the selected folder.", // 296
        "Follow Project Readiness, then preview", // 297
        "The readiness panel counts anything still required and marks problem fields. When it says READY, choose Preview Changes and review the identifier, old and new versions, URLs, architectures, installer types, hashes, and filenames.", // 298
        "Review the Preview", // 299
        "Save with recoverable backups", // 300
        "Choose Save Manifests only after the preview is correct. New files are created in the output folder. Existing files are copied into a timestamped .manifest-backups folder before they are replaced.", // 301
        "Save or Validate", // 302
        "Validate before submission", // 303
        "Validate Locally runs the official Winget validator against a clean temporary copy. If it reports an error, fix the related field and validate again. Validation does not modify the saved manifests.", // 304
        "Open Validation", // 305
        "Run test step 1 — Safe Preflight", // 306
        "The Test Center first checks whether Winget itself works, then rechecks attached file hashes and signatures, runs official validation, and searches Winget plus microsoft/winget-pkgs for the exact package identifier. It does not install anything.", // 307
        "Run test steps 2, 3, and 4", // 308
        "Enable Local Testing requests one Windows administrator approval. Test Install Here validates again before running winget install --manifest. Verify Installation checks the Winget ID, then falls back to the exact MSI ProductCode or installed application name when Winget does not retain the local manifest ID.", // 309
        "Open Installation Tests", // 310
        "Use Windows Sandbox when available", // 311
        "Sandbox install runs Microsoft's official SandboxTest.ps1 in a disposable environment. Sandbox install + uninstall also verifies removal before the Sandbox closes. The first run can take several minutes while Microsoft dependencies are prepared. A manifest using elevationProhibited must use Test Install Here instead because Microsoft's Sandbox runs Winget as Administrator.", // 312
        "Open Sandbox Test", // 313
        "Submit directly from Test Center", // 314
        "After all four required tests pass, choose Submit to Winget at the bottom of the Test Center steps. It opens Microsoft's WingetCreate workflow for sign-in and pull-request creation. The GitHub token stays in Windows Credential Manager.", // 315
        "Do not use a leading v in the version, a release web-page URL instead of the direct asset URL, a hash from a different file, or the wrong architecture. For ZIP packages, review NESTED TYPE and ZIP CONTENTS. Reattach and inspect the exact published file whenever it changes.", // 316
    ];

    public static IReadOnlyDictionary<string, string> Create(IReadOnlyList<string> translations, string language)
    {
        if (translations.Count != English.Length)
            throw new InvalidOperationException($"{language} has {translations.Count} full translations; expected {English.Length}.");
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        for (int index = 0; index < English.Length; index++) result[English[index]] = translations[index];
        return result;
    }
}

