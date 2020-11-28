import React from "react";

import SplashScreen from "./SplashScreen";
import LocationPicker from "../location/LocationPicker";
import Weather from "../weather/Weather";

function Shell(props) {
  return (
    <>
      {props.status === "INIT" && <SplashScreen />}
      {props.status === "LOCATION" && (
        <LocationPicker
          location={props.location}
          onLocationChange={props.onLocationChange}
          onHomepage={props.onHomepage}
        />
      )}
      {props.status === "WEATHER" && (
        <Weather
          location={props.location}
          onLocationEdit={props.onLocationEdit}
        />
      )}
    </>
  );
}

export default Shell;
