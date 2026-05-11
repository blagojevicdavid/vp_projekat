using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string dataPath = ConfigurationManager.AppSettings["DataPath"];

            if (string.IsNullOrEmpty(dataPath) || !Directory.Exists(dataPath))
            {
                Console.WriteLine("Data directory not found. Check DataPath in App.config.");
                Console.ReadLine();
                return;
            }

            string[] folders = Directory.GetDirectories(dataPath);

            if (folders.Length == 0)
            {
                Console.WriteLine("No vehicle folders found in: " + dataPath);
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Available vehicles:");
            for (int i = 0; i < folders.Length; i++)
                Console.WriteLine(string.Format("  [{0}] {1}", i + 1, Path.GetFileName(folders[i])));

            Console.Write("Select vehicle (1-{0}): ", folders.Length);
            int choice;
            if (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > folders.Length)
            {
                Console.WriteLine("Invalid selection.");
                Console.ReadLine();
                return;
            }

            string selectedFolder = folders[choice - 1];
            string vehicleId = Path.GetFileName(selectedFolder);
            string csvPath = Path.Combine(selectedFolder, "Charging_Profile.csv");

            if (!File.Exists(csvPath))
            {
                Console.WriteLine("Charging_Profile.csv not found in: " + selectedFolder);
                Console.ReadLine();
                return;
            }

            List<string> errorLog = new List<string>();

            using (ChargingClient client = new ChargingClient())
            using (CsvReader csv = new CsvReader(csvPath))
            {
                try
                {
                    client.StartSession(vehicleId);

                    foreach (var sample in csv.ReadRows(vehicleId, errorLog))
                    {
                        client.PushSample(sample);
                        Console.WriteLine("Sent row: " + sample.RowIndex);
                    }

                    client.EndSession(vehicleId);
                    Console.WriteLine("Transfer complete.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error during transfer: " + ex.Message);
                }
            }

            if (errorLog.Count > 0)
            {
                string logPath = Path.Combine(selectedFolder, "parse_errors.log");
                File.WriteAllLines(logPath, errorLog.ToArray());
                Console.WriteLine(string.Format("{0} invalid rows logged to: {1}", errorLog.Count, logPath));
            }

            Console.ReadLine();
        }
    }
}
