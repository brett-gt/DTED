using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System;


namespace DTEDAnalysis
{
    
    class Post
    {
        public int _alt_m;
        public int _alt_ft;
        public double _lat;
        public double _lon;

        //Constructor
        public Post(int alt_m, double lat, double lon)
        {
            _alt_m = alt_m;
            _alt_ft = (int)(alt_m * 3.28084);
            _lat = lat;
            _lon = lon;
        }
    }

    class cDTED
    {
        public List<Post> posts;
        string filename;  //Includes path

        public int lon_cnt;
        public int lat_cnt;

        public double lat_orig;
        public double lon_orig;

        public double lat_intrvl;
        public double lon_intrvl;

        //Constructor
        public cDTED()
        {
            posts = new List<Post>();
        }
        
        //Data Loader
        public unsafe void readFile(string fileName)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                Debug.WriteLine("DTED::readFile(): File open...");

                int size = 80 + 648 + 2700; //Size of intro stuff
                char[] hdr = new char[size];
                hdr = reader.ReadChars(size);
                
                //User Header Label
                char[] sub = new char[3];
                Array.Copy(hdr, sub, 3);
                string UHL = new string(sub);

                //Number of lines of longitude - Skip to 48
                sub = new char[4];
                Array.Copy(hdr, 47, sub, 0, 4);
                lon_cnt = Int32.Parse(new string(sub));

                //Number of lines of latitude
                sub = new char[4];
                Array.Copy(hdr, 51, sub, 0, 4);
                lat_cnt = Int32.Parse(new string(sub));

                Debug.WriteLine("DTED::readFile(): Num lon: " + lon_cnt + " Num lat: " + lat_cnt);

                //Latitude Origin - per standard this is SW corner (lower left) of the data
                sub = new char[8];
                Array.Copy(hdr, 12, sub, 0, 8);
                string lat_orig_str = new string(sub);

                lat_orig = Double.Parse(lat_orig_str.Substring(0, 7)) / 10000; //Not strictly true, assumign always even degrees
                string lat_hemi = lat_orig_str.Substring(7, 1);
                if (lat_hemi == "S")
                    lat_orig = lat_orig * -1;


                sub = new char[8];
                Array.Copy(hdr, 4, sub, 0, 8);
                string lon_orig_str = new string(sub);

                lon_orig = Double.Parse(lon_orig_str.Substring(0, 7)) / 10000;
                string lon_hemi = lon_orig_str.Substring(7, 1);
                if (lon_hemi == "W")
                    lon_orig = lon_orig * -1;

                Debug.WriteLine("DTED::readFile(): Origin: (" + lat_orig + " , " + lon_orig + ")");


                sub = new char[3];
                Array.Copy(hdr, 80, sub, 0, 3);
                string DSI = new string(sub);
                Debug.WriteLine("DSI: " + DSI);

                sub = new char[4];
                Array.Copy(hdr, 80 + 273, sub, 0, 4);
                lon_intrvl = Double.Parse(new string(sub)) * 2.77778 * Math.Pow(10.0, -5.0);

                sub = new char[4];
                Array.Copy(hdr, 80 + 277, sub, 0, 4);
                lat_intrvl = Double.Parse(new string(sub)) * 2.77778 * Math.Pow(10.0, -5.0);

                Debug.WriteLine("DTED::readFile(): Lat intrvl: " + lat_intrvl + " Lon intrvl: " + lon_intrvl);


                sub = new char[3];
                Array.Copy(hdr, 80+648, sub, 0, 3);
                string ACC = new string(sub);
                Debug.WriteLine("DTED::readFile(): ACC: " + ACC);

