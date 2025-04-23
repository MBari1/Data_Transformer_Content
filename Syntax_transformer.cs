using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

class Program
{
    static void Main()
    {
        string wxsFilePath = @"path\to\your\file.wxs";
        string baseDirectory = @"base\folder\path"; // Root where files like "Folder1\All_files\xyz.txt" reside

        string wxsContent = File.ReadAllText(wxsFilePath);
        string uncommentedXml = RemoveXmlComments(wxsContent);
        XDocument doc = XDocument.Parse(uncommentedXml);

        XNamespace ns = "http://schemas.microsoft.com/wix/2006/wi";

        // Step 1: Get useful file IDs
        var usefulFileIds = doc.Descendants(ns + "CustomTable")
            .Where(ct => ct.Attribute("Id")?.Value == "Gres")
            .Descendants(ns + "Row")
            .Select(row => row.Element(ns + "Data")?.Value.Trim())
            .Where(val => !string.IsNullOrEmpty(val))
            .ToHashSet();

        // Step 2: Get File elements with Source and process only those with allowed extensions
        var targetFiles = doc.Descendants(ns + "File")
            .Where(f => {
                var id = f.Attribute("Id")?.Value;
                var src = f.Attribute("Source")?.Value;
                return id != null &&
                       src != null &&
                       usefulFileIds.Contains(id) &&
                       !src.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                       !src.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            })
            .Select(f => Path.Combine(baseDirectory, f.Attribute("Source")?.Value))
            .Where(File.Exists) // Ensure the file actually exists
            .ToList();

        Console.WriteLine($"Processing {targetFiles.Count} files...");

        foreach (var filePath in targetFiles)
        {
            string content = File.ReadAllText(filePath);

            // Replace all $HeadSomeText$ → {{SomeText}}
            string updatedContent = Regex.Replace(
            content,
            @"\$(.*?Head.*?)\$",
            m =>
            {
                var cleaned = Regex.Replace(m.Groups[1].Value, "Head", "", RegexOptions.IgnoreCase);
                return $"{{{{{cleaned}}}}}";
            },
            RegexOptions.IgnoreCase
            );



            // Write back only if modified
            if (updatedContent != content)
            {
                File.WriteAllText(filePath, updatedContent);
                Console.WriteLine($"Updated: {filePath}");
            }
        }

        Console.WriteLine("All eligible files processed.");
    }

    static string RemoveXmlComments(string input)
    {
        while (true)
        {
            int start = input.IndexOf("<!--");
            if (start == -1) break;
            int end = input.IndexOf("-->", start + 4);
            if (end == -1) break;
            input = input.Remove(start, end - start + 3);
        }
        return input;
    }

static void WritePackageCommandFile()
    {
        var modifiedFilesPath = "modified-files.txt";
        var packageCommandsPath = "PackageCommands.txt";

        if (!File.Exists(modifiedFilesPath) || !File.Exists(packageCommandsPath))
        {
            Console.WriteLine("Required file(s) not found.");
            return;
        }

        // Load modified file paths
        var modifiedFilePaths = File.ReadAllLines(modifiedFilesPath)
                                    .Select(path => path.Replace('\\', '/').Trim())
                                    .ToList();

        // Build a dictionary of fileName -> fullPath for lookup
        var modifiedFileLookup = modifiedFilePaths
            .ToDictionary(path => Path.GetFileName(path), path => path);

        var lines = File.ReadAllLines(packageCommandsPath).ToList();
        var outputLines = new List<string>();

        foreach (var line in lines)
        {
            outputLines.Add(line);

            var parts = SplitQuotedLine(line);
            if (parts.Length != 2)
                continue;

            var si_1 = parts[0].Trim('"');
            var si_2 = parts[1].Trim('"');

            // Check if si_1 contains any modified file path
            var matched = modifiedFilePaths.FirstOrDefault(modPath =>
                si_1.Contains(modPath, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(matched))
            {
                var fileName = Path.GetFileName(matched);
                var sa = $"{si_2}/{fileName}";
                var sb = $"{fileName}.mykey";

                outputLines.Add($"\"{sa}\" \"{sb}\" \"pin\"");
            }
        }

        File.WriteAllLines(packageCommandsPath, outputLines);
        Console.WriteLine("PackageCommands.txt updated successfully.");
    }

    // Helper method to split by quoted segments (handles space safely)
    static string[] SplitQuotedLine(string line)
    {
        var result = new List<string>();
        bool inQuote = false;
        int start = 0;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
            {
                if (inQuote)
                {
                    result.Add(line.Substring(start, i - start + 1).Trim());
                    inQuote = false;
                }
                else
                {
                    inQuote = true;
                    start = i;
                }
            }
        }
        return result.ToArray();
    }

}
