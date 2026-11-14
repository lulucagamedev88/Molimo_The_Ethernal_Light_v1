---
name: "audio-tools-export-processed-audio"
description: "Use this skill whenever the user asks to 'save the edited audio', export a processed clip as WAV, choose a safe output name, or verify an Audio Tools export. Use audio-tools-trim-and-process-audio when processing has not yet been completed."
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
  last-verified: "2026-10-02"
---

# Export Processed Audio

Write verified processed samples to a new WAV file with the correct channel, frequency, bit-depth, and project import behavior.

## When to use this skill

- Exporting Audio Tools output as WAV.
- Avoiding accidental filename collisions.
- Choosing PCM or 32-bit float output and confirming Unity imports the file.

## Prerequisites

- Processed samples have been previewed and approved.
- The destination folder and overwrite behavior are explicit.

## Quick start

1. Choose a project-relative or explicit filesystem destination.
2. Generate a unique name unless the user explicitly confirms replacement.
3. Export with the source channel count and frequency. The Audio Editor's Save Copy action writes 16-bit PCM; use the public editor API for requested 32-bit float output.
4. Refresh or inspect the resulting asset and compare its duration and playback.

## Workflows

### Workflow: Export a project WAV

1. Choose a writable folder under the project's `Assets` directory.
2. Generate a non-colliding filename.
3. For the Audio Editor workflow, use Save Copy for 16-bit PCM. For 32-bit float output, call `AudioProcessor.ExportToWav(..., use32BitFloat: true)` from editor code with the approved processed samples; there is no bit-depth selector in the window.
4. Let Unity import the new file and verify its AudioClip metadata.

### Workflow: Verify exported output

1. Reload or preview the written WAV.
2. Compare duration, channels, frequency, and audible boundaries with the processed preview.
3. Report the final path and whether the original remained untouched.

## Verification

- The destination file exists and has a valid WAV header.
- Unity imports the file when it is under `Assets`.
- Duration, channels, frequency, and audible content match the processed preview.
- No existing file was replaced without explicit approval.

## API quick reference

- `AudioProcessor.GenerateUniqueFilename` avoids destination collisions.
- `AudioProcessor.ExportToWav(outputPath, samples, channels, frequency, use32BitFloat)` writes output.
- `AudioHeaderReader.ReadHeader` can verify file metadata quickly.
- Await `AudioManager.LoadAudioFromFile` to reload exported output for preview. Set `allowStreaming: false` when verification needs decoded samples through `AudioClip.GetData`.

## Common issues

- **The file is not visible in Unity:** Export under `Assets` and refresh the Asset Database.
- **The output plays at the wrong speed:** Use the original or explicitly requested sample frequency.
- **The export replaced an older file:** Use `GenerateUniqueFilename` unless replacement was explicitly confirmed.

## Boundaries

- Audio Tools exports WAV, not arbitrary compressed formats.
- Do not claim mastering, loudness compliance, or codec conversion.
- Do not overwrite a source or destination without explicit confirmation.
- Do not leave temporary preview files behind.
