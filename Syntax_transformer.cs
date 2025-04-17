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
}
