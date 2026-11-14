# Audio Tools AI Skills

These skills teach a coding assistant to open Audio Tools, work with its non-destructive waveform editor, process selected audio, and export verified WAV output.

## Use the skills

1. Point a compatible coding assistant at this package's `AI/Skills` folder.
2. Ask it to read the skill descriptions and load `audio-tools-setup-and-overview` first when setup or routing is unclear.
3. Load only the narrowest skill that matches the requested job.
4. Review every proposed project change before allowing the assistant to apply it.

These Markdown files are documentation only. They do not add an AI service, subscription, runtime dependency, or generated content to Audio Tools.

## Included skills

- `audio-tools-setup-and-overview`: Open the installed Audio Tools editor, confirm that a supported source is available, and route the request to the narrowest editing skill.
- `audio-tools-preview-and-select-audio`: Use Audio Tools playback and waveform controls to inspect a source and define an exact sample-aligned selection without changing the original file.
- `audio-tools-trim-and-process-audio`: Apply Audio Tools processing to an explicit selection or full source while preserving the original asset until export.
- `audio-tools-export-processed-audio`: Write verified processed samples to a new WAV file with the correct channel, frequency, bit-depth, and project import behavior.

Documentation: https://www.wetzold.com/tools/audiotools/docs/

Support: https://discord.com/invite/uzeHzEMM4B
