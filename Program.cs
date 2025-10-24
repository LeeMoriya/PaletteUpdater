using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        bool finished = false;

    START:
        Console.WriteLine("Enter the path to your mod's world folder...");
        Console.WriteLine("--------------------------------------------");
        Console.WriteLine("E.g. '..\\StreamingAssets\\mods\\YOUR_MOD\\world'");

        string worldDirectory = Console.ReadLine();

        if (Directory.Exists(worldDirectory))
        {
            Console.Clear();
            List<String> regions = new List<String>();

            var dirs = Directory.GetDirectories(worldDirectory);
            for (int i = 0; i < dirs.Length; i++)
            {
                if (!dirs[i].ToLower().Contains("rooms"))
                {
                    string[] path = dirs[i].Split(Path.DirectorySeparatorChar);
                    regions.Add(path.Last());
                }
            }
            if(regions.Count > 0)
            {
                REGIONSELECT:
                Console.WriteLine("Select the region you want to manage:\n-------------");
                for (int i = 0; i < regions.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {regions[i].ToUpper()}");
                }
                var selection = Console.ReadLine();
                int index;
                bool valid = int.TryParse(selection, out index);
                if (valid)
                {
                    if(index > 0 && index < regions.Count + 1)
                    {
                        PALETTESELECT:
                        int oldPal;
                        int newPal;

                        Console.Clear();
                        Console.WriteLine("Enter the old palette number...");
                        var oldNum = Console.ReadLine();
                        bool oldValid = int.TryParse(oldNum, out oldPal);
                        if (!oldValid)
                        {
                            Console.WriteLine("Invalid selection!");
                            goto PALETTESELECT;
                        }
                        Console.WriteLine("Enter the new palette number...");
                        var newNum = Console.ReadLine();
                        bool newValid = int.TryParse(newNum, out newPal);
                        if (!newValid)
                        {
                            Console.WriteLine("Invalid selection!");
                            goto PALETTESELECT;
                        }
                        CONFIRMATION:
                        Console.Clear();
                        Console.WriteLine("WARNING: Backup your mod folder before continuing!\n--------------------------------------------------");
                        Console.WriteLine($"Are you sure you want to update all references of palette {oldPal} in region {regions[index-1].ToUpper()} to palette {newPal}?\n");
                        Console.WriteLine("Y / N");
                        var confirm = Console.ReadLine();

                        if(confirm.ToLower() == "y")
                        {
                            try
                            {
                                UpdatePaletteReferences(worldDirectory, regions[index - 1], oldPal, newPal);
                            }
                            catch (Exception ex) 
                            { 
                                Console.WriteLine(ex.Message); 
                                Console.WriteLine("The was an error when updating region files. I hope you made a backup like I told you to!");
                                goto START;
                            }
                            //Finish
                            finished = true;
                        }
                        else if(confirm.ToLower() == "n")
                        {
                            Console.WriteLine("Process aborted!");
                            Console.Clear();
                            goto START;
                        }
                        else
                        {
                            Console.WriteLine("Invalid selection!");
                            goto CONFIRMATION;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid selection!");
                        goto REGIONSELECT;
                    }
                }
            }
            else
            {
                Console.WriteLine("No regions found...");
            }
        }
        else
        {
            Console.WriteLine("The directory could not be found!");
        }


        if (finished)
        {

            Console.WriteLine("\n\nRegion files updated successfully!");
            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
        }
        else
        {
            goto START;
        }
    }

    public static void UpdatePaletteReferences(string worldDirectory, string regionAcronym, int oldPal, int newPal)
    {
        //Files to update:
        //All rooms settings files
        //Individual room template files
        //Properties file

        string worldFolder = $"{worldDirectory}\\{regionAcronym}";
        string roomFolder = $"{worldDirectory}\\{regionAcronym}-rooms";

        //Find properties file
        if (File.Exists($"{worldFolder}\\properties.txt"))
        {
            string[] propertiesFile = File.ReadAllLines($"{worldFolder}\\properties.txt");
            for (int i = 0; i < propertiesFile.Length; i++)
            {
                if (propertiesFile[i].ToLower().StartsWith("palette:"))
                {
                    if (propertiesFile[i].ToLower() == $"palette: {oldPal}")
                    {
                        propertiesFile[i] = $"Palette: {newPal}";
                        File.WriteAllLines($"{worldFolder}\\properties.txt", propertiesFile);
                        Console.WriteLine("Updated Properties.txt");
                    }
                    else
                    {
                        Console.WriteLine("No changes made to Properties.txt - old palette number doesn't match");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Properties file missing");
        }

        //Find templates
        List<string> templates = new List<string>();
        var files = Directory.GetFiles(worldFolder);
        foreach (var file in files)
        {
            if (file.Contains("settingstemplate"))
            {
                templates.Add(file);
            }
        }

        for (int i = 0; i < templates.Count; i++)
        {
            string templateName = templates[i].Split(Path.DirectorySeparatorChar).Last();
            string[] currentTemplate = File.ReadAllLines(templates[i]);
            for (int s = 0; s < currentTemplate.Length; s++)
            {
                if (currentTemplate[s].ToLower().StartsWith("palette:"))
                {
                    if (currentTemplate[s].ToLower() == $"palette: {oldPal}")
                    {
                        currentTemplate[s] = $"Palette: {newPal}";
                        File.WriteAllLines(templates[i], currentTemplate);
                        Console.WriteLine($"Updated {templateName}");
                    }
                    else
                    {
                        Console.WriteLine($"No changes made to {templateName} - old palette number doesn't match");
                    }
                }
            }
        }

        List<string> roomSettingsFiles = new List<string>();
        var rooms = Directory.GetFiles(roomFolder);
        foreach(var room in rooms)
        {
            if (room.EndsWith("settings.txt"))
            {
                roomSettingsFiles.Add(room);
            }
        }

        for (int i = 0; i < roomSettingsFiles.Count; i++)
        {
            string roomName = roomSettingsFiles[i].Split(Path.DirectorySeparatorChar).Last().Split("_settings.txt")[0];

            string[] roomFile = File.ReadAllLines (roomSettingsFiles[i]);
            bool mainPaletteChanged = false;
            bool fadePaletteChanged = false;
            for(int s = 0;s < roomFile.Length; s++)
            {
                if (roomFile[s].ToLower().StartsWith("palette:"))
                {
                    if (roomFile[s].ToLower() == $"palette: {oldPal}")
                    {
                        roomFile[s] = $"Palette: {newPal}";
                        mainPaletteChanged = true;
                    }
                }
                if (roomFile[s].ToLower().StartsWith("fadepalette:"))
                {
                    string[] fadePal = roomFile[s].Split(',');
                    if (fadePal[0].ToLower() == $"fadepalette: {oldPal}")
                    {
                        fadePal[0] = roomFile[s] = $"FadePalette: {newPal}";
                        string newFadePal = String.Join(",", fadePal);
                        roomFile[s] = newFadePal;    
                        fadePaletteChanged = true;
                    }
                }
            }
            Console.WriteLine($"{roomName} - Main Palette: {(mainPaletteChanged ? "Updated" : "Unchanged")} | FadePalette: {(fadePaletteChanged ? "Updated" : "Unchanged")}");
            File.WriteAllLines(roomSettingsFiles[i], roomFile);
        }
    }
}

