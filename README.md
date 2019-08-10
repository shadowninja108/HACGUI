# HACGUI
A simple interface for extracting Nintendo Switch contents.
Still in development, so expect things to break.

# Features
- Derive keys without running CFW on your device
- Extract titles off NAND/SD/local files
  - NCAs
  - Romfs/exefs
  - NSP (Untested)
- Mount the following filesystems (with the help of the Dokan driver)
  - Romfs
  - Exefs
  - Saves
  - NAND partitions (SYSTEM, USER, PRODINFOF, ...)
- Automatically derive and save title keys when NAND is mounted
- Integrated payload, memloader and ini injecting

# Credits / Special Thanks
- Moosehunter for their amazing work on LibHac
- Simon for helping with certificate dumping
- shchmue for Lockpick, which this program relies on to get necessary keys off the console
- rajkosto for their amazing program memloader
- SMH, Kosmos, and ReSwitched Discord servers
- Everyone who has helped along the way :)
