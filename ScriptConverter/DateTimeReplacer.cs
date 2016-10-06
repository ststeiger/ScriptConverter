
namespace ScriptConverter
{


    class DateTimeReplacer
    {


        public static void Test()
        {
            string sql = @"
      SELECT len('00442801A8330B')
UNION SELECT len('01A5920BA8330B') 
UNION SELECT len('0271BA73A8330B')
UNION SELECT len('036D488504A8330B')
UNION SELECT len('0446D4342DA8330B')
UNION SELECT len('05BE4A10C401A8330B')
UNION SELECT len('066FEBA2A811A8330B')
UNION SELECT len('0757325D96B0A8330B')


      SELECT CAST(0x00442801A8330B AS DateTime2),1 as ord 
UNION SELECT CAST(0x01A5920BA8330B AS DateTime2),2 as ord 
UNION SELECT CAST(0x0271BA73A8330B AS DateTime2),3 as ord 
UNION SELECT CAST(0x036D488504A8330B AS DateTime2),4 as ord 
UNION SELECT CAST(0x0446D4342DA8330B AS DateTime2),5 as ord 
UNION SELECT CAST(0x05BE4A10C401A8330B AS DateTime2),6 as ord 
UNION SELECT CAST(0x066FEBA2A811A8330B AS DateTime2),7 as ord 
UNION SELECT CAST(0x0757325D96B0A8330B AS DateTime2),8 as ord 

ORDER BY ord 




INSERT INTO someTime (mytime, myText) VALUES (CAST(0x0700000000000000 AS Time), '00:00:00.0000000')
INSERT INTO someTime (mytime, myText) VALUES (CAST(0x070068C461080000 AS Time), '01:00:00.0000000')
INSERT INTO someTime (mytime, myText) VALUES (CAST(0x070010ACD1530000 AS Time), '10:00:00.0000000')
INSERT INTO someTime (mytime, myText) VALUES (CAST(0x07001882BA7D0000 AS Time), '15:00:00.0000000')
INSERT INTO someTime (mytime, myText) VALUES (CAST(0x07002058A3A70000 AS Time), '20:00:00.0000000')
INSERT INTO someTime (mytime, myText) VALUES (CAST(0x07007AA606C90000 AS Time), '23:59:00.0000000')
INSERT INTO someTime (mytime, myText) VALUES (CAST(0x0780103F07C90000 AS Time), '23:59:01.0000000')
INSERT INTO someTime (mytime, myText) VALUES (CAST(0x07001D8818C90000 AS Time), '23:59:30.0000000')
INSERT INTO someTime (mytime, myText) VALUES (CAST(0x07007F1EA7740000 AS Time), '13:55:02.0000000')
INSERT INTO someTime (mytime, myText) VALUES (CAST(0x073FD11E19C90000 AS Time), '23:59:30.9876543')

";
            
            sql = ReplaceDateTime(sql);
            sql = ReplaceDateTime2(sql);
            sql = ReplaceStringGuid(sql);
            sql = ReplaceTime(sql);

            System.Console.WriteLine(sql);
        } // End Sub Test 


        // https://msdn.microsoft.com/en-us/library/ewy2t5e0(v=vs.110).aspx
        public static string ReplaceStringGuid(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, @"N('[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}')"
                , "$1"
            , System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }


