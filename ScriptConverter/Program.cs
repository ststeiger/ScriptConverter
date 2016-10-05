
namespace ScriptConverter
{


    class MainClass
    {


        public static void Main(string[] args)
        {
            ScriptConverter.DateTimeReplacer.Test();


            string sql = System.IO.File.ReadAllText(@"/home/someUser/Downloads/somefile.sql", System.Text.Encoding.UTF8);
            // sql = ScriptConverter.DateTimeReplacer.ReplaceDate(sql);
            sql = ScriptConverter.DateTimeReplacer.ReplaceDateTime(sql);
            sql = ScriptConverter.DateTimeReplacer.ReplaceDateTime2(sql);
            sql = ScriptConverter.DateTimeReplacer.ReplaceStringGuid(sql);

            System.Console.WriteLine(System.Environment.NewLine);
            System.Console.WriteLine(" --- Press any key to continue --- ");
            System.Console.ReadKey();
        }


    }


}
