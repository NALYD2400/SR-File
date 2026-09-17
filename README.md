<div align="center">
  <img src="sr_logo.png" alt="SR File Logo" width="220" />
  <h1>SR File — Modern GTA V Archive, Modding & Asset Suite</h1>
  <p><strong>Suite logicielle nouvelle génération pour l'ingénierie d'archives, l'analyse d'assets, le mapping 3D et le modding avancé de Grand Theft Auto V (PC & Gen9).</strong></p>

  <p>
    <img src="https://img.shields.io/badge/Tauri-v2.0-blue?logo=tauri&logoColor=white" alt="Tauri v2" />
    <img src="https://img.shields.io/badge/React-19-61dafb?logo=react&logoColor=black" alt="React 19" />
    <img src="https://img.shields.io/badge/.NET-8.0-512bd4?logo=dotnet&logoColor=white" alt=".NET 8" />
    <img src="https://img.shields.io/badge/Rust-2021-orange?logo=rust&logoColor=white" alt="Rust" />
    <img src="https://img.shields.io/badge/TypeScript-5.6-3178c6?logo=typescript&logoColor=white" alt="TypeScript" />
    <img src="https://img.shields.io/badge/TailwindCSS-v4-38bdf8?logo=tailwindcss&logoColor=white" alt="Tailwind CSS" />
    <img src="https://img.shields.io/badge/Platform-Windows_x64-0078d7?logo=windows&logoColor=white" alt="Windows x64" />
    <img src="https://img.shields.io/badge/License-MIT-green" alt="MIT License" />
  </p>
</div>

---

## 📑 Sommaire

