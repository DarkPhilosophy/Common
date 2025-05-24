# Jurnal de modificări (Changelog)

## Română

### [1.0.2.0] - 2025-05-24

#### Adăugat
- Sistem de animații îmbunătățit pentru butoane cu suport pentru efecte multiple simultane
- Animații de pulsare și strălucire pentru butoanele albastre

#### Modificat
- Reducerea semnificativă a jurnalizării verbale în SoundPlayer pentru o experiență de depanare mai curată
- Utilizarea exclusivă a Logger din Common pentru jurnalizare, eliminând System.Diagnostics.Debug.WriteLine
- Îmbunătățirea gestionării transformărilor pentru animații mai fiabile
- Optimizarea performanței animațiilor prin prioritizarea corectă a efectelor

#### Remediat
- Remediat problema cu animațiile butonului btnReplaceSCHtoBER când devine albastru
- Remediat problema cu butonul Generate din CSVGenerator care dispărea după clic
- Remediat problema cu animațiile de creștere/micșorare care nu funcționau corect
- Eliminat avertismentul compilatorului pentru variabila 'assembly' neutilizată

### [1.0.1.0] - 2025-04-28

#### Adăugat
- Jurnalizare îmbunătățită în SoundPlayer pentru diagnosticarea problemelor de redare a sunetului
- Suport pentru fișier local settings.json în plus față de locația AppData
- Reorganizare completă a structurii bibliotecii în namespace-uri specializate

#### Modificat
- Mecanism îmbunătățit de redare a sunetului cu gestionare mai bună a erorilor și opțiuni de rezervă
- Capacități de jurnalizare îmbunătățite cu informații mai detaliate
- Mecanism de încărcare a configurației actualizat pentru a gestiona mai bine proprietățile lipsă
- Adăugat suport pentru framework-ul .NET 10.0
- Structură de directoare organizată pe funcționalități (Configuration, Logging, UI, Audio, Utilities)
- Namespace-uri actualizate pentru a reflecta noua structură organizațională

#### Remediat
- Remediate probleme de redare a sunetului prin adăugarea de jurnalizare detaliată și descoperire îmbunătățită a resurselor
- Remediate probleme de încărcare a assembly-urilor în SoundPlayer

## English

### [1.0.2.0] - 2025-05-24

#### Added
- Enhanced button animation system with support for multiple simultaneous effects
- Pulsing and glowing animations for blue buttons

#### Changed
- Significantly reduced verbose logging in SoundPlayer for a cleaner debugging experience
- Exclusive use of Logger from Common for logging, removing System.Diagnostics.Debug.WriteLine
- Improved transform handling for more reliable animations
- Optimized animation performance through proper effect prioritization

#### Fixed
- Fixed issue with btnReplaceSCHtoBER animations when it becomes blue
- Fixed issue with CSVGenerator's Generate button disappearing after click
- Fixed issue with scaling animations not working correctly
- Removed compiler warning for unused 'assembly' variable

### [1.0.1.0] - 2025-04-28

#### Added
- Enhanced logging in SoundPlayer to help diagnose sound playback issues
- Support for local settings.json file in addition to AppData location
- Complete reorganization of library structure into specialized namespaces

#### Changed
- Improved sound playback mechanism with better error handling and fallback options
- Enhanced logging capabilities with more detailed information
- Updated configuration loading mechanism to handle missing properties more gracefully
- Added support for .NET 10.0 framework
- Directory structure organized by functionality (Configuration, Logging, UI, Audio, Utilities)
- Updated namespaces to reflect the new organizational structure

#### Fixed
- Fixed sound playback issues by adding detailed logging and improved resource discovery
- Fixed assembly loading issues in SoundPlayer
