# Ashura Custom Language

The `custom` profile is a syntax-neutral mode for `.at` files. It keeps the editor focused on the file contents without assuming a compiler, interpreter, or file extension beyond `.at`.

## Recommended workflow

1. Create a file with `MAKEFILE -code/example.at`.
2. Select `custom`, or run `LANGUAGE -custom`.
3. Edit the file with `CODE -code/example.at -custom`.
4. Save with `Shift+Enter` in the editor.

Custom files can contain any text format, including notes, pseudocode, configuration, or a language design in progress. Ashura Terminal stores the content and does not execute it.

## Commands

- `LANGUAGE -custom` selects the custom profile.
- `DOCS -custom` opens this document.
- `CODE -path.at -custom` opens a file in custom mode.