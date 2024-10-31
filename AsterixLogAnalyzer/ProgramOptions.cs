namespace AsterixLogAnalyzer;

/**
 * <summary>Represents Program start options</summary>
 */
public class ProgramOptions
{
    public static string InputFilePath { get; set; } = string.Empty;
    public static string OutputDirectory { get; set; } = string.Empty;

    public static string GetBitmapFileName()
    {
        var directory = Directory.CreateDirectory(OutputDirectory);
        var inputFileName = Path.GetFileNameWithoutExtension(InputFilePath);
        var fileName = $"{inputFileName}-{DateTime.Now:yyMMdd-HHmmss}.bmp";

        var imageFullName = Path.Combine(directory.FullName, fileName);


        return imageFullName;
    }
    public static void ProcessProgramArgs(string[] args, bool usetestdata = true)
    {
        if (args.Length == 0 || usetestdata)
            DisplayPrompt();

        InputFilePath = @".\Data\Testdaten.txt";
        OutputDirectory = @".\Output";
        if (args.Length == 1)
        {
            InputFilePath = args[0];
        }
    }

    public static void DisplayPrompt()
    {
        Console.WriteLine("Prompt");
        Console.WriteLine("AsterixLogAnalyzer.exe [<LOG_FILE> [-o <BITMAP_OUTPUT_DIRECTORY>]]");
    }
}
