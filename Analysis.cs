using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace DTEDAnalysis
{
    class Analysis
    {
        // Compare
        public void Compare(double startLat, double startLon, double endLat, double endLon, string SRTM_fold, string DTED_fold)
        {
            string extension = ".dt2";

            if (startLat > endLat)
                Debug.WriteLine("startLat needs to be less than endLat");

            if (startLon > endLon)
                Debug.WriteLine("startLon needs to be less than endLon");

            int roundStartLat = (int)Math.Floor(startLat);
            int roundEndLat = (int)Math.Floor(endLat);

            int roundStartLon = (int)Math.Floor(startLon);
            int roundEndLon = (int)Math.Floor(endLon);


                for (int curLon = roundStartLon; curLon <= roundEndLon; curLon++)
            {
                //TODO: May need to add leading zeros
                string curLonStr;
                if (curLon < 0)
                    curLonStr = "W" + (-1 * curLon).ToString("D3");
                else
                    curLonStr = "E" + curLon.ToString("D3");

                for (int curLat = roundStartLat; curLat <= roundEndLat; curLat++)
                {
                    //TODO: May need to add leading zeros
                    string curLatStr;
                    if (curLat < 0)
                        curLatStr = "S" + (-1 * curLat).ToString("D2");
                    else
                        curLatStr = "N" + curLat.ToString("D2");

                    string SRTM_path = SRTM_fold + curLonStr + "\\" + curLatStr + extension;
                    string DTED_path = DTED_fold + curLonStr + "\\" + curLatStr + extension;

                    Debug.WriteLine("Analysis::Compare(): SRTM Path: " + SRTM_path);
                    Debug.WriteLine("Analysis::Compare(): DTED Path: " + DTED_path);

                    cDTED SRTM = new cDTED();
                    cDTED DTED = new cDTED();

                    Debug.WriteLine("Analysis::Compare(): Loading SRTM File...");
                    SRTM.readFile(SRTM_path);

                    Debug.WriteLine("Analysis::Compare(): Loading DTED File...");
                    DTED.readFile(DTED_path);

                    Debug.WriteLine("SRTM Size: " + SRTM.posts.Count + " DTED Size: " + DTED.posts.Count);

                    AnalyzeArea(SRTM, DTED, startLat, endLon, 250);
                    //Analyze(SRTM, DTED);
                }
            }

        }

        // Analyze
        public void Analyze(cDTED SRTM, cDTED DTED)
        {
            if (SRTM.posts.Count != DTED.posts.Count)
            {
                Debug.WriteLine("Analysis:Analyze() - SRTM and DTED sizes do not agree");
                return;
            }

            var csv = new System.Text.StringBuilder();

            Post largest = new Post(-10000, 0, 0);

            double average_delta = 0;


            //for (int m = 0; m < SRTM.posts.Count; m++)
            for (int m = 400000; m < 440000; m++)
            {

                int difference = SRTM.posts[m]._alt_ft - DTED.posts[m]._alt_ft;
                //Debug.WriteLine("SRTM: " + SRTM.posts[m]._alt_ft + "- DTED: " + DTED.posts[m]._alt_ft + " = " + difference);

                if (m % 1000 == 0)
                    Debug.WriteLine("Writing: " + m);

                var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6}", SRTM.posts[m]._lat, SRTM.posts[m]._lon, SRTM.posts[m]._alt_ft, DTED.posts[m]._lat, DTED.posts[m]._lon, DTED.posts[m]._alt_ft, difference);
                csv.AppendLine(newLine);

                File.WriteAllText("CompareTest.csv", csv.ToString());
            }
        }


        public void AnalyzeArea(cDTED SRTM, cDTED DTED, double startLat, double endLon, int size)
        {
            int[,] DTED_points = new int[size, size];
            int[,] SRTM_points = new int[size, size];

            double[] longitudes = new double[size];
            double[] latitudes = new double[size];


            int lonIndex = -1;
            //Find first point
            for (int i = 0; i <= SRTM.posts.Count; i++) //Loop through Lon
            {
                if (SRTM.posts[i]._lon >= endLon) //File is index to west, so find furthest west of the two (start/end)
                {
                    lonIndex = i;
                    break;
                }
            }
            if (lonIndex == -1)
            {
                Debug.WriteLine("Analysis:AnalyzeArea() - Start Longitude not found.");
            }

            int latIndex = -1;
            //Find first point
            for (int i = 0; i <= SRTM.posts.Count; i++) //Loop through Lon
            {
                if (SRTM.posts[i]._lat >= startLat)
                {
                    latIndex = i;
                    break;
                }
            }
            if (latIndex == -1)
            {
                Debug.WriteLine("Analysis:AnalyzeArea() - Start Latitude not found.");
            }

            Debug.WriteLine("LatIdx: " + latIndex + " LonIdx: " + lonIndex);


            //Actually pull at points
            for (int i = size - 1; i >= 0; i--) //Loop through Lon
            {
                int row_idx = lonIndex + i * DTED.lon_cnt;

                //for (int j = size - 1; j >= 0; j--) //Loop through lat
                for (int j = 0; j < size; j++) //Loop through lat
                {
                    int idx = row_idx + latIndex + j;

                    //Debug.WriteLine("SRTM: (" + SRTM.posts[idx]._lat + "," + SRTM.posts[idx]._lon + "): " + SRTM.posts[idx]._alt_ft );
                    SRTM_points[j, i] = SRTM.posts[idx]._alt_ft;
                    DTED_points[j, i] = DTED.posts[idx]._alt_ft;
                }
            }

            //Doing this seperate to try to save some processing time
            for (int i = size - 1; i >= 0; i--) //Loop through Lon
            {
                int idx = lonIndex + i * DTED.lon_cnt;
                longitudes[i] = SRTM.posts[idx]._lon;
            }
            //for (int j = size - 1; j >= 0; j--) //Loop through lat
            for (int j = 0; j < size; j++) //Loop through lat
            {
                int idx = latIndex + j;
                latitudes[j] = SRTM.posts[idx]._lat;
            }

            /*
            for(int i = 0;i < size;i++)
            {
                Debug.WriteLine("Latitudes: " + i + " " + latitudes[i]);
                Debug.WriteLine("Longitudes: " + i + " " + longitudes[i]);
            }
            */


            var SRTM_CSV = new System.Text.StringBuilder();
            var DTED_CSV = new System.Text.StringBuilder();
            var Diff_CSV = new System.Text.StringBuilder();

            string line = "";
            for (int i = 0; i < size; i++)
            {
                line = line + "," + longitudes[i];

            }
            SRTM_CSV.AppendLine(line);
            DTED_CSV.AppendLine(line);
            Diff_CSV.AppendLine(line);

            string dted_line = "";
            string srtm_line = "";
            string diff_line = "";

            //lat, lon is order
            for (int i = 0; i < size; i++)
            {
                srtm_line = latitudes[i] + ",";
                dted_line = latitudes[i] + ",";
                diff_line = latitudes[i] + ",";

                for (int j = 0; j < size; j++)
                {
                    srtm_line = srtm_line + SRTM_points[i, j] + ",";
                    dted_line = dted_line + DTED_points[i, j] + ",";
                    diff_line = diff_line + (SRTM_points[i, j] - DTED_points[i, j]) + ",";

                    //Debug.WriteLine(dted_line);
                }

                SRTM_CSV.AppendLine(srtm_line);
                DTED_CSV.AppendLine(dted_line);
                Diff_CSV.AppendLine(diff_line);
            }
            File.WriteAllText("SRTM_Area.csv", SRTM_CSV.ToString());
            File.WriteAllText("DTED_Area.csv", DTED_CSV.ToString());
            File.WriteAllText("Diff_Area.csv", Diff_CSV.ToString());
        }
    }
}

    /*
    // Analyze
    public void AnalyzeArea(cDTED SRTM, cDTED DTED, int middlePoint, int size)
    { 
        {
            int offset = (int)Math.Round((double)size * 0.5);

            int lag = middlePoint - offset;

            if((size*DTED.lon_cnt + lag > SRTM.posts.Count))
            {
                Debug.WriteLine("Analysis:Analyze() - Size/MiddlePoint do not fit.  Max size is " + size*size);
                return;
            }

            if(lag < 0)
            {
                Debug.WriteLine("Analysis:Analyze() - Size/MiddlePoint result in less than zero.");
                return;
            }

            int[,] DTED_points = new int[size, size];
            int[,] SRTM_points = new int[size, size];

            double[] longitudes = new double[size];
            double[] latitudes = new double[size];


            //Actually pull at points
            for (int i = size - 1;i >= 0;i--) //Loop through Lon
            {
                int row_idx = lag + i * DTED.lon_cnt;

                for (int j = size - 1; j >= 0; j--) //Loop through lat
                {
                    int idx = row_idx + lag + j;

                    //Debug.WriteLine("SRTM: (" + SRTM.posts[idx]._lat + "," + SRTM.posts[idx]._lon + "): " + SRTM.posts[idx]._alt_ft );
                    SRTM_points[j, i] = SRTM.posts[idx]._alt_ft;
                    DTED_points[j ,i] = DTED.posts[idx]._alt_ft;
                }
            }

            //Doing this seperate to try to save some processing time
            for (int i = size-1; i >= 0; i--) //Loop through Lon
            {
                int idx = lag + i * DTED.lon_cnt;
                longitudes[i] = SRTM.posts[idx]._lon;
            }
            for (int j = size - 1; j >= 0; j--) //Loop through lat
            {
                int idx = lag + j;
                latitudes[j] = SRTM.posts[idx]._lat;
            }

            /*
            for(int i = 0;i < size;i++)
            {
                Debug.WriteLine("Latitudes: " + i + " " + latitudes[i]);
                Debug.WriteLine("Longitudes: " + i + " " + longitudes[i]);
            }
            


            var SRTM_CSV = new System.Text.StringBuilder();
            var DTED_CSV = new System.Text.StringBuilder();
            var Diff_CSV = new System.Text.StringBuilder();

            string line = "";
            for (int i = 0; i < size; i++)
            {
                line = line + "," + longitudes[i];

            }
            SRTM_CSV.AppendLine(line);
            DTED_CSV.AppendLine(line);
            Diff_CSV.AppendLine(line);

            string dted_line = "";
            string srtm_line = "";
            string diff_line = "";

            //lat, lon is order
            for (int i = 0;i < size;i++)
            {
                srtm_line = latitudes[i] + ",";
                dted_line = latitudes[i] + ",";
                diff_line = latitudes[i] + ",";

                for (int j = 0;j < size;j++)
                {
                    srtm_line = srtm_line + SRTM_points[i, j] + ",";
                    dted_line = dted_line + DTED_points[i, j] + ",";
                diff_line = diff_line + (SRTM_points[i, j] - DTED_points[i, j]) + ",";

                    //Debug.WriteLine(dted_line);
                }

                SRTM_CSV.AppendLine(srtm_line);
                DTED_CSV.AppendLine(dted_line);
                Diff_CSV.AppendLine(diff_line);
            }
            File.WriteAllText("SRTM_Area.csv", SRTM_CSV.ToString());
            File.WriteAllText("DTED_Area.csv", DTED_CSV.ToString());
            File.WriteAllText("Diff_Area.csv", DTED_CSV.ToString());
        }

    }
}
*/
