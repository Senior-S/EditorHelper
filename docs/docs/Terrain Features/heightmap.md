---
title: "Heightmap features"
sidebar_label: "Heightmap Features"
sidebar_position: 0
slug: /Heightmap
---

# Heightmap features

The **Heightmap Importer** adds file-based terrain height import tools to the Terrain Height tab.

## Import heightmap

Use **Import Heightmap** to apply a height source to the current map terrain.

- **Whole Map** imports across the existing map terrain bounds.
- **Current Tile** imports only into the selected terrain tile.
- **Path** accepts PNG/JPG images, square 16-bit raw files, or Unturned `.heightmap` files.
- **Min Height** and **Max Height** map the source black-to-white height range into world height values.
- **Smoothing** applies box blur passes before the import.
- **Flip Y** flips the source vertically before sampling.

The importer only writes to existing terrain tiles in the selected scope. After import, EditorHelper2 refreshes terrain LOD and marks the level dirty so the changes can be saved.

