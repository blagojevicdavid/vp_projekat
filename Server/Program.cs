using System;
using System.ServiceModel;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(ChargingService));

            try
            {
                host.Open();
                Console.WriteLine("Server started. Press Enter to stop...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                host.Close();
            }
        }
    }
}
