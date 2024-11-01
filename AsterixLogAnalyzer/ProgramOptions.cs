using System.IO;

namespace AsterixLogAnalyzer;

/**
 * <summary>Represents program start options</summary>
 */
public class ProgramOptions
{
    private static string[] helpOptions = ["-H", "--HELP", "-?"];
    private static string[] outputOptions = ["-O", "--OUTPUT-DIRECTORY"];
    public static string InputFilePath { get; set; } = string.Empty;
    public static string OutputDirectory { get; set; } = Directory.GetCurrentDirectory();

    /**
     * <summary>Generates bitmap file name</summary>
     */
    public static string GetBitmapFileName()
    {
        var directory = Directory.CreateDirectory(OutputDirectory);
        var inputFileName = Path.GetFileNameWithoutExtension(InputFilePath);
        var fileName = $"{inputFileName}-{DateTime.Now:yyMMdd-HHmmss}.bmp";

        var imageFullName = Path.Combine(directory.FullName, fileName);


        return imageFullName;
    }

    /**
     * <summary>Process program arguments</summary>
     */

    public static bool ProcessProgramArgs(string[] args)
    {
        var retValue = args.Length > 0;

        for (int i = 0; i < args.Length; i++)
        {
            if (i == 0)
            {
                InputFilePath = args[i];
            }

            var arg = args[i].ToUpper();
            if (helpOptions.Contains(arg))
            {
                retValue = false;
                break;
            }

            if (outputOptions.Contains(arg))
            {
                if (args.Length < i + 1)
                    OutputDirectory = args[i + 1];
            }

        }



        if (retValue)
        {
            if (!File.Exists(InputFilePath))
            {
                Console.WriteLine($"Input file does not exisis.\n{InputFilePath}"); retValue = false;
            }
        }
        else
            DisplayPrompt();

        return retValue;


    }

    /**
     * <summary>Displays program propt</summary>
     */
    public static void DisplayPrompt()
    {
        Console.WriteLine("PROMPT:");
        Console.WriteLine("AsterixLogAnalyzer.exe [<INPUT_FILE>] [OPTIONS]");
        Console.WriteLine("");
        Console.WriteLine("<INPUT_FILE>:\t\tFile with input data (Aterix log queue_log)");
        Console.WriteLine("Options:");
        Console.WriteLine($"{string.Join(", ", helpOptions)}:\t\tShow this help");
        Console.WriteLine($"{string.Join(", ", outputOptions)}\tWrite bitmap to output directory <OUTPUT_DIRECTORY>");

    }
}
