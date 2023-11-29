using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Data.Common;
using System.Data.SqlClient;
using System.Runtime.Serialization.Json;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;


namespace ConsoleApplication2
{
    class Program
    {
        private const String cs = @"Data Source=HUAWEI-NICKY;Initial Catalog=INSEE;Trusted_Connection=true";
        //private const String cs = @"Data Source=HUAWEI-NICKY;Initial Catalog=INSEE;User=test;Password=MotdepAss3;TrustServerCertificate=True";
        private const string baseURL = "https://geo.api.gouv.fr/communes/";
        private const string endurl = "?fields=&format=json";

        static void Main(string[] args)
        {
            readDataCommune();
        }
        private static void readDataCommune()
        {
            List<Ville> villes = new List<Ville>();
            
            using (SqlConnection myConnection = new SqlConnection(cs))
            {
                // On lit les communes de la base 
                string oString = "Select * from Commune";
                SqlCommand oCmd = new SqlCommand(oString, myConnection);
                myConnection.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        Ville city = new Ville();
                        city.code = oReader["Code_commune_INSEE"].ToString();
                        villes.Add(city);
                    }
                    
                    myConnection.Close();
                }

                // pour chaque commune on intérroge l'api
                foreach (Ville ville in villes)
                {
                    string uri = string.Concat(baseURL + ville.code + endurl);
                    HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(string.Format(uri));

                    WebReq.Method = "GET";

                    HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();

                    Console.WriteLine(WebResp.StatusCode);

                    string jsonString;
                    using (Stream stream = WebResp.GetResponseStream())   
                    {
                        // on utilise le stream avec son dispose - pour libérer des ressources non managées

                        StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                        jsonString = reader.ReadToEnd();
                    }

                    // on déserialise notre ville
                    Ville item = JsonConvert.DeserializeObject<Ville>(jsonString);

                    // si la population est null on met à zéro
                    if (item.population == null)
                    {
                        item.population = "0";
                    }
                    myConnection.Open();

                    // on insère la population dans la table commune
                    try
                    {
                        SqlCommand command5 = new SqlCommand("update Commune " +
                            "SET population = (@population) where [Code_commune_INSEE] = (@code)", myConnection);

                        command5.Parameters.AddWithValue("@population", item.population);
                        command5.Parameters.AddWithValue("@code", item.code);

                        command5.ExecuteNonQuery();
                        Console.WriteLine("population de "+item.nom + " à jour");

                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine(ex.ErrorCode);
                    }
                    myConnection.Close();
                }
            }
        }
    }
}