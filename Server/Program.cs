using System;
using System.IO;
using System.ServiceModel;

namespace Server
{
    class Program
    {
        private static readonly string LogFile = "transfer.log";

        static void Main(string[] args)
        {
            ChargingService service = new ChargingService();

            service.OnTransferStarted += (s, e) =>
                Log(string.Format("[EVENT] Sesija pokrenuta - Vozilo: {0} u {1}",
                    e.VehicleId, e.Time));

            service.OnSampleReceived += (s, e) =>
                Log(string.Format("[prenos u toku] Red {0} primljen - Vozilo: {1}",
                    e.RowIndex, e.VehicleId));

            service.OnTransferCompleted += (s, e) =>
                Log(string.Format("[prenos završen] Sesija završena - Vozilo: {0} u {1}",
                    e.VehicleId, e.Time));

            service.OnWarningRaised += (s, e) =>
                Log(string.Format("[UPOZORENJE] {0} | Vozilo: {1} | Red: {2} | Pre: {3:F4} | Posle: {4:F4} | {5}",
                    e.WarningType, e.VehicleId, e.RowIndex, e.ValueBefore, e.ValueAfter, e.Message));

            ServiceHost host = new ServiceHost(service);

            try
            {
                host.Open();
                Console.WriteLine("Server pokrenut. Pritisnite Enter za zaustavljanje...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška: " + ex.Message);
            }
            finally
            {
                host.Close();
            }
        }

        private static void Log(string message)
        {
            string line = string.Format("[{0}] {1}", DateTime.Now, message);
            Console.WriteLine(line);
            File.AppendAllText(LogFile, line + Environment.NewLine);
        }
    }
}
