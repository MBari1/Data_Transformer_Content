using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

class Program
{
    static void Main()
    {
        string wxsFilePath = @"path\to\your\file.wxs";
        string wxsContent = File.ReadAllText(wxsFilePath);

        string uncommentedXml = RemoveXmlComments(wxsContent);
        XDocument doc = XDocument.Parse(uncommentedXml);

        XNamespace ns = "http://schemas.microsoft.com/wix/2006/wi";

        // Step 1: Extract useful file IDs from CustomTable Gres
        var usefulFileIds = doc.Descendants(ns + "CustomTable")
            .Where(ct => ct.Attribute("Id")?.Value == "Gres")
            .Descendants(ns + "Row")
            .Select(row => row.Element(ns + "Data")?.Value.Trim())
            .Where(val => !string.IsNullOrEmpty(val))
            .ToHashSet(); // Using HashSet for fast lookup

        Console.WriteLine($"Total useful File IDs found: {usefulFileIds.Count}");

        // Step 2: Find File elements with matching IDs and extract Source extensions
        var extensions = doc.Descendants(ns + "File")
            .Where(f => {
                var id = f.Attribute("Id")?.Value;
                return id != null && usefulFileIds.Contains(id);
            })
            .Select(f => Path.GetExtension(f.Attribute("Source")?.Value ?? "").ToLower())
            .Where(ext => !string.IsNullOrEmpty(ext))
            .Distinct()
            .OrderBy(ext => ext)
            .ToList();

        Console.WriteLine("Distinct file extensions used:");
        foreach (var ext in extensions)
        {
            Console.WriteLine(ext);
        }
    }

    // Removes XML comments from the input string
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