---
title: "Map optimizer"
sidebar_label: "Map Optimizer"
sidebar_position: 4
slug: /Map-Optimizer
---

# Map optimizer

The **Mod Usage Optimizer Extension** adds an optimizer panel to the editor pause menu.
It builds a new optimized mod output folder from the installed mod assets used by the current map.

## Optimize mod usage

Enter an absolute output folder path, then click **Optimize Mod Usage**.

The optimizer:

- scans the current map for used object, resource, item spawn, and vehicle spawn assets from installed mods
- saves the level before exporting
- exports the used assets into the selected output folder
- patches saved object and resource references to the regenerated GUIDs
- writes an optimization report when finished
- creates a backup of the original patched map files in the optimizer output

## Options

- **Parallel bundle jobs** controls how many master bundles are trimmed at the same time. Values above `2` require confirmation because they can use more memory and disk bandwidth.
- **Metadata-only trim** skips streamed payload compaction. This is faster, but the optimized bundle output can be larger.

## Notes

Run the optimizer only after saving any map changes you want to keep. If assets referenced by the map are not installed locally, the optimizer skips those references and lists warnings in the final report.

