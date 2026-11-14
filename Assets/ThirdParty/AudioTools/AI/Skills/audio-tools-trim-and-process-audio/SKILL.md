---
name: "audio-tools-trim-and-process-audio"
description: "Use this skill whenever the user asks to 'trim this clip', remove silence, normalize levels, adjust volume, or add fades with Audio Tools. Use audio-tools-export-processed-audio when the user mainly needs a WAV file written to disk."
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

# Trim and Process Audio

Apply Audio Tools processing to an explicit selection or full source while preserving the original asset until export.

## When to use this skill

- Trimming or cropping to a selected range.
- Detecting and removing leading or trailing silence.
- Normalizing peaks, adjusting volume, or applying fades.

## Prerequisites

- The intended source and selection have been verified.
- Processing settings, channels, and sample frequency are known.

## Quick start

1. Create or confirm the intended waveform selection.
2. Choose only the requested processing operations. The Audio Editor applies Normalize, Fade In, Fade Out, then Volume; when calling `AudioProcessor` directly, keep the chosen order explicit.
3. Preview the processed result before export.
4. Retain the original source and pass the processed samples to the export workflow.

## Workflows

### Workflow: Trim silence and normalize

1. Detect the audible range with an appropriate silence threshold.
2. Trim interleaved samples using the source channel count.
3. Normalize to the requested peak, defaulting to 0.95 only when the user provides no target.
4. Preview the processed clip.

### Workflow: Apply volume and fades

1. Confirm whether processing applies to the whole clip or selection.
2. Adjust volume before final peak validation.
3. Apply fade-in or fade-out with an explicit sample duration and optional curve.
4. Check for clipping and audible discontinuities.

## Verification

- Processed sample count matches the chosen trim range and channel layout.
- Peak amplitude is within the requested target.
- Fade endpoints are smooth and no channel is offset.
- The original project asset has not been overwritten.

## API quick reference

- `AudioProcessor.DetectSilence(samples, channels, threshold)` returns per-channel sample-frame boundaries, with an exclusive end.
- `AudioProcessor.TrimAudio(samples, channels, startSample, endSample)` returns trimmed interleaved data. Pass frame indices, not indices into the flat interleaved array; output length is `(endSample - startSample) * channels`.
- `AudioProcessor.Normalize`, `AdjustVolume`, and `ApplyFade` modify the working samples.
- Await `AudioProcessor.CreateClipFromSamples` to create a preview clip from processed data.

## Common issues

- **Stereo output sounds shifted:** Pass frame-aligned sample indices and the correct channel count.
- **Volume adjustment clips the normalized result:** Lower Volume or the normalization target in the Audio Editor and recheck the preview. A direct API workflow can apply volume before normalization when that order is intended.
- **Too much quiet content was removed:** Lower the silence threshold and preview the detected range before trimming.

## Boundaries

- Never overwrite the source implicitly.
- Do not process compressed file bytes directly; work with decoded sample data.
- Keep channel interleaving intact.
- Writing the result belongs to audio-tools-export-processed-audio.
