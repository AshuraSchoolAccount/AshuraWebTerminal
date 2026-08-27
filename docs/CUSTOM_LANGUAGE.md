# Ashura Custom Language

The `custom` profile is a syntax-neutral mode for `.at` files. It keeps the editor focused on the file contents without assuming a compiler, interpreter, or file extension beyond `.at`.

## Recommended workflow

1. Create a file with `MAKEFILE -code/example.at`.
2. Select `custom`, or run `LANGUAGE -custom`.
3. Edit the file with `CODE -code/example.at -custom`.
4. Save with `Shift+Enter` in the editor.

Custom files are executable terminal scripts. They can contain one command per line and are intended to feel like a small Bash-style language for Ashura Terminal.

## Commands

- `LANGUAGE -custom` selects the custom profile.
- `DOCS -custom` opens this document.
- `CODE -path.at -custom` opens a file in custom mode.
- `RUN -path.at` executes a custom script.

## Script commands

- `echo text` or `print text` writes output.
- `set NAME value` stores a variable. Use it later as `$NAME`.
- `load path` prints a saved file.
- `make path content` creates or replaces a file.
- `list` lists saved files.
- `mkdir folder` creates a folder.
- `run path` runs another supported source file.
- Lines beginning with `#` are comments.