1. [Aperçu du Projet](#-aperçu-du-projet)
2. [Architecture Technique](#-architecture-technique)
3. [Modules & Fonctionnalités](#-modules--fonctionnalités)
   - [Hub Dashboard & Monitoring Système](#1-hub-dashboard--monitoring-système)
   - [RPF Explorer & Archive Browser](#2-rpf-explorer--archive-browser)
   - [Texture Studio & YTD Viewer](#3-texture-studio--ytd-viewer)
   - [Audio Lab & AWC Player / Decoder](#4-audio-lab--awc-player--decoder)
   - [Mod Manager & OIV Installer](#5-mod-manager--oiv-installer)
   - [Gen9 Next-Gen Converter](#6-gen9-next-gen-converter-ps5xbox-series-vers-pc)
   - [Map & Project Editor (YMAP / YTYP)](#7-map--project-editor-ymap--ytyp)
   - [Monaco Code Editor & Split-Diff](#8-monaco-code-editor--split-diff)
   - [Virtualized Hex Viewer & Type Inspector](#9-virtualized-hex-viewer--type-inspector)
   - [Jenkins JOAAT 32-bit Hasher & Lookup](#10-jenkins-joaat-32-bit-hasher--reverse-lookup)
   - [AES / NG Cryptography Diagnostics](#11-aes--ng-cryptography-diagnostics)
   - [GXT2 & Text Search / Compilateur](#12-gxt2--text-search--compilateur-de-chaînes)
4. [Référence des API Backend (.NET 8 Sidecar)](#-référence-des-api-backend-net-8-sidecar)
5. [Installation & Prérequis](#-installation--prérequis)
6. [Compilation & Lancement](#-compilation--lancement)
7. [Tests Automatisés & Assurance Qualité](#-tests-automatisés--assurance-qualité)
8. [Licence & Crédits](#-licence--crédits)

---

## 🚀 Aperçu du Projet

**SR File** est une suite logicielle moderne conçue pour transcender les outils traditionnels de la communauté GTA V (tels qu'OpenIV et CodeWalker classique). Elle combine une interface utilisateur ultra-réactive conçue avec **Tauri v2**, **React 19** et **Tailwind CSS**, adossée à un backend headless ultra-performant en **C# .NET 8** (`SRFile.Sidecar`) intégrant l'intégralité du moteur de bas niveau de **CodeWalker.Core**.

### Points Forts
- **Zéro surcharge Electron** : Empreinte mémoire minime (< 80 Mo RAM pour l'UI native) et affichage à plus de 60 FPS.
- **Indexation et Cache In-Memory O(1)** : Navigation instantanée dans les archives géantes de GTA V (`x64a.rpf` à `x64w.rpf`, packs DLC, mods) sans recalcul d'arborescence à chaque clic.
- **Extraction par lot & Export Zip natif** : Exportation récursive de dossiers entiers ou de listes d'entrées directement vers le disque ou en archive `.zip`.
- **Support hybride PC & Gen9** : Déchiffrement AES/NG, extraction des formats Next-Gen (PlayStation 5 et Xbox Series) et rétro-conversion vers le PC.
- **Diagnostics en temps réel** : Métriques avancées sur l'utilisation mémoire (Working Set, Private Bytes, GC Collections) et l'état des caches.

---

## 🏗 Architecture Technique

```
┌────────────────────────────────────────────────────────────────────────┐
│                        SR File Desktop (Tauri v2)                      │
│                                                                        │
│   ┌────────────────────────────────────────────────────────────────┐   │
│   │                 Interface Utilisateur (React 19)               │   │
│   │   • TanStack Query (Gestion d'état serveur & cache frontend)   │   │
│   │   • Tailwind CSS (Design System sombre SR élégant)             │   │
│   │   • Monaco Editor & Virtualized Hex Grid                       │   │
│   │   • Lucide Icons & Composants modulaires                       │   │
│   └────────────────────────────────┬───────────────────────────────┘   │
│                                    │ IPC / Loopback HTTP (5890)        │
│   ┌────────────────────────────────▼───────────────────────────────┐   │
│   │                 Tauri Core Runtime (Rust 2021)                 │   │
│   │   • Gestion du cycle de vie des fenêtres borderless            │   │
│   │   • Sélecteurs de fichiers et dossiers natifs Windows OS       │   │
│   │   • Supervision & démarrage automatique du Sidecar .NET 8      │   │
│   └────────────────────────────────┬───────────────────────────────┘   │
└────────────────────────────────────┼───────────────────────────────────┘
                                     │ HTTP REST Loopback
┌────────────────────────────────────▼───────────────────────────────────┐
│              SRFile.Sidecar (ASP.NET Core Minimal API / .NET 8)        │
│                                                                        │
│   ┌────────────────────────────────────────────────────────────────┐   │
│   │                      Couche Services                           │   │
│   │   • RpfService          (Cache indexé, Batch Zip, Extraction) │   │
│   │   • ModManagerService   (Parsing OIV, Détection conflits)      │   │
│   │   • Gen9ConverterService(Transcodage textures & shaders)       │   │
│   │   • CryptoService       (JOAAT Jenkins, Dictionnaire, AES/NG)  │   │
│   │   • ProjectEditorService(Gestion .cwproj, YMAP, YTYP)          │   │
│   │   • SystemService       (Métriques RAM, GC, Taux de hit cache) │   │
│   │   • TextService         (Parsing GXT2, recherche globale, build)│  │
│   └────────────────────────────────┬───────────────────────────────┘   │
│                                    │ Interopérabilité directe          │
│   ┌────────────────────────────────▼───────────────────────────────┐   │
│   │                         CodeWalker.Core                        │   │
│   │   • RpfFile / RpfEntry   • YtdFile (DDSIO, DXT/BC)             │   │
│   │   • AwcFile (Audio PCM)  • Gxt2File & GlobalText               │   │
│   │   • GTA5Keys & Crypto    • JenkHash (Jenkins 32-bit JOAAT)     │   │
│   └────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 🧩 Modules & Fonctionnalités

### 1. Hub Dashboard & Monitoring Système
- **Centre de contrôle unifié** : Vue d'ensemble de l'état de l'application, détection du dossier GTA V (Steam, Epic Games, Rockstar Launcher ou chemin personnalisé), statut des clés cryptographiques AES et NG.
- **Métriques système en temps réel** : Moniteur de télémétrie affichant la mémoire vive utilisée (Working Set en Mo), la mémoire privée (Private Bytes), l'état du garbage collector .NET (Gen 0, Gen 1, Gen 2, taille du tas managé), le nombre de threads et handles actifs.
- **Statistiques de cache RPF** : Suivi du nombre d'archives chargées, du volume d'entrées indexées, du nombre de cache hits/misses et du pourcentage d'efficacité.
- **Raccourcis rapides** : Accès instantané à l'explorateur, au gestionnaire de mods, au convertisseur et aux outils de hachage.

### 2. RPF Explorer & Archive Browser
- **Navigation arborée haute performance** : Parcours fluide des archives `.rpf` hiérarchiques et des sous-archives imbriquées.
- **Indexation O(1)** : Chaque archive ouverte est indexée dans un cache concurrent en mémoire, éliminant les latences de parcours disque lors des allers-retours dans l'arborescence.
- **Recherche instantanée** : Filtrage en temps réel dans les dizaines de milliers d'entrées avec détection automatique du type de ressource.
- **Batch Export & Extraction de Dossiers** :
  - Extraction de sous-dossiers complets avec conservation de l'arborescence sur le disque.
  - Export direct en archive `.zip` compressée en streaming.
  - Extraction multiple par sélection de fichiers.
- **Gestion du Cache RPF** : Possibilité de décharger individuellement une archive ou de purger complètement le cache mémoire (`/api/rpf/close`, `/api/rpf/cache/clear`).

### 3. Texture Studio & YTD Viewer
- **Décodeur Texture Dictionary (.ytd)** : Visualisation immédiate de l'ensemble des textures contenues dans un dictionnaire YTD sans conversion préalable.
- **Rendu Direct3D / DXT vers PNG** : Décompression à la volée des formats de compression de textures DirectX (DXT1, DXT3, DXT5, ATI1, ATI2/BC5, BC7) en flux PNG standard pour l'affichage WebGL/HTML5.
- **Inspecteur de Mipmaps** : Visualisation niveau par niveau des paliers de résolution (Mip 0, Mip 1, Mip 2...).
- **Export polyvalent** : Sauvegarde unitaire ou par lot sous forme d'images `.png` haute définition ou de conteneurs natifs `.dds` pour les logiciels de graphisme (Photoshop, GIMP, Substance).

### 4. Audio Lab & AWC Player / Decoder
- **Explorateur d'Audio Wave Container (.awc)** : Ouverture des banques audio de GTA V (effets sonores d'armes, véhicules, dialogues d'ambiance, stations de radio).
- **Lecteur audio intégré** : Décodage PCM / ADPCM en flux WAV 16-bit temps réel avec contrôle de lecture, barre de progression et réglage de volume.
- **Support multicanal** : Prise en charge des pistes mono, stéréo et multicanaux surround 5.1 / 7.1.
- **Métadonnées audio détaillées** : Fréquence d'échantillonnage (Sample Rate), débit, durée précise et taille en octets.

### 5. Mod Manager & OIV Installer
- **Détection & Gestion des Mods** : Répertoire centralisé des mods installés (`mods`, `plugins`, scripts ASI, packages DLC).
- **Inspecteur de Packages OIV** : Décompression et analyse du manifeste `assembly.xml` des archives OpenIV Package Installer (`.oiv`), prévisualisation des métadonnées (auteur, version, description) et des actions cibles avant installation.
- **File d'attente d'installation sécurisée** : Installation asynchrone non-bloquante avec suivi du pourcentage de progression.
- **Détection des conflits** : Identification des fichiers écrasés ou partagés entre plusieurs mods afin d'éviter les corruptions de jeu.

### 6. Gen9 Next-Gen Converter (PS5/Xbox Series vers PC)
- **Portage Nouvelle Génération** : Outil dédié à la conversion des assets issus des versions PS5 et Xbox Series X/S de GTA V vers le format PC compatible.
- **Transcodage intelligent** : Adaptation des formats de compression de textures next-gen et adaptation des déclarations de shaders selon la matrice `ShadersGen9Conversion.xml`.
- **Traitement récursif par lot** : Possibilité de traiter des dossiers entiers de fichiers convertibles avec options de remplacement ou de copie de secours.
- **Journalisation détaillée** : Console de logs intégrée avec suivi de chaque fichier traité en temps réel.

### 7. Map & Project Editor (YMAP / YTYP)
- **Gestionnaire de Projets CodeWalker (.cwproj)** : Création, ouverture et sauvegarde de projets de mapping structurés avec associations de fichiers `.ymap`, `.ytyp` et `.ybn`.
- **Inspecteur d'Entités 3D** :
  - Paramétrage spatial avec contrôles vectoriels de précision style Blender (Position X, Y, Z ; Rotation Quaternion X, Y, Z, W et Angles d'Euler en degrés).
  - Gestion des distances d'affichage (LOD Distance, Child LOD Distance), des Flags et des identifiants uniques (GUID).
- **Synchronisation d'Archetypes** : Consultation des propriétés physiques et visuelles des objets (Boîte englobante BB Min/Max, Sphère englobante BS Centre/Rayon, dictionnaires de textures et de collision).
- **Import / Export XML OpenIV** : Conversion bidirectionnelle entre le format XML de métadonnées et la structure interne pour intégration directe dans vos serveurs FiveM ou mods solo.

### 8. Monaco Code Editor & Split-Diff
- **Éditeur de Code Professionnel** : Intégration du moteur **Monaco Editor** (au cœur de Visual Studio Code) pour visualiser et éditer les fichiers texte et XML du jeu (`.xml`, `.meta`, `.pso`, `.dat`, `.json`, `.ini`).
- **Coloration syntaxique & Numérotation de lignes** : Thème sombre haute lisibilité adapté aux structures XML verbeuses de GTA V.
- **Mode Split-Diff côte à côte** : Comparateur visuel de différences permettant de confronter une version modifiée à une version originale vanilla pour auditer rapidement les changements.
- **Sauvegarde directe** : Écriture sur disque avec préservation de l'encodage UTF-8.

### 9. Virtualized Hex Viewer & Type Inspector
- **Gestion des Fichiers Massifs** : Visionneuse hexadécimale virtualisée avec streaming par blocs (chunk paging) capable d'afficher des binaires de plusieurs gigaoctets sans aucune chute de framerate.
- **Inspecteur de Données Interactif** : Cliquez sur n'importe quel octet pour décoder instantanément la valeur sous-jacente en :
  - Entiers 8, 16, 32 et 64 bits (signés et non signés).
  - Flottants simple et double précision (Float32, Double64).
  - Représentation binaire (bits), hexadécimale et chaînes ASCII / UTF-8.
- **Navigation par Offset** : Saut direct à un offset spécifique en hexadécimal ou décimal.

### 10. Jenkins JOAAT 32-bit Hasher & Reverse Lookup
- **Calculateur Jenkins One-At-A-Time** : Générateur de hash JOAAT standard utilisé par le moteur RAGE de GTA V pour identifier les véhicules, armes, props, os (bones), modèles et variables.
- **Formats multiples** : Affichage simultané en UInt32, Int32 (signé) et Hexadécimal (`0x...`).
- **Traitement par lot (Batch)** : Hash instantané de listes de centaines de chaînes en un seul clic.
- **Dictionnaire inversé haute performance** : Recherche dans la base de données intégrée pour retrouver le nom d'origine associé à un hash inconnu.

### 11. AES / NG Cryptography Diagnostics
- **Diagnostic Cryptographique GTA V** : Analyse de l'état de chargement des clés de chiffrement maîtresses (clés AES PC, clés AES consoles, clés NG, tables de déchiffrement).
- **Inspection d'en-tête RPF** : Analyse de l'en-tête d'un fichier RPF pour vérifier sa validité, sa version (RPF7) et son algorithme de chiffrement (`OPEN`, `AES`, `NG`).
- **Configuration dynamique de clés** : Possibilité de spécifier une clé AES personnalisée en hexadécimal ou base64 pour les versions de test ou modifiées du jeu.

### 12. GXT2 & Text Search / Compilateur de Chaînes
- **Exploration des Tables de Texte (.gxt2)** : Lecture des tables de sous-titres, notifications de mission, noms de véhicules et textes d'interface (`american.gxt2`, `french.gxt2`, `global.gxt2`).
- **Recherche globale dans l'archive** : Moteur de recherche capable de scanner tous les fichiers `.gxt2` d'un RPF pour trouver une chaîne ou un hash donné.
- **Export & Édition en texte clair** : Conversion du binaire GXT2 en fichier texte lisible au format `0xHASH = Mon Texte` ou `MON_LABEL = Mon Texte`.
- **Compilateur GXT2 intégré** : Génération directe de binaires `.gxt2` valides à partir de vos fichiers texte pour vos traductions ou serveurs personnalisés.

---

## 🔌 Référence des API Backend (.NET 8 Sidecar)

Le backend expose une API REST locale sur le port configurable `5890` (ou paramétré via `--port`) :

| Méthode | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/status` | Statut général, version, dossier GTA V et état des clés |
| `GET` | `/api/system/metrics` | Diagnostic mémoire (RAM, GC, tas), processeur, threads et stats de cache |
| `POST` | `/api/system/clear-cache` | Purge du cache des archives et libération forcée de la mémoire |
| `POST` | `/api/config/gta-folder` | Configuration et chargement des clés depuis le dossier GTA V |
| `POST` | `/api/rpf/open` | Chargement et indexation d'une archive `.rpf` |
| `GET` | `/api/rpf/info` | Métadonnées d'une archive RPF ouverte |
| `GET` | `/api/rpf/entries` | Liste des fichiers et dossiers dans un répertoire RPF |
| `GET` | `/api/rpf/search` | Recherche d'entrées par motif de nom |
| `GET` | `/api/rpf/file` | Téléchargement brut d'un fichier extrait d'un RPF |
| `GET` | `/api/rpf/file/text` | Extraction et lecture d'un fichier sous forme de texte |
| `GET` | `/api/rpf/cache/stats` | Statistiques détaillées du cache d'archives (hits, misses, ratio) |
| `POST` | `/api/rpf/cache/clear` | Purge complète du cache mémoire des archives |
| `POST` | `/api/rpf/close` | Fermeture et libération des handles d'une archive RPF |
| `POST` | `/api/rpf/extract-folder` | Extraction d'un dossier complet (sur disque ou archive `.zip`) |
| `POST` | `/api/rpf/extract-batch` | Extraction par lot d'une liste de fichiers (sur disque ou `.zip`) |
| `GET` | `/api/rpf/textures` | Liste des textures contenues dans un fichier `.ytd` |
| `GET` | `/api/rpf/texture/png` | Extraction et conversion d'une texture en PNG à la volée |
| `GET` | `/api/rpf/texture/dds` | Extraction d'une texture au format natif DDS |
| `GET` | `/api/rpf/audio/streams` | Liste des pistes audio d'une archive `.awc` |
| `GET` | `/api/rpf/audio/wav` | Décodage et streaming d'une piste audio en WAV |
| `GET` | `/api/text/search` | Recherche GTA V universelle (par texte, hex 0x..., hash ou dictionnaire) |
| `POST` | `/api/text/parse-gxt2` | Parsing et extraction d'un binaire GXT2 (upload ou base64) |
| `GET` | `/api/text/gxt2` | Extraction et parsing d'une table de texte GXT2 depuis un RPF |
| `GET` | `/api/text/gxt2/search` | Recherche dans un fichier GXT2 spécifique |
| `GET` | `/api/text/search-rpf` | Recherche textuelle dans tous les GXT2 d'un RPF |
| `POST` | `/api/text/gxt2/export-text` | Formatage d'entrées GXT2 en texte brut |
| `POST` | `/api/text/build-gxt2` | Compilation de texte clair ou JSON vers binaire `.gxt2` téléchargeable |
| `POST` | `/api/text/gxt2/build` | Alias de compilation binaire GXT2 |
| `GET` | `/api/mods` | Liste des mods installés |
| `POST` | `/api/mods/toggle` | Activation / désactivation d'un mod |
| `POST` | `/api/mods/inspect-oiv` | Analyse du manifeste d'une archive `.oiv` |
| `POST` | `/api/mods/install` | Mise en file d'attente d'installation d'un package |
| `POST` | `/api/gen9/convert` | Démarrage d'une tâche de conversion Gen9 |
| `GET` | `/api/gen9/status` | Statut et journaux de la conversion Gen9 en cours |
| `POST` | `/api/crypto/joaat` | Calcul du hash JOAAT Jenkins 32-bit |
| `POST` | `/api/crypto/joaat/batch` | Calcul groupé de hashes JOAAT |
| `GET` | `/api/crypto/joaat/lookup` | Résolution inverse d'un hash depuis le dictionnaire |
| `GET` | `/api/project/current` | Synthèse du projet CodeWalker en cours |
| `POST` | `/api/project/open` | Chargement d'un projet `.cwproj` |
| `POST` | `/api/project/save` | Sauvegarde du projet et de ses fichiers YMAP |
| `GET` | `/api/file/read-text` | Lecture d'un fichier texte arbitraire sur le disque |
| `POST` | `/api/file/save-text` | Écriture d'un fichier texte arbitraire sur le disque |
| `GET` | `/api/file/read-bytes` | Lecture fragmentée (chunk) par offset pour la vue hexadécimale |
| `POST` | `/api/shutdown` | Arrêt propre du sidecar lors de la fermeture de l'application |

---

## 💻 Installation & Prérequis

### Prérequis Système
- **Système d'exploitation** : Windows 10 ou Windows 11 (64-bit).
- **.NET 8 SDK** : [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- **Node.js** : Version 18 LTS ou supérieure (Node.js 20+ recommandé) avec `npm`.
- **Rust & Cargo** : Version stable de Rust (`rustup toolchain install stable`).
- **Outils C++ de compilation** : Microsoft Visual Studio C++ Build Tools (inclus dans Visual Studio Community).
- **Copie légale de GTA V** (PC ou fichiers d'archives consoles pour la conversion Gen9).

### Clonage du Répertoire
```bash
git clone https://github.com/NALYD2400/SR-File.git
cd SR-File
```

### Installation des Dépendances Frontend
```bash
cd SRFile.Desktop
npm install
cd ..
```

### Restauration des Projets .NET
```bash
dotnet restore CodeWalker.sln
```

---

## 🛠 Compilation & Lancement

### 1. Mode Développement (Rechargement à chaud)
Lancez l'application en mode développement complet (Tauri v2 + Vite + Sidecar .NET) :

```bash
cd SRFile.Desktop
npm run tauri dev
```
*Le serveur Vite se lance sur `http://localhost:1420` et le sidecar .NET 8 démarre automatiquement sur `http://127.0.0.1:5890`.*

### 2. Compilation de Production (Installateur & Exécutable)
Pour produire l'exécutable autonome et l'installateur Windows natif (.exe / .msi) :

```bash
# 1. Compilation du sidecar en binaire autonome optimisé
dotnet publish SRFile.Sidecar/SRFile.Sidecar.csproj -c Release -r win-x64 --self-contained false -o SRFile.Desktop/src-tauri/binaries/

# 2. Construction de l'application Tauri
cd SRFile.Desktop
npm run tauri build
```
Les fichiers générés se trouveront dans `SRFile.Desktop/src-tauri/target/release/bundle/`.

---

## 🧪 Tests Automatisés & Assurance Qualité

Le projet dispose d'une suite exhaustive de tests unitaires et d'intégration validant tous les modules backend :

```bash
dotnet test SRFile.Sidecar.Tests/SRFile.Sidecar.Tests.csproj
```

### Couverture des Tests :
- **PngEncoderTests** : Encodage RGBA, respect du format IHDR/IEND, gestion des mips de textures.
- **RpfServiceTests** : Création d'archives RPF réelles à la volée, arborescences imbriquées, extraction de fichiers XML et métadonnées.
- **RpfCacheAndBatchExtractionTests** : Vérification des hits/misses de cache, fermeture d'archives, extraction récursive de répertoires vers le disque, génération de ZIP compressés.
- **SystemServiceTests** : Validation de la télémétrie mémoire (Working Set, tas GC, handles, threads, identification OS).
- **TextServiceTests** : Aller-retour complet de compilation/décompilation GXT2 (magic signature, hashes, labels de texte, recherche multi-fichiers).
- **CryptoServiceTests** : Algorithme Jenkins JOAAT 32-bit, recherches inversées dans le dictionnaire, validation des clés AES.
- **ModManagerTests** : Parsing de manifestes OIV, détection des conflits de fichiers.
- **ProjectEditorTests** : Import/export XML YMAP, entités 3D, coordonnées vectorielles.

---

## 📄 Licence & Crédits

- **Auteur principal & Direction du projet** : Dylan ([NALYD2400](https://github.com/NALYD2400))
- **Base moteur RAGE & CodeWalker** : Inspiré et dérivé des travaux pionniers de Dexyfex et de la communauté de recherche GTA V.
- **Licence** : Projet distribué sous licence MIT. Consultez le fichier [LICENSE](LICENSE) pour plus d'informations.

<div align="center">
  <sub>Développé avec passion pour la communauté des créateurs, mappeurs et moddeurs GTA V.</sub>
</div>