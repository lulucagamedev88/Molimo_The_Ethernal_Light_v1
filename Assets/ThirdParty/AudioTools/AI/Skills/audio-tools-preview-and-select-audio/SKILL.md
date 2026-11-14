---
name: "audio-tools-preview-and-select-audio"
description: "Use this skill whenever the user asks to 'preview this clip', loop part of a waveform, zoom or pan through audio, or select an exact time range in Audio Tools. Use audio-tools-trim-and-process-audio when the selected range must be changed."
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

# Preview and Select Audio

Use Audio Tools playback and waveform controls to inspect a source and define an exact sample-aligned selection without changing the original file.

## When to use this skill

- Previewing a whole clip or a selected range.
- Looping a region while choosing edit boundaries.
- Zooming, panning, and translating between time, samples, pixels, and normalized positions.

## Prerequisites

- A supported source is loaded in the Audio Editor.
- Waveform preview processing has completed.

## Quick start

1. Start playback and confirm the playhead advances.
2. Drag across the waveform to create a selection.
3. Use Ctrl or Cmd plus the mouse wheel to zoom and right-drag to pan.
4. Loop the selected range and refine its boundaries before processing.

## Workflows

### Workflow: Select and loop a region

1. Drag from the intended start to end on the waveform.
2. Enable or retain loop playback and play the range.
3. Adjust both handles until the audible boundaries are correct.

### Workflow: Navigate a long clip

1. Place the pointer over the intended waveform position and zoom around that position with Ctrl or Cmd plus the mouse wheel.
2. Right-drag to pan without changing the selection.
3. Use displayed timing labels to record the final range.

## Verification

- Playback starts and stops without Console errors.
- The loop remains inside the selected sample range.
- Selection handles and timing labels agree.
- Clicking within the waveform outside the selected region clears only the selection, not the loaded source.

## API quick reference

- `AudioManager.PlayClip` previews a full clip.
- `AudioManager.PlayClipRange` previews a bounded sample range.
- `AudioManager.GetCurrentSamplePosition` reports the active sample.
- `AudioWaveformRenderer` converts among sample, normalized, and pixel coordinates.

## Common issues

- **Playback ignores the selection:** Stop existing preview playback, confirm both range handles, then start range playback.
- **Zoom changes the wrong region:** Place the pointer over the intended waveform position before using Ctrl or Cmd plus the wheel.
- **Timing looks offset:** Use sample positions and the clip frequency rather than rounded display text for processing.

## Boundaries

- Selection is non-destructive and does not create output.
- Do not manipulate Unity's internal audio preview utilities directly.
- Do not assume displayed seconds are precise enough for sample-level edits.
- Use the processing skill for trim, normalization, volume, or fades.
