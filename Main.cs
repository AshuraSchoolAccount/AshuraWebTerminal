using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace Main 
{ 
    public class Program 
    { 
        private const string Version = "Pre-beta 1";
        public static string commands = "HELP, EXIT, FETCH -IP, MAKEFILE [-path.at] [-content], EDITFILE [-path.at], CODE -path.at [-language], LOADFILE [-path.at], LISTFILES, LISTFOLDERS, OPENFOLDER -name, ADDFOLDER -name, PRESETFOLDERS, LANGUAGES, LANGUAGE -name, DOCS -name "; 
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly IAtFileStore fileStore = CreateFileStore();
        private static string currentLanguage = "custom";
        private static readonly Dictionary<string, string> languageDocs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cpp"] = "https://github.com/isocpp/CppCoreGuidelines",
            ["c++"] = "https://github.com/isocpp/CppCoreGuidelines",
            ["csharp"] = "https://learn.microsoft.com/dotnet/csharp/",
            ["c#"] = "https://learn.microsoft.com/dotnet/csharp/",
            ["cs"] = "https://learn.microsoft.com/dotnet/csharp/",
            ["python"] = "https://github.com/python/cpython/tree/main/Doc",
            ["custom"] = "https://github.com/AshuraSchoolAccount/AshuraWebTerminal/blob/main/docs/CUSTOM_LANGUAGE.md"
        };

        private interface IAtFileStore
        {
            IReadOnlyCollection<string> Files { get; }
            IReadOnlyCollection<string> Folders { get; }
            void Save(string fileName, string contents);
            string Load(string fileName);
            void AddFolder(string folderName);
            IReadOnlyCollection<string> ListFolder(string folderName);
        }

        private sealed class LocalAtFileStore : IAtFileStore
        {
            private readonly string directory;

            public LocalAtFileStore()
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                directory = Path.Combine(appData, "AshuraTerminal", "files");
                Directory.CreateDirectory(directory);
            }

            public IReadOnlyCollection<string> Files => Directory.GetFiles(directory, "*.at")
                .Select(fileName => Path.GetRelativePath(directory, fileName))
                .Where(fileName => fileName != null)
                .Cast<string>()
                .ToArray();

            public IReadOnlyCollection<string> Folders => Directory.GetDirectories(directory, "*", SearchOption.AllDirectories)
                .Select(folderName => Path.GetRelativePath(directory, folderName))
                .ToArray();

            public void Save(string fileName, string contents)
            {
                string path = Path.Combine(directory, fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, contents, Encoding.UTF8);
            }

            public string Load(string fileName) =>
                File.ReadAllText(Path.Combine(directory, fileName), Encoding.UTF8);

            public void AddFolder(string folderName) => Directory.CreateDirectory(Path.Combine(directory, folderName));

            public IReadOnlyCollection<string> ListFolder(string folderName)
            {
                string path = Path.Combine(directory, folderName);
                if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"Folder '{folderName}' does not exist.");
                return Directory.GetFileSystemEntries(path)
                    .Select(entry => Path.GetRelativePath(path, entry) + (Directory.Exists(entry) ? "/" : ""))
                    .ToArray();
            }
        }

        private sealed class BrowserAtFileStore : IAtFileStore
        {
            private readonly Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);

            public IReadOnlyCollection<string> Files => files.Keys.ToArray();

            public IReadOnlyCollection<string> Folders { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public void Save(string fileName, string contents)
            {
                files[fileName] = contents;
                string? parent = Path.GetDirectoryName(fileName)?.Replace('\\', '/');
                while (!string.IsNullOrEmpty(parent))
                {
                    ((HashSet<string>)Folders).Add(parent);
                    parent = Path.GetDirectoryName(parent)?.Replace('\\', '/');
                }
            }

            public string Load(string fileName) => files.TryGetValue(fileName, out string? contents)
                ? contents
                : throw new FileNotFoundException("The file does not exist.", fileName);

            public void AddFolder(string folderName)
            {
                string? current = folderName;
                while (!string.IsNullOrEmpty(current))
                {
                    ((HashSet<string>)Folders).Add(current);
                    current = Path.GetDirectoryName(current)?.Replace('\\', '/');
                }
            }

            public IReadOnlyCollection<string> ListFolder(string folderName)
            {
                string prefix = folderName.TrimEnd('/') + "/";
                if (!Folders.Contains(folderName) && !files.Keys.Any(fileName => fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new DirectoryNotFoundException($"Folder '{folderName}' does not exist.");
                }

                return Folders.Where(folder => folder.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(folder => folder[prefix.Length..].Split('/')[0] + "/")
                    .Concat(files.Keys.Where(fileName => fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        .Select(fileName => fileName[prefix.Length..].Split('/')[0]))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        private static IAtFileStore CreateFileStore() =>
            OperatingSystem.IsBrowser() ? new BrowserAtFileStore() : new LocalAtFileStore();

        private static string NormalizeFilePath(string fileName)
        {
            string normalized = fileName.Trim();
            if (!normalized.EndsWith(".at", StringComparison.OrdinalIgnoreCase)) normalized += ".at";

            normalized = normalized.Replace('\\', '/');
            if (normalized.Length <= 3 || normalized.StartsWith('/') || normalized.Contains("../", StringComparison.Ordinal) ||
                normalized.Contains("/..", StringComparison.Ordinal) || normalized.Split('/').Any(part =>
                    part.Length == 0 || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            {
                throw new ArgumentException("Use a relative .at path such as documents/example.at.");
            }

            return normalized;
        }

        private static string NormalizeFolderPath(string folderName)
        {
            string normalized = folderName.Trim().Replace('\\', '/').Trim('/');
            if (normalized.Length == 0 || normalized.Contains("..", StringComparison.Ordinal) || normalized.Split('/').Any(part =>
                part.Length == 0 || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            {
                throw new ArgumentException("Use a relative folder name such as documents.");
            }

            return normalized;
        }

        private static string ReadRequiredInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine()?.Trim() ?? "";
        }

        private static string ReadFileContent()
        {
            Console.WriteLine("Enter the file contents. Submit an empty line to save.");
            var contents = new StringBuilder();
            while (true)
            {
                string line = Console.ReadLine() ?? "";
                if (line.Length == 0) break;
                if (contents.Length > 0) contents.AppendLine();
                contents.Append(line);
            }

            return contents.ToString();
        }

        private static void MakeFile(IReadOnlyList<string> arguments, bool edit)
        {
            if (arguments.Count > 2) throw new ArgumentException("The file command accepts a path and content only.");
            string fileName = NormalizeFilePath(arguments.Count > 0 ? arguments[0] : ReadRequiredInput("File path (.at): "));
            if (edit && arguments.Count == 0) throw new ArgumentException("EDITFILE needs a file path.");
            string content = arguments.Count > 1 ? arguments[1] : ReadFileContent();

            fileStore.Save(fileName, content);
            Console.WriteLine($"{(edit ? "Edited" : "Saved")} {fileName}.");
        }

        private static void LoadFile(IReadOnlyList<string> arguments)
        {
            if (arguments.Count > 1) throw new ArgumentException("LOADFILE accepts one file name only.");
            string fileName = NormalizeFilePath(arguments.Count > 0
                ? arguments[0]
                : ReadRequiredInput("File path (.at): "));
            Console.WriteLine(fileStore.Load(fileName));
        }

        private static void AddFolder(IReadOnlyList<string> arguments)
        {
            if (arguments.Count != 1) throw new ArgumentException("ADDFOLDER needs one folder name.");
            string folderName = NormalizeFolderPath(arguments[0]);
            fileStore.AddFolder(folderName);
            Console.WriteLine($"Created folder {folderName}.");
        }

        private static void ListFolder(IReadOnlyList<string> arguments)
        {
            if (arguments.Count != 1) throw new ArgumentException("OPENFOLDER needs one folder name.");
            string folderName = NormalizeFolderPath(arguments[0]);
            foreach (string entry in fileStore.ListFolder(folderName)) Console.WriteLine(entry);
        }

        private static void SetLanguage(IReadOnlyList<string> arguments)
        {
            if (arguments.Count != 1 || !languageDocs.ContainsKey(arguments[0]))
            {
                throw new ArgumentException("Choose cpp, csharp, python, or custom.");
            }

            currentLanguage = arguments[0].ToLowerInvariant() switch
            {
                "c++" => "cpp",
                "c#" or "cs" => "csharp",
                var language => language
            };
            Console.WriteLine($"Coding language set to {currentLanguage}.");
        }

        private static void CodeFile(IReadOnlyList<string> arguments)
        {
            if (arguments.Count is < 1 or > 2) throw new ArgumentException("CODE needs a path and optional language.");
            string fileName = NormalizeFilePath(arguments[0]);
            if (arguments.Count == 2) SetLanguage(new[] { arguments[1] });
            Console.WriteLine($"Editing {fileName} as {currentLanguage}. Press Shift+Enter to save or Escape to cancel.");
            var content = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("Edit cancelled.");
                    return;
                }

                if (key.Key == ConsoleKey.Enter && key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    fileStore.Save(fileName, content.ToString());
                    Console.WriteLine($"Saved {fileName}.");
                    return;
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    content.AppendLine();
                    Console.WriteLine();
                }
                else if (key.Key == ConsoleKey.Backspace && content.Length > 0)
                {
                    content.Length--;
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    content.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
        }

        private static (string Command, List<string> Arguments) ParseInput(string input)
        {
            var tokens = new List<string>();
            var token = new StringBuilder();
            bool quoted = false;
            foreach (char character in input)
            {
                if (character == '"')
                {
                    quoted = !quoted;
                }
                else if (char.IsWhiteSpace(character) && !quoted)
                {
                    if (token.Length > 0)
                    {
                        tokens.Add(token.ToString());
                        token.Clear();
                    }
                }
                else
                {
                    token.Append(character);
                }
            }

            if (quoted) throw new ArgumentException("Arguments cannot contain an unmatched quote.");
            if (token.Length > 0) tokens.Add(token.ToString());
            if (tokens.Count == 0) return ("", new List<string>());

            string command = tokens[0].ToUpperInvariant();
            var arguments = new List<string>();
            for (int index = 1; index < tokens.Count; index++)
            {
                if (!tokens[index].StartsWith("-"))
                {
                    throw new ArgumentException($"Argument '{tokens[index]}' must start with '-'.");
                }

                arguments.Add(tokens[index][1..]);
            }

            return (command, arguments);
        }
        
        static async Task StartTerminal()
        { 
            string? userInput = Console.ReadLine();
            (string command, List<string> arguments) parsedInput;
            try { parsedInput = ParseInput(userInput?.Trim() ?? ""); }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            switch (parsedInput.command) 
            { 
                case "HELP": 
                    Console.WriteLine("Commands that you can run are: " + commands); 
                    break; 
                case "EXIT":
                    Console.WriteLine("Exiting terminal...");
                    Environment.Exit(0);
                    break;

                case "REPEAT":
                    Console.WriteLine(string.Join(" ", parsedInput.arguments));
                    break;

                case "MAKEFILE":
                    try { MakeFile(parsedInput.arguments, false); }
                    catch (Exception e) { Console.WriteLine("Could not save the file: " + e.Message); }
                    break;

                case "EDITFILE":
                    try { MakeFile(parsedInput.arguments, true); }
                    catch (Exception e) { Console.WriteLine("Could not edit the file: " + e.Message); }
                    break;

                case "CODE":
                    try { CodeFile(parsedInput.arguments); }
                    catch (Exception e) { Console.WriteLine("Could not edit the file: " + e.Message); }
                    break;

                case "LOADFILE":
                    try { LoadFile(parsedInput.arguments); }
                    catch (Exception e) { Console.WriteLine("Could not load the file: " + e.Message); }
                    break;

                case "LISTFILES":
                    foreach (string fileName in fileStore.Files) Console.WriteLine(fileName);
                    foreach (string folderName in fileStore.Folders) Console.WriteLine(folderName + "/");
                    break;

                case "LISTFOLDERS":
                    foreach (string folderName in fileStore.Folders) Console.WriteLine(folderName + "/");
                    break;

                case "OPENFOLDER":
                    try { ListFolder(parsedInput.arguments); }
                    catch (Exception e) { Console.WriteLine("Could not open the folder: " + e.Message); }
                    break;

                case "ADDFOLDER":
                    try { AddFolder(parsedInput.arguments); }
                    catch (Exception e) { Console.WriteLine("Could not create the folder: " + e.Message); }
                    break;

                case "PRESETFOLDERS":
                    fileStore.AddFolder("documents");
                    fileStore.AddFolder("code");
                    Console.WriteLine("Created folders documents and code.");
                    break;

                case "LANGUAGES":
                    Console.WriteLine("Available languages: cpp, csharp, python, custom");
                    break;

                case "LANGUAGE":
                    try { SetLanguage(parsedInput.arguments); }
                    catch (Exception e) { Console.WriteLine(e.Message); }
                    break;

                case "DOCS":
                    if (parsedInput.arguments.Count != 1 || !languageDocs.TryGetValue(parsedInput.arguments[0], out string? documentationUrl))
                    {
                        Console.WriteLine("Use DOCS -cpp, DOCS -csharp, DOCS -python, or DOCS -custom.");
                        break;
                    }
                    Console.WriteLine(documentationUrl);
                    break;

                case "FETCH":
                    if (parsedInput.arguments.Count != 1 || !parsedInput.arguments[0].Equals("IP", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Use FETCH -IP.");
                        break;
                    }
                    try
                    {
                        string publicIp = await httpClient.GetStringAsync("https://api.ipify.org");
                        Console.WriteLine("Public IP: " + publicIp.Trim());
                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine("Could not fetch the public IP: " + e.Message);
                    }
                    break;
                
                default: 
                    Console.WriteLine("Command is not a valid command. Enter the command help for a list of the current commands"); 
                    break; 
            } 
        } 
        
        public static async Task Main(string[] args)
        { 
            Console.WriteLine($"Ashura Terminal {Version}");
            Console.WriteLine(OperatingSystem.IsBrowser()
                ? "Web storage is available for this session."
                : "Files are stored in the AshuraTerminal/files application-data folder.");
            
            while (true) 
            {
                await StartTerminal();
                Console.WriteLine();
            } 
        } 
    } 
}