        private static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];

            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = System.Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        } // End Function StringToByteArray


        // This is the same as 
        // byte[] ba = StringToByteArray(hexBytesToReverse);
        // System.Array.Reverse(ba);
        // hexBytesToReverse = System.BitConverter.ToString(ba).Replace("-", ""); // = ByteArrayToString(ba);
        private static string ReverseBytes(string hexBytesToReverse)
        {
            char[] ca = new char[hexBytesToReverse.Length];

            for (int i = 0; i < hexBytesToReverse.Length; i += 2)
            {
                ca[hexBytesToReverse.Length - 2 - i] = hexBytesToReverse[i];
                ca[hexBytesToReverse.Length - 1 - i] = hexBytesToReverse[i + 1];
            } // Next i 

            hexBytesToReverse = new string(ca);
            ca = null;

            return hexBytesToReverse;
        } // End Function ReverseBytes


        // datetime: 3 integer values; 1st: precision (1 byte), 2nd: number of <precision dep> since midnight (x Bytes), 3rd: day since jan 1 0001 (3 Bytes),
        // timepart: dependent on precision
        // Precision 0: timepart in seconds 
        // precision 1: timepart in 1/10 seconds 
        // precision 2: timepart in 1/100 seconds 
        // precision 3: timepart in 1/1'000 seconds 
        // precision 4: timepart in 1/10'000 seconds 
        // precision 5: timepart in 1/100'000 seconds 
        // precision 6: timepart in 1/1'000'000 seconds 
        // precision 7: timepart in 1/10'000'000 seconds 
        // http://weblogs.sqlteam.com/peterl/archive/2010/12/15/the-internal-storage-of-a-datetime2-value.aspx
        private static string HexDateTime2ToDateTimeString(string dateTimeHexString)
        {
            string prefix = dateTimeHexString.Substring(0, 2);
            string dayPart = dateTimeHexString.Substring(dateTimeHexString.Length - 6, 6);
            string timePart = dateTimeHexString.Substring(2, dateTimeHexString.Length - 8);
            dayPart = ReverseBytes(dayPart);
            timePart = ReverseBytes(timePart);

            int iPrecision = System.Convert.ToInt32(prefix, 16);
            int iDayPart = System.Convert.ToInt32(dayPart, 16);
            long lngTimePart = System.Convert.ToInt64(timePart, 16);

            System.DateTime dateTimeFinal = (new System.DateTime(1, 1, 1)).AddDays(iDayPart);

            // dateTimeFinal = dateTimeFinal.AddSeconds(lngTimePart * 1.0 / System.Math.Pow(10, iPrecision)); // Not precise enough, stops at millisecond level...
            dateTimeFinal = dateTimeFinal.AddTicks((long)(lngTimePart * 1.0 / System.Math.Pow(10, iPrecision) * System.TimeSpan.TicksPerSecond));

            // return dateTimeFinal.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff"); // 7 max-precision
            return dateTimeFinal.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff");
        } // End Function HexDateTime2ToDateTimeString 


        public static string ReplaceDateTime2(string sql)
        {
            // 14 or 16 or 18
            //sql = System.Text.RegularExpressions.Regex.Replace(sql, @"CAST\s*\(\s*0x[a-f0-9]{14,18}\s*AS\s*datetime2\s*\)"
            return System.Text.RegularExpressions.Regex.Replace(sql, @"CAST\s*\(\s*0x[a-f0-9]{14}(?:[a-f0-9]{2}){0,2}\s*AS\s*datetime2\s*\)"
                ,new System.Text.RegularExpressions.MatchEvaluator(ReplaceDateTime2Match)
                ,System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        } // End Function ReplaceDateTime2 


        private static string ReplaceDateTime2Match(System.Text.RegularExpressions.Match ma)
        {
            string str = System.Text.RegularExpressions.Regex.Replace(ma.Value, @"\s*", "");
            str = str.ToLower();
            str = str.Replace("cast(0x", "");
            str = str.Replace("asdatetime2)", "");
            str = "'" + HexDateTime2ToDateTimeString(str) + "'";

            return str;
        } // End Function ReplaceDateTime2Match 


        public static string ReplaceDate(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, @"CAST\s*\(\s*0x[a-f0-9]{6}\s*AS\s*date\s*\)"
                , new System.Text.RegularExpressions.MatchEvaluator(ReplaceDateMatch)
                , System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        } // End Function ReplaceDate


        public static string ReplaceTime(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, @"CAST\s*\(\s*0x[a-f0-9]{16}\s*AS\s*time\s*\)"
                , new System.Text.RegularExpressions.MatchEvaluator(ReplaceTimeMatch)
                , System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        } // End Function ReplaceTime


        public static string ReplaceDateTime(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, @"CAST\s*\(\s*0x[a-f0-9]{16}\s*AS\s*datetime\s*\)"
                , new System.Text.RegularExpressions.MatchEvaluator(ReplaceDateTimeMatch)
                , System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        } // End Function ReplaceDateTime


        private static string ReplaceDateTimeMatch(System.Text.RegularExpressions.Match ma)
        {
            string str = System.Text.RegularExpressions.Regex.Replace(ma.Value, @"\s*", "");
            str = str.ToLower();
            str = str.Replace("cast(0x", "");
            str = str.Replace("asdatetime)", "");
            str = "'" + HexDateTimeToDateTimeString(str) + "'";

            return str;
        } // End Function ReplaceDateTimeMatch 


        private static string ReplaceDateMatch(System.Text.RegularExpressions.Match ma)
        {
            string str = System.Text.RegularExpressions.Regex.Replace(ma.Value, @"\s*", "");
            str = str.ToLower();
            str = str.Replace("cast(0x", "");
            str = str.Replace("asdate)", "");
            str = "'" + HexDateToDateString(str) + "'";

            return str;
        } // End Function ReplaceDateMatch 


        private static string ReplaceTimeMatch(System.Text.RegularExpressions.Match ma)
        {
            string str = System.Text.RegularExpressions.Regex.Replace(ma.Value, @"\s*", "");
            str = str.ToLower();
            str = str.Replace("cast(0x", "");
            str = str.Replace("astime)", "");
            str = "'" + HexTimeToTimeString(str) + "'";

            return str;
        } // End Function ReplaceDateMatch 


        // http://stackoverflow.com/questions/7412944/convert-datetime-to-hex-equivalent-in-vb-net
        // datetime: two integers, first day since jan 1900, 2nd number of tick since midnight
        // 1 tick = 1/300 of a second ==> 
        // x ticks * 1s/300 ticks = x ticks * 1s/300ticks *1000ms/s = x *1/300*1000 = x * 10/3 ms
        private static string HexDateTimeToDateTimeString(string dateTimeHexString)
        {
            string datePartHexString = dateTimeHexString.Substring(0, 8);
            int datePartInt = System.Convert.ToInt32(datePartHexString, 16);
            System.DateTime dateTimeFinal = (new System.DateTime(1900, 1, 1)).AddDays(datePartInt);

            string timePartHexString = dateTimeHexString.Substring(8, 8);
            int timePartInt = System.Convert.ToInt32(timePartHexString, 16);
            double timePart = timePartInt * 10 / 3;
            dateTimeFinal = dateTimeFinal.AddMilliseconds(timePart);

            return dateTimeFinal.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff");
        } // End Function HexDateTimeToDateTimeString 


        // http://stackoverflow.com/questions/7412944/convert-datetime-to-hex-equivalent-in-vb-net
        private static string HexDateToDateString(string dateHexString)
        {
            int days = byte.Parse(dateHexString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber)
                | byte.Parse(dateHexString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) << 8
                | byte.Parse(dateHexString.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) << 16;

            System.DateTime dateFinal = new System.DateTime(1, 1, 1).AddDays(days);
            return dateFinal.ToString("yyyyMMdd");
        } // End Function HexDateToDateString


        public static string HexTimeToTimeString(string timeHexString)
        {
            string precision = timeHexString.Substring(0, 2);
            precision = ReverseBytes(precision);
            int iPrecision = System.Convert.ToInt32(precision, 16);


            string timePart = timeHexString.Substring(2);
            timePart = ReverseBytes(timePart);
            long lngTimePart = System.Convert.ToInt64(timePart, 16);
            System.DateTime dateTimeFinal = new System.DateTime(1, 1, 1);
            dateTimeFinal = dateTimeFinal.AddTicks((long)(lngTimePart * 1.0 / System.Math.Pow(10, iPrecision) * System.TimeSpan.TicksPerSecond));
            string time = dateTimeFinal.ToString("HH:mm:ss.fffffff");
            System.Console.WriteLine(time);

            return time;
        } // End Function HexTimeToTimeString 


        public string GetDateAsHex(System.DateTime dt)
        {
            System.DateTime zero = new System.DateTime(1, 1, 1);

            int j = (int) dt.Subtract(zero).TotalDays;
            string s = j.ToString("X06");
            s = ReverseBytes(s);
            return "0x" + s;
        } // End Function GetDateAsHex 


        // System.DateTime to SQL-Server 0xHEX datetime-value
        public string GetDateTimeAsHex(System.DateTime dt)
        {
            System.DateTime zero = new System.DateTime(1900, 1, 1);

            System.TimeSpan ts = dt - zero;
            System.TimeSpan ms = ts.Subtract(new System.TimeSpan(ts.Days, 0, 0, 0));

            // resolution for datetime: 3.33 ms ...
            // BUG @ new DateTime(2012, 12, 31, 23, 59, 59, 999)
            // string hex = "0x" + ts.Days.ToString("X8") + ((int)(ms.TotalMilliseconds / 3.3333333333)).ToString("X8");

            // double x = System.Math.Round(ms.TotalMilliseconds / 3.3333333333, 0, System.MidpointRounding.AwayFromZero);
            // double x = System.Math.Ceiling(ms.TotalMilliseconds / 3.3333333333);
            double x = System.Math.Floor(ms.TotalMilliseconds / 3.3333333333);

            string hex = "0x" + ts.Days.ToString("X8") + System.Convert.ToInt32(x).ToString("X8");
            return hex;
        } // End Function GetDateTimeAsHex 


/*
-- https://stackoverflow.com/questions/14124439/cannot-persist-computed-column-not-deterministic
-- https://www.mssqltips.com/sqlservertip/3338/change-all-computed-columns-to-persisted-in-sql-server/

CREATE TABLE dbo.MyTable
(
	 value datetime2(7) NULL 
	,string varchar(50) NULL 
	,mydate date NULL 
	,mytime time(7) NULL 
); 

ALTER TABLE MyTable 
ADD MyDateTime2 AS 
(
	DATEADD
	(
		 day 
		,DATEDIFF
		(
			 day
			,CONVERT( DATE, '19000101', 112)
			, mydate
		)
		,CONVERT(datetime2(7), mytime)
	)
) PERSISTED
;
*/
        

    } // End Class DateTimeReplacer


} // End Namespace 
