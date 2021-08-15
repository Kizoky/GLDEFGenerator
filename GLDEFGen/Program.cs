using System;
using System.IO;

namespace GLDEFGen
{
    class Program
    {
        public static class Globals
        {
            // Console applications can apparently change the current path if it was executed from elsewhere, pretty annoying in our case
            public static readonly string ExeLoc = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            public static readonly string Output = Path.Combine(ExeLoc, "GLDEFGen_Output.txt");
            public static readonly string Config = Path.Combine(ExeLoc, "GLDEFGen.cfg");

            public static string[] ConfigFile = null;
            public static string ConfigPath = null;
        }
        private static void InitConfig()
        {
            // Do not iterate this EVERYTIME we go through a texture, just only once
            Globals.ConfigFile = File.ReadAllLines(Globals.Config);
            foreach (string s in Globals.ConfigFile)
            {
                if (s.Length > 0)
                {
                    if (s[0] != '/' && s[1] != '/' && s.Contains("<path>"))
                    {
                        Globals.ConfigPath = s;
                    }
                }
            }
        }

        private static bool IsWindows()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return true;
            }

            return false;
        }

        private static void GLDEF_Generate(string Path)
        {
            // Remove the path that is unnecessary to GLDEF, we only need what the user wants
            string GLDEF = null;
            for (int i = 0; i < Path.Length; i++)
            {
                if (i > Globals.ExeLoc.Length)
                {
                    GLDEF = $"{GLDEF + Path[i]}";
                }
            }

            // Replace \s with /
            GLDEF = GLDEF.Replace("\\", "/");

            // Replace <path> with the path to texture
            string ConfigPath = Globals.ConfigPath.Replace($"<path>", $"\"{GLDEF}\"");

            using (StreamWriter sw = File.AppendText(Globals.Output))
            {
                sw.WriteLine($"{ConfigPath}");
                sw.WriteLine("{");

                foreach (string s in Globals.ConfigFile)
                {
                    if (s.Length > 0)
                    {
                        if (s[0] != '/' && s[1] != '/' && !s.Contains("<path>"))
                        {
                            sw.WriteLine($"   {s}");
                        }
                    }
                }

                sw.WriteLine("}\n");
            }

            Console.WriteLine($"{GLDEF}");
        }

        static void Main(string[] args)
        {
            Console.Title = "GLDEF Shader Definition Output Generator";
            Console.BackgroundColor = ConsoleColor.Black;

            if (!File.Exists(Globals.Config))
            {
                using (StreamWriter sw = File.CreateText(Globals.Config))
                {
                    sw.WriteLine("// Do not delete <path>, it must exist otherwise the program will fail!");
                    sw.WriteLine("material texture <path>");
                    sw.WriteLine("// Add more shaders if desired below");
                    sw.WriteLine("shader \"shaders/SimSun.fp\"");
                }
            }

            // Save the top line of definition once
            InitConfig();

            string FullPathToTextures = null;
            bool fileDrop = false;
            bool multipleFolders = false;

            if (args.Length <= 0)
            {
                Console.WriteLine("Manual type example: 'models/textures/'\n");
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("GLDEFGen MUST be in the root directory of your mod!");
                Console.WriteLine("Pressing enter without input will include ALL png files found in every folder\n");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("GLDEFGen.cfg will be recreated if you delete it and restart the program\n");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Path: ");
                Console.ForegroundColor = ConsoleColor.White;
                FullPathToTextures = $"{Directory.GetCurrentDirectory()}/{Console.ReadLine()}";

                Console.WriteLine();
            }
            // Detect if a folder or file was dropped onto the exe
            else if (args.Length > 0)
            {
                if (Directory.Exists(args[0]))
                {
                    if (args.Length > 1)
                    {
                        multipleFolders = true;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Multiple folder dropping detected");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        FullPathToTextures = args[0];
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Folder dropping detected");
                        Console.ForegroundColor = ConsoleColor.White;

                        // Simulate user input for Unix users, afaik there's no such thing as file or folder dropping
                        if (!IsWindows())
                        {
                            FullPathToTextures = $"{Directory.GetCurrentDirectory()}/{FullPathToTextures}";
                        }
                    }
                }
                else if (File.Exists(args[0]))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("File dropping detected");
                    Console.ForegroundColor = ConsoleColor.White;
                    fileDrop = true;
                }
            }

            try
            {
                // Add a "reminder"
                using (StreamWriter sw = File.AppendText(Globals.Output))
                {
                    sw.WriteLine($"// Generated: {DateTime.Now}");
                }

                int texcount = 0;
                // It wasn't file dropping
                if (!fileDrop)
                {
                    // Only one folder was dragged onto the exe (or via input)
                    if (!multipleFolders)
                    {
                        foreach (var path in Directory.GetFiles(FullPathToTextures, "*.png", SearchOption.AllDirectories))
                        {
                            GLDEF_Generate(path);
                            texcount++;
                        }
                    }
                    // Support for multiple folders dragged onto the exe
                    else if (multipleFolders)
                    {
                        for (int i = 0; i < args.Length; i++)
                        {
                            foreach (var path in Directory.GetFiles(args[i], "*.png", SearchOption.AllDirectories))
                            {
                                GLDEF_Generate(path);
                                texcount++;
                            }
                        }
                    }
                }
                // File dropping
                else if (fileDrop)
                {
                    // User wants to only output several files, not a folder
                    foreach (var path in args)
                    {
                        GLDEF_Generate(path);
                        texcount++;
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nAdded ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{texcount} ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("textures to output file.\n");
                Console.ForegroundColor = ConsoleColor.White;
            }

            // Just in case...
            catch (DirectoryNotFoundException)
            {
                FullPathToTextures = FullPathToTextures.Replace("/", "\\");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nERROR: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"The path to '{FullPathToTextures}' is non-existent.\n");
            }

            Console.ReadKey();
        }
    }
}
