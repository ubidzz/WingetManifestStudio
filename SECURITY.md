# Security Policy

Winget Manifest Studio handles installer files, public URLs, generated YAML, elevated Winget settings, and submission authentication. Security reports are taken seriously.

## Supported Versions

Security fixes are provided for the latest published release and the current default branch.

| Version | Supported |
| --- | --- |
| Latest release | Yes |
| Current default branch | Yes |
| Older releases | Best effort; upgrade may be required |

## Report a Vulnerability Privately

Do **not** open a public issue for a vulnerability that could expose users, credentials, installer execution, or unreleased technical details.

Preferred reporting method:

1. Open the repository's [Security Advisories](https://github.com/ubidzz/WingetManifestStudio/security/advisories) page.
2. Choose **Report a vulnerability** when GitHub Private Vulnerability Reporting is available.
3. If that option is unavailable, contact the repository owner through the private contact method published on the [ubidzz GitHub profile](https://github.com/ubidzz). Do not include exploit details in a public message.

Include:

- The affected version or commit.
- A clear description of the problem and its possible impact.
- Reproduction steps or a minimal proof of concept.
- Whether administrator permission or user interaction is required.
- Relevant logs with tokens, usernames, paths, and other private data removed.
- Any suggested mitigation, if known.

## Security-Relevant Areas

Reports are especially useful for problems involving:

- Command or argument injection in Winget, WingetCreate, PowerShell, or installer workflows.
- YAML parsing, unsafe paths, directory traversal, or backup replacement.
- Incorrect installer hashes or public URL verification.
- Authenticode or application-package signature handling.
- Windows administrator elevation or local-manifest settings.
- GitHub token, Windows Credential Manager, certificate, or private-key exposure.
- Unintended installer execution or silent installation.
- Unsafe temporary files, logs, crash reports, or test artifacts.
- Loading a malicious manifest or project profile.

## What to Expect

- A good-faith report should receive an acknowledgement within seven days when practical.
- The maintainer may request additional information or a safe reproduction.
- Confirmed issues will be prioritized according to impact and exploitability.
- Public disclosure should wait until a fix or reasonable mitigation is available.
- Credit will be offered unless the reporter prefers to remain anonymous.

Response times are goals, not guarantees for this community-maintained project.

## Safe Research

Good-faith research should:

- Use systems, accounts, packages, and files you own or are authorized to test.
- Avoid accessing other people's data or credentials.
- Avoid disrupting GitHub, Winget, Microsoft services, or other users.
- Stop when a test could cause harm, install unwanted software, or require unauthorized access.
- Share enough information to reproduce the issue without publicly releasing a working exploit before remediation.

## Secrets and Personal Data

Never attach real GitHub tokens, certificate private keys, passwords, personal manifest folders, or unredacted Credential Manager data. Replace them with clearly marked placeholders.

If a secret is accidentally committed or posted, revoke or rotate it immediately. Removing it from Git history does not make the exposed credential safe again.

## Non-Security Bugs

Crashes, confusing validation messages, incorrect field guidance, and ordinary functional bugs that do not create a security impact may be reported through the repository's public issue tracker.