                //Loop through each longitude band
                for (int i = 0; i < lon_cnt; i++)
                {
                    size = 12 + 2 * lat_cnt;
                    byte[] data = new byte[size];
                    data= reader.ReadBytes(size);

                    byte[] sub1 = new byte[4];
                    Array.Copy(data, 0, sub1, 0, 4);
                    sub1[0] = 0;
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(sub1);
                    int blk_cnt = BitConverter.ToInt32(sub1, 0);

                    /*
                    sub1 = new byte[2];
                    Array.Copy(data, 4, sub1, 0, 2);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(sub1);
                    int temp_lon_cnt = BitConverter.ToInt16(sub1, 0);

                    sub1 = new byte[2];
                    Array.Copy(data, 6, sub1, 0, 2);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(sub1);
                    int temp_lat_cnt = BitConverter.ToInt16(sub1, 0);

                    Debug.WriteLine("Lon Cnt: " + temp_lon_cnt + " lat cnt: " + temp_lat_cnt);
                    */

                    sub1 = new byte[2];
                    Array.Copy(data, 4, sub1, 0, 2);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(sub1);
                    short curLon = BitConverter.ToInt16(sub1, 0);

                    sub1 = new byte[2];
                    Array.Copy(data, 6, sub1, 0, 2);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(sub1);
                    short curLat = BitConverter.ToInt16(sub1, 0);

                    //Debug.WriteLine("Lon Cnt, Lat Cnt: " + curLon + " " + curLat);


                    if (blk_cnt % 250 == 0)
                        Debug.WriteLine("DTED::readFile(): Blk Cnt: " + blk_cnt);



                    //Loop through each latitude n the longitude band
                    for (int j = 0;j < lat_cnt;j++)
                    {
                        sub1 = new byte[2];
                        Array.Copy(data, 8+2*j, sub1, 0, 2);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(sub1);
                        short alt_m = BitConverter.ToInt16(sub1, 0);
                        double lat = j * lat_intrvl + lat_orig;
                        double lon = i * lon_intrvl + lon_orig;

                        //Debug.WriteLine("(" + lat + " , " + lon + ") = " + alt_ft);
                        posts.Add(new Post(alt_m, lat, lon));
                    }
                }
            }
        }

        public void toCSV(string csvName)
        {
            var csv = new System.Text.StringBuilder();

            foreach(Post x in posts)
            {
                var newLine = string.Format("{0},{1},{2}", x._lat, x._lon, x._alt_m);
                csv.AppendLine(newLine);
            }
            File.WriteAllText(csvName, csv.ToString());
        }

        public int getClosest(double lat, double lon)
        {
            double adj_lat = Math.Abs(lat - lat_orig);
            double adj_lon = Math.Abs(lon - lon_orig);

            int lon_index = (int)(adj_lon / lon_intrvl);
            int lat_index = (int)(adj_lat / lat_intrvl);

            int index = lon_index * lon_cnt + lat_index;
            Debug.WriteLine("Index is: " + index);

            for (int i = index - 4; i < index+4;i++)
                Debug.WriteLine("(" + posts[i]._lat + " , " + posts[i]._lon + ") = " + posts[i]._alt_ft);

            return posts[index]._alt_ft;
        }
    }

}


/*Mostly debug code
sub = new char[7];
Array.Copy(hdr, 80+205, sub, 0, 7);
string lat_str = new string(sub);

sub = new char[8];
Array.Copy(hdr, 80+212, sub, 0, 8);
string lon_str = new string(sub);

Debug.WriteLine("SW: (" + lat_str + " , " + lon_str + ")");

sub = new char[7];
Array.Copy(hdr, 80 + 220, sub, 0, 7);
lat_str = new string(sub);

sub = new char[8];
Array.Copy(hdr, 80 + 227, sub, 0, 8);
lon_str = new string(sub);

Debug.WriteLine("NW: (" + lat_str + " , " + lon_str + ")");

sub = new char[7];
Array.Copy(hdr, 80 + 235, sub, 0, 7);
lat_str = new string(sub);

sub = new char[8];
Array.Copy(hdr, 80 + 242, sub, 0, 8);
lon_str = new string(sub);

Debug.WriteLine("NE: (" + lat_str + " , " + lon_str + ")");


sub = new char[7];
Array.Copy(hdr, 80 + 250, sub, 0, 7);
lat_str = new string(sub);

sub = new char[8];
Array.Copy(hdr, 80 + 257, sub, 0, 8);
lon_str = new string(sub);

Debug.WriteLine("SE: (" + lat_str + " , " + lon_str + ")");
end mostly debug*/
