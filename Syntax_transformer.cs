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

        // Read the file as a string
        string wxsContent = File.ReadAllText(wxsFilePath);

        // Load only uncommented XML using XElement.Parse after stripping comments
        string uncommentedXml = RemoveXmlComments(wxsContent);

        // Parse the XML
        XDocument doc = XDocument.Parse(uncommentedXml);

        // Get all Data elements under CustomTable[@Id='Gres']
        var fileIds = doc.Descendants("CustomTable")
                         .Where(ct => ct.Attribute("Id")?.Value == "Gres")
                         .Descendants("Row")
                         .Select(row => row.Element("Data")?.Value.Trim())
                         .Where(value => !string.IsNullOrEmpty(value))
                         .ToList();

        // Sort and display
        fileIds.Sort();
        foreach (var fileId in fileIds)
        {
            Console.WriteLine(fileId);
        }
    }

    // Remove XML comments
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
