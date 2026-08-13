# Unturned Territories
Unturned plugin for faction style Domination gameplay

## How to Install
Install OpenMod and ask Developer(Tre Munet) for up to date dll

## Commands
"|" means "or", choose one of these arguments  


/faction create|remove|delete|list  
/territory claim|info  
/zone set|status|remove|list  


## Documentation

Search for *Config.Yaml* in SteamLibrary\steamapps\common\U3DS\Servers\Default\OpenMod\plugins\TerritoryPlugin  
```yaml
capture_zones:
  time_zone_id: "America/Santiago" # This field is ignored for the time being
  scoring_start: "00:00"           # When Scoring for zones open
  scoring_end: "23:59"             # When Scoring for zones end
  capture_time: 60                 # This field is ignored for the time being
  zones:                           # Use these fields to create preset zones, attributes are self descriptive
    - name: "Base North"
      x: 100.5
      y: 50.0
      z: 200.3
      radius: 75.0
      weight: 2
    - name: "Base South"
      x: -150.2
      y: 45.0
      z: -180.5
      radius: 60.0
      weight: 1

pvp_schedule:                # Pvp window
  enabled_start: "18:00"
  enabled_end: "20:00"
```

## Server Announcements

This plugin is accompanied by Wild.Announcer, a separate plugin configured to run server announcements.  
Install from the server terminal by running: 'openmod install Wild.Announcer'  
Search for *Config.Yaml* in SteamLibrary\steamapps\common\U3DS\Servers\Default\OpenMod\plugins\Wild.Announcer  

Replace the entire config with the following:  
```yaml
Interval: 500 # Seconds between each announcement - Must be an int value
Random-Enabled: true # If announcements should be random and not by order - Must be a boolean value
Prevent-Duplicates: true # If random duplicate announcements should be prevented - Must be a boolean value

Announcements:
  - URL: null # URL to retrieve image from - Must be a full URL, no quotation marks
    Message: "Use /z, /t, and /f to display commands you can use" # Message to be sent to the entire server - Must be a string value - Useable Parameters: Rich Text <>
  - URL: null 
    Message: "Win a Capture Zone by accumulating the most points by standing in it" 
  - URL: null 
    Message: "Earn more points by having more people from your faction stand in the Capture Zone"
  - URL: null 
    Message: "Kill other players in the Capture Zone to prevent them from accumulating points"
  - URL: null 
    Message: "Craft a *Gas Mask* to loot Deadzones! Full suit Deadzones requires a *Biohazard Outfit*"
  - URL: null 
    Message: "You can only build and destory structures in your own Territory"
```

