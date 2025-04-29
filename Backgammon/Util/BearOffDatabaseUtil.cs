using Backgammon.Util.AI;
using Newtonsoft.Json;

public static class BearOffDatabaseUtil
{
    public static Dictionary<string, float[]> LoadOrCreateBearOffDatabase(string modelsDir)
    {
        string getBearOffFileName(int maxCheckers) => Path.Combine(modelsDir, $"bearoff{maxCheckers}.json");

        var jsonBearOffFilename = getBearOffFileName(BearOffUtility.MaxCheckers);
        Dictionary<string, float[]> bearOffDatabase;

        try
        {
            string jsonBearOff = File.ReadAllText(jsonBearOffFilename);
            bearOffDatabase = JsonConvert.DeserializeObject<Dictionary<string, float[]>>(jsonBearOff);
        }
        catch
        {
            Console.WriteLine("Could not load bear-off database. Creating a new one...");
            bearOffDatabase = CreateBearOffDatabase(modelsDir);
        }

        return bearOffDatabase;
    }

    private static Dictionary<string, float[]> CreateBearOffDatabase(string modelsDir)
    {
        string getBearOffFileName(int maxCheckers) => Path.Combine(modelsDir, $"bearoff{maxCheckers - 1}.json");

        var jsonBearOffFilenameSmaller = getBearOffFileName(BearOffUtility.MaxCheckers);
        Dictionary<string, float[]> smallerBearOffDatabase = new();

        try
        {
            string jsonBearOff = File.ReadAllText(jsonBearOffFilenameSmaller);
            smallerBearOffDatabase = JsonConvert.DeserializeObject<Dictionary<string, float[]>>(jsonBearOff);
        }
        catch
        {
            Console.WriteLine("Could not load smaller bear-off database.");
        }

        var bearOffDatabase = BearOffUtility.CreateBearOffDataBase(smallerBearOffDatabase);

        // Save the new bear-off database
        string jsonBearOffFilename = Path.Combine(modelsDir, $"bearoff{BearOffUtility.MaxCheckers}.json");
        string json = JsonConvert.SerializeObject(bearOffDatabase, Formatting.Indented);
        File.WriteAllText(jsonBearOffFilename, json);

        return bearOffDatabase;
    }
}