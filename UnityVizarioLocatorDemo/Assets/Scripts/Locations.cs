using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Locations : MonoBehaviour
{

    public struct Location
    {
        public string Name { get; set; }
        public string Country { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
    }

    private string locationFile = "cities";

    private List<Location> locations = new List<Location>();

    // Start is called before the first frame update
    void Start()
    {
        TextAsset calibAsset = (TextAsset)Resources.Load(locationFile);

        var json = JObject.Parse(calibAsset.text);

        foreach (var elem in json)
        {
            Location l = new Location();

            l.Name = elem.Key.ToString();
            var innerJ = JObject.Parse(elem.Value[0].ToString());
            l.Country = innerJ["Country"].ToString();
            string f = innerJ["Latitude"].ToString().Substring(0, innerJ["Latitude"].ToString().Length - 1);

            float lat;
            float.TryParse(f, out lat);
            lat = innerJ["Latitude"].ToString()[innerJ["Latitude"].ToString().Length - 1] == 'S' ? lat * (-1) : lat;
            l.Latitude = lat;


            f = innerJ["Longitude"].ToString().Substring(0, innerJ["Longitude"].ToString().Length - 1);
            float lon;
            float.TryParse(f, out lon);
            lon = innerJ["Longitude"].ToString()[innerJ["Longitude"].ToString().Length - 1] == 'W' ? lon * (-1) : lon;
            l.Longitude = lon;

            locations.Add(l);
        }

        Debug.Log("loc count: " + locations.Count.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<Location> getRandomLocations(int amount)
    {

        if (amount >= locations.Count)
            return locations;

        List<int> usedRands = new List<int>();
        List<Location> return_list = new List<Location>();

        //debug
        return_list.Add(locations[11]);
        for (int i = 0; i < amount; i++)
        {
            int r;
            do
            {
                r = Random.Range(0, locations.Count);
                
            } while (usedRands.Contains(r));

            usedRands.Add(r);
            return_list.Add(locations[r]);

        }

        
        return return_list;
    }
}
