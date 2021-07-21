# vizario_locator_demo


## setup map data

Go to https://www.openstreetmap.org/export

-> manually select a different area
-> right side share -> Image set custom dimension and align to area selected before
-> now download png 
-> create xml with the 4 lat/lon values in Export window :

 ```
<?xml version="1.0"?>
<MapData xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <left>15.45700</left>	
  <right>15.45975</right>
  <top>47.05894</top>	
  <bottom>47.05782</bottom>
</MapData>
```



