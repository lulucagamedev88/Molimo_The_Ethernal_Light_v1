---
name: "audio-tools-setup-and-overview"
description: "Use this skill whenever the user asks to 'open Audio Tools', 'edit an audio clip in Unity', understand what Audio Tools can do, or choose the correct Audio Tools workflow. Use audio-tools-preview-and-select-audio for detailed waveform selection and playback work."
metadata:
  asset: "Audio Tools"
  publisher: "Impossible Robert"
  asset-version: "1.2.0"
  skill-version: "1.0.0"
  unity: "2022.3"
  render-pipelines: "Not applicable"
  category: "Tools/Audio"
  asset-store-url: "https://assetstore.unity.com/packages/tools/audio/audio-tools-356424"
  documentation-url: "https://www.wetzold.com/tools/audiotools/docs/"
  support-url: "https://discord.com/invite/uzeHzEMM4B"
  last-verified: "2026-08-29"
---

# Audio Tools Setup and Overview

Open the installed Audio Tools editor, confirm that a supported source is available, and route the request to the narrowest editing skill.

## When to use this skill

- Opening Audio Tools from the Tools menu or an audio asset.
- Checking the installed package and editor-only workflow.
- Choosing between preview, processing, and WAV export.

## Prerequisites

- Audio Tools is imported into a Unity 2022.3 or newer project.
- The operation runs in the Unity Editor; Audio Tools does not add a player runtime workflow.

## Quick start

1. Open `Tools > Audio Tools > Audio Editor` for an empty editor.
2. To load a project clip directly, select it and use `Assets > Edit Audio`.
3. Confirm the waveform and source metadata load before editing.
4. Continue with the sibling skill that matches preview, processing, or export.

## Workflows

### Workflow: Open a selected project clip

1. Select one supported audio asset in the Project window.
2. Choose `Assets > Edit Audio` and wait for waveform processing.
3. Confirm the editor identifies the source and enables playback.

### Workflow: Open the standalone workspace

1. Choose `Tools > Audio Tools > Audio Editor`.
2. Use the empty-state action to select or load a source.
3. Keep the original asset unchanged until an explicit export.

## Verification

- The Audio Editor window is open.
- The intended source name, duration, channels, and waveform are visible.
- There are no Console errors from loading or preview preparation.
- The next workflow has been chosen without editing the package source.

## API quick reference

- Menu: `Tools > Audio Tools > Audio Editor`.
- Project context menu: `Assets > Edit Audio`.
- `IAudioSource` and `FileAudioSource` represent materialized editor audio sources.
- `AudioManager`, `AudioProcessor`, and `AudioWaveformRenderer` are the supported editor API surfaces.

## Common issues

- **The context action is disabled:** Select exactly one supported audio asset or open the standalone editor.
- **The waveform is still empty:** Wait for cancellable preview processing and check the Console for source-loading errors.
- **The window opens after a domain reload:** Reload restoration does not restart playback; start preview explicitly.

## Boundaries

- Do not add runtime audio behavior unless the user explicitly asks for a separate runtime implementation.
- Do not edit third-party audio utility code.
- Do not assume the package is installed under a particular `Assets` path.
- Use audio-tools-export-processed-audio before writing a new file.
