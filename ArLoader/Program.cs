namespace ArLoader
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string definationFilePath = @"armast.def";
            string arFilePath = @"ARMAST_20220512.txt";

            var processor = new ArProcessor(definationFilePath, arFilePath);
            processor.Process();

            Console.WriteLine("Done");
        }
    }
}