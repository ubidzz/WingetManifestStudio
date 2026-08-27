namespace ManifestUpdater;

internal static class StudioFrenchFullTranslations
{
	private static readonly string[] Translations =
	[
		"Winget Manifest Studio", // 001
		"Activer et tester l’installation", // 002
		"Tests locaux activés", // 003
		"Facultatif : tester dans Sandbox", // 004
		"LES TESTS D’INSTALLATION NÉCESSITENT VOTRE CONFIRMATION", // 005
		"English", // 006
		"Español", // 007
		"Alias", // 008
		"Copyright", // 009
		"URL du copyright", // 010
		"URL d’achat", // 011
		"Canal", // 012
		"Type interne partagé", // 013
		"Contenu ZIP partagé", // 014
		"Protocoles", // 015
		"Extensions de fichier", // 016
		"Architectures non prises en charge", // 017
		"Codes de réussite supplémentaires", // 018
		"Nom de famille du package", // 019
		"Comportement de réparation", // 020
		"L’installateur ferme le terminal", // 021
		"Emplacement d’installation obligatoire", // 022
		"Mise à niveau explicite obligatoire", // 023
		"Afficher les avertissements d’installation", // 024
		"Interdire la commande de téléchargement", // 025
		"Les binaires de l’archive dépendent de PATH", // 026
		"Option silencieuse", // 027
		"Silencieux avec progression", // 028
		"Option interactive", // 029
		"Option d’emplacement d’installation", // 030
		"Option de journal", // 031
		"Option de mise à niveau", // 032
		"Option personnalisée", // 033
		"Option de réparation", // 034
		"Accords", // 035
		"Liens de documentation", // 036
		"Fonctionnalités restreintes", // 037
		"Marchés autorisés", // 038
		"Marchés exclus", // 039
		"Codes de retour attendus", // 040
		"Arguments Winget non pris en charge", // 041
		"Emplacement d’installation par défaut", // 042
		"Fichiers installés", // 043
		"Type d’authentification", // 044
		"Ressource Entra", // 045
		"Étendue Entra", // 046
		"Champs de langue supplémentaires", // 047
		"Champs d’installateur supplémentaires", // 048
		"La désactivation est plus sûre. Activez uniquement si HTTPS n’est pas disponible.", // 049
		"Accès complet à WingetCreate pour New, Update, New-Locale, Update-Locale, Submit, Show, Token, Settings, Cache, Info et DSC. Les commandes s’exécutent directement sans cmd.exe. Celles qui posent des questions ouvrent une vraie console WingetCreate afin que vous puissiez répondre.", // 050
		"SHA-256 DE LA SIGNATURE MSIX", // 051
		"Valeurs communes à tous les fichiers manifeste.", // 052
		"Informations affichées aux utilisateurs par le Gestionnaire de package Windows.", // 053
		"Utilisez des liens HTTPS publics lorsqu’ils sont disponibles.", // 054
		"Champs facultatifs du schéma Winget actuel. Laissez un champ vide s’il ne s’applique pas.", // 055
		"Winget utilise ces options de ligne de commande pour les actions de l’installateur. Les types Inno, Nullsoft, MSI et MSIX connus ne nécessitent généralement aucune valeur personnalisée.", // 056
		"Des formats simples sur une ligne créent le YAML imbriqué pour vous. Utilisez une entrée par ligne et laissez toute la zone vide si elle ne s’applique pas.", // 057
		"Règles facultatives pour les packages qui dépendent d’un autre package Winget ou d’une fonctionnalité Windows, des fonctionnalités MSIX ou de restrictions de marché.", // 058
		"Décrivez les résultats inhabituels de l’installateur et les fichiers installés sans écrire de YAML. Ces valeurs sont facultatives et la validation officielle vérifie leur schéma.", // 059
		"Seules les sources privées protégées par Entra ID utilisent ces champs. Les packages du dépôt communautaire doivent laisser les trois champs vides.", // 060
		"Utilisez ces zones uniquement pour les champs de schéma qui n’ont pas encore de contrôle guidé. Les clés personnalisées existantes restent conservées même si ces zones restent vides.", // 061
		"Format obligatoire : Publisher.Application (exemple : Contoso.Sample)", // 062
		"Ne mettez pas de v au début", // 063
		"Généralement en-US", // 064
		"Choisissez un dossier vide ou un dossier de manifestes existant", // 065
		"Exemple : MIT, Proprietary, Freeware", // 066
		"Séparé par des virgules", // 067
		"Alias de commandes séparés par des virgules. Conservés pendant les mises à jour", // 068
		"Affiché à l’utilisateur après l’installation", // 069
		"Version de schéma utilisée par le YAML généré ; 1.12.0 est recommandée pour les soumissions à la communauté Microsoft Winget", // 070
		"Nom public du produit affiché dans Winget", // 071
		"Entreprise ou personne qui publie l’application", // 072
		"Auteur d’origine de l’application s’il diffère de l’éditeur", // 073
		"Nom de la licence, par exemple MIT, GPL-3.0, Proprietary ou Freeware", // 074
		"Une phrase claire qui explique le rôle de l’application", // 075
		"Une description publique plus longue de l’application et de son objectif", // 076
		"Un alias court et facile à saisir pour rechercher le package", // 077
		"Mots de recherche séparés par des virgules ; n’ajoutez pas de symbole #", // 078
		"Noms des commandes installées par le package, séparés par des virgules", // 079
		"Page d’accueil HTTPS publique de l’éditeur", // 080
		"Page HTTPS publique où les utilisateurs peuvent obtenir de l’aide", // 081
		"Page HTTPS publique de politique de confidentialité", // 082
		"Page d’accueil HTTPS publique de cette application", // 083
		"Page HTTPS publique contenant les conditions de licence", // 084
		"Avis de copyright affiché avec le package", // 085
		"Page HTTPS publique contenant les informations de copyright", // 086
		"Page d’achat HTTPS publique si l’application est payante", // 087
		"Page HTTPS publique des notes de cette version précise", // 088
		"Modifications apportées dans cette version précise", // 089
		"Instructions affichées par Winget après l’installation", // 090
		"Exemple : stable ou beta", // 091
		"Exemple : en-US", // 092
		"Séparé par des virgules ; généralement Windows.Desktop", // 093
		"Exemple : 10.0.19041.0", // 094
		"Chemins dans le ZIP séparés par des points-virgules ; ajoutez | commande après un fichier portable si nécessaire", // 095
		"Protocoles URL séparés par des virgules", // 096
		"Séparé par des virgules, sans points", // 097
		"Nombres entiers séparés par des virgules", // 098
		"AAAA-MM-JJ", // 099
		"Type partagé facultatif ; les lignes inspectées gardent leur propre type. Laissez vide si les installateurs sont différents", // 100
		"Type réel de l’installateur dans un package ZIP", // 101
		"Chemins partagés dans un ZIP ; séparez-les par des points-virgules et ajoutez | commande uniquement pour les fichiers portables", // 102
		"Portée partagée facultative ; choisissez user pour un compte, machine pour tout l’ordinateur, ou laissez vide si elle varie selon l’installateur", // 103
		"Modes pris en charge séparés par des virgules : interactive, silent, silentWithProgress", // 104
		"Instruction facultative pour les mises à niveau ; laissez vide sauf si l’installateur exige un comportement particulier", // 105
		"Indique si l’installateur exige une élévation ; laissez vide si vous ne savez pas", // 106
		"Protocoles URL enregistrés par l’application, séparés par des virgules", // 107
		"Extensions de fichier enregistrées par l’application, séparées par des virgules et sans points", // 108
		"Architectures qui ne peuvent pas utiliser cet installateur, séparées par des virgules", // 109
		"Codes de sortie supplémentaires considérés comme réussis, séparés par des virgules", // 110
		"Nom de famille de package Microsoft Store ou MSIX", // 111
		"Date de publication au format AAAA-MM-JJ", // 112
		"Méthode de réparation par Winget : modify, uninstaller ou installer", // 113
		"Entrez true uniquement si l’installation ferme le terminal de l’utilisateur", // 114
		"Entrez true uniquement si un emplacement d’installation personnalisé est obligatoire", // 115
		"Entrez true si Winget ne doit pas effectuer la mise à niveau automatiquement", // 116
		"Entrez true si Winget doit afficher les avertissements de l’installateur", // 117
		"Entrez true si winget download doit être bloqué", // 118
		"Pour les archives, entrez true si les commandes extraites dépendent de PATH", // 119
		"Argument de l’installateur pour une installation entièrement silencieuse", // 120
		"Argument pour une installation silencieuse avec progression", // 121
		"Argument de l’installateur qui force l’interface interactive", // 122
		"Modèle d’argument pour un dossier d’installation personnalisé", // 123
		"Modèle d’argument pour le chemin du fichier journal", // 124
		"Argument utilisé spécialement pendant les mises à niveau", // 125
		"Argument que Winget doit ajouter à chaque commande d’installation", // 126
		"Argument de l’installateur utilisé pour la réparation", // 127
		"Un accord par ligne au format libellé | URL HTTPS | texte de l’accord", // 128
		"Un lien de documentation par ligne au format libellé | URL HTTPS", // 129
		"Une dépendance Winget par ligne au format Publisher.Application | version minimale", // 130
		"Noms des fonctionnalités Windows requises par l’application, séparés par des virgules", // 131
		"Fonctionnalités MSIX requises par le package, séparées par des virgules", // 132
		"Fonctionnalités MSIX restreintes, séparées par des virgules", // 133
		"Codes de marché où l’installation est autorisée, séparés par des virgules", // 134
		"Codes de marché où l’installation est bloquée, séparés par des virgules", // 135
		"Un résultat d’installateur par ligne au format code | réponse Winget | URL d’aide HTTPS facultative", // 136
		"Choisissez log, location ou les deux uniquement si l’installateur ne prend pas en charge ces arguments Winget", // 137
		"Dossier habituel de l’application installée ; les variables d’environnement comme %ProgramFiles% sont autorisées", // 138
		"Un fichier installé par ligne au format chemin relatif | type de fichier | SHA-256 facultatif | argument facultatif | nom affiché facultatif", // 139
		"Authentification d’une source privée ; laissez vide pour les packages du dépôt communautaire", // 140
		"Ressource Microsoft Entra utilisée par une source privée", // 141
		"Étendue Microsoft Entra utilisée par une source privée", // 142
		"YAML de langue avancé uniquement ; la plupart des utilisateurs doivent laisser ce champ vide", // 143
		"YAML d’installateur avancé uniquement ; la plupart des utilisateurs doivent laisser ce champ vide", // 144
		"Laissez vide si cette valeur ne s’applique pas ou est inconnue", // 145
		"Prêt", // 146
		"Choisissez la langue utilisée par le Studio. Les données du package et le YAML généré ne sont jamais traduits ni modifiés.", // 147
		"La recherche de mise à jour nécessite votre attention : {0}", // 148
		"Téléchargement et vérification de la mise à jour sélectionnée...", // 149
		"Téléchargement de la mise à jour vérifiée du Studio depuis GitHub...", // 150
		"Téléchargement... {0}%", // 151
		"Téléchargement et vérification de {0} : {1}%", // 152
		"La mise à jour vérifiée est prête. Winget Manifest Studio va se fermer pour terminer la mise à jour.", // 153
		"Le téléchargement de la mise à jour a été annulé. Aucun fichier de l’application n’a été modifié.", // 154
		"Créez une soumission Winget sans modifier le YAML à la main.", // 155
		"Créez un nouvel ensemble de trois manifestes ou mettez à jour un ensemble existant en toute sécurité. Les fichiers locaux donnent le vrai hachage SHA-256 ; les URL publiques indiquent à Winget où les télécharger.", // 156
		"LOCAL D’ABORD\n\nLe jeton GitHub reste dans le Gestionnaire d’informations d’identification Windows\nAucun manifeste écrasé sans sauvegarde\nAucun installateur téléchargé sans confirmation", // 157
		"Créez un package vide, chargez des fichiers YAML déjà présents sur cet ordinateur, ou saisissez l’identifiant d’un package Winget existant pour télécharger ses manifestes actuels dans une nouvelle copie de travail.", // 158
		"Charger des manifestes existants", // 159
		"Importer un package Winget existant", // 160
		"Créer un nouveau projet", // 161
		"Saisissez vous-même les informations du package ou collez l’URL publique d’une version GitHub. L’importateur remplit uniquement les champs vides et demande votre accord avant de télécharger les fichiers pris en charge pour calculer les hachages et les inspecter.", // 162
		"Importer une version GitHub", // 163
		"Ouvrir les détails du package", // 164
		"Choisissez les fichiers locaux MSI, EXE, MSIX, APPX, ZIP, d’application portable ou de police que vous publierez. Le Studio lit ces fichiers exacts et calcule leur SHA-256. Saisissez ensuite l’URL publique de téléchargement de chaque fichier.", // 165
		"Ouvrir Installateurs et hachages", // 166
		"L’aperçu construit les trois manifestes en mémoire. L’enregistrement ne les écrit qu’après validation et conserve des sauvegardes horodatées des fichiers existants.", // 167
		"Ouvrir Aperçu et envoi", // 168
		"Ouvrir les outils officiels", // 169
		"Ouvrez le guide intégré pour débutants qui explique les champs, les identifiants d’installateur, les hachages, la validation et l’envoi.", // 170
		"Maintenir Winget Manifest Studio à jour", // 171
		"La page de démarrage recherche la dernière version stable GitHub après l’ouverture de la fenêtre. Une copie installée utilise StudioSetup.msi ; une copie portable remplace uniquement WingetManifestStudio.exe. Rien n’est téléchargé ni installé tant que vous n’avez pas choisi le bouton de mise à jour et confirmé.", // 172
		"Chaque zone ci-dessous est modifiable. Le chargement d’un dossier lit uniquement ses fichiers YAML ; il ne télécharge jamais les installateurs et ne modifie jamais les manifestes.", // 173
		"Champs de package avancés facultatifs", // 174
		"La plupart des débutants n’ont pas besoin de remplacer le comportement de l’installateur, de définir des options personnalisées ou d’écrire du YAML avancé. Ouvrez cette section uniquement si la documentation de l’installateur ou un manifeste existant exige l’une de ces valeurs.", // 175
		"1 Ajoutez chaque fichier de version exact. 2 Collez son URL HTTPS publique directe. 3 Inspectez-le pour remplir le hachage et les métadonnées. 4 Vérifiez les URL après la publication. L’architecture, le type et la portée restent visibles à côté de l’URL et peuvent être corrigés dans leurs listes.", // 176
		"1 Ajouter les fichiers de version", // 177
		"2 Saisir l’URL publique", // 178
		"3 Inspecter et remplir la sélection", // 179
		"4 Vérifier les URL publiques", // 180
		"VÉRIFIER ET ENREGISTRER EN SÉCURITÉ", // 181
		"Utilisez l’unique action mise en évidence ci-dessous. La vérification ne modifie aucun fichier avant que vous choisissiez Enregistrer, et les manifestes existants sont sauvegardés avant leur remplacement.", // 182
		"LISTE DE VÉRIFICATION", // 183
		"Le Studio déverrouille les étapes dans le bon ordre.", // 184
		"1  Aperçu", // 185
		"Construit le YAML proposé en mémoire", // 186
		"2  Enregistrer en sécurité", // 187
		"Crée des sauvegardes avant de remplacer des fichiers", // 188
		"3  Valider", // 189
		"Exécute le validateur Winget officiel", // 190
		"4  Tester et envoyer", // 191
		"Continue dans le centre de tests guidé", // 192
		"OPTIONS D’AFFICHAGE\r\nLa vérification en langage clair reste sélectionnée par défaut.", // 193
		"Afficher le YAML technique", // 194
		"Afficher la vérification en langage clair", // 195
		"Ouvrir le dossier de sauvegarde", // 196
		"VÉRIFICATION EN LANGAGE CLAIR", // 197
		"Corriger les informations du package", // 198
		"Le Studio vous ramènera à la bonne page.", // 199
		"OBLIGATOIRE · L’aperçu reste verrouillé jusqu’à la correction", // 200
		"OBLIGATOIRE · Les tests restent verrouillés jusqu’à la correction", // 201
		"La version du package est obligatoire et ne doit pas commencer par v", // 202
		"Le nom du package est obligatoire", // 203
		"L’éditeur est obligatoire", // 204
		"La description courte est obligatoire", // 205
		"La licence est obligatoire", // 206
		"Choisissez un dossier de sortie des manifestes", // 207
		"Ajoutez au moins un installateur", // 208
		"Ouvrir le champ à corriger", // 209
		"Corriger le problème de validation", // 210
		"Le résultat en langage clair ci-dessous indique le problème et l’endroit à corriger. Générez ensuite un nouvel aperçu et enregistrez de nouveau.", // 211
		"ARRÊT · L’envoi reste verrouillé jusqu’à la réussite de la validation", // 212
		"Ouvrir les champs à corriger", // 213
		"Prévisualiser les modifications proposées", // 214
		"Construit en mémoire les modifications exactes des manifestes et les explique ci-dessous. Aucun fichier n’est écrit.", // 215
		"SÛR · L’aperçu ne modifie aucun fichier", // 216
		"Enregistrer les manifestes vérifiés", // 217
		"Écrit le YAML vérifié dans le dossier de sortie après avoir créé des sauvegardes récupérables des fichiers existants.", // 218
		"PROTÉGÉ · Les manifestes existants sont d’abord sauvegardés", // 219
		"Valider avec Winget", // 220
		"Exécute le validateur Winget de Microsoft sur une copie temporaire propre. Le package n’est pas installé.", // 221
		"SÛR · La validation ne modifie pas les manifestes enregistrés", // 222
		"Continuer vers le centre de tests", // 223
		"Exécutez le contrôle préalable, testez l’installation, vérifiez le résultat et envoyez depuis un seul écran guidé.", // 224
		"SUIVANT · Les tests et l’envoi continuent sans revenir ici", // 225
		"Prêt à envoyer dans le centre de tests", // 226
		"Toutes les vérifications et tous les tests d’installation obligatoires ont réussi. L’action d’envoi est prête dans le centre de tests.", // 227
		"PRÊT · WingetCreate de Microsoft gère l’envoi", // 228
		"WINGET A SIGNALÉ UN PROBLÈME   •   RIEN N’A ÉTÉ ENVOYÉ", // 229
		"APERÇU PRÊT   •   RIEN N’A ÉTÉ ENREGISTRÉ", // 230
		"ENREGISTRÉ EN SÉCURITÉ   •   PRÊT POUR LA VALIDATION OFFICIELLE", // 231
		"VALIDATION RÉUSSIE   •   PRÊT POUR LE CENTRE DE TESTS", // 232
		"TOUTES LES VÉRIFICATIONS ET TOUS LES TESTS D’INSTALLATION ONT RÉUSSI", // 233
		"Ouvrir le centre de tests pour envoyer", // 234
		"TESTER ET TERMINER", // 235
		"Suivez la ligne de progression, puis utilisez l’unique action mise en évidence ci-dessous. Le Studio déverrouille chaque test dans le bon ordre et autorise l’envoi lorsque les quatre ont réussi.", // 236
		"LISTE DES VÉRIFICATIONS OBLIGATOIRES", // 237
		"Elles sont effectuées automatiquement dans l’ordre.", // 238
		"1  Contrôle préalable sûr", // 239
		"Vérifications du manifeste, du hachage, de la signature et du dépôt", // 240
		"2  Tests locaux", // 241
		"Paramètre Windows à activer une seule fois", // 242
		"3  Test d’installation", // 243
		"Installe cette version exacte avec Winget", // 244
		"4  Résultat installé", // 245
		"Confirme la version installée", // 246
		"DIAGNOSTICS FACULTATIFS\r\nDétails supplémentaires uniquement — ce ne sont pas des étapes obligatoires.", // 247
		"Vérifier la configuration de Winget", // 248
		"Installation uniquement dans Sandbox", // 249
		"Installation et désinstallation dans Sandbox", // 250
		"RÉSULTATS ET INSTRUCTIONS", // 251
		"Réparer la configuration de test Winget", // 252
		"Le Gestionnaire de package Windows n’est pas prêt. Exécutez la vérification de configuration pour afficher les instructions exactes de réparation.", // 253
		"SÛR · Vérifie uniquement Winget sans rien modifier", // 254
		"Vérifie le YAML, les hachages, les signatures, la validation officielle Winget et l’existence éventuelle de ce package.", // 255
		"SÛR · Rien ne sera installé ni modifié", // 256
		"Autoriser les tests de manifestes locaux", // 257
		"Windows exige une approbation administrateur unique avant que Winget puisse installer un manifeste depuis cet ordinateur.", // 258
		"CONFIGURATION UNIQUE · Approuvez l’invite Windows", // 259
		"Tester l’installation de cette version", // 260
		"Exécute winget install --manifest avec les fichiers générés exacts. Vérifiez la console de l’installateur, puis fermez-la.", // 261
		"CONFIRMATION OBLIGATOIRE · Ceci installe un logiciel sur ce PC", // 262
		"Confirmer le résultat installé", // 263
		"Vérifie l’identifiant du package Winget, puis l’identité MSI ou le nom de l’application installée si nécessaire.", // 264
		"SÛR · La vérification ne réinstalle pas le package", // 265
		"Vérifier l’installation", // 266
		"Tous les tests ont réussi — prêt à envoyer", // 267
		"Démarrez l’envoi officiel WingetCreate de Microsoft sans revenir à la page de vérification.", // 268
		"PRÊT · WingetCreate gère la connexion et la création de la demande de tirage", // 269
		"Dossier de sauvegarde", // 270
		"Aucune sauvegarde pour le moment", // 271
		"Test d’installation et de désinstallation dans Sandbox", // 272
		"Ce guide explique chaque écran et les informations requises par Winget. Vous pouvez le lire à tout moment ; les boutons vous conduisent uniquement à l’écran décrit.", // 273
		"Démarrer ou ouvrir un projet de manifeste", // 274
		"Pour une première version, choisissez Nouveau projet. Pour une mise à jour, chargez un dossier YAML local ou choisissez Importer un package Winget existant et saisissez son identifiant exact. L’importation du dépôt télécharge les manifestes les plus récents dans un dossier de travail séparé et n’écrase jamais un dossier de manifestes existant.", // 275
		"Accéder aux détails du package", // 276
		"Saisir l’identité du package", // 277
		"L’identifiant du package est le nom Winget permanent, généralement Publisher.Application. Saisissez d’abord l’éditeur et le nom du package, puis utilisez Suggérer un ID si vous le souhaitez. La version ne commence pas par v. Ne modifiez pas l’identifiant lors des mises à jour.", // 278
		"Modifier l’identité du package", // 279
		"Compléter les informations publiques du package", // 280
		"Le nom du package, l’éditeur, la licence et la description courte sont obligatoires. Saisissez-les ou utilisez Importer une version GitHub depuis Démarrer. L’importateur remplit uniquement les champs vides et demande avant de télécharger temporairement les fichiers pris en charge. Les champs guidés facultatifs créent les dépendances, accords, documents, codes de retour, règles de marché et données de détection sans modifier manuellement le YAML.", // 281
		"Modifier les informations du package", // 282
		"FICHIERS D’INSTALLATION ET LIENS DE TÉLÉCHARGEMENT", // 283
		"Winget télécharge depuis une URL publique, mais le Studio utilise le fichier local correspondant pour calculer la valeur SHA-256 fiable.", // 284
		"Ajouter le fichier de version exact", // 285
		"Choisissez Ajouter les fichiers de version pour chaque installateur publié. Sélectionnez le même fichier MSI, EXE, MSIX, APPX, bundle, ZIP, application portable ou police qui sera envoyé. Utilisez une ligne pour chaque architecture, portée ou variante. Aucune architecture x64 n’est supposée.", // 286
		"Saisir son URL HTTPS publique", // 287
		"Collez l’URL directe de téléchargement de chaque installateur, pas une page Web avec un bouton de téléchargement. L’URL doit rester publique et télécharger exactement le fichier local de la ligne. Les URL de fichiers de versions GitHub conviennent.", // 288
		"Saisir les URL de téléchargement", // 289
		"Inspecter et vérifier l’installateur publié", // 290
		"Inspecter et compléter calcule le SHA-256, indique si le fichier est signé ou non, et détecte les indices MSI, MSIX, Inno, NSIS, WiX Burn, Squirrel, Velopack, InstallShield, Advanced Installer et EXE auto-extractible. Les EXE/MSI non signés sont acceptés avec un avertissement ; les packages MSIX/APPX exigent toujours leur signature. Les ZIP affichent les chemins internes. Vérifier les URL publiques prouve que le fichier publié correspond au hachage.", // 291
		"Inspecter les fichiers d’installation", // 292
		"TYPES DE PACKAGE PARTICULIERS", // 293
		"Les EXE portables peuvent ressembler à des installateurs EXE normaux ; choisissez portable dans la ligne si nécessaire. Les packages de polices utilisent la racine de manifestes fonts séparée de Microsoft et ont des règles plus strictes. La prise en charge PWA varie selon le client Winget et la politique du dépôt ; vérifiez toujours la validation officielle et le test d’installation.", // 294
		"VÉRIFIER, ENREGISTRER ET PUBLIER", // 295
		"L’aperçu est votre contrôle de sécurité. Il crée le YAML proposé en mémoire sans écrire dans le dossier sélectionné.", // 296
		"Suivre l’état du projet, puis prévisualiser", // 297
		"Le panneau d’état compte les informations obligatoires restantes et marque les champs problématiques. Lorsqu’il indique PRÊT, choisissez Aperçu des modifications et vérifiez l’identifiant, les anciennes et nouvelles versions, les URL, architectures, types d’installateur, hachages et noms de fichier.", // 298
		"Vérifier l’aperçu", // 299
		"Enregistrer avec des sauvegardes récupérables", // 300
		"Choisissez Enregistrer les manifestes seulement après avoir vérifié l’aperçu. Les nouveaux fichiers sont créés dans le dossier de sortie. Les fichiers existants sont copiés dans un dossier .manifest-backups horodaté avant d’être remplacés.", // 301
		"Enregistrer ou valider", // 302
		"Valider avant l’envoi", // 303
		"Valider localement exécute le validateur Winget officiel sur une copie temporaire propre. S’il signale une erreur, corrigez le champ concerné et validez à nouveau. La validation ne modifie pas les manifestes enregistrés.", // 304
		"Ouvrir la validation", // 305
		"Exécuter l’étape 1 — Contrôle préalable sûr", // 306
		"Le centre de tests vérifie d’abord le fonctionnement de Winget, puis les hachages et signatures des fichiers joints, exécute la validation officielle et recherche l’identifiant exact dans Winget et microsoft/winget-pkgs. Il n’installe rien.", // 307
		"Exécuter les étapes 2, 3 et 4", // 308
		"Activer les tests locaux demande une approbation administrateur Windows unique. Tester l’installation ici valide de nouveau avant d’exécuter winget install --manifest. Vérifier l’installation contrôle l’identifiant Winget, puis utilise le ProductCode MSI exact ou le nom de l’application installée si Winget ne conserve pas l’identifiant local.", // 309
		"Ouvrir les tests d’installation", // 310
		"Utiliser Windows Sandbox lorsqu’il est disponible", // 311
		"L’installation Sandbox exécute le script officiel SandboxTest.ps1 de Microsoft dans un environnement jetable. L’installation et désinstallation Sandbox vérifie aussi la suppression avant la fermeture. Le premier lancement peut prendre plusieurs minutes pour préparer les dépendances Microsoft. Un manifeste avec elevationProhibited doit utiliser Tester l’installation ici, car Sandbox exécute Winget en tant qu’administrateur.", // 312
		"Ouvrir le test Sandbox", // 313
		"Envoyer directement depuis le centre de tests", // 314
		"Après la réussite des quatre tests obligatoires, choisissez Envoyer à Winget en bas des étapes du centre de tests. Le flux WingetCreate officiel de Microsoft s’ouvre pour la connexion et la création de la demande de tirage. Le jeton GitHub reste dans le Gestionnaire d’informations d’identification Windows.", // 315
		"N’utilisez pas de v au début de la version, d’URL de page Web de version à la place de l’URL directe du fichier, de hachage provenant d’un autre fichier, ni de mauvaise architecture. Pour les ZIP, vérifiez TYPE INTERNE et CONTENU ZIP. Joignez et inspectez de nouveau le fichier publié exact chaque fois qu’il change.", // 316
	];

	public static readonly IReadOnlyDictionary<string, string> Values =
		StudioFullTranslationCatalog.Create(Translations, "fr-FR");
}